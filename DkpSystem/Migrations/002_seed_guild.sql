-- Seed default guild (only if no guilds exist)
INSERT INTO guilds (name)
SELECT 'My Guild'
WHERE NOT EXISTS (SELECT 1 FROM guilds WHERE name = 'My Guild');
