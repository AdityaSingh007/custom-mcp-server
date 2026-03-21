# API Changelog - v1.4.0

## Release Summary
Improved authenticated session handling and added a shortcut endpoint for the current user profile.

## Added
- Added `POST /auth/refresh` to renew expired access tokens.
- Added `GET /me` to retrieve the authenticated user's profile.
- Added refresh token validation errors with explicit error codes.

## Fixed
- Fixed inconsistent status codes returned for invalid login attempts.
- Fixed missing `WWW-Authenticate` headers on some unauthorized responses.

## Endpoint Notes
### `POST /auth/refresh`
- Accepts a refresh token in the request body.
- Returns a new access token without requiring a fresh login.

### `GET /me`
- Returns the authenticated user profile associated with the bearer token.
- Designed to reduce extra client lookups after login.

## Example Refresh Response
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.refreshed",
  "expiresAt": "2026-03-21T19:00:00Z",
  "tokenType": "Bearer"
}
```

## Upgrade Notes
- Clients that currently re-run login on token expiry can switch to `POST /auth/refresh`.

## Notes
- Token refresh improves session continuity for client applications.
