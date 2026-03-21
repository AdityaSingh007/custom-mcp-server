# API Changelog - v1.9.0

## Release Summary
Expanded reporting support with export functionality and more complete audit-oriented filtering.

## Added
- Added `GET /users/export` to export user data as CSV.
- Added `includeDeleted=true` support for admin audit queries.
- Added support for `fields` selection on export requests.

## Fixed
- Fixed pagination metadata when filtering returned zero results.
- Fixed CSV escaping for names containing commas and quotes.

## Endpoint Notes
### `GET /users/export`
- Returns `text/csv`.
- Supports filters consistent with `GET /users`.
- Honors `includeDeleted=true` for audit and compliance workflows.

## Example Request
```http
GET /users/export?status=active&includeDeleted=true&fields=id,name,email,status
Accept: text/csv
```

## Example CSV Header
```text
id,name,email,status
```

## Compatibility
- Existing JSON-based list integrations are unaffected.
- Export is intended for reporting tools, admin dashboards, and offline review.

## Notes
- Export support is designed for reporting and compliance workflows.
