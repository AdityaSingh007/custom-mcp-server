# API Changelog - v1.5.0

## Release Summary
Enhanced the list endpoint for admin and dashboard use cases with filtering, sorting, and better query ergonomics.

## Added
- Added filtering by `status` and `role` on `GET /users`.
- Added sorting support with `sortBy` and `order` query parameters.
- Added support for compound query strings such as `?status=active&role=admin`.

## Changed
- Optimized list endpoint performance for large datasets.
- Stabilized default sort order to `createdAt desc`.

## Query Notes
### Supported Parameters
- `status`: Filters by lifecycle state such as `active` or `suspended`.
- `role`: Filters by assigned role such as `admin` or `member`.
- `sortBy`: Supports `createdAt`, `name`, and `email`.
- `order`: Supports `asc` and `desc`.

## Example Request
```http
GET /users?status=active&role=admin&sortBy=name&order=asc&page=1&limit=25
```

## Compatibility
- Existing consumers can ignore the new query parameters.
- Sorting is deterministic even when the caller omits explicit sort settings.

## Notes
- These query options help admin clients build richer user management views.
