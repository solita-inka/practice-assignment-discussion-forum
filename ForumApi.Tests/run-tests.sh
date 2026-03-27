#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# 1. Start SQL Server container (remove stale container if exists)
docker rm -f forum-sql &>/dev/null || true
echo "Starting SQL Server container..."
docker compose -f "$SCRIPT_DIR/docker-compose.yaml" up -d

# 2. Wait for SQL Server to be ready
echo "Waiting for SQL Server to be ready..."
for i in $(seq 1 30); do
  if docker exec forum-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P 'Forum_Password123!' -Q "SELECT 1" &>/dev/null 2>&1; then
    echo "SQL Server is ready."
    break
  fi
  if [ "$i" -eq 30 ]; then
    echo "Warning: SQL Server readiness check timed out, proceeding anyway..."
  else
    echo "  Attempt $i/30..."
    sleep 2
  fi
done

# 3. Run tests
echo "Running tests..."
dotnet test "$SCRIPT_DIR/ForumApi.Tests.csproj" --verbosity normal

# 4. Cleanup
echo "Stopping SQL Server container..."
docker compose -f "$SCRIPT_DIR/docker-compose.yaml" down
