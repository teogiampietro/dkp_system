# DKP System — Development Playbook

## How to use this document
Each module is an independent unit of work. When a module is complete, the system must compile, run, and be fully functional up to that point. Every module includes its own unit tests. Do not move on to the next module until the current one is 100% functional and all tests pass.

Before starting any module, provide the AI with the full technical documentation (`DKP_SYSTEM_DOC.md`) as base context.

---

## Coding Standards

These standards are non-negotiable and apply to every module.

- All code must be written in **English**: variable names, method names, class names, properties, parameters, and comments.
- Follow **C# and .NET naming conventions**: PascalCase for classes, methods, and properties; camelCase for local variables and parameters; `_camelCase` for private fields.
- Every public method and class must include **XML documentation comments** (`/// <summary>`).
- Use **dependency injection** throughout. Services and repositories must never be instantiated with `new` outside of `Program.cs`.
- No magic numbers or hardcoded strings. Use constants or configuration values.
- Methods must be small and follow the single responsibility principle.
- No commented-out code or unresolved TODOs in any delivered module.
- All unit tests must be written using **xUnit**. Test method names must follow the pattern `MethodName_Scenario_ExpectedResult`.

---

## Module 0 — Project Foundation

### Context
No business logic yet. This module establishes the full scaffolding so every subsequent module can be built on a solid, consistent base.

### What must work when this module is done
- The project compiles and runs without errors or warnings (`dotnet build`, `dotnet run`).
- The app is accessible on localhost and renders the default Blazor page.
- The SQL migration scripts can be executed against a local PostgreSQL database and all 8 tables are created correctly.
- All unit tests pass (`dotnet test`).

### Functional requirements
- ASP.NET Core 8 project with Blazor Server.
- PostgreSQL connection using Npgsql and Dapper. No Entity Framework.
- A `DbConnectionFactory` class that centralizes database connection creation and is registered in the DI container.
- Folder structure matching the one defined in the technical documentation: Components, Data/Repositories, Services, Models, Migrations.
- `appsettings.json` with a `ConnectionStrings` section containing a placeholder value.
- `appsettings.Production.json` empty and ready for Railway.
- `Migrations/001_initial_schema.sql` with the full DDL for all tables: guilds, users, events, event_reward_lines, dkp_earnings, auctions, auction_items, auction_bids.
- `Migrations/002_seed_guild.sql` that inserts a default guild named "My Guild".
- Model classes in `Models/`, one file per class, mapping exactly to the database tables: Guild, User, Event, EventRewardLine, DkpEarning, Auction, AuctionItem, AuctionBid.

### Unit tests
- `DbConnectionFactory_CreateConnection_ReturnsOpenConnection`: verifies the factory produces a valid, open database connection.
- One test per model class verifying that all properties exist with the correct C# types matching their SQL column counterparts.

---

## Module 1 — Authentication

### Context
The entry point to the system. No other module can be used without passing through here. When this module is done, a real user must be able to register, log in, and log out.

### What must work when this module is done
- A new user can register at `/register`.
- The user can log in at `/login` and log out.
- The registration is stored in the `users` table with a populated `password_hash`.
- A raider cannot access any admin route — they are redirected to an "Unauthorized" page.
- The admin seed script works and allows the first admin login.

### Functional requirements
- Registration with email, username, and password. Email must be unique.
- Login with email and password. Session managed via ASP.NET Core Identity cookies.
- Logout that invalidates the session.
- Passwords hashed using ASP.NET Core Identity's default mechanism (PBKDF2).
- On registration, the user is created with `role = 'raider'` and no guild assigned.
- After successful registration, the user is automatically signed in and redirected to `/profile`.
- All routes require an active session. Routes under `/admin/*` require `role = 'admin'`. A raider attempting to access `/admin/*` sees an "Unauthorized" page.
- The Identity store must be implemented on PostgreSQL with Dapper. No Entity Framework store.
- `Migrations/003_seed_admin.sql` inserts the first admin user with known credentials, ready to be executed in a fresh environment.
- The main layout navbar displays the logged-in username and a logout button.

### Unit tests
- `Register_WithValidData_CreatesUserWithRaiderRole`
- `Register_WithDuplicateEmail_ReturnsError`
- `Login_WithValidCredentials_CreatesAuthenticatedSession`
- `Login_WithWrongPassword_ReturnsGenericError`
- `PasswordHash_IsNeverStoredAsPlainText`
- `AdminRoute_AccessedByRaider_ReturnsUnauthorized`

---

## Module 2 — Member Management

### Context
The admin needs to manage who belongs to the system and with what role. This module also covers the raider's main view, where they can see their own information.

### What must work when this module is done
- The admin can view, edit, deactivate, and reset the password of any member.
- A deactivated member cannot log in.
- The raider sees their profile with balance and history (both empty if no events have been created yet).
- The raider can change their own password.
- The raider cannot access another member's profile or any admin page.

### Functional requirements

**Admin panel:**
- List of all members with columns: username, guild, DKP balance, role, status (active/inactive), actions.
- Edit a member: change their role (raider/admin) and assign or change their guild.
- Soft delete a member (`active = false`). Historical DKP records are preserved.
- Password reset: the admin enters a temporary password, the system hashes and saves it. The admin is responsible for communicating the password to the raider outside the system.
- Member ranking sorted by `dkp_balance` descending.

**Raider profile (`/profile`):**
- Current DKP balance displayed prominently.
- Earnings history: table with event, reason, points earned, and date — sorted by date descending.
- Won items history: table with auction name, item name, DKP paid, and date — sorted by date descending. These are the raider's DKP spendings via auctions.
- Form to change own password: requires current password, new password, and confirmation.

### Unit tests
- `UpdateRole_WithValidMember_ChangesRoleCorrectly`
- `SoftDelete_DeactivatesMember_PreventsLogin`
- `SoftDelete_DeactivatesMember_PreservesHistoricalRecords`
- `AdminResetPassword_WithValidData_UpdatesPasswordHash`
- `ChangeOwnPassword_WithCorrectCurrentPassword_Succeeds`
- `ChangeOwnPassword_WithWrongCurrentPassword_ReturnsError`
- `GetMemberProfile_ByRaider_CannotAccessOtherMembersProfile`
- `GetRanking_ReturnsMembersSortedByBalanceDescending`

---

## Module 3 — Event Management (DKP Earnings)

### Context
Events are the mechanism by which raiders earn DKP. The creation flow is two-staged: first the admin confirms who was present, then assigns DKP to the group — with the ability to add individual awards on top.

### What must work when this module is done
- The admin can create an event, confirm attendees, and assign DKP both globally and individually.
- Raider balances are updated correctly after each DKP assignment.
- All DKP assignments within a single operation happen within a SQL transaction — if anything fails, no partial changes remain in the database.
- The raider sees the event in the list and only their own points in the detail.
- An event with associated earnings cannot be deleted.

### Event creation flow

**Step 1 — Attendance confirmation:**
- Admin clicks "New Event" and enters a name and optional description.
- The system loads all active guild members, all pre-selected by default.
- The admin unchecks any raiders who were absent.
- On confirmation, the event is created and the confirmed attendee list is saved. No DKP is assigned yet at this point.

**Step 2 — DKP assignment (on the event detail page):**
- Once the event exists, the admin can assign DKP in two ways, and both can be used together on the same event:
  - **Group award**: enter a reason (free text) and a DKP amount applied to all confirmed attendees at once (e.g. "Raid completion → +20 DKP").
  - **Individual award**: select a specific raider from the attendee list, enter a reason and a DKP amount that applies only to them (e.g. "First kill bonus → +10 DKP").
- Each award — group or individual — creates the corresponding rows in `dkp_earnings` and updates `users.dkp_balance` within a single SQL transaction.
- Multiple awards can be added over time. The event detail always shows the full accumulated history of all awards.

### Functional requirements
- Event list: visible to all users. Shows name, registration date, total DKP distributed, and number of confirmed attendees.
- Event detail (admin): shows the confirmed attendee list, the full award history (reason, amount, type — group/individual, date), and the forms to add new group or individual awards.
- Event detail (raider): shows only the raider's own earnings for this event (reason, amount, date). No other raiders' data is visible.
- Edit an event: name and description only.
- Delete an event: only allowed if it has no associated earnings. If it does, the deletion is blocked with a clear message.
- A DKP amount of zero or below must be rejected on any award.

### Unit tests
- `CreateEvent_ConfirmsAllActiveGuildMembersAsAttendeesByDefault`
- `CreateEvent_WithSomeAttendeesRemoved_OnlySavesConfirmedAttendees`
- `AddGroupAward_CreatesEarningsForAllConfirmedAttendees`
- `AddGroupAward_UpdatesBalanceForAllConfirmedAttendees`
- `AddIndividualAward_CreatesEarningOnlyForTargetRaider`
- `AddIndividualAward_UpdatesBalanceOnlyForTargetRaider`
- `AddAward_TransactionFailure_NoPartialChangesPersistedInDatabase`
- `GetEventDetail_ByRaider_ReturnsOnlyOwnEarnings`
- `DeleteEvent_WithExistingEarnings_IsBlockedWithClearMessage`
- `DeleteEvent_WithNoEarnings_SucceedsAndRemovesEvent`
- `AddAward_WithZeroOrNegativeDkpAmount_IsRejected`

---

## Module 4 — Item Auctions

### Context
Auctions are the primary DKP sink. The admin creates a bidding session with items and minimum bids, opens it for a set time window, and raiders compete by placing bids. When the auction closes, bids are revealed and the admin delivers items one by one, deducting DKP from each winner. The raider's total active bids can never exceed their current balance.

### What must work when this module is done
- The admin can create an auction with multiple items, set a duration, start it, and close it early if needed.
- Raiders can place, modify, and retract bids on any item while the auction is open.
- A raider's total active bids across the auction cannot exceed their current `dkp_balance`.
- When the auction closes, all bids are revealed sorted by amount descending, then by bid type priority (main > alt > greed).
- Ties in amount and bid type are resolved by a system die roll (1–100), shown publicly.
- The admin can deliver items one by one; delivery deducts DKP from the winner within a transaction.
- Delivery is blocked if the winner's balance would go negative.
- Closed auctions and their results are visible to all users in the auction history.

### Functional requirements

**Auction creation and lifecycle:**
- Admin creates an auction with a name, a list of items (each with a name and minimum bid), and a duration in minutes.
- Duplicate item names within the same auction are not allowed.
- Admin explicitly starts the auction — status changes from `pending` to `open` and `closes_at` is set.
- The auction never closes automatically. When `closes_at` is reached, the UI shows a visual alert to the admin that the time window has elapsed. The admin must explicitly click "Close Auction" to move status to `closed`. This is a deliberate human validation step.
- Admin can also close early before the timer elapses using the same "Close Auction" button.
- Admin can cancel the auction at any time while it is `pending` or `open`. Cancellation sets status to `cancelled`, discards all bids, and deducts no DKP. Once cancelled, the admin can edit items and restart the auction from scratch.

**Bidding (raider):**
- Raiders can only bid while the auction status is `open`.
- Each raider can place one bid per item, selecting an amount (≥ minimum bid) and a bid type: Main, Alt, or Greed.
- The sum of all active bids by the raider within the auction cannot exceed their `dkp_balance` at the time of placing/updating.
- Raiders can raise, lower, or retract their bids at any time while the auction is open.
- Raiders can bid on multiple items simultaneously.
- Bid amounts from other raiders are hidden while the auction is open.

**Auction close and item delivery:**
- When closed, all bids are revealed per item sorted by: amount descending → bid type priority (main > alt > greed) → earliest bid timestamp.
- If two or more raiders tie on amount and bid type, the system rolls a virtual die (1–100) per tied raider, displays all results publicly, and the highest roll wins.
- Admin delivers items one by one with a "Deliver" button. Delivery records `winner_id`, `final_price`, and `delivered_at`, and deducts the amount from the winner's `dkp_balance` within a SQL transaction.
- If delivery would leave the winner's balance negative, it is blocked with a descriptive error showing current balance and bid amount.

**Visibility:**
- Auction list and history are visible to all authenticated users.
- Closed auction results (winner per item, amount paid, bid type) are visible to all.
- A raider's own won items (DKP spendings) appear in their `/profile` as a won items history.

### Unit tests
- `CreateAuction_WithDuplicateItemNames_IsRejected`
- `StartAuction_SetsStatusToOpenAndCalculatesClosesAt`
- `CloseAuctionEarly_SetsStatusToClosedImmediately`
- `PlaceBid_BelowMinimumBid_IsRejected`
- `PlaceBid_ThatExceedsRaiderBalance_IsRejected`
- `PlaceBid_WhereTotalActiveBidsExceedBalance_IsRejected`
- `UpdateBid_WhileAuctionIsOpen_Succeeds`
- `RetractBid_WhileAuctionIsOpen_Succeeds`
- `PlaceBid_OnClosedAuction_IsRejected`
- `GetAuctionDetail_WhileOpen_HidesOtherRaidersBidAmounts`
- `GetAuctionDetail_WhenClosed_RevealsAllBidsSortedByPriorityAndAmount`
- `SortBids_WithSameAmount_AppliesBidTypePriority_MainBeforeAltBeforeGreed`
- `ResolveTie_WithSameAmountAndBidType_RollsDieAndPicksHighest`
- `DeliverItem_DeductsDkpFromWinnerWithinTransaction`
- `DeliverItem_WhenWinnerBalanceWouldGoNegative_IsBlockedWithDescriptiveError`
- `DeliverItem_TransactionFailure_NoPartialChangesPersistedInDatabase`
- `AuctionHistory_IsVisibleToAllAuthenticatedUsers`
- `CancelAuction_WhilePending_DiscardsBidsAndDeductsNoDkp`
- `CancelAuction_WhileOpen_DiscardsBidsAndDeductsNoDkp`
- `CancelAuction_WhileClosed_IsRejected`
- `AuctionItem_WithNoBids_IsMarkedAsUnclaimed_NoAdminActionRequired`

---

## Module delivery checklist

Before closing any module, verify:

- [ ] `dotnet build` with no errors or warnings
- [ ] `dotnet run` starts the app with no exceptions
- [ ] All unit tests for the module pass (`dotnet test`)
- [ ] Functionality can be manually verified in the browser
- [ ] No commented-out code or unresolved TODOs
- [ ] All identifiers (variables, methods, classes, properties) are in English
- [ ] Generated files respect the folder structure defined in the technical documentation
- [ ] All public methods and classes have XML documentation comments
