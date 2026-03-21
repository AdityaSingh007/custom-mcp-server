# API Changelog - v1.2.0

## Release Summary
Completed the first CRUD milestone by adding update and delete operations, plus a more structured error model.

## Added
- Added `PUT /users/{id}` to update user profile details.
- Added `DELETE /users/{id}` to remove a user.
- Added field-level validation details in error payloads.

## Changed
- Standardized error responses to include `code`, `message`, and `details`.
- Validation failures now return `400 Bad Request` consistently.

## Endpoint Notes
### `PUT /users/{id}`
- Replaces mutable user fields.
- Requires `name` and `email` when updating.

### `DELETE /users/{id}`
- Returns `204 No Content` on success.
- Returns `404 Not Found` for unknown user IDs.

## Example Error Response
```json
{
  "code": "validation_error",
  "message": "Request body is invalid.",
  "details": {
    "email": "Email must be a valid address."
  }
}
```

## Upgrade Notes
- If clients previously parsed plain-text errors, update them to read the JSON error object.

## Notes
- This release completes the initial CRUD surface for user resources.
