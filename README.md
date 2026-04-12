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
- 🚧 DKP event management
- 🚧 Auction system
- 🚧 Statistics dashboard

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
