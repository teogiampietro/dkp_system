-- Add invitation_code column to guilds table
-- This allows each guild to have a unique invitation code for registration

-- Only add column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'guilds' AND column_name = 'invitation_code'
    ) THEN
        ALTER TABLE guilds
        ADD COLUMN invitation_code VARCHAR(100) UNIQUE;
        
        -- Set a default invitation code for existing guilds
        -- You should change this code via database after deployment
        UPDATE guilds
        SET invitation_code = 'CHANGE-ME-' || SUBSTRING(id::text, 1, 8)
        WHERE invitation_code IS NULL;
        
        -- Make the column NOT NULL after setting default values
        ALTER TABLE guilds
        ALTER COLUMN invitation_code SET NOT NULL;
    END IF;
END $$;

-- Create index for faster lookups during registration (only if not exists)
CREATE INDEX IF NOT EXISTS idx_guilds_invitation_code ON guilds(invitation_code);
