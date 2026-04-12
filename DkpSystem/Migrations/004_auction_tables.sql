-- Module 4: Item Auctions
-- Auction session table
CREATE TABLE auctions (
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
CREATE TABLE auction_items (
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
CREATE TABLE auction_bids (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  auction_item_id UUID NOT NULL REFERENCES auction_items(id) ON DELETE CASCADE,
  user_id         UUID NOT NULL REFERENCES users(id),
  amount          INTEGER NOT NULL CHECK (amount > 0),
  bid_type        VARCHAR(10) NOT NULL,  -- 'main' | 'alt' | 'greed'
  placed_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (auction_item_id, user_id)  -- one bid per raider per item
);

-- Indexes for performance
CREATE INDEX idx_auctions_guild_id ON auctions(guild_id);
CREATE INDEX idx_auctions_status ON auctions(status);
CREATE INDEX idx_auction_items_auction_id ON auction_items(auction_id);
CREATE INDEX idx_auction_bids_auction_item_id ON auction_bids(auction_item_id);
CREATE INDEX idx_auction_bids_user_id ON auction_bids(user_id);
