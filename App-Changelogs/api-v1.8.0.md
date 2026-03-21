# API Changelog - v1.8.0

## Release Summary
Introduced targeted status updates and improved lifecycle visibility with soft deletion metadata.

## Added
- Added `PATCH /users/{id}/status` to activate or suspend a user.
- Added support for soft deletion metadata in user responses.
- Added explicit transition validation for invalid status changes.

## Changed
- Updated validation rules to reject malformed email domains earlier.
- Reduced payload size for status-only updates by supporting partial mutation.

## Endpoint Notes
### `PATCH /users/{id}/status`
- Accepts a minimal body such as `{ "status": "suspended" }`.
- Returns the updated user record after the state transition.
- Rejects unsupported transitions with `409 Conflict`.

## Example Request
```http
PATCH /users/usr_1001/status
Content-Type: application/json

{
  "status": "suspended"
}
```

## Example Response Fields
- `status`
- `deletedAt`
- `deletedBy`

## Upgrade Notes
- Clients that only need status changes should prefer this endpoint over full `PUT /users/{id}` calls.

## Notes
- Partial updates reduce payload size for status-only operations.
