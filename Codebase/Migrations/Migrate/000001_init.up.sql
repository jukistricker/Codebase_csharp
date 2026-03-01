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