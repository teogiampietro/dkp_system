# DKP System — Technical Documentation

## Coding Standards

These standards apply to every line of code written in this project, without exception.

- All code must be written in **English**: variable names, method names, class names, properties, comments, and any other identifier.
- Code must follow **C# and .NET conventions**: PascalCase for classes, methods and properties; camelCase for local variables and parameters; `_camelCase` for private fields.
- Every public method and class must have **XML documentation comments** (`/// <summary>`).
- No magic numbers or hardcoded strings. Use constants or configuration.
- Methods must be small and have a single responsibility.
- No commented-out code or unresolved TODOs in delivered modules.
- Dependency injection must be used throughout — no `new` keyword for services or repositories outside of `Program.cs`.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| Frontend | Blazor Server |
| Data access | Dapper + raw SQL |
| Authentication | ASP.NET Core Identity |
| Database | PostgreSQL |
| Deployment | Railway (app + database in the same project) |

A single C# project end-to-end, with no frontend/backend separation.

---

## Data Model

```sql
-- Guilds (extensible to multi-guild in the future)
CREATE TABLE guilds (
  id         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name       VARCHAR(100) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Users registered via email + password
CREATE TABLE users (
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
CREATE TABLE events (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  guild_id    UUID NOT NULL REFERENCES guilds(id),
  name        VARCHAR(150) NOT NULL,
  description TEXT,
  -- No fixed date: created_at is used as the temporal reference
  created_by  UUID NOT NULL REFERENCES users(id),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Reward lines within an event (e.g. "Kill dragon +15")
CREATE TABLE event_reward_lines (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  event_id    UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  reason      VARCHAR(200) NOT NULL,  -- "Kill dragon", "On time", etc.
  dkp_amount  INTEGER NOT NULL CHECK (dkp_amount > 0)
);

-- DKP earned: which raiders participated in which reward line
CREATE TABLE dkp_earnings (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id          UUID NOT NULL REFERENCES users(id),
  event_id         UUID NOT NULL REFERENCES events(id),
  reward_line_id   UUID NOT NULL REFERENCES event_reward_lines(id),
  dkp_amount       INTEGER NOT NULL CHECK (dkp_amount > 0),
  earned_at        TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- DKP spent: tracked via auction_items (winner_id + final_price) in Module 4.
-- No separate spendings table. The raider profile queries auction_items directly.
```

---

## Module 1 — Security & Authentication

### Goal
Allow raiders to create their account with email and password and access the system. An administrator can later assign roles and guilds.

### Implementation
**ASP.NET Core Identity** with a custom store on PostgreSQL via Dapper (no Entity Framework store). Identity handles password hashing (PBKDF2) and session management via cookies.

### Registration flow
1. User fills in the Blazor form: email, username, and password.
2. Server validates that the email is not already registered.
3. `UserManager.CreateAsync` hashes the password and creates the user with `role = 'raider'` and no guild assigned.
4. User is automatically signed in after registration via `SignInManager.SignInAsync`.

### Login flow
1. User enters email and password in the Blazor form.
2. `SignInManager.PasswordSignInAsync` validates the credentials.
3. If valid, a session cookie is created with a `ClaimsPrincipal` containing `id`, `username`, and `role`.
4. If invalid, a generic error is shown ("Invalid credentials").

### Roles
| Role | Permissions |
|---|---|
| `raider` | View own profile and DKP history, change own password |
| `admin` | Everything above + manage members, create events, register auctions |

### Page protection
Blazor pages are protected with `[Authorize]` and `[Authorize(Roles = "admin")]` as appropriate. The `<AuthorizeView>` component is used to show or hide sections within a page based on role.

### Required environment variables
```
ConnectionStrings__DefaultConnection=Host=...;Database=dkp;Username=...;Password=...
```

### Considerations
- The first admin is created via a seed script (`Migrations/003_seed_admin.sql`) or by manually updating the DB.
- Automatic password recovery is out of scope. The admin can assign a temporary password from the member management panel (see Module 2).

---

## Module 2 — Member Management

### Goal
Full CRUD for raiders: listing, role editing, guild assignment, password reset, and DKP balance overview.

### Blazor pages

| Page | Route | Description | Required role |
|---|---|---|---|
| Member list | `/admin/members` | Table of all members | admin |
| Detail / edit | `/admin/members/{id}` | Full profile + DKP history | admin |
| Reset password | `/admin/members/{id}/reset-password` | Temporary password form | admin |
| My profile | `/profile` | Own balance + history | raider / admin |

### Business logic
- Deleting a member uses soft delete (`active = false`) to preserve the historical records in `dkp_earnings` and `auction_items` (won items).
- `dkp_balance` is denormalized in `users.dkp_balance` to avoid recalculating sums on every render. It is updated on every earning or spending transaction.
- **Password reset**: the admin enters a temporary password. `UserManager.RemovePasswordAsync` + `UserManager.AddPasswordAsync` are used. The admin communicates the password to the raider outside the system. The raider is encouraged to change it after logging in.

### Module views
- **Member table**: columns username, guild, DKP balance, role, status, actions. *(admin only)*
- **DKP ranking**: sorted by balance descending. *(admin only)*
- **Member profile**: username, DKP stats, chronological history of earnings and won auction items. *(admin sees anyone; raider sees only their own)*

### Raider dashboard (`/profile`)
The raider sees only:
- Current DKP balance displayed prominently.
- Personal earnings history: event, reason, points, date — sorted by date descending.
- Won items history: table with auction name, item name, DKP paid, and date — sorted by date descending. These represent DKP spent via auctions.
- Form to change own password (requires current password, new password, and confirmation).

---

## Module 3 — Event Management (DKP Earnings)

### Goal
Register raid events and assign DKP to participating raiders. The flow is two-staged: attendance is confirmed first, then DKP is assigned — either to the whole group or to individual raiders.

### Blazor pages

| Page | Route | Description | Required role |
|---|---|---|---|
| Event list | `/events` | All guild events | admin / raider |
| Create event | `/admin/events/new` | Step 1: name + attendance confirmation | admin |
| Event detail | `/events/{id}` | Step 2: award history + add group/individual awards | admin / raider* |
| Edit event | `/admin/events/{id}/edit` | Edit name or description only | admin |

*The raider sees only their own earnings in the detail.

### Event creation flow

**Step 1 — Attendance confirmation (`/admin/events/new`):**
1. Admin enters a name and optional description.
2. The system loads all active guild members, all pre-selected by default.
3. Admin unchecks any raiders who were absent.
4. On confirmation, the event is created and the attendee list is saved. No DKP is assigned yet.

**Step 2 — DKP assignment (`/events/{id}`):**
Once the event exists, the admin can assign DKP in two ways — both can be used together on the same event:
- **Group award**: enter a reason and a DKP amount applied to all confirmed attendees at once (e.g. "Raid completion → +20 DKP").
- **Individual award**: select a specific raider from the attendee list, enter a reason and a DKP amount that applies only to them (e.g. "First kill bonus → +10 DKP").

Each award creates the corresponding rows in `dkp_earnings` and updates `users.dkp_balance` within a single SQL transaction. Multiple awards can be added over time.

### Critical rule
> Before applying a DKP **spending**, verify that `dkp_balance - dkp_spent >= 0`. If the result would be negative, reject the operation with a clear message.

*(This rule applies to Module 4 but is defined here as a global system policy.)*

### Module views
- **Event list**: name, registration date, total DKP distributed, number of confirmed attendees.
- **Event detail (admin)**: confirmed attendee list, full award history (reason, amount, type, date), forms to add group or individual awards.
- **Event detail (raider)**: only their own earnings for this event (reason, amount, date). No other raiders' data visible.

---

## Module 4 — Item Auctions

### Goal
Run structured DKP auctions where the admin creates a bidding session with a list of items and a minimum bid per item. Raiders place their bids during the open window. When the auction closes, all bids are revealed and the admin delivers items one by one, deducting DKP from each winner.

### Database tables

```sql
-- Auction session
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
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  auction_item_id UUID NOT NULL REFERENCES auction_items(id) ON DELETE CASCADE,
  user_id      UUID NOT NULL REFERENCES users(id),
  amount       INTEGER NOT NULL CHECK (amount > 0),
  bid_type     VARCHAR(10) NOT NULL,  -- 'main' | 'alt' | 'greed'
  placed_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (auction_item_id, user_id)  -- one bid per raider per item
);
```

### Blazor pages

| Page | Route | Description | Required role |
|---|---|---|---|
| Auction list | `/auctions` | All auctions (open + history) | admin / raider |
| Create auction | `/admin/auctions/new` | Setup form: name, items, minimum bids, duration | admin |
| Auction detail | `/auctions/{id}` | Live bidding view (open) or results view (closed) | admin / raider |
| Auction history | `/auctions/history` | All closed auctions with final results | admin / raider |

### Admin flow

**Creating an auction:**
1. Admin enters a name for the auction session.
2. Admin adds one or more items, each with a name and a minimum bid (DKP). Duplicate item names are not allowed within the same auction.
3. Admin sets the duration (in minutes) for the bidding window.
4. Admin clicks "Start" — the auction status changes to `open` and `closes_at` is calculated from that moment.
5. When `closes_at` is reached, the UI displays a visual alert to the admin indicating the time window has elapsed and the auction is ready to close. The status does NOT change automatically — the admin must explicitly click "Close Auction" to move to `closed` status. This acts as a human validation that the items exist, were traded, and received by the winners in-game.
6. The admin can also close the auction early at any time before the timer elapses using the same "Close Auction" button.

**Cancelling an auction:**
- While the auction is in `pending` or `open` status, the admin can cancel it using a "Cancel Auction" button shown alongside the "Close Auction" button.
- Cancelling sets the status to `cancelled`. No DKP is deducted from any raider. All bids are discarded.
- Once cancelled, the admin can edit the item list (add, remove, or modify items and minimum bids) and restart the auction from scratch by clicking "Start" again, which resets the status to `open` with a new `closes_at`.
- A cancelled auction retains its history for traceability but is clearly marked as cancelled in the auction list.

**Delivering items (after auction closes):**
- The closed auction detail shows all items, each with their bids sorted by: amount descending, then by bid type priority (main > alt > greed), then by earliest bid timestamp for tiebreakers.
- If two or more raiders have the exact same amount and bid type, the system rolls a virtual die (1–100) per tied raider, shows all results publicly, and the highest roll wins.
- The admin delivers items one by one using a "Deliver" button next to the top bid of each item.
- On delivery: the system deducts `amount` from the winner's `dkp_balance` within a transaction, marks the item as `delivered`, and records `winner_id`, `final_price`, and `delivered_at`.
- If the winner's balance would go negative, the delivery is blocked with a descriptive error showing their current balance and the bid amount.

### Raider flow

**During open auction:**
- The raider sees all items being auctioned with their name and minimum bid. Bid amounts from other raiders are hidden.
- The raider can place a bid on any item by entering an amount (must be ≥ minimum bid) and selecting a bid type: Main, Alt, or Greed.
- The sum of all active bids by the raider across the auction cannot exceed their current `dkp_balance`. If placing or updating a bid would exceed the balance, it is rejected with a descriptive error.
- The raider can modify (raise or lower) or retract any of their bids at any time while the auction is open.
- The raider can bid on multiple items simultaneously.

**After auction closes:**
- All bids are revealed. Each item shows the full list of bidders, their amounts and bid types, sorted by priority.
- Items marked as delivered show who won and how much they paid.

### Bid type priority
When sorting bids for delivery order within the same amount:
1. **Main** — highest priority
2. **Alt** — second priority
3. **Greed** — lowest priority

Tiebreaker within same amount and same bid type: system rolls a virtual die (1–100) for each tied raider. Result is shown publicly. Highest roll wins.

### Critical rules
- A raider's total active bids across an auction cannot exceed their current `dkp_balance`.
- Minimum bid per item must be respected. Bids below minimum are rejected.
- Bids can only be placed or modified while the auction status is `open`.
- Item delivery can only be performed by an admin on a `closed` auction.
- Delivering an item that would leave the winner's balance negative is blocked with a descriptive error.
- If no raider placed a bid on an item, it is marked as "unclaimed" and remains visible in the closed auction detail and history. The admin takes no action on it — it stays with the guild.

### Module views
- **Auction list**: name, status (pending/open/closed/cancelled), closing time, number of items, number of bids placed.
- **Auction detail — open (raider)**: item list with name and minimum bid; own bids visible with type selector and modify/retract option; other raiders' bids hidden.
- **Auction detail — open (admin)**: same as raider view plus early close button.
- **Auction detail — closed**: full bid reveal per item sorted by priority; deliver button per item (admin only); delivered items show winner and final price; items with no bids are shown as "Unclaimed".
- **Auction history**: closed auctions visible to all, showing final results per item (winner, amount paid, bid type).

---

## Project Folder Structure

```
DkpSystem/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor        # Main layout with navbar
│   │   └── NavMenu.razor           # Role-aware navigation menu
│   ├── Pages/
│   │   ├── Auth/
│   │   │   ├── Login.razor
│   │   │   └── Register.razor
│   │   ├── Profile.razor           # Raider dashboard
│   │   ├── Events/
│   │   │   ├── EventList.razor
│   │   │   └── EventDetail.razor
│   │   └── Admin/
│   │       ├── Members/
│   │       │   ├── MemberList.razor
│   │       │   ├── MemberDetail.razor
│   │       │   └── ResetPassword.razor
│   │       ├── Events/
│   │       │   ├── EventForm.razor
│   │       │   └── EventEdit.razor
│   │       └── Spendings/
│   │           ├── SpendingList.razor
│   │           └── SpendingForm.razor
│   └── Shared/
│       └── DkpHistoryTable.razor   # Reusable DKP history component
├── Data/
│   ├── DbConnectionFactory.cs      # Dapper/Npgsql connection factory
│   └── Repositories/
│       ├── UserRepository.cs
│       ├── EventRepository.cs
│       ├── EarningRepository.cs
│       ├── AuctionRepository.cs
│       └── BidRepository.cs
├── Services/
│   ├── DkpService.cs               # Business logic: balance, validations
│   ├── MemberService.cs
│   ├── EventService.cs
│   └── AuctionService.cs           # Auction lifecycle, bid validation, delivery, tiebreaker
├── Models/
│   ├── User.cs
│   ├── Guild.cs
│   ├── Event.cs
│   ├── EventRewardLine.cs
│   ├── DkpEarning.cs
│   ├── Auction.cs
│   ├── AuctionItem.cs
│   └── AuctionBid.cs
├── Migrations/
│   ├── 001_initial_schema.sql
│   ├── 002_seed_guild.sql
│   └── 003_seed_admin.sql
├── appsettings.json
├── appsettings.Production.json
└── Program.cs
```

---

## Railway Deployment

1. Create a project on [railway.app](https://railway.app).
2. Add a **PostgreSQL** service → Railway provides the connection string automatically.
3. Add a service connected to the GitHub repository. Railway detects the .NET project and runs `dotnet publish`.
4. Set the environment variable in Railway:
   - `ConnectionStrings__DefaultConnection` (built from the PostgreSQL service credentials)
5. Run `001_initial_schema.sql` against the database once (via Railway CLI or any PostgreSQL client such as pgAdmin or DBeaver).
6. Run `002_seed_guild.sql` and `003_seed_admin.sql`.

---

## Future Considerations

- **Multi-guild**: the data model already includes `guild_id` on users and events to support this.
- **Immutable ledger**: add a `dkp_ledger` table as an append-only log of every balance movement for full auditability.
- **Export reports**: CSV export of history per raider or per event from the admin panel.
- **Discord notifications**: webhook to the guild channel when an event or auction is registered.
- **Password recovery**: automated email flow with `MailKit` + reset token stored in DB.
