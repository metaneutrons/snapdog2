# Git Hooks Configuration

## 🎯 **Hook Strategy**

Lightweight local-only Git hooks that ensure code quality without Docker dependencies:

- **Build must succeed** before commits
- **Test failures are acceptable** (informational only)
- **Code formatting is enforced** automatically
- **Fast and lightweight** - no Docker overhead
- **Local .NET CLI only** - works anywhere .NET is installed

## 🪝 **Active Hooks**

### **Pre-Commit Hook**
**Purpose:** Ensure build succeeds and code is properly formatted

**What it does:**
1. ✅ **Tool Restoration** - Ensures CSharpier is available
2. ✅ **Code Formatting Check** - Runs CSharpier to ensure consistent formatting
3. ✅ **Auto-Format** - Automatically formats code if issues found
4. ✅ **Build Verification** - Ensures project compiles successfully
5. ❌ **Blocks commit** if build fails

**Requirements:**
- .NET SDK installed locally
- CSharpier tool (auto-restored if needed)

### **Pre-Push Hook**
**Purpose:** Run tests before push (informational only)

**What it does:**
1. 🧪 **Test Execution** - Runs all tests locally
2. ⚠️ **Informational Results** - Shows test status but doesn't block push
3. ✅ **Always Allows Push** - Test failures don't prevent push

## 🖥️ **Local Commands Used**

```bash
# Tool restoration
dotnet tool restore

# Code formatting
dotnet csharpier check .
dotnet csharpier format .

# Build verification
dotnet build --verbosity quiet

# Test execution
dotnet test --verbosity quiet
```

## 📋 **Example Workflows**

### **Successful Commit**
```bash
$ git commit -m "Add new API endpoint"
🎨 Checking code formatting with CSharpier...
🏗️ Building project...
🧪 Note: Tests are not run during pre-commit (test failures are acceptable)
💡 To run tests manually: 'dotnet test' or 'make test'
✅ Pre-commit checks passed! Build successful, ready to commit.
[main abc1234] Add new API endpoint
```

### **Failed Build (Blocked)**
```bash
$ git commit -m "Broken code"
🎨 Checking code formatting with CSharpier...
🏗️ Building project...
❌ Build failed. Please fix compilation errors before committing.
💡 Run 'dotnet build' to see detailed errors.
```

### **Code Formatting Issues**
```bash
$ git commit -m "Unformatted code"
🎨 Checking code formatting with CSharpier...
❌ Code formatting issues found. Running formatter...
✅ Code formatted. Please review changes and commit again.
```

### **Push with Test Results**
```bash
$ git push origin main
🧪 Running tests before push (informational only)...
⚠️ Some tests are failing, but push will continue.
💡 Test failures are acceptable during development.
💡 Run 'dotnet test' or 'make test' to see detailed results.
🚀 Pre-push checks complete. Proceeding with push...
```

## 🔧 **Manual Commands**

### **Format Code**
```bash
dotnet csharpier format .
```

### **Check Formatting**
```bash
dotnet csharpier check .
```

### **Build Project**
```bash
dotnet build
```

### **Run Tests**
```bash
dotnet test
# or use make command for Docker environment
make test
```

## 🚫 **Bypassing Hooks**

### **Skip Pre-Commit Hook**
```bash
git commit --no-verify -m "Emergency commit"
```

### **Skip Pre-Push Hook**
```bash
git push --no-verify origin main
```

**⚠️ Warning:** Only use `--no-verify` in emergency situations.

## 🛠️ **Troubleshooting**

### **Hook Not Running**
```bash
# Check if hooks are executable
ls -la .git/hooks/pre-commit
ls -la .git/hooks/pre-push

# Make executable if needed
chmod +x .git/hooks/pre-commit
chmod +x .git/hooks/pre-push
```

### **.NET CLI Not Found**
```bash
# Install .NET SDK
# macOS: brew install dotnet
# Windows: Download from https://dotnet.microsoft.com/download
# Linux: Follow distribution-specific instructions
```

### **CSharpier Not Available**
```bash
# Install CSharpier globally
dotnet tool install -g csharpier

# Or restore local tools (recommended)
dotnet tool restore
```

### **Build Issues**
```bash
# Restore packages
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

## ✅ **Benefits**

1. **Fast Execution** - No Docker overhead, runs directly on local machine
2. **Simple Dependencies** - Only requires .NET SDK
3. **Consistent Code Quality** - All commits have properly formatted, buildable code
4. **Developer Friendly** - Clear messages and helpful suggestions
5. **Flexible Testing** - Tests provide information but don't block development
6. **Works Everywhere** - Any environment with .NET SDK

## 🎯 **Design Philosophy**

- **Lightweight** - Git hooks should be fast and simple
- **Local-First** - No external dependencies like Docker
- **Build-Critical** - Build failures block commits (quality gate)
- **Test-Informational** - Test failures inform but don't block (velocity)
- **Format-Automatic** - Code formatting happens automatically
