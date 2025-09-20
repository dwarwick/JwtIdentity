# Surveys API

## Endpoint summary
- `GET /api/surveys` — list surveys available to the authenticated user.
- `POST /api/surveys` — create a survey with optional AI-generated questions.
- `GET /api/surveys/{id}` — retrieve survey details and status.
- `PUT /api/surveys/{id}` — update metadata or questions.
- `POST /api/surveys/{id}/close` — close a survey to stop collecting responses.

## Create survey request
Send JSON with `title`, `description`, and optional `useAiGenerator` and `aiInstructions` fields. Include the JWT in the `Authorization` header.

## Important response fields
`id`, `status`, `createdUtc`, `ownerId`, and optional `aiSummary` describe the survey state. Hypermedia links expose preview URLs.

## Error handling
401 indicates missing or expired tokens, 403 indicates insufficient permissions, 400 results from validation failures, and 500 denotes an internal error that should be retried later.
