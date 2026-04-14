# Startup Errors Fix - April 2026

## Issues Fixed

### 1. Migration Order Error
**Error:** `column "invitation_code" of relation "guilds" does not exist`

**Root Cause:** Migration [`002_seed_guild.sql`](../DkpSystem/Migrations/002_seed_guild.sql) was trying to insert data with `invitation_code` column before [`005_add_invitation_code.sql`](../DkpSystem/Migrations/005_add_invitation_code.sql) created the column.

**Solution:** Reordered migrations in [`DatabaseMigrator.cs`](../DkpSystem/Data/DatabaseMigrator.cs):
- Migration order changed from: 001 → 002 → 003 → 004 → 005
- New order: 001 → **005** → 002 → 003 → 004

### 2. Duplicate Table Creation
**Error:** `relation "auctions" already exists`

**Root Cause:** [`004_auction_tables.sql`](../DkpSystem/Migrations/004_auction_tables.sql) was trying to create tables that already existed in [`001_initial_schema.sql`](../DkpSystem/Migrations/001_initial_schema.sql).

**Solution:** 
- Modified [`004_auction_tables.sql`](../DkpSystem/Migrations/004_auction_tables.sql) to only create indexes (tables already exist)
- Added `IF NOT EXISTS` clause to all index creation statements
- Modified [`005_add_invitation_code.sql`](../DkpSystem/Migrations/005_add_invitation_code.sql) to check if column exists before adding it

### 3. Data Protection Key Errors
**Error:** `The key {082584a5-4c0d-433a-b075-11585d6ea021} was not found in the key ring`

**Root Cause:** ASP.NET Core Data Protection keys were being stored in `/root/.aspnet/DataProtection-Keys` inside the container, which is lost when the container restarts.

**Solution:**
1. Added Data Protection configuration in [`Program.cs`](../DkpSystem/Program.cs):
   - Keys now persist to `/app/DataProtection-Keys`
   - Added application name for key isolation
   
2. Added NuGet package to [`DkpSystem.csproj`](../DkpSystem/DkpSystem.csproj):
   - `Microsoft.AspNetCore.DataProtection` version 8.0.0

3. Updated [`Dockerfile`](../Dockerfile):
   - Created `/app/DataProtection-Keys` directory with proper permissions

4. Updated [`docker-compose.yml`](../docker-compose.yml):
   - Added persistent volume `dataprotection_keys` mounted to `/app/DataProtection-Keys`

### 4. Migration Error Handling
**Existing Solution:** The application already had proper error handling in [`Program.cs`](../DkpSystem/Program.cs) that catches migration errors and continues startup, preventing the application from crashing.

## Files Modified

1. [`DkpSystem/Data/DatabaseMigrator.cs`](../DkpSystem/Data/DatabaseMigrator.cs) - Fixed migration execution order
2. [`DkpSystem/Migrations/004_auction_tables.sql`](../DkpSystem/Migrations/004_auction_tables.sql) - Removed duplicate table creation
3. [`DkpSystem/Migrations/005_add_invitation_code.sql`](../DkpSystem/Migrations/005_add_invitation_code.sql) - Added idempotency checks
4. [`DkpSystem/Program.cs`](../DkpSystem/Program.cs) - Added Data Protection configuration
5. [`DkpSystem/DkpSystem.csproj`](../DkpSystem/DkpSystem.csproj) - Added Data Protection package
6. [`Dockerfile`](../Dockerfile) - Created DataProtection-Keys directory
7. [`docker-compose.yml`](../docker-compose.yml) - Added persistent volume for keys

## Testing

After these changes, the application should start without errors:

```bash
# Rebuild and restart the application
docker-compose down -v
docker-compose build --no-cache
docker-compose up
```

Expected output:
- ✅ All migrations execute successfully
- ✅ No duplicate table errors
- ✅ No Data Protection key warnings
- ✅ Application starts and listens on http://[::]:8080

## Notes

- The `libgssapi_krb5.so.2` warning is from the PostgreSQL driver and can be safely ignored. It's related to Kerberos authentication which is not used in this application.
- Data Protection keys will now persist across container restarts, preventing antiforgery token errors.
- All migrations are now idempotent and can be run multiple times safely.
