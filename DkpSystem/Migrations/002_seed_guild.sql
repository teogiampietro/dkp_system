-- Seed default guild (only if no guilds exist)
INSERT INTO guilds (name, invitation_code)
SELECT 'My Guild', 'MYGUILD2024'
WHERE NOT EXISTS (SELECT 1 FROM guilds WHERE name = 'My Guild');
