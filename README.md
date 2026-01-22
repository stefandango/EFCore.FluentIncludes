# [PROJECT_NAME]

> [Brief one-line description of what this project does]

## Overview

[PROJECT_NAME] is a [type of application] built with .NET 10 that [main purpose/functionality].

### Key Features

- [Feature 1]
- [Feature 2]
- [Feature 3]
- [Add more features as needed]

## Technology Stack

- **.NET 10** - Core framework
- **C# 13** - Language version
- **ASP.NET Core** - Web framework (if applicable)
- **Entity Framework Core** - Data access (if applicable)
- **[Database Name]** - Database (if applicable)
- **xUnit** - Testing framework
- **[Add other key technologies]**

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Additional prerequisites like databases, tools, etc.]

## Getting Started

### 1. Clone the Repository

```bash
git clone [REPOSITORY_URL]
cd [PROJECT_NAME]
```

### 2. Setup Configuration

```bash
# Copy appsettings template
cp appsettings.example.json appsettings.Development.json

# Update connection strings and other settings in appsettings.Development.json
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Database Setup (if applicable)

```bash
# Update database with migrations
dotnet ef database update

# Or if using different database provider:
# [Add specific database setup instructions]
```

### 5. Run the Application

```bash
# Development
dotnet run

# Or with hot reload
dotnet watch run
```

The application will be available at:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger` (in development)

## Project Structure

```
[PROJECT_NAME]/
├── .claude/                  # Claude Code configuration
│   ├── PLANNING.md          # Architecture and design decisions
│   ├── TASK.md              # Current tasks and progress
│   ├── rules/               # Coding standards and guidelines
│   └── PRPs/                # Product Requirement Plans
├── src/
│   └── [ProjectName].Api/   # Main API project
│       ├── Features/        # Business features (vertical slices)
│       │   ├── [Feature1]/
│       │   └── [Feature2]/
│       └── Program.cs       # Application entry point
├── tests/
│   └── [ProjectName].Api.Tests/  # Test project
├── Directory.Build.props    # Shared MSBuild properties
├── Directory.Packages.props # Central package version management
├── global.json              # .NET SDK version pinning
└── [project-name].slnx      # XML-based solution file
```

## Configuration

Key configuration sections in `appsettings.json`:

### Connection Strings (if applicable)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "[Your connection string]"
  }
}
```

### Authentication (if applicable)  

```json
{
  "Authentication": {
    "JwtSettings": {
      "SecretKey": "[Your JWT secret]",
      "Issuer": "[Your issuer]",
      "Audience": "[Your audience]",
      "ExpirationInMinutes": 60
    }
  }
}
```

### [Add other configuration sections as needed]

## API Documentation (if applicable)

When running in development mode, API documentation is available at:

- **Swagger UI:** `https://localhost:5001/swagger`
- **OpenAPI Spec:** `https://localhost:5001/swagger/v1/swagger.json`

### Key Endpoints

- `GET /api/[endpoint]` - [Description]
- `POST /api/[endpoint]` - [Description]
- `PUT /api/[endpoint]/{id}` - [Description]
- `DELETE /api/[endpoint]/{id}` - [Description]

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Tests/UnitTests/
```

### Code Quality

```bash
# Format code
dotnet format

# Lint/analyze (if using analyzers)
dotnet build --verbosity normal
```

### Database Migrations (if applicable)

```bash
# Add new migration
dotnet ef migrations add [MigrationName]

# Update database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update [MigrationName]
```

## Deployment

### Local/Development

```bash
dotnet run --environment Development
```

### Production

```bash
# Build for production
dotnet publish -c Release -o ./publish

# Run published application
cd publish
dotnet [PROJECT_NAME].dll
```

### Docker (if applicable)

```bash
# Build image
docker build -t [project-name] .

# Run container
docker run -p 5000:5000 [project-name]
```

### [Add cloud deployment instructions if applicable]

## Environment Variables

Required environment variables for production:

- `ASPNETCORE_ENVIRONMENT` - Set to `Production`
- `ConnectionStrings__DefaultConnection` - Database connection string
- `Authentication__JwtSettings__SecretKey` - JWT secret key
- `[Add other required environment variables]`

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes following the coding standards
4. Add tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Coding Standards

- Follow the guidelines in `.claude/PLANNING.md`
- Ensure all public methods have XML documentation
- Write unit tests for new features
- Keep files under 500 lines
- Use vertical slice architecture

## Testing

This project follows a comprehensive testing strategy:

- **Unit Tests:** Test individual components in isolation
- **Integration Tests:** Test API endpoints and database interactions
- **Test Coverage:** Aim for >80% code coverage

Run tests before committing:

```bash
dotnet test --verbosity normal
```

## Troubleshooting

### Common Issues

#### Issue: [Common Problem]

**Solution:** [How to fix it]

#### Issue: Database Connection Fails

**Solution:** Verify connection string in `appsettings.json` and ensure database server is running

#### Issue: [Another Common Problem]  

**Solution:** [How to fix it]

### Logging

Application logs are written to:

- Console (Development)
- [Log file location or external service] (Production)

Enable detailed logging by setting:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## License

[Specify your license - MIT, Apache 2.0, etc.]

## Support

- **Documentation:** See `.claude/PLANNING.md` for architecture details
- **Issues:** [Link to issue tracker]
- **Contact:** [Contact information]

---

**Version:** [VERSION]
**Last Updated:** [DATE]
**Framework:** .NET 10 / C# 13

