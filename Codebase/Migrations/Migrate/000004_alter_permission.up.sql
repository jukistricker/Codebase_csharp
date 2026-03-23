ALTER TABLE public.permissions
    ADD COLUMN IF NOT EXISTS name VARCHAR(255);

-- Xóa dữ liệu cũ hoặc dùng ON CONFLICT để update name
INSERT INTO public.permissions (code, name, permission_group_id)
VALUES
    ('auth.login', 'Login', (SELECT id FROM public.permission_groups WHERE code = 'auth_group')),
    ('auth.logout', 'Logout', (SELECT id FROM public.permission_groups WHERE code = 'auth_group')),

    ('user.view_users', 'View Users', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    ('user.read', 'View User''s Details', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    ('user.create', 'Add New User', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    ('user.update', 'Update User''s Details', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),
    ('user.delete', 'Delete User', (SELECT id FROM public.permission_groups WHERE code = 'user_group')),

    ('rbac.save_permission_group', 'Save Permission Group', (SELECT id FROM public.permission_groups WHERE code = 'rbac_group.admin')),
    ('rbac.save_role', 'Save Role', (SELECT id FROM public.permission_groups WHERE code = 'rbac_group.admin')),
    ('rbac.search_roles', 'Search Roles', (SELECT id FROM public.permission_groups WHERE code = 'rbac_group.admin'))
    ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name;