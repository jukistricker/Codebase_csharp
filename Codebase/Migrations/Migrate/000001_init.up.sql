-- Kích hoạt extension để tự động tạo UUID nếu cần
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Bảng Permission Groups
CREATE TABLE IF NOT EXISTS permission_groups (
                                                 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    code VARCHAR(100) NOT NULL UNIQUE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID
    );

-- 2. Bảng Permissions
CREATE TABLE IF NOT EXISTS permissions (
                                           id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(100) NOT NULL UNIQUE,
    permission_group_id UUID NOT NULL REFERENCES permission_groups(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID
    );

-- 3. Bảng Roles
CREATE TABLE IF NOT EXISTS roles (
                                     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID
    );

-- 4. Bảng Users
CREATE TABLE IF NOT EXISTS users (
                                     id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(100) NOT NULL UNIQUE,
    password TEXT NOT NULL,
    lang INTEGER DEFAULT 0, -- Mapping từ LanguageEnum
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by UUID
    );

-- 5. Bảng trung gian Role Permissions (N-N)
CREATE TABLE IF NOT EXISTS role_permissions (
                                                role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
    );

-- 6. Bảng trung gian User Roles (N-N)
CREATE TABLE IF NOT EXISTS user_roles (
                                          user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
    );

-- =========================================
-- INIT ROLES
-- =========================================

INSERT INTO roles (id, name)
VALUES
    (gen_random_uuid(), 'admin'),
    (gen_random_uuid(), 'user')
    ON CONFLICT (name) DO NOTHING;



-- =========================================
-- INIT PERMISSION GROUPS
-- =========================================
-- Chúng ta tạo các group trước
INSERT INTO permission_groups (id, name, code, sort_order)
VALUES
    (gen_random_uuid(), 'Auth', 'auth_group', 1),
    (gen_random_uuid(), 'User', 'user_group', 2)
    ON CONFLICT (code) DO NOTHING;


-- =========================================
-- INIT PERMISSIONS 
-- =========================================
INSERT INTO permissions (id, code, permission_group_id)
VALUES
    -- Các quyền thuộc group Xác thực
    (gen_random_uuid(), 'auth.login', (SELECT id FROM permission_groups WHERE code = 'auth_group')),
    (gen_random_uuid(), 'auth.logout', (SELECT id FROM permission_groups WHERE code = 'auth_group')),

    -- Các quyền thuộc group Người dùng
    (gen_random_uuid(), 'user.read', (SELECT id FROM permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.create', (SELECT id FROM permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.update', (SELECT id FROM permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.delete', (SELECT id FROM permission_groups WHERE code = 'user_group'))
    ON CONFLICT (code) DO NOTHING;



-- =========================================
-- ASSIGN PERMISSIONS TO ROLES
-- =========================================

-- ADMIN = ALL PERMISSIONS
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
         CROSS JOIN permissions p
WHERE r.name = 'admin'
    ON CONFLICT DO NOTHING;


-- USER = LIMITED PERMISSIONS
INSERT INTO role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM roles r
         JOIN permissions p ON p.code IN (
                                          'auth.login',
                                          'auth.logout',
                                          'user.read'
    )
WHERE r.name = 'user'
    ON CONFLICT DO NOTHING;

-- =========================================
-- TẠO USER ADMIN
-- =========================================

DO $$
DECLARE
    v_role_id UUID;
    v_admin_id UUID; 
    v_password_hash TEXT := '$2a$11$R9h/lIPzHZ7fJLTMp.p3cOnB.V3pKN8nNM8fWjM9L5W6S8V9Y6uS.';
BEGIN
    -- 1. Lấy ID của role 'admin'
SELECT id INTO v_role_id FROM roles WHERE name = 'admin' LIMIT 1;

-- 2. Chèn User Admin nếu chưa tồn tại
INSERT INTO users (username, password, lang, created_at, updated_at)
VALUES (
           'admin',
           v_password_hash,
           1,
           NOW(),
           NOW()
       )
    ON CONFLICT (username) DO NOTHING;

-- 3. Lấy lại ID thực tế của user 'admin'
SELECT id INTO v_admin_id FROM users WHERE username = 'admin';

-- 4. Gán Role Admin cho User này
IF v_role_id IS NOT NULL AND v_admin_id IS NOT NULL THEN
        INSERT INTO user_roles (user_id, role_id)
        VALUES (v_admin_id, v_role_id)
        ON CONFLICT DO NOTHING;
        
        -- 5. Cập nhật Audit fields cho chính nó (Self-reference)
UPDATE users
SET created_by = v_admin_id, updated_by = v_admin_id
WHERE id = v_admin_id AND (created_by IS NULL OR created_by = '00000000-0000-0000-0000-000000000000');

RAISE NOTICE 'Admin user initialized with ID: %', v_admin_id;
ELSE
        RAISE WARNING 'Could not find Admin Role or Admin User';
END IF;

END $$;