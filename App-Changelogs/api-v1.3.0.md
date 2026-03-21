# API Changelog - v1.3.0

## Release Summary
Introduced authentication and moved write operations for user management behind bearer token authorization.

## Added
- Added `POST /auth/login` for user authentication.
- Added bearer token support for protected endpoints.
- Added token expiry information in authentication responses.

## Changed
- Protected user update and delete operations behind authentication.
- Unauthenticated access to secured endpoints now returns `401 Unauthorized`.

## Endpoint Notes
### `POST /auth/login`
- Accepts `email` and `password`.
- Returns an access token and expiry timestamp on success.

## Example Request
```http
POST /auth/login
Content-Type: application/json

{
  "email": "ava.patel@example.com",
  "password": "example-password"
}
```

## Example Response
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.sample",
  "expiresAt": "2026-03-21T18:00:00Z",
  "tokenType": "Bearer"
}
```

## Compatibility
- `GET` endpoints remain public in this release.
- Clients must send `Authorization: Bearer <token>` for secured routes.

## Notes
- Consumers should send `Authorization: Bearer <token>` for secured routes.
