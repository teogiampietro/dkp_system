-- Migration 006: Add image_url to auction_items
ALTER TABLE auction_items ADD COLUMN IF NOT EXISTS image_url TEXT;
