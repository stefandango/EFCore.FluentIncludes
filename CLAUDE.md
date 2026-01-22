# Project Instructions

## Project Awareness

- Read `.claude/PLANNING.md` at the start of a new conversation to understand architecture, goals, and constraints
- Check `.claude/TASK.md` before starting a new task - add it if not listed
- Use `dotnet` CLI commands for all .NET operations

## Key Files

- `.claude/PLANNING.md` - Architecture and design decisions
- `.claude/TASK.md` - Current tasks and progress tracking
- `README.md` - Project overview and setup

## Tech Stack

- **Language:** C# with .NET 10
- **Web:** ASP.NET Core
- **Data:** Entity Framework Core
- **Testing:** xUnit

## Project Configuration

- **Solution Format:** Use `.slnx` files (XML-based solution format) instead of traditional `.sln` files
- **Central Package Management:** Use `Directory.Build.props` and `Directory.Packages.props` for centralized NuGet package version management
  - Define package versions in `Directory.Packages.props` at the solution root
  - Use `Directory.Build.props` for shared MSBuild properties across all projects
  - Project files should reference packages without explicit versions (versions come from `Directory.Packages.props`)

## Detailed Rules

See `.claude/rules/` for specific guidelines:

- `architecture.md` - Vertical slice, modular monolith patterns
- `code-style.md` - C# conventions and formatting
- `testing.md` - Test requirements and patterns
- `documentation.md` - Documentation standards
- `security.md` - Security best practices

## AI Behavior

- Never assume missing context - ask questions if uncertain
- Never hallucinate libraries or NuGet packages
- Confirm file paths and namespaces exist before referencing them
