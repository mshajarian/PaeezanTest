# Paeezan Server — Server-Authoritative Version

## Highlights
- Server-authoritative game simulation (server runs ticks, resolves combat, tower damage, and ends matches).
- JWT authentication for clients; the SignalR hub accepts a token via `access_token` for WebSocket connections.
- MongoDB persistence for users and match results.
- Dockerfile and docker-compose (Mongo + server) for easy local deployment.
- Swagger (Swashbuckle) for API exploration and health endpoints for runtime checks.

## Gameplay Notes
- The game supports **offline mode**.
- You can enable or disable offline mode via a checkbox in the `GameplayController` script.
- In offline mode, you can switch between users using the **F1** and **F2** keys.
- Game configuration files are located in the `server/Config` directory, and configuration data is also sent from the server to the client.

## Known Issues and Development Notes
- The codebase is not perfect and requires further optimization and improvements.
- There is a timing mismatch between the server and client tick speeds, which can cause gameplay lag.
  - This issue is known and can be resolved with synchronization adjustments.

## Quick Start
1. Edit `appsettings.json` and replace the value of `Jwt:Key` with a secure random secret.
2. From the `/server` directory, run:
   ```bash
   docker-compose up --build
After the server starts:

Swagger UI: http://localhost:5000/swagger

SignalR Hub: http://localhost:5000/hubs/game (connect using an access_token query parameter containing a valid JWT)

Design Notes
RoomService manages all active rooms and starts a GameSession when a second player joins.

GameSession runs ticks at the configured interval and sends state snapshots to clients via SignalR.

Anti-cheat: The server validates all DeployUnit requests. Clients send only intents, and the server enforces cooldowns and validity checks. All simulation runs server-side.

Match results are persisted in the matches collection, and the winner’s stats are updated accordingly.

Stack
.NET Core

SignalR

MongoDB

Docker / Docker Compose

Swagger (Swashbuckle)