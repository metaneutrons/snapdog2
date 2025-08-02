# Contributing to SnapDog2

## Development Setup

1. Clone the repository
2. Install dependencies: `dotnet restore`
3. Setup git hooks: `./setup-hooks.sh`
4. Install CSharpier: `dotnet tool restore`

## Git Hooks

This project enforces code quality and consistency through git hooks:

- **pre-commit**: Formats code and runs build
- **commit-msg**: Enforces conventional commit format
- **pre-push**: Runs tests

## Conventional Commits

All commit messages must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>(scope): <description>

[optional body]

[optional footer]
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `build`: Build system changes
- `ci`: CI/CD changes
- `perf`: Performance improvements
- `revert`: Reverting changes

### Examples

- `feat(api): add user authentication`
- `fix(ui): resolve button alignment issue`
- `docs: update installation instructions`
- `test(auth): add unit tests for login flow`
- `chore: update dependencies`

## Code Formatting

This project uses [CSharpier](https://csharpier.com/) for code formatting. The pre-commit hook will automatically format your code, but you can also run it manually:

```bash
dotnet csharpier .
```

## Testing

Run tests before pushing:

```bash
dotnet test
```

The pre-push hook will automatically run tests, but it's good practice to run them locally during development.
