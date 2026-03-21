# API Changelog - v1.6.0

## Release Summary
Added high-volume import support and improved request tracing for operational visibility.

## Added
- Added `POST /users/bulk-import` for batch user creation.
- Added request ID propagation in response headers for tracing.
- Added per-record validation feedback in bulk import results.

## Fixed
- Fixed edge cases where empty payloads returned generic server errors.
- Fixed import validation to identify duplicate emails within the same batch.

## Endpoint Notes
### `POST /users/bulk-import`
- Accepts an array of user records.
- Returns counts for imported, skipped, and failed records.
- Partial success is supported and surfaced in the response body.

## Example Response
```json
{
  "imported": 18,
  "skipped": 1,
  "failed": 1,
  "errors": [
    {
      "index": 19,
      "code": "duplicate_email",
      "message": "Email already exists."
    }
  ]
}
```

## Operational Notes
- Responses now include an `X-Request-Id` header for easier log correlation.

## Notes
- Bulk import is intended for internal admin and migration workflows.
