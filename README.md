<!--
  Auto-generated README based on the repository source code.
  Content is derived from the code in this solution and is safe to publish.
-->

# Uniia.TGBot 🚀

![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![License](https://img.shields.io/badge/license-UNSPECIFIED-lightgrey)

Uniia.TGBot is a production-oriented, layered .NET 10 application that receives Telegram webhook updates, validates them, and processes user-related actions while using Google Sheets as the primary data store and Google Drive for file management.

Key behaviors (from the codebase):
- Receives Telegram webhook updates at a dedicated endpoint and validates them using a secret token header.
- Queues incoming updates in an in-memory Channel and processes them in a BackgroundService (UpdateProcessingBackgroundService).
- Dispatches updates to a set of strategy-based handlers using the Strategy pattern (admin commands, event creation, group moderation, role-based welcome flows).
- Persists and queries user data in Google Sheets via a repository abstraction and reflection-based attribute mapping.
- Manages file uploads to Google Drive with resilient error handling.
- Syncs calendar events with Google Calendar API for persistent event management.
- Exposes health checks for external integrations (Telegram, Google Sheets, Google Calendar, Google Drive).
- Provides Swagger UI in development mode for API exploration.

---

## About the project 🧩

Uniia.TGBot implements a comprehensive bot middleware for organizational workflows with personal and group chat management:

**Core Features:**
- **Webhook receiver**: A Web API controller accepts Telegram webhook POSTs at `api/webhook` and validates the `X-Telegram-Bot-Api-Secret-Token` header via TelegramSecretTokenFilter middleware.
- **Queue + background processing**: Received updates are written to a `Channel<Update>` and processed by `UpdateProcessingBackgroundService`. Each update is handled in a DI scope and routed by `TelegramUpdateDispatcher` to the first matching strategy handler.
- **Command strategies**: 
  - `/create_event` — `CreateEventCommandStrategy` with multi-stage event creation (name, description, date, assignees, notifications) and Google Calendar sync
  - `/everyone` — `EveryoneCommandStrategy` for group-wide notifications with @-mentions
- **Personal welcome flows**: Role-based welcome strategies (`AdminWelcomeStrategy`, `StandardUserWelcomeStrategy`) handle `/start` command initialization
- **Moderation & access control**: `DeleteFromGeneralStrategy` automatically removes non-admin messages from group general topics based on Google Sheets role data.
- **Event management**: 
  - Calendar event and task models (`CalendarEvent`, `CalendarTask`) stored in Google Sheets
  - `EventReminderService` for scheduled notifications and reminder delivery via `ReminderDistributionBackgroundService`
  - Two-way sync with Google Calendar API for event persistence
- **File storage & management**:
  - `GoogleDriveStorageService` handles file uploads to Google Drive with support for both Service Account and OAuth 2.0 authentication
  - `GoogleDriveClient` abstracts Drive API interactions
  - File upload results tracked via `DriveUploadResult` DTO
- **Data storage**: 
  - Primary: Google Sheets via generic `GoogleSheetsRepository<T>` with reflection-based attribute mapping (`SheetColumn`)
  - User repository wrapper for identity and role management
  - Calendar event/task repositories for event persistence
  - Stage repository for tracking multi-step command states
  - Google Sheets schema mapping via custom attributes
- **Webhook lifecycle**: `WebhookInitializer` hosted service registers bot webhook on startup and removes it on shutdown.
- **Command management**: `TelegramCommandsInitializer` and `TelegramCommandsGranter` register and manage available bot commands (admin, user, group-level)
- **File service**: `TelegramFileService` handles Telegram file operations (downloads, metadata retrieval)
- **Observability & resilience**: Health checks for Telegram, Google Sheets, Google Calendar, and Google Drive; Serilog for structured logging; Polly-based resilience pipelines with retries around external API calls.
- **Localization**: Full i18n support via `IStringLocalizer` and RESX resources (English `Messages.en.resx`, Ukrainian `Messages.uk.resx`) for multi-language bot messages.
- **Global exception handling**: `GlobalExceptionHandler` middleware provides centralized error handling with problem details response format.
- **Health check reporting**: Custom health check endpoint with formatted JSON responses via `HealthCheckResponseWriter`.

---

## Architecture & Tech stack 🏗️

- Language / Platform: .NET 10 (TargetFramework: net10.0)
- Architectural style: Layered / Clean-ish separation with DI-based composition
  - Projects: 
    - **Api** (presentation layer) — Web API controllers, middleware, health checks, global exception handling
    - **Application** (services & business logic) — Command strategies, update dispatchers, event reminders, user services, file operations
    - **Infrastructure** (data access, external integrations) — Repositories, Google API clients, background services, health checks
    - **Domain** (models & interfaces) — Entities, repository contracts, mapper interfaces, attribute definitions
    - **Shared** (constants, DTOs, resources) — Application constants, request/response DTOs, localization RESX files, custom exceptions
    - **Tests** (unit & integration tests) — xUnit tests with fixtures, mocks, factories for full coverage

- Key NuGet packages (from project files):
  - **Telegram.Bot** 22.10.1 (Telegram API integration)
  - **Google.Apis.Sheets.v4** 1.74.0.4061 (Google Sheets API)
  - **Google.Apis.Calendar.v3** 1.75.0.4182 (Google Calendar API)
  - **Google.Apis.Drive.v3** 1.74.0.4135 (Google Drive API)
  - **Polly** 8.7.0 + **Polly.Extensions** 8.7.0 (resilience & retry policies)
  - **Microsoft.Extensions.Http.Resilience** 10.7.0 (HTTP client resilience)
  - **Serilog** 4.3.1, **Serilog.AspNetCore** 10.0.0, **Serilog.Sinks.File** 7.0.0 (structured logging)
  - **AutoMapper** 16.1.1 (object mapping)
  - **Microsoft.AspNetCore.OpenApi** 10.0.9, **Swashbuckle.AspNetCore** 10.2.1 (Swagger/OpenAPI)
  - **Microsoft.Extensions.*** (Options, Localization, Hosting abstractions, HealthChecks)
  - **xUnit** 2.9.3, **NSubstitute** 5.3.0, **Microsoft.AspNetCore.Mvc.Testing** 10.0.9 (test frameworks)

- Design patterns & notable practices:
  - **Strategy pattern** for update handling via `ITelegramUpdateStrategy` implementations
  - **Repository pattern** for data access with `IGoogleSheetsRepository<T>`, `IUserRepository`, `ICalendarEventRepository`, etc.
  - **Background queue processing** via `Channel<T>` + `BackgroundService` for async update handling
  - **Options pattern** for configuration binding (`IOptions<GoogleSheetsOptions>`, `IOptions<GoogleDriveOptions>`, etc.)
  - **Localization** via `IStringLocalizer` and RESX resources (multi-language support)
  - **Health checks** for external dependencies (Telegram, Google Sheets, Google Calendar, Google Drive)
  - **Resilience pipelines** via Polly for retries and fault tolerance on external API calls

---

## Prerequisites ✅

- .NET 10 SDK (dotnet)
- A Google Service Account with access to target Google Spreadsheet (credentials JSON)
- Telegram Bot token and a secret token used for webhook validation
- (Optional) Docker / docker-compose to run containerized builds (project contains Dockerfile)

---

## Configuration 🔐

The application reads configuration from appsettings.json and environment variables. Do NOT store secrets in source-controlled files. Below are the configuration keys and shapes inferred from the codebase (structure only — never include secrets here).

Required top-level keys (representative):

- **Telegram** (object)
  - BotToken (string) — required to instantiate Telegram client
  - SecretToken (string) — expected value for `X-Telegram-Bot-Api-Secret-Token` header in webhook requests

- **BaseUrl** (string) — used by webhook registrar to construct full webhook URL (e.g., https://example.com)

- **GoogleSheets** (object) → bound to `GoogleSheetsOptions`
  - SpreadsheetId (string) — target spreadsheet ID
  - CredentialsJson (object) — service account credentials (JSON key file contents)
  - Mappings (object)
	- User (object)
	  - SheetName (string) — e.g., "Users"
	  - Range (string) — A1 range like "A2:Z"

- **GoogleCalendar** (object) → bound to `GoogleCalendarOptions`
  - CredentialsJson (object) — service account credentials for Calendar API

- **GoogleDrive** (object) → bound to `GoogleDriveOptions`
  - UseServiceAccount (boolean) — set to `true` for Service Account (Production), `false` for OAuth 2.0 (Development)
  - CredentialsJson (object) — service account credentials or OAuth client ID/secret

Example shape (non-secret placeholder):

```json
{
  "Telegram": {
	"BotToken": "<BOT_TOKEN>",
	"SecretToken": "<SECRET_TOKEN>"
  },
  "BaseUrl": "https://your-host.example.com",
  "GoogleSheets": {
	"SpreadsheetId": "<SPREADSHEET_ID>",
	"CredentialsJson": {
	  "type": "service_account",
	  "project_id": "...",
	  "private_key_id": "...",
	  "private_key": "...",
	  "client_email": "..."
	},
	"Mappings": {
	  "User": {
		"SheetName": "Users",
		"Range": "A2:Z"
	  }
	}
  },
  "GoogleCalendar": {
	"CredentialsJson": {
	  "type": "service_account",
	  "project_id": "...",
	  "private_key": "..."
	}
  },
  "GoogleDrive": {
	"UseServiceAccount": true,
	"CredentialsJson": {
	  "type": "service_account",
	  "project_id": "...",
	  "private_key": "..."
	}
  }
}
```

Important notes:
- Provide BotToken and Google credentials via environment variables or a secret manager in CI/CD.
- Telegram secret token is validated by `TelegramSecretTokenFilter` on incoming webhook requests.
- Google Drive can use either a Service Account (for Shared Drives in production) or OAuth 2.0 (for personal drives in development).
- Localization is configured via the application and supports multiple languages (English, Ukrainian).

---

## Getting started (local development) 🛠️

### Prerequisites
- .NET 10 SDK
- A Google Service Account (with keys downloaded as JSON)
- Telegram Bot token
- A target Google Spreadsheet (for user data)
- (Optional) Google Drive folder ID and Google Calendar for event management

### Clone, restore, build, and run

```powershell
git clone https://github.com/uniia-com-ua/uniia_tg_bot.git
cd uniia_tg_bot
dotnet restore
dotnet build

# Start the web API (reads configuration from appsettings.json or environment variables)
dotnet run --project src/Uniia.TGBot.Api
```

In Development environment, Swagger UI is registered and available at `https://localhost:5001/swagger` (or configured port).

### Configuration for local development

Create or update `src/Uniia.TGBot.Api/appsettings.Development.json` with your local credentials:

```json
{
  "Telegram": {
    "BotToken": "<YOUR_BOT_TOKEN>",
    "SecretToken": "<YOUR_SECRET_TOKEN>"
  },
  "BaseUrl": "https://localhost:5001",
  "GoogleSheets": {
    "SpreadsheetId": "<YOUR_SPREADSHEET_ID>",
    "CredentialsJson": {
      // Paste contents of your Google Service Account JSON key
    },
    "Mappings": {
      "User": {
        "SheetName": "Users",
        "Range": "A2:Z"
      }
    }
  },
  "GoogleCalendar": {
    "CredentialsJson": {
      // Paste contents of your Google Calendar Service Account key
    }
  },
  "GoogleDrive": {
    "UseServiceAccount": false,
    "CredentialsJson": {
      // For local dev, use OAuth 2.0 or Service Account key
    }
  }
}
```

### Running with Docker

The repository contains a Dockerfile for containerized builds. Build and run:

```powershell
docker build -t uniia-tgbot:latest .
docker run -e Telegram__BotToken=<BOT_TOKEN> -e Telegram__SecretToken=<SECRET> -p 5001:8080 uniia-tgbot:latest
```

Or use docker-compose (ensure environment variables are set):

```powershell
docker-compose up -d
```

If you use Docker, the repository contains a Dockerfile and docker-compose snippet in the repo root — configure environment variables or mount a production appsettings file (the repository's CI writes appsettings.Production.json in the workflow).

---

## Project structure (visual) 📁

Root
```
├─ src/
│  ├─ Uniia.TGBot.Api/                 # Presentation: Web API, controllers, middleware, health checks, Swagger, global exception handling
│  ├─ Uniia.TGBot.Services/            # Application: business logic, command strategies, dispatchers, services, event reminders
│  ├─ Uniia.TGBot.Infrastructure/      # Infrastructure: Google APIs (Sheets, Calendar, Drive), repositories, mappers, background services, health checks
│  ├─ Uniia.TGBot.Domain/              # Domain: entities, models, attributes, repository interfaces, mapper interfaces
│  └─ Uniia.TGBot.Shared/              # Shared: constants, DTOs, resources (RESX), custom exceptions, enums
└─ src/Uniia.TGBot.Tests/              # Unit & integration tests (xUnit, NSubstitute, Mvc.Testing, fixtures, factories)
```

Each project follows a single responsibility principle:
- **Api**: HTTP surface, route handlers (WebhookController), middleware (GlobalExceptionHandler, TelegramSecretTokenFilter), health check endpoints, Swagger/OpenAPI
- **Application**: Command strategies (CreateEventCommandStrategy, DeleteFromGeneralStrategy, EveryoneCommandStrategy, StartCommandStrategy, WelcomeStrategies), TelegramUpdateDispatcher, event reminders, user services, file services, role-based welcome flows
- **Infrastructure**: Repository implementations (GoogleSheetsRepository, UserRepository, CalendarEventRepository, StageRepository), Google API clients (Sheets, Calendar, Drive), data mappers (AttributeSheetMapper, AutoMapper profiles), background services (UpdateProcessingBackgroundService, ReminderDistributionBackgroundService, WebhookInitializer), health checks (TelegramHealthCheck, GoogleSheetsHealthCheck, GoogleCalendarHealthCheck, GoogleDriveHealthCheck)
- **Domain**: Core entities (User, CalendarEvent, CalendarTask, Stage), BaseEntity, SheetColumnAttribute, repository interfaces, mapper interfaces
- **Shared**: Application constants (TelegramRoutes, TelegramBotCommands, HealthCheckNames, GoogleConsts, etc.), DTOs (UserDto, EventReminderDto, DriveUploadResult), localization resources (Messages.en.resx, Messages.uk.resx), custom exceptions (FileUploadException), enums (EventCreationStage)
- **Tests**: Comprehensive test coverage with unit and integration tests, test fixtures (ControllerWebAppFactory), substitute providers, and factory patterns

---

## Usage examples ✨

**Webhook endpoint**
- POST updates to: `https://<your-host>/api/webhook`
- Required header: `X-Telegram-Bot-Api-Secret-Token: <SecretToken>`
- Request body: Telegram Update JSON payload

**Bot commands (registered via TelegramCommandsInitializer):**
- **Admin commands:**
  - `/create_event` — handled by `CreateEventCommandStrategy`: multi-stage interactive event creation flow (name → description → day → assignees → notification settings) that persists events to Google Sheets and Google Calendar; supports interactive state management via `SetEventNameStrategy`, `SetEventDescriptionStrategy`, `SetEventDayStrategy`, `SetEventAssigneesStrategy`, `SetEventNotificationStrategy`

- **Group commands:**
  - `/everyone` — handled by `EveryoneCommandStrategy`: group notification command that mentions all users in a coordinated manner

- **User initialization:**
  - `/start` — handled by `StartCommandStrategy`: validates that the Telegram username exists in the Google Sheet, syncs Telegram ID, and sends a localized welcome message using role-based welcome strategies (`AdminWelcomeStrategy` for admins, `StandardUserWelcomeStrategy` for standard users)

**Moderation behavior**
- Messages posted in a group's general topic are evaluated by `DeleteFromGeneralStrategy`. If the sender is not an admin (based on Google Sheets role data), the message is deleted and a temporary warning message is posted (auto-removed after ~5 seconds).
- Strategy is invoked for every new message update in group chats.

**Event reminders & notifications**
- `EventReminderService` runs in the background via `ReminderDistributionBackgroundService` and checks for upcoming events from Google Sheets at scheduled intervals.
- When an event is due or about to occur, `EventReminderService` sends notifications to registered attendees via Telegram.
- Event data syncs bi-directionally with Google Calendar API via `GoogleCalendarRepository` and `GoogleCalendarPingService`.

**File management**
- `TelegramFileService` handles file downloads from Telegram (photos, documents, etc.).
- `GoogleDriveStorageService` handles file uploads to Google Drive with support for both Service Account (production) and OAuth 2.0 (development) authentication.
- `GoogleDriveClient` abstracts the Google Drive API interactions.

**Developer tips**
- **Health checks**: Exposed at `/health` endpoint with formatted JSON responses via `HealthCheckResponseWriter`. Health checks include Telegram, Google Sheets, Google Calendar, and Google Drive integrations.
- **Localization**: Strings are kept in shared RESX resources (English `Messages.en.resx`, Ukrainian `Messages.uk.resx`) and injected via `IStringLocalizer<T>`. Culture-aware message formatting via `LocalizationExtensions`.
- **Command strategies**: Implement `ITelegramUpdateStrategy` and are registered in the DI container for dynamic dispatch via `TelegramUpdateDispatcher`.
- **Staged commands**: Multi-step commands implement `IStagedCommandStrategy` and use `StateStrategyResolver` to resolve the next strategy based on user state.
- **Attribute-based mapping**: `AttributeSheetMapper` uses reflection over `SheetColumn` attributes to automatically map between C# models and Google Sheet columns.
- **Resilience**: External API calls are wrapped with Polly retry policies (configured via `Microsoft.Extensions.Http.Resilience`).
- **Background processing**: The `Channel<Update>` + `BackgroundService` pattern ensures non-blocking webhook processing with graceful shutdown.

---

## Tests & CI 🔬

**Test framework & structure:**
- xUnit 2.9.3 + NSubstitute 5.3.0 for unit and integration tests
- Microsoft.AspNetCore.Mvc.Testing for integration tests with test server
- Test fixtures (ControllerWebAppFactory, SubstituteProvider) for dependency setup
- Comprehensive coverage across Infrastructure, Application, Middleware, Services, and Extensions layers

**Test organization:**
- Unit tests in `src/Uniia.TGBot.Tests/UnitTests/` for isolated component testing
- Integration tests in `src/Uniia.TGBot.Tests/IntegrationTests/` for end-to-end workflows
- Fixture-based test setup for shared test dependencies

**Running tests locally:**

```powershell
# Run all tests
dotnet test

# Run tests from a specific project
dotnet test src/Uniia.TGBot.Tests/Uniia.TGBot.Tests.csproj

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**CI/CD:**
- Automated workflows in `.github/workflows/` (deployment job writes appsettings.Production.json)
- Tests are run as part of CI validation before deployment
- Environment variables provide secrets (BotToken, GoogleCredentials) during CI runs

---

## Troubleshooting 🔧

**"Invalid webhook token"**
- Verify `X-Telegram-Bot-Api-Secret-Token` header matches `Telegram:SecretToken` in configuration
- Check that the header is being sent by your Telegram bot endpoint

**"Google Sheets API error 404"**
- Verify `SpreadsheetId` is correct and the service account has read/write access
- Ensure the Google Service Account email is added as an editor to the target spreadsheet

**"Google Drive upload fails"**
- If using Service Account, verify the service account has access to the target folder/shared drive
- If using OAuth 2.0, ensure the OAuth token is valid and refresh token hasn't expired
- Check `GoogleDrive:UseServiceAccount` configuration matches your auth method

**"Health check fails"**
- Navigate to `https://localhost:5001/health` (or configured port) to see individual health check statuses
- Check credentials and API access for each failing service
- Verify Serilog logs in `src/Uniia.TGBot.Api/Logs/` for detailed error messages

**"Locale not found"**
- Ensure RESX files (Messages.en.resx, Messages.uk.resx) are embedded as resources in the Shared project
- Verify your bot sends a valid `language_code` in Telegram user object

---

## Security & Hardening recommendations 🔒

- **Secrets management**: Never commit BotToken, Google credentials, or API keys to git. Use environment variables, Azure Key Vault, or a secrets manager in CI/CD.
- **Service Account permissions**: Limit Google Service Account permissions to the minimum required (read/write for specific sheets, upload to specific folders).
- **Network security**: Use HTTPS only; configure webhook URL with a valid SSL certificate.
- **Rate limiting**: Implement rate limiting on the webhook endpoint to prevent DDoS attacks.
- **Input validation**: The webhook payload is validated by `TelegramSecretTokenFilter` before processing.
- **Background job safety**: Use cancellation tokens (CancellationToken) in background services for graceful shutdown.
- **Test coverage**: Add more unit/integration tests around repository mapping and error handling (especially for edge cases in multi-stage commands).
- **Performance optimization**: Consider caching Google Sheet queries or implementing an in-memory index to avoid repeated full-sheet scans for lookups.
- **Audit logging**: Serilog is configured for structured logging; ensure logs are sent to a secure location (not local logs in production).

---

## Contributing & Future work

This project is actively maintained. Areas for enhancement:
- Webhook signature verification using Telegram's X-Telegram-Bot-Api-Secret-Token (currently implemented)
- Caching layer for Google Sheets queries (planned)
- Rate limiting and quota management for Google APIs (planned)
- Additional command strategies for group management (in progress)
- Metrics and performance monitoring (planned)

---

## License

Check the repository root for a LICENSE file. No license file was detected in code analysis — add or confirm license before reusing code.

