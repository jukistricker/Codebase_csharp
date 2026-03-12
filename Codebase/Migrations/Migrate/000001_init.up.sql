SET client_encoding = 'UTF8';
CREATE SCHEMA IF NOT EXISTS public;

ALTER SCHEMA public OWNER TO postgres;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;


ALTER DATABASE postgres SET search_path TO public, "$user";
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" SCHEMA public;
ALTER EXTENSION "uuid-ossp" SET SCHEMA public;

CREATE TABLE IF NOT EXISTS public.permission_groups
(
    id
    UUID
    PRIMARY
    KEY
    DEFAULT
    uuid_generate_v4
(
),
    name VARCHAR
(
    255
) NOT NULL,
    code VARCHAR
(
    100
) NOT NULL UNIQUE,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    updated_by UUID
    );

CREATE TABLE IF NOT EXISTS public.permissions
(
    id
    UUID
    PRIMARY
    KEY
    DEFAULT
    uuid_generate_v4
(
),
    code VARCHAR
(
    100
) NOT NULL UNIQUE,
    permission_group_id UUID NOT NULL REFERENCES public.permission_groups
(
    id
) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    updated_by UUID
    );

CREATE TABLE IF NOT EXISTS public.roles
(
    id
    UUID
    PRIMARY
    KEY
    DEFAULT
    uuid_generate_v4
(
),
    name VARCHAR
(
    100
) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    updated_by UUID
    );

CREATE TABLE IF NOT EXISTS users
(
    id
    UUID
    PRIMARY
    KEY
    DEFAULT
    uuid_generate_v4
(
),
    username VARCHAR
(
    100
) NOT NULL UNIQUE,
    password TEXT NOT NULL,
    lang INTEGER DEFAULT 0, 
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    created_by UUID,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW
(
),
    updated_by UUID
    );

CREATE TABLE IF NOT EXISTS public.role_permissions
(
    role_id
    UUID
    NOT
    NULL
    REFERENCES
    roles
(
    id
) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES public.permissions
(
    id
)
  ON DELETE CASCADE,
    PRIMARY KEY
(
    role_id,
    permission_id
)
    );

CREATE TABLE IF NOT EXISTS public.user_roles
(
    user_id
    UUID
    NOT
    NULL
    REFERENCES
    users
(
    id
) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES public.roles
(
    id
)
  ON DELETE CASCADE,
    PRIMARY KEY
(
    user_id,
    role_id
)
    );


INSERT INTO public.roles (id, name)
VALUES (gen_random_uuid(), 'admin'),
       (gen_random_uuid(), 'user') ON CONFLICT (name) DO NOTHING;


INSERT INTO public.permission_groups (id, name, code, sort_order)
VALUES (gen_random_uuid(), 'Auth', 'auth_group', 1),
       (gen_random_uuid(), 'User', 'user_group', 2) ON CONFLICT (code) DO NOTHING;


INSERT INTO public.permissions (id, code, permission_group_id)
VALUES
    -- Các quyền thuộc group Xác thực
    (gen_random_uuid(), 'auth.login', (SELECT id FROM public.permission_groups WHERE code = 'auth_group')),
    (gen_random_uuid(), 'auth.logout', (SELECT id FROM public.permission_groups WHERE code = 'auth_group')),

    -- Các quyền thuộc group Người dùng
    (gen_random_uuid(), 'user.read', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.create', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.update', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    (gen_random_uuid(), 'user.delete',
     (SELECT id FROM public.permission_groups WHERE code = 'user_group')) ON CONFLICT (code) DO NOTHING;



INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
         CROSS JOIN public.permissions p
WHERE r.name = 'admin' ON CONFLICT DO NOTHING;


INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
         JOIN public.permissions p ON p.code IN (
                                          'auth.login',
                                          'auth.logout',
                                          'user.read'
    )
WHERE r.name = 'user' ON CONFLICT DO NOTHING;


DO $$
DECLARE
v_role_id UUID;
    v_admin_id UUID;
    v_hash TEXT := 'AQAAAAIAACcQAAAAEJL3PEfuwNrQOTsclnmWeXII/9NzpgehrbMF6gOzBfg4BjsiMVqewvfP5/LtaNKj4w==';
BEGIN

SELECT id INTO v_role_id FROM public.roles WHERE name = 'admin' LIMIT 1;

INSERT INTO public.users (username, password, lang)
VALUES ('admin', v_hash, 1)
    ON CONFLICT (username) DO UPDATE SET updated_at = NOW() 
                                  RETURNING id INTO v_admin_id;


IF v_role_id IS NOT NULL AND v_admin_id IS NOT NULL THEN
        INSERT INTO user_roles (user_id, role_id)
        VALUES (v_admin_id, v_role_id)
        ON CONFLICT DO NOTHING;
        

UPDATE public.users SET created_by = v_admin_id, updated_by = v_admin_id WHERE id = v_admin_id;

RAISE NOTICE ' MIGRATION SUCCESSFUL';
END IF;
END $$;