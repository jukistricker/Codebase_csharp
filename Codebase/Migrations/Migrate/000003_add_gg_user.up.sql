-- 1. Thêm các cột mới cho Profile và Social Login (Tất cả để NULL)
ALTER TABLE public.users
    ADD COLUMN IF NOT EXISTS email varchar(255),
    ADD COLUMN IF NOT EXISTS full_name varchar(255),
    ADD COLUMN IF NOT EXISTS avatar_url text,
    ADD COLUMN IF NOT EXISTS provider varchar(50),
    ADD COLUMN IF NOT EXISTS external_id text;

-- 2. Chỉnh sửa cột Password: Cho phép NULL (vì Login Google không dùng pass)
ALTER TABLE public.users ALTER COLUMN "password" DROP NOT NULL;

-- 3. Thiết lập các Ràng buộc (Constraints) để định danh duy nhất
-- Unique Email: Đảm bảo một email chỉ đăng ký 1 lần
ALTER TABLE public.users ADD CONSTRAINT users_email_key UNIQUE (email);

-- Unique Pair (external_id, provider): Một ID từ Google chỉ map với 1 User
ALTER TABLE public.users ADD CONSTRAINT users_external_id_provider_key UNIQUE (external_id, provider);

-- 4. Tạo các Index "Thần tốc" cho việc Search và Login
-- Tối ưu cho việc tìm kiếm User khi Google trả về Callback
CREATE INDEX IF NOT EXISTS idx_users_external_auth ON public.users (external_id, provider);

-- Tối ưu cho việc tìm kiếm theo Email
CREATE INDEX IF NOT EXISTS idx_users_email ON public.users (email);

-- Tối ưu cho FilterUtil (Sắp xếp theo thời gian tạo)
CREATE INDEX IF NOT EXISTS idx_users_created_at ON public.users (created_at DESC);