# Build a Cross-Platform Desktop Application

I want you to build a production-ready desktop application using the following architecture:

```text
                    Lujke
                      │
             ┌────────┴────────┐
             │                 │
          Tauri 2             Vue 3
             │                 │
             └───────┬─────────┘
                     │ HTTP
                     ▼
              ASP.NET Core
                     │
                     ▼
                  SQLite
```

## Application Description

**Lujke** is a cross-platform desktop trading terminal that combines real-time market data with **AI price forecasting** to help traders anticipate what comes next and act on it. It is built as a Tauri 2 desktop shell wrapping a Vue 3 frontend, backed by a local ASP.NET Core API and SQLite (see the architecture above). The UI is captured in `UIdraft.html`.

### Core Concept

The defining feature is the **chart panel**, where the AI forecast reads as *what comes next* rather than an overlay:

- The **actual market** (candlesticks) fills the left portion of the chart and is **horizontally scrollable**, so the user can scroll back through the history.
- The **AI forecast** starts at the **last actual candle** and projects the next candles forward into the right portion of the chart (a shaded "forecast zone").
- The view **auto-scrolls to the end** by default (showing the latest market + forecast) and re-pins to the end on resize when the user is already at the latest point.
- A timeframe selector switches the resolution: **1m, 15m, 1D, 1W, 1M, 1Y**.

### Desktop Layout (Main Trading Screen)

| Area | Contents |
| --- | --- |
| **Top navigation bar** | Brand (*Lujke*), nav links (*Predict*, *Markets*), a live **ticker strip** (BTC, ETH, AAPL, EURUSD with % change), an **account summary** (Equity, Balance, P&L, Margin), and a **dark/light theme toggle**. |
| **Chart panel** (center) | Symbol header (*AAPL / USD*) + *Market · AI Forecast · 1D*; a prediction summary (**Actual**, **Predicted**, **Accuracy %**); a legend (Actual market / AI forecast); the candlestick + forecast chart; and the timeframe selector. |
| **Order panel** | Buy/Sell toggle, order type (*Limit*), Price / Quantity / Amount inputs, and a submit button (*Buy AAPL*). |
| **Watchlist** | Live list of symbols with price and % change (AAPL, TSLA, NVDA, BTC, EURUSD). |
| **Positions** | Open positions with size and P&L (up/down). |
| **Order book** | Bid/ask ladder around a mid price. |
| **Account stats** | Equity, Balance, P&L (day), Margin. |

### Visual Style

- A minimalist aesthetic (the draft is titled *"Minimalism"*): a warm neutral palette, thin typography, a warm brown accent color, and monochrome candlesticks with an accent-colored dashed forecast line.
- Full **light/dark theme** support (persisted via `localStorage`).

### Marketing / Landing Sections

The draft also contains landing-page content (hero *"Less Complexity, More Focus"*, a stats strip, and a timeline) intended for the app's landing/about screens, kept separate from the trading terminal itself.

### How It Maps to the Stack

- **Vue 3** renders the UI (chart, order panel, watchlist, positions, order book, account stats) and holds client state (Pinia).
- **ASP.NET Core** serves market data, order submission, positions, and the **AI prediction** (the forecast series shown after the last actual candle).
- **SQLite** stores the local, persistent data (watchlist, positions, orders, settings).

## Technology Stack

### Desktop

* Tauri 2
* Rust
* Cross-platform support:

  * Windows
  * macOS Apple Silicon
  * macOS Intel
  * Linux

### Frontend

* Vue 3
* TypeScript
* Vite
* Pinia
* Vue Router
* Use a modern UI approach such as Element Plus or Tailwind CSS

### Backend

* ASP.NET Core
* C#
* .NET 8+ / current stable LTS version available in the environment
* REST API
* Entity Framework Core
* SQLite

---

# 1. Project Structure

Create a clean monorepo structure similar to:

```text
Lujke/
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── views/
│   │   ├── stores/
│   │   ├── services/
│   │   ├── types/
│   │   ├── router/
│   │   ├── App.vue
│   │   └── main.ts
│   ├── package.json
│   ├── vite.config.ts
│   └── tsconfig.json
│
├── backend/
│   ├── Controllers/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   ├── Models/
│   ├── Services/
│   ├── DTOs/
│   ├── Program.cs
│   └── Lujke.Backend.csproj
│
├── src-tauri/
│   ├── src/
│   │   └── main.rs
│   ├── binaries/
│   ├── capabilities/
│   ├── tauri.conf.json
│   └── Cargo.toml
│
├── README.md
└── .gitignore
```

Adjust the structure if there is a better Tauri 2 convention, but keep frontend, backend, and desktop/native concerns clearly separated.

---

# 2. Frontend

Create a Vue 3 + TypeScript + Vite application.

Requirements:

* Vue 3 Composition API
* TypeScript
* Pinia
* Vue Router
* Modern responsive UI
* Clean component architecture
* API service layer instead of putting fetch calls directly into components

Create an API client such as:

```text
frontend/src/services/api/
```

The frontend must be able to communicate with the locally running ASP.NET Core backend.

Do not hardcode a fixed port such as:

```text
http://localhost:5000
```

The backend port should be dynamically determined.

---

# 3. ASP.NET Core Backend

Create a normal ASP.NET Core Web API.

Use:

* Controllers or minimal APIs, whichever is cleaner
* Dependency injection
* Entity Framework Core
* SQLite
* DTOs
* Services for business logic
* Proper configuration
* Logging
* Health endpoint

Create:

```text
GET /api/health
```

which returns something similar to:

```json
{
  "status": "ok"
}
```

Also create a simple example CRUD resource, such as `User`, to prove that:

```text
Vue → ASP.NET → EF Core → SQLite
```

works correctly.

---

# 4. SQLite

Use Entity Framework Core with SQLite.

Do NOT store the SQLite database next to the application executable.

The database must be stored in the appropriate per-user application-data directory.

Examples:

Windows:

```text
%LOCALAPPDATA%\Lujke\app.db
```

macOS:

```text
~/Library/Application Support/Lujke/app.db
```

Linux:

```text
~/.local/share/Lujke/app.db
```

Use Tauri's application-data directory facilities or another clean cross-platform mechanism to determine the correct location.

The backend must receive/use the correct database path dynamically.

Do not hardcode OS-specific paths.

---

# 5. Database Migration

Set up EF Core migrations.

The application should be capable of creating/updating the SQLite database automatically when it starts.

For example:

```csharp
await db.Database.MigrateAsync();
```

Make this safe for production use.

Document how developers can create new migrations.

---

# 6. Tauri + ASP.NET Integration

This is the most important part.

The ASP.NET Core backend must be bundled with the Tauri application.

Use Tauri 2's sidecar/external-binary mechanism.

The final desktop application should behave as a single application from the user's perspective.

Expected startup:

```text
User launches Lujke
        ↓
Tauri starts
        ↓
Tauri starts ASP.NET Core backend
        ↓
ASP.NET binds to localhost
        ↓
Vue frontend connects to backend
        ↓
Application is ready
```

The user should NOT have to:

* install .NET separately
* manually start ASP.NET
* run a terminal command
* configure a port
* install Node.js
* install Rust

Everything required for the application runtime should be bundled appropriately.

---

# 7. Backend Port

Do not assume port `5000`, `5001`, `3000`, etc.

Use a dynamic localhost port.

The application should:

1. Find/select an available localhost port.
2. Start ASP.NET Core on that port.
3. Make the port available to the Vue frontend.
4. Wait until the backend is actually ready.
5. Only then consider the desktop application ready.

Prefer a robust mechanism over arbitrary sleeps such as:

```text
sleep(3000)
```

Implement an actual readiness check against:

```text
/api/health
```

---

# 8. Backend Lifecycle

Tauri should own the backend process lifecycle.

When the desktop application starts:

```text
start ASP.NET process
```

When the desktop application exits:

```text
terminate ASP.NET process
```

Handle abnormal shutdowns gracefully.

Avoid leaving orphaned ASP.NET processes running after the application closes.

---

# 9. Security

The ASP.NET backend is intended to be local to the desktop application.

Bind the backend to:

```text
127.0.0.1
```

rather than exposing it to the entire network.

Do not expose the local backend publicly unless explicitly required.

If appropriate, add a simple local authentication mechanism or randomly generated startup token so another local process cannot freely call the API.

Do not use insecure CORS configuration such as:

```text
AllowAnyOrigin
```

unless it is genuinely required.

Configure CORS specifically for the Tauri frontend.

---

# 10. Development Mode

Development should be convenient.

I want to be able to run something conceptually like:

```bash
npm run tauri dev
```

and have:

```text
Vue development server
        +
Tauri
        +
ASP.NET Core backend
```

start together.

The developer should not need to manually start three terminals every time.

If Tauri's configuration makes this difficult, create appropriate scripts to automate the process.

---

# 11. Production Build

Create a production build process that:

1. Builds the Vue frontend.
2. Publishes the ASP.NET Core backend.
3. Places the correct backend binary into the Tauri sidecar location.
4. Builds the Tauri application.
5. Produces the appropriate installer/package for the target OS.

Support:

```text
Windows x64
macOS Apple Silicon
macOS Intel
Linux x64
Linux ARM64 where practical
```

Use framework-independent/self-contained .NET publishing where appropriate so the end user does not need to install .NET separately.

---

# 12. Tauri Sidecar Naming

Handle Tauri's platform-specific binary naming requirements correctly.

Do not simply assume the same executable filename works on every platform.

Configure:

```text
Windows → .exe
macOS → native executable
Linux → native executable
```

and use Tauri's recommended sidecar configuration.

---

# 13. Example Application

Create a minimal but functional example application.

Frontend:

```text
Dashboard
```

with:

* Application title
* Backend connection status
* SQLite database status
* Example Users list
* Add User
* Delete User
* Refresh

Backend:

```text
GET    /api/health
GET    /api/users
POST   /api/users
DELETE /api/users/{id}
```

SQLite:

```text
Users
-----
Id
Name
CreatedAt
```

This example exists primarily to prove that the entire stack works.

---

# 14. Error Handling

Implement sensible error handling.

The frontend should display a useful message if:

```text
ASP.NET backend fails to start
backend becomes unavailable
SQLite cannot be opened
API request fails
database migration fails
```

Do not silently swallow errors.

The backend should log useful diagnostic information.

Tauri should also log startup/shutdown problems.

---

# 15. Configuration

Separate development and production configuration.

Do not hardcode:

* database paths
* backend ports
* machine-specific paths
* developer usernames
* secrets

Use environment/configuration where appropriate.

---

# 16. README

Create a detailed `README.md`.

It should explain:

### Requirements

What is needed for development:

```text
Node.js
npm
Rust
Tauri prerequisites
.NET SDK
```

### Development

Explain exactly how to start the application.

Example:

```bash
npm install
npm run tauri dev
```

### Backend

Explain how the ASP.NET backend works and how to run it independently for debugging.

### Database

Explain:

* SQLite location
* migrations
* how to create a migration
* how migrations are applied

### Production Build

Explain how to build:

```text
Windows
macOS
Linux
```

### Architecture

Include an architecture diagram explaining:

```text
Tauri
 ├── Vue
 └── ASP.NET Core
          └── SQLite
```

---

# 17. Important Development Rules

Do not over-engineer the initial implementation.

First make the complete flow work:

```text
Vue
 ↓
Tauri
 ↓
ASP.NET Core
 ↓
EF Core
 ↓
SQLite
```

Then improve it.

Prefer simple, maintainable code.

Use current stable versions of the technologies unless there is a compatibility reason not to.

Before adding a dependency, consider whether it is actually necessary.

Do not introduce Docker because the goal is a self-contained desktop application.

Do not require the user to install a separate database server.

Do not require the user to manually run the backend.

Do not duplicate business logic between Vue, Rust, and C#.

The responsibilities should remain:

```text
Vue
→ UI / presentation / client state

Tauri
→ Desktop lifecycle / native OS integration / process management

ASP.NET Core
→ API / business logic / validation / database access

SQLite
→ Local persistent data
```

---

# 18. Execution Strategy

Do not just provide instructions.

Actually create and configure the project.

Work incrementally:

1. Verify installed versions/tools.
2. Create the project structure.
3. Create Vue application.
4. Create ASP.NET Core backend.
5. Add EF Core + SQLite.
6. Implement health API.
7. Implement Users CRUD.
8. Verify Vue → ASP.NET → SQLite.
9. Add Tauri 2.
10. Configure ASP.NET as Tauri sidecar.
11. Implement dynamic backend port handling.
12. Implement backend readiness detection.
13. Implement backend shutdown handling.
14. Test development mode.
15. Test production build.
16. Fix platform-specific issues.
17. Update README.

After each major step, run the relevant build/test command and fix errors before continuing.

At the end, provide a concise summary of:

* Files created/changed
* Commands used
* How to run in development
* How to build production
* Any platform-specific limitations
* Any remaining TODOs

The final result should be a working foundation for a real cross-platform desktop application, not merely a proof-of-concept folder structure.
