# Paeezan Server — Server-Authoritative Version

### Highlights
- Server-authoritative game simulation (server runs ticks, resolves combat, tower damage, ends matches).
- JWT authentication for clients; SignalR hub accepts token via `access_token` for WebSocket.
- Swagger (Swashbuckle) for API exploration; health endpoints for runtime check.
- MongoDB persistence for users and match results.
- Dockerfile + docker-compose (Mongo + server) for easy local runs.
- Simple seeding of an `admin` user on startup for quick testing.

### Run (quick)
1. Edit `appsettings.json` -> replace `Jwt:Key` with a secure random secret.
2. From `/server` run: `docker-compose up --build`
3. Swagger UI: `http://localhost:5000/swagger` (after server starts).
4. SignalR Hub: `http://localhost:5000/hubs/game` (connect with `access_token` query containing a JWT).

### Design notes (short)
- RoomService holds rooms and starts `GameSession`s when a second player joins.
- `GameSession` ticks every configured ms and emits snapshots to players via SignalR.
- Anti-cheat: server validates all DeployUnit requests; clients only send intents. Server enforces cooldown & validity; all simulation runs server-side.
- Match results persisted into `matches` collection; winner wins increment persisted.
