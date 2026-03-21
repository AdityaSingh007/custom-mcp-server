# API Changelog - v1.7.0

## Release Summary
Added usage observability features to help clients and operators understand traffic patterns and platform limits.

## Added
- Added rate limit headers to authenticated API responses.
- Added `GET /metrics/usage` for aggregated usage reporting.
- Added `retry-after` guidance when throttling is applied.

## Changed
- Refined audit logging for user and auth endpoints.
- Standardized throttling responses to use `429 Too Many Requests`.

## Header Notes
### Rate Limit Headers
- `X-RateLimit-Limit`: Total requests allowed in the window.
- `X-RateLimit-Remaining`: Remaining requests in the current window.
- `X-RateLimit-Reset`: UNIX timestamp for the next reset.

## Example Metrics Response
```json
{
  "window": "24h",
  "requests": 14820,
  "uniqueClients": 132,
  "topEndpoints": [
    "/users",
    "/auth/login",
    "/me"
  ]
}
```

## Compatibility
- Existing clients do not need changes unless they want to actively react to rate limit metadata.

## Notes
- Clients can now monitor request consumption more accurately.
