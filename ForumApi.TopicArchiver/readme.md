# ForumApi.TopicArchiver

Azure Functions project containing scheduled maintenance tasks for the Forum API.

## TopicArchiveTimer

Automatically archives inactive forum topics on a daily schedule.

- **Trigger:** Timer (`0 0 0 * * *`) — runs daily at midnight UTC
- **Behavior:** Finds all non-archived topics where the most recent message (or topic creation date, if no messages exist) is older than the configured cutoff, and marks them as archived.
- **Default cutoff:** 30 days of inactivity

### Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `SqlConnectionString` | SQL Server connection string (shared database with ForumApi) | — |
| `TopicArchiveTimeRangeInDays` | Number of days of inactivity before a topic is archived | `30` |

These are set in `local.settings.json` for local development and as application settings in Azure.

## Running locally

1. Start the SQL Server container via `docker-compose up -d`
2. Build the project: `dotnet build`
3. Start the Functions host: `func start` from the build output directory (`bin/Debug/net10.0`)