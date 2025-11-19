# Retail Monolith App

A lightweight ASP.NET Core 9 Razor Pages application that simulates a retail monolith before decomposition.  
It includes product listing, shopping cart, checkout, and inventory management — built to demonstrate modernisation and refactoring patterns.

**Current Status:** Phase 4 Complete - All decomposition phases verified and cleanup complete.

---

## Documentation

### Decomposition Project
- [Guiding Star](docs/monolith%20decomposition/1_Guiding_Star.md) - Strategic vision and architecture goals
- [Phased Plan](docs/monolith%20decomposition/2_Phased_Plan.md) - Step-by-step decomposition roadmap
- [Coding Standards](docs/monolith%20decomposition/3_Coding_Standards.md) - Development guidelines and best practices
- [Checkout API Reference](docs/monolith%20decomposition/4_Checkout_API_Reference.md) - Complete API documentation

### Project Progress
- ✅ Phase 1: Scaffold New API (Completed 19 Nov 2025)
- ✅ Phase 2: Migrate Business Logic (Completed 19 Nov 2025)
- ✅ Phase 3: Refactor Monolith to Proxy (Completed 19 Nov 2025)
- ✅ Phase 4: Verification & Cleanup (Completed 19 Nov 2025)

---

## Features

### Monolith (Legacy)
- ASP.NET Core 9 (Razor Pages)
- Entity Framework Core (SQL Server LocalDB)
- Dependency Injection with modular services:
  - `CartService`
  - `CheckoutService` (HTTP proxy to microservice only)
- 50 sample seeded products with random inventory
- End-to-end retail flow:
  - Products → Cart → Checkout (proxied to API) → Orders
- Minimal APIs:
  - `POST /api/checkout` (legacy endpoint - also proxies to microservice)
  - `GET /api/orders/{id}`
- Health-check endpoint at `/health`

### Checkout Microservice (New)
- ASP.NET Core 9 Web API
- RESTful endpoint: `POST /api/checkout`
- Full checkout business logic extracted
- Comprehensive test coverage (10 tests: 9 unit + 1 integration)
- Docker-ready with multi-stage Dockerfile
- Health checks at `/health`
- OpenAPI/Swagger documentation
- Container-ready (stateless, config via env vars, console logging)
- See [Checkout API Reference](docs/monolith%20decomposition/4_Checkout_API_Reference.md) for complete documentation

---

## 🏠 Home Page
![Home Page Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/HomePage.jpg)

## 🛍 Products
![Products Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/Products.jpg)

## 🧺 Cart
![Cart Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/Cart.jpg)

## 💳 Checkout
![Checkout Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/CheckOut.jpg)

## 📦 Orders
![Orders Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/Orders.jpg)

## 📦 Order Details
![Orders Screenshot](https://github.com/lavann/ads_monotlith_app/blob/main/Images/OrderDetails.jpg)

---

## Development Setup

You can run and edit this application in three different ways:

### 1. Local Development Environment

Run the application directly on your local machine with your preferred IDE or editor.

**Prerequisites:**
- .NET 9 SDK installed ([download](https://dotnet.microsoft.com/download/dotnet/9.0))
- SQL Server LocalDB (included with Visual Studio) or SQL Server instance
- Your favorite code editor (Visual Studio, VS Code, Rider, etc.)

**Steps:**
```bash
git clone https://github.com/lavann/ads_monotlith_app.git
cd ads_monotlith_app
dotnet restore
dotnet ef database update
dotnet run
```

### 2. Docker-Hosted Dev Container

Use a Docker container with a pre-configured development environment. This ensures consistency across different machines without installing dependencies locally.

**Prerequisites:**
- Docker Desktop installed and running
- Visual Studio Code with the Dev Containers extension

**Steps:**
1. Clone the repository
2. Open the folder in VS Code
3. When prompted, click "Reopen in Container" (or use Command Palette: `Dev Containers: Reopen in Container`)
4. VS Code will build and start the dev container with all dependencies pre-installed
5. Run `dotnet ef database update` and `dotnet run` inside the container terminal

### 3. GitHub Codespaces

Develop entirely in the cloud with zero local setup. Codespaces provides a full VS Code environment in your browser.

**Prerequisites:**
- GitHub account with Codespaces access

**Steps:**
1. Navigate to the repository on GitHub
2. Click the green "Code" button
3. Select the "Codespaces" tab
4. Click "Create codespace on main"
5. Wait for the environment to initialize
6. Run `dotnet ef database update` and `dotnet run` in the integrated terminal

All three environments provide the same development experience with the .NET SDK, C# extension, and all necessary tools pre-configured.

---

## Database & Migrations

### Initial Setup

**Start SQL Server LocalDB:**
```bash
sqllocaldb start MSSQLLocalDB
```

**Connection String:**
- Development: `Server=(localdb)\MSSQLLocalDB;Database=ApplicationDB;Trusted_Connection=True;TrustServerCertificate=True;`
- Database name: `ApplicationDB`

### Apply existing migrations
```bash
dotnet ef database update
```

### Create a new migration

- If you modify models:
	- `dotnet ef migrations add <MigrationName>`
	- `dotnet ef database update`

- EF Core uses DesignTimeDbContextFactory (Data/DesignTimeDbContextFactory.cs)

### Seeding Sample Data

At startup, the app automatically runs `await AppDbContext.SeedAsync(db);` which seeds 50 sample products with random categories, prices, and inventory.

To reseed manually:
```bash
dotnet ef database drop -f
dotnet ef database update
dotnet run
```

---

## Running the Application

### Prerequisites

Ensure SQL Server LocalDB is running:
```bash
sqllocaldb start MSSQLLocalDB
```

### Monolith Only
Start the monolith application:
```bash
dotnet run --project RetailMonolith.csproj
```

Access the app at `http://localhost:5068` (configured in `Properties/launchSettings.json`).

### Checkout Microservice Only
Start the Checkout API:
```bash
dotnet run --project RetailMonolith.Checkout.Api
```

Access the API at `https://localhost:5101` or `http://localhost:5100`.

Test the health endpoint:
```bash
curl http://localhost:5100/health
```

### Both Services (Phase 3+)
After Phase 3 is complete, you'll need to run both services:
```bash
# Terminal 1: Start the Checkout API
dotnet run --project RetailMonolith.Checkout.Api

# Terminal 2: Start the Monolith
dotnet run --project RetailMonolith.csproj
```

**Note:** The monolith automatically creates the database and runs migrations on startup.

### Available Endpoints

**Monolith:** (Default: `http://localhost:5068`)
| Path               | Description                      |
| ------------------ | -------------------------------- |
| `/`                | Home Page                        |
| `/Products`        | Product catalogue                |
| `/Cart`            | Shopping cart                    |
| `/Checkout`        | Checkout page (proxies to API)   |
| `/Orders`          | Order history                    |
| `/api/checkout`    | Checkout API (proxies to API)    |
| `/api/orders/{id}` | Order details API                |
| `/health`          | Health check endpoint            |

**Checkout Microservice:**
| Path            | Description                    |
| --------------- | ------------------------------ |
| `/api/checkout` | Process checkout               |
| `/health`       | Health check endpoint          |
| `/swagger`      | OpenAPI documentation (dev)    |

---

## Testing

### Run All Tests
```bash
# Run all tests in the solution
dotnet test

# Run only Checkout API tests
dotnet test RetailMonolith.Checkout.Tests

# Run only Monolith tests
dotnet test RetailMonolith.Tests

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage
- **Checkout API:** 10 tests (9 unit + 1 integration)
  - Happy paths: valid cart, multiple items
  - Validation failures: empty cart, invalid payment token
  - Business rules: insufficient stock
  - External failures: payment gateway, database errors

- **Monolith:** 8 tests (7 proxy unit + 1 E2E integration)
  - Proxy tests: All HTTP response scenarios (200, 400, 500, 503, timeout)
  - E2E test: Full checkout flow using WebApplicationFactory

**Total:** 18 tests passing

---

## Environment Variables (optional)
You can override the default connection string by setting the `ConnectionStrings__DefaultConnection` environment variable.
| Variable                               | Description                | Default          |
| -------------------------------------- | -------------------------- | ---------------- |
| `ConnectionStrings__DefaultConnection` | Database connection string | LocalDB instance |
| `ASPNETCORE_ENVIRONMENT`               | Environment mode           | `Development`    |
