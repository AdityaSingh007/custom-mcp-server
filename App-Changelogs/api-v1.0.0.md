# API Changelog - v1.0.0

## Release Summary
Initial public release of the API with core user management endpoints and a basic service health probe.

## Added
- Added `GET /health` for service availability checks.
- Added `GET /users` to retrieve a list of users.
- Added `POST /users` to create a new user.
- Added JSON request and response support for all public endpoints.

## Endpoint Notes
### `GET /health`
- Returns `200 OK` when the service is reachable.
- Intended for monitoring, uptime checks, and container readiness probes.

### `GET /users`
- Returns a flat collection of user records.
- Response includes `id`, `name`, `email`, and `createdAt`.

### `POST /users`
- Accepts `name` and `email` in the request body.
- Returns `201 Created` with the newly created user object.

## Example Request
```http
POST /users
Content-Type: application/json

{
  "name": "Ava Patel",
  "email": "ava.patel@example.com"
}
```

## Example Response
```json
{
  "id": "usr_1001",
  "name": "Ava Patel",
  "email": "ava.patel@example.com",
  "createdAt": "2026-03-01T10:00:00Z"
}
```

## Compatibility
- No authentication is required in this release.
- Responses are returned as UTF-8 encoded JSON.

## Notes
- This release establishes the base REST contract for user management.
