-- Module 4: Item Auctions
-- Tables already exist in 001_initial_schema.sql, so this migration only adds indexes
-- Auction session table (already exists)
-- Items within an auction (already exists)
-- Bids placed by raiders (already exists)

-- Indexes for performance (only create if they don't exist)
CREATE INDEX IF NOT EXISTS idx_auctions_guild_id ON auctions(guild_id);
CREATE INDEX IF NOT EXISTS idx_auctions_status ON auctions(status);
CREATE INDEX IF NOT EXISTS idx_auction_items_auction_id ON auction_items(auction_id);
CREATE INDEX IF NOT EXISTS idx_auction_bids_auction_item_id ON auction_bids(auction_item_id);
CREATE INDEX IF NOT EXISTS idx_auction_bids_user_id ON auction_bids(user_id);
