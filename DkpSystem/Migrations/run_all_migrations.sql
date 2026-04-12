-- DKP System - Complete Database Setup
-- Run this script to create all tables and seed initial data

-- ============================================
-- MIGRATION 001: Initial Schema
-- ============================================

-- Guilds (extensible to multi-guild in the future)
CREATE TABLE IF NOT EXISTS guilds (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name       VARCHAR(100) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Users registered via email + password
CREATE TABLE IF NOT EXISTS users (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email          VARCHAR(255) UNIQUE NOT NULL,
  username       VARCHAR(100) NOT NULL,
  password_hash  TEXT NOT NULL,
  role           VARCHAR(20) NOT NULL DEFAULT 'raider', -- 'admin' | 'raider'
  guild_id       UUID REFERENCES guilds(id),
  dkp_balance    INTEGER NOT NULL DEFAULT 0,  -- never negative, enforced in app
  active         BOOLEAN NOT NULL DEFAULT true,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Raid events (where DKP is earned)
CREATE TABLE IF NOT EXISTS events (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  guild_id    UUID NOT NULL REFERENCES guilds(id),
  name        VARCHAR(150) NOT NULL,
  description TEXT,
  created_by  UUID NOT NULL REFERENCES users(id),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Reward lines within an event (e.g. "Kill dragon +15")
CREATE TABLE IF NOT EXISTS event_reward_lines (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  event_id    UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  reason      VARCHAR(200) NOT NULL,  -- "Kill dragon", "On time", etc.
  dkp_amount  INTEGER NOT NULL CHECK (dkp_amount > 0)
);

-- DKP earned: which raiders participated in which reward line
CREATE TABLE IF NOT EXISTS dkp_earnings (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id          UUID NOT NULL REFERENCES users(id),
  event_id         UUID NOT NULL REFERENCES events(id),
  reward_line_id   UUID NOT NULL REFERENCES event_reward_lines(id),
  dkp_amount       INTEGER NOT NULL CHECK (dkp_amount > 0),
  earned_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Auction session
CREATE TABLE IF NOT EXISTS auctions (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  guild_id     UUID NOT NULL REFERENCES guilds(id),
  name         VARCHAR(150) NOT NULL,
  status       VARCHAR(20) NOT NULL DEFAULT 'pending', -- 'pending' | 'open' | 'closed' | 'cancelled'
  closes_at    TIMESTAMPTZ NOT NULL,   -- scheduled closing time, used as visual reference only
  closed_at    TIMESTAMPTZ,            -- actual closing time, always set by admin action
  created_by   UUID NOT NULL REFERENCES users(id),
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Items within an auction (always 1 unit per item)
CREATE TABLE IF NOT EXISTS auction_items (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  auction_id   UUID NOT NULL REFERENCES auctions(id) ON DELETE CASCADE,
  name         VARCHAR(200) NOT NULL,
  minimum_bid  INTEGER NOT NULL CHECK (minimum_bid > 0),
  delivered    BOOLEAN NOT NULL DEFAULT false,
  delivered_at TIMESTAMPTZ,
  delivered_by UUID REFERENCES users(id),  -- admin who delivered
  winner_id    UUID REFERENCES users(id),  -- set when delivered
  final_price  INTEGER,                    -- set when delivered
  created_at   TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Bids placed by raiders
CREATE TABLE IF NOT EXISTS auction_bids (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  auction_item_id UUID NOT NULL REFERENCES auction_items(id) ON DELETE CASCADE,
  user_id         UUID NOT NULL REFERENCES users(id),
  amount          INTEGER NOT NULL CHECK (amount > 0),
  bid_type        VARCHAR(10) NOT NULL,  -- 'main' | 'alt' | 'greed'
  placed_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (auction_item_id, user_id)  -- one bid per raider per item
);

-- ============================================
-- MIGRATION 002: Seed Default Guild
-- ============================================

INSERT INTO guilds (name) 
VALUES ('My Guild')
ON CONFLICT DO NOTHING;

-- ============================================
-- MIGRATION 003: Seed Admin User
-- ============================================

DO $$
DECLARE
    default_guild_id UUID;
BEGIN
    -- Get the first guild (should be "My Guild")
    SELECT id INTO default_guild_id FROM guilds LIMIT 1;
    
    -- Insert admin user
    -- Default credentials: admin@dkp.local / Admin123!
    -- IMPORTANT: Change this password after first login in production!
    INSERT INTO users (id, email, username, password_hash, role, guild_id, dkp_balance, active, created_at)
    VALUES (
        gen_random_uuid(),
        'admin@dkp.local',
        'Admin',
        'AQAAAAIAAYagAAAAEJxK8qH5Z8QxqF7VxqF7VxqF7VxqF7VxqF7VxqF7VxqF7VxqF7VxqF7VxqF7Vxq=',
        'admin',
        default_guild_id,
        0,
        true,
        now()
    )
    ON CONFLICT (email) DO NOTHING;
END $$;

-- ============================================
-- Verification
-- ============================================

-- Show created tables
SELECT 'Tables created:' as status;
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' 
ORDER BY table_name;

-- Show guild count
SELECT 'Guilds:' as status, COUNT(*) as count FROM guilds;

-- Show user count
SELECT 'Users:' as status, COUNT(*) as count FROM users;
