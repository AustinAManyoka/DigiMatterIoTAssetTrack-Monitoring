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
│    ├── index.html                 ✓ Dashboard landing page with feature cards
│    ├── devices.html               ✓ Device management (table + map views)
│    ├── groups.html                ✓ Hierarchical group management
│    └── firmware.html              ✓ Firmware version tracking
├── css/
│   └── styles.css             ✓ Complete responsive design system
└── js/
    ├── api.js                 ✓ API utilities, formatters, helpers
    ├── layout.js              ✓ Sidebar navigation, page shell
    ├── devices.js             ✓ Device management logic 
    ├── groups.js              ✓ Group management logic 
    └── firmware.js            ✓ Firmware management logic 
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

![Architecture](docs/images/IoTAssetTrack_Architecture.png)

## Database Schema

| Table | Description |
|-------|-------------|
| `DeviceType` | Hardware profiles (Griffin Air, Yabby3 LoRaWAN, etc.) |
| `Firmware` | Versioned firmware per device type |
| `DeviceGroup` | Hierarchical groups (self-referencing) |
| `Device` | Individual IoT devices with location and firmware assignment |

### Entity Relationship Diagram

![Database Schema](docs/images/database-schema.png)


### Prerequisites

- .NET SDK
- SQL Server
- Visual Studio 2022

### Steps

1. Run `01_CreateDatabase.sql`
2. Run `02_CreateTables.sql`
3. Run `03_SeedData.sql`
4. Update the SQL Server connection string
5. Run the application

## Features

### Device Management

- Create, update and view devices
- Assign firmware
- Assign groups
- Device location management
- Active/inactive status
- Map visualisation using Leaflet

### Firmware Management

- Create firmware versions
- Update firmware
- Filter by device type
- Prevent duplicate firmware versions

### Group Management

- Hierarchical parent/child groups
- Recursive tree view
- Assign devices to groups
- Parent group validation

The frontend consists of four primary pages.

| Page | Purpose |
|------|---------|
| Dashboard | Overview and navigation |
| Devices | Manage IoT devices and map |
| Groups | Manage hierarchical groups |
| Firmware | Manage firmware versions |


## Assumptions

- A device belongs to one firmware version.
- Firmware belongs to a single device type.
- Groups support unlimited nesting.
- Devices may optionally belong to a group.

## Future Improvements

- Unit tests
- Soft deletes(Firmware,Devices)
- Serial number should be editable only when creating a new device 
- Authentication and authorization
- Audit logging
- Pagination
- Bulk device import
- Docker support ??


## License

See [LICENSE](LICENSE).
