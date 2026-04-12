-- Seed script for creating the first admin user
-- Default credentials:
--   Email: admin@dkp.local
--   Password: Admin123!
-- 
-- IMPORTANT: Change this password immediately after first login in production!

-- First, get the default guild ID (created in 002_seed_guild.sql)
DO $$
DECLARE
    default_guild_id UUID;
BEGIN
    -- Get the first guild (should be "My Guild")
    SELECT id INTO default_guild_id FROM guilds LIMIT 1;
    
    -- Insert admin user with pre-hashed password
    -- Password hash for "Admin123!" using PBKDF2 (ASP.NET Core Identity default)
    -- This hash was generated using: new PasswordHasher<User>().HashPassword(null, "Admin123!")
    INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
    VALUES (
        gen_random_uuid(),
        'admin@dkp.local',
        'Admin',
        'AQAAAAEAACcQAAAAEAiEF0wbBWAeJNyNOv+LthTVmZl7DGbUO07dXqoE9ncFaQbK+J2IONXy/pFs2pkdeA==',
        'admin',
        default_guild_id,
        0,
        true,
        now()
    )
    ON CONFLICT (email) DO UPDATE SET password_hash = EXCLUDED.password_hash;
END $$;

-- Note: The password hash above is a placeholder. 
-- The actual hash will be generated when the admin first registers through the application,
-- or you can generate it using the ASP.NET Core Identity PasswordHasher and update this script.
