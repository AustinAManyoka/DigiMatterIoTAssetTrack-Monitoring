# DigiMatter IoT Asset Track

IoT asset tracking and sensor monitoring solution.

## Features

- Device Groups — Create nested groups with parent-child relationships and cycle prevention
- Firmware Management — Versioned firmware per device type with CRUD operations
- Device Tracking — Paginated device queries with search, filtering, and sorting

## Tech Stack

- ASP.NET Core 10 (.NET 10)
- C# with N-tier architecture
- SQL Server
- Entity Framework Core + Dapper


## Project Structure

```
DigiMatterIoTAssetTrack-Monitoring/
├── backend/
│   ├── IoTAssetTrack.sln
│   ├── IoTAssetTrack.Api/           # Web API & static file hosting
│   ├── IoTAssetTrack.Application/   # Services, DTOs, business logic
│   ├── IoTAssetTrack.Domain/        # Entity models
│   └── IoTAssetTrack.Infrastructure/# EF Core, repositories
│   
├── database/
│   ├── CreateTables.sql
│   └── SeedData.sql
└── docs/
│   ├── images
│   └──
├── frontend/
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express, or full instance)

### 1. Create the Database

Run the SQL scripts against your SQL Server instance:

```sql
-- In SQL Server Management Studio or sqlcmd:
:r database/CreateTables.sql
:r database/SeedData.sql
```

Or execute `CreateTables.sql` and `SeedData.sql` manually in order.

### 2. Configure Connection String

Update the connection string in `backend/IoTAssetTrack.Api/appsettings.json` if needed:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=IoTAssetTracker_db;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Run the Application

```bash
cd backend
dotnet run --project IoTAssetTrack.Api
```
## Architecture

The backend follows a clean  architecture:

1. API Layer — Controllers handle HTTP requests and return JSON
2. Application Layer — Services enforce business rules (hierarchy validation, uniqueness checks)
3. Infrastructure Layer — EF Core and Dapper repositories execute database operations
4. Domain Layer — Entity models representing the database schema


## Database Schema

| Table | Description |
|-------|-------------|
| `DeviceType` | Hardware profiles (Griffin Air, Yabby3 LoRaWAN, etc.) |
| `Firmware` | Versioned firmware per device type |
| `DeviceGroup` | Hierarchical groups (self-referencing) |
| `Device` | Individual IoT devices with location and firmware assignment |

## License

See [LICENSE](LICENSE).
