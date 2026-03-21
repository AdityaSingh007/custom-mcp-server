# API Changelog - v1.1.0

## Release Summary
Expanded read capabilities by introducing single-user retrieval and collection pagination.

## Added
- Added `GET /users/{id}` to fetch a single user by ID.
- Added pagination support to `GET /users` with `page` and `limit` query parameters.
- Added `total`, `page`, and `pageSize` metadata to list responses.

## Changed
- Improved validation messages for user creation requests.
- Normalized invalid ID responses to `404 Not Found`.

## Endpoint Notes
### `GET /users/{id}`
- Returns `200 OK` when a user exists.
- Returns `404 Not Found` when the ID is unknown.

### `GET /users?page=1&limit=20`
- Default `page` is `1`.
- Default `limit` is `25`.
- Maximum `limit` is `100`.

## Example Response
```json
{
  "items": [
    {
      "id": "usr_1001",
      "name": "Ava Patel",
      "email": "ava.patel@example.com"
    }
  ],
  "total": 1,
  "page": 1,
  "pageSize": 20
}
```

## Compatibility
- Existing `GET /users` integrations continue to work.
- Clients should tolerate paginated envelope metadata when parsing list responses.

## Notes
- Clients can now fetch individual resources without filtering full collections.
