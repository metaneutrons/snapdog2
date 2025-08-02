#!/bin/bash
# Setup git hooks for SnapDog2

echo "Setting up git hooks for conventional commits..."

# Create commit-msg hook
cat > .git/hooks/commit-msg << 'EOF'
#!/bin/sh
# Conventional commit format validation

commit_msg_file="$1"
first_line=$(head -n1 "$commit_msg_file")

# Check conventional commit format
if ! echo "$first_line" | grep -qE "^(feat|fix|docs|style|refactor|test|chore|build|ci|perf|revert)(\([a-z0-9-]+\))?: .+"; then
    echo "❌ Commit message must follow conventional commit format"
    echo "Format: <type>(scope): <description>"
    echo "Types: feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert"
    echo "Example: feat(api): add user authentication"
    echo ""
    echo "Your commit message:"
    echo "$first_line"
    exit 1
fi

echo "✅ Commit message follows conventional commit format"
EOF

# Create pre-commit hook
cat > .git/hooks/pre-commit << 'EOF'
#!/bin/sh
# Pre-commit hook for SnapDog2

echo "🎨 Checking code formatting with CSharpier..."
dotnet csharpier check .

if [ $? -ne 0 ]; then
    echo "❌ Code formatting issues found. Running formatter..."
    dotnet csharpier format .
    echo "✅ Code formatted. Please review and commit again."
    exit 1
fi

echo "🏗️ Building project..."
dotnet build --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please fix errors before committing."
    exit 1
fi

echo "✅ Pre-commit checks passed!"
EOF

# Create pre-push hook
cat > .git/hooks/pre-push << 'EOF'
#!/bin/sh
# Pre-push hook for SnapDog2

echo "🧪 Running tests..."
dotnet test --verbosity quiet

if [ $? -ne 0 ]; then
    echo "❌ Tests failed. Please fix failing tests before pushing."
    exit 1
fi

echo "✅ Pre-push checks passed!"
EOF

# Make hooks executable
chmod +x .git/hooks/commit-msg .git/hooks/pre-commit .git/hooks/pre-push

echo "✅ Git hooks installed successfully!"
echo ""
echo "Conventional commit format:"
echo "  <type>(scope): <description>"
echo ""
echo "Types: feat, fix, docs, style, refactor, test, chore, build, ci, perf, revert"
echo "Example: feat(api): add user authentication"
