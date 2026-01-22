## FEATURE:
Build a .NET 8 Web API for task management with the following functionality:
- CRUD operations for tasks (Create, Read, Update, Delete)
- Task categories and priority levels
- User authentication using JWT tokens
- Task assignment to users
- RESTful endpoints following OpenAPI/Swagger documentation
- SQLite database with Entity Framework Core
- Input validation using FluentValidation
- Structured logging with Serilog

## EXAMPLES:
- `examples/TaskController.cs` - Example controller showing proper API endpoint structure
- `examples/TaskService.cs` - Business logic implementation following vertical slice pattern
- `examples/appsettings.json` - Configuration file with connection strings and JWT settings
- `examples/Program.cs` - Application startup configuration with dependency injection
- `examples/TaskEntity.cs` - Entity model with proper data annotations

## DOCUMENTATION:
- [ASP.NET Core Web API Tutorial](https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [JWT Authentication in .NET](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [Serilog with ASP.NET Core](https://github.com/serilog/serilog-aspnetcore)

## OTHER CONSIDERATIONS:
- Ensure proper error handling with global exception middleware
- Use async/await patterns for all database operations
- Implement proper HTTP status codes (200, 201, 404, 400, 500)
- Include comprehensive unit tests using xUnit and Moq
- Follow vertical slice architecture - group related functionality together
- Respect domain boundaries between User and Task management
- Use nullable reference types and handle null scenarios appropriately
- Configure CORS policies for frontend integration
- Implement request/response DTOs to avoid exposing entity models directly
- Add health checks endpoint for monitoring