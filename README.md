# UserManagementAPI

An ASP.NET Core Web API project demonstrating custom **LoggingMiddleware** and **ErrorHandlingMiddleware** with JWT authentication.

## Features
- CRUD operations for users (Create, Read, Update, Delete).
- JWT-based authentication (`/api/Users/login`).
- Custom middleware:
  - **LoggingMiddleware**: logs incoming requests and outgoing responses.
  - **ErrorHandlingMiddleware**: catches unhandled exceptions and returns structured JSON errors.

## Prerequisites
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or later
- A configured `appsettings.json` with JWT settings:
  ```json
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "UserManagementAPI",
    "Audience": "UserManagementAPIUsers"
  }

## Running the Project
<pre>
  dotnet build
  dotnet run
</pre>

The API will be available at http://localhost:5062.

## Testing the middleware

Use curl to trigger different responses:

200 OK (valid token + valid input):

<pre>
  curl -X GET "http://localhost:5062/api/Users" -H "Authorization: Bearer <valid-token>"</valid-token>
</pre>

400 Bad Request (invalid email):

<pre>
  curl -X POST "http://localhost:5062/api/Users" \
  -H "Authorization: Bearer <valid-token>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","email":"bad-email"}'
</pre>

401 Unauthorized (no token):
<pre>
  curl -X GET "http://localhost:5062/api/Users"
</pre>

404 Not Found:
<pre>
  curl -X GET "http://localhost:5062/api/NotReal"
</pre>

500 Internal Server Error (forced error):
<pre>
  curl -X GET "http://localhost:5062/api/Users/error" -H "Authorization: Bearer <valid-token>"
</pre>
## Logs

Middleware logs will appear in the console:

Incoming Request: GET /api/Users
Outgoing Response: 200
Exception caught: Forced error for testing

## Middleware Validation Script

| Scenario | Curl Command | Expected Status | Expected Log |
|----------|--------------|-----------------|--------------|
| **200 OK** (valid token + valid input) | `curl -X GET "http://localhost:5062/api/Users" -H "Authorization: Bearer <valid-token>"` | 200 | Incoming Request: GET /api/Users<br>Outgoing Response: 200 |
| **400 Bad Request** (invalid email) | `curl -X POST "http://localhost:5062/api/Users" -H "Authorization: Bearer <valid-token>" -H "Content-Type: application/json" -d '{"name":"Test","email":"bad-email"}'` | 400 | Incoming Request: POST /api/Users<br>Outgoing Response: 400 |
| **401 Unauthorized** (no token) | `curl -X GET "http://localhost:5062/api/Users"` | 401 | Incoming Request: GET /api/Users<br>Outgoing Response: 401 |
| **404 Not Found** | `curl -X GET "http://localhost:5062/api/NotReal"` | 404 | Incoming Request: GET /api/NotReal<br>Outgoing Response: 404 |
| **500 Internal Server Error** (forced error) | `curl -X GET "http://localhost:5062/api/Users/error" -H "Authorization: Bearer <valid-token>"` | 500 | Incoming Request: GET /api/Users/error<br>Outgoing Response: 500<br>Exception caught: Forced error for testing |

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

2025
