# DKP System

DKP (Dragon Kill Points) management system built with Blazor Server and PostgreSQL.

## Description

DKP System is a web application for managing DKP points in MMORPG guilds. It allows you to manage events, item auctions, and track guild members' points.

## Technologies

- **Framework**: .NET 8.0 / Blazor Server
- **Database**: PostgreSQL
- **ORM**: Dapper
- **Authentication**: ASP.NET Core Identity with Cookie Authentication

## Features

- ✅ Authentication system (Login/Register)
- ✅ User and role management
- ✅ Modular architecture with services and repositories
- ✅ Database migrations
- ✅ Unit tests
- ✅ DKP event management
- ✅ Auction system

## Modules

### Events Management (DKP Earnings)
This module allows administrators to register raid events and assign DKP points to participating guild members. The process is divided into two stages:

1. **Attendance Confirmation**: The admin creates an event with a name and description, and confirms which guild members attended by pre-selecting all active members and allowing unchecking absentees.

2. **DKP Assignment**: Once the event is created, the admin can assign DKP through:
   - **Group Awards**: Apply a DKP amount to all confirmed attendees for reasons like raid completion.
   - **Individual Awards**: Assign specific DKP amounts to individual raiders for achievements like first kills.

All DKP assignments update member balances within database transactions to ensure data integrity. Raiders can view their earnings history, while admins see full event details.

### Item Auctions
The auction system enables structured DKP spending through competitive bidding on guild items. Key features include:

- **Auction Creation**: Admins create auctions with multiple items, each having a minimum bid and a set duration.
- **Bidding Process**: Raiders place bids on items with types (Main, Alt, Greed) while the auction is open. Bid amounts are hidden until closure, and total active bids cannot exceed a raider's DKP balance.
- **Auction Closure and Delivery**: Admins close auctions manually, revealing all bids sorted by amount, bid type priority, and timestamp. Ties are resolved with random die rolls. Admins then deliver items one by one, deducting DKP from winners within transactions.
- **Visibility**: Open auctions show item details and minimum bids; closed auctions display full results. Raiders can track their won items in their profiles.

This module ensures fair distribution of loot while maintaining DKP balance integrity.

## Project Structure

```
dkp_system/
├── DkpSystem/              # Main Blazor application
│   ├── Components/         # Razor components
│   ├── Data/              # Data access and repositories
│   ├── Models/            # Domain models
│   ├── Services/          # Business services
│   └── Migrations/        # SQL migration scripts
├── DkpSystem.Tests/       # Test project
└── HashGenerator/         # Hash generation utility
```

## Prerequisites

- .NET 8.0 SDK
- PostgreSQL 12+
- Visual Studio 2022, Rider, or VS Code

## Setup

1. Clone the repository:
```bash
git clone https://github.com/YOUR_USERNAME/dkp_system.git
cd dkp_system
```

2. Configure PostgreSQL database and update the connection string in `appsettings.json`

3. Run migrations:
```bash
psql -U your_user -d your_database -f DkpSystem/Migrations/run_all_migrations.sql
```

4. Run the application:
```bash
cd DkpSystem
dotnet run
```

5. Access the application at `https://localhost:5001`

## Default Credentials

- **Username**: admin
- **Password**: Admin123!

## Tests

Run unit tests:
```bash
dotnet test
```

## Documentation

- [System Documentation](DKP_SYSTEM_DOC.md)
- [Development Playbook](DKP_DEVELOPMENT_PLAYBOOK.md)

## License

This project is open source and available under the MIT License.

## Author

Developed by Teo Giampietro
