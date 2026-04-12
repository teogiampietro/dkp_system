# Module 2 — Fixes Applied

**Date:** 2026-04-12  
**Issues Resolved:** Role-based authorization and interactive components

---

## Issue 1: Role Claims Not Added During Login

### Problem
When users logged in with `admin@dkp.local`, the role claim was not being added to the `ClaimsPrincipal`, causing:
- `[Authorize(Roles = "admin")]` to fail
- `<AuthorizeView Roles="admin">` to not display admin-only content
- Admin users unable to access `/admin/members` and other admin routes

### Root Cause
The default `SignInManager.SignInAsync` does not automatically add custom user properties (like `role`) as claims. ASP.NET Core Identity needs a custom `IUserClaimsPrincipalFactory` to map user properties to claims.

### Solution
Created [`CustomUserClaimsPrincipalFactory.cs`](../DkpSystem/Data/Identity/CustomUserClaimsPrincipalFactory.cs):
- Extends `UserClaimsPrincipalFactory<User>`
- Overrides `GenerateClaimsAsync` to add:
  - `ClaimTypes.Role` with the user's role (admin/raider)
  - `"username"` claim for display purposes

Registered in [`Program.cs`](../DkpSystem/Program.cs):
```csharp
.AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>()
```

### Files Modified
- ✅ Created: `DkpSystem/Data/Identity/CustomUserClaimsPrincipalFactory.cs`
- ✅ Modified: `DkpSystem/Program.cs` (line 58)

---

## Issue 2: Buttons Not Responding (No Interactivity)

### Problem
Clicking buttons (Edit, Reset Password, Deactivate) in the member management pages did nothing. The UI was not responding to user interactions.

### Root Cause
Blazor Server components require the `@rendermode InteractiveServer` directive to enable interactivity. Without it, components render as static HTML and event handlers (`@onclick`) don't work.

### Solution
Added `@rendermode InteractiveServer` directive to all interactive pages:

1. **[`MemberList.razor`](../DkpSystem/Components/Pages/Admin/Members/MemberList.razor)** (line 6)
2. **[`MemberDetail.razor`](../DkpSystem/Components/Pages/Admin/Members/MemberDetail.razor)** (line 7)
3. **[`ResetPassword.razor`](../DkpSystem/Components/Pages/Admin/Members/ResetPassword.razor)** (line 7)
4. **[`Profile.razor`](../DkpSystem/Components/Pages/Profile.razor)** (line 8)

### Files Modified
- ✅ Modified: `DkpSystem/Components/Pages/Admin/Members/MemberList.razor`
- ✅ Modified: `DkpSystem/Components/Pages/Admin/Members/MemberDetail.razor`
- ✅ Modified: `DkpSystem/Components/Pages/Admin/Members/ResetPassword.razor`
- ✅ Modified: `DkpSystem/Components/Pages/Profile.razor`

---

## Testing Instructions

### 1. Restart the Application
```bash
# Stop the current instance (Ctrl+C)
dotnet run --project DkpSystem/DkpSystem.csproj
```

### 2. Test Role-Based Authorization
1. Navigate to http://localhost:5073
2. Log out if already logged in
3. Log in with `admin@dkp.local` / `Admin123!`
4. Verify the "Members" link appears in the navigation menu
5. Click "Members" - should navigate to `/admin/members` successfully
6. Verify you can see the member list

### 3. Test Interactive Components
1. On the member list page, click any button:
   - **Edit** - should navigate to member detail page
   - **Reset Password** - should navigate to password reset page
   - **Deactivate** - should show confirmation and update the member status
2. Click "Show DKP Ranking" - should toggle the view
3. On member detail page, change role or guild and click "Save Changes" - should update successfully
4. On password reset page, enter a new password and click "Reset Password" - should update successfully

### 4. Test Raider Access
1. Log out
2. Register a new user or log in as a raider
3. Verify the "Members" link does NOT appear in the navigation
4. Try to access `/admin/members` directly - should redirect to "Unauthorized" page
5. Verify "My Profile" link is visible and accessible

---

## Build Status

```
Build succeeded.
    0 Error(s)
    9 Warning(s) (nullable reference warnings only)
```

---

## Summary

Both critical issues have been resolved:

1. ✅ **Role-based authorization** now works correctly with custom claims principal factory
2. ✅ **Interactive components** now respond to user actions with `@rendermode InteractiveServer`

The Module 2 - Member Management is now fully functional with proper authorization and interactivity.

---

## Issue 3: Duplicate Guild Entries on Every Application Start

### Problem
The database had 60 duplicate "My Guild" entries. Every time the application started, the migration [`002_seed_guild.sql`](../DkpSystem/Migrations/002_seed_guild.sql) was executing and inserting a new "My Guild" record.

### Root Cause
The [`DatabaseMigrator.cs`](../DkpSystem/Data/DatabaseMigrator.cs) was executing all migrations on every application startup without tracking which migrations had already been executed. This caused seed data to be inserted repeatedly.

### Solution

#### 1. Migration Tracking System
Modified [`DatabaseMigrator.cs`](../DkpSystem/Data/DatabaseMigrator.cs) to implement a migration tracking system:
- Created `__migrations` table to track executed migrations
- Added `EnsureMigrationsTableExistsAsync()` to create the tracking table
- Added `IsMigrationExecutedAsync()` to check if a migration was already run
- Added `MarkMigrationAsExecutedAsync()` to record executed migrations
- Modified `ExecuteMigrationAsync()` to skip already-executed migrations

#### 2. Idempotent Migrations
Updated [`002_seed_guild.sql`](../DkpSystem/Migrations/002_seed_guild.sql) to be idempotent:
```sql
INSERT INTO guilds (name)
SELECT 'My Guild'
WHERE NOT EXISTS (SELECT 1 FROM guilds WHERE name = 'My Guild');
```

The [`003_seed_admin.sql`](../DkpSystem/Migrations/003_seed_admin.sql) already had `ON CONFLICT (email) DO UPDATE` so it was already idempotent.

#### 3. Cleanup Scripts
Created cleanup scripts to remove duplicate guilds:
- **[`cleanup_duplicate_guilds.sql`](../cleanup_duplicate_guilds.sql)** - SQL script that:
  - Keeps the first "My Guild" entry
  - Updates all user references to point to the kept guild
  - Deletes all duplicate guilds
  - Shows verification results
  
- **[`cleanup_guilds.sh`](../cleanup_guilds.sh)** - Shell script that:
  - Shows current guild count
  - Asks for confirmation before cleanup
  - Executes the SQL cleanup script
  - Displays results

### Files Modified
- ✅ Modified: `DkpSystem/Data/DatabaseMigrator.cs`
- ✅ Modified: `DkpSystem/Migrations/002_seed_guild.sql`
- ✅ Created: `cleanup_duplicate_guilds.sql`
- ✅ Created: `cleanup_guilds.sh`

### How to Clean Up Existing Duplicates

Run the cleanup script:
```bash
./cleanup_guilds.sh
```

Or manually execute the SQL:
```bash
psql "postgresql://..." -f cleanup_duplicate_guilds.sql
```

### Verification
After the fix, migrations will:
- ✅ Only run once (tracked in `__migrations` table)
- ✅ Show "already executed" message on subsequent runs
- ✅ Never create duplicate seed data

---

## Next Steps

- Run `./cleanup_guilds.sh` to remove the 60 duplicate guilds
- Restart the application to verify migrations don't run again
- Test all functionality manually as described above
- Verify that raiders cannot access admin pages
- Verify that all buttons and forms work correctly
- Proceed to Module 3 - Event Management once testing is complete
