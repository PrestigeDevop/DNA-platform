# How to Run DNA Platform

## Quick Start (Fastest Way)

```bash
# Navigate to the console app
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp

# Run the demo
dotnet run
```

**Expected Output**: 3 workflow demonstrations with execution timing

---

## Web API Scaffold

Start the current server API:

```bash
dotnet run --project src/05_DevUI/DNAPlatform.DevUI.AGUI/Server/DNAPlatform.DevUI.AGUI.Server
```

The API listens on `http://localhost:5000`. Check `http://localhost:5000/health` to verify it is running. This is an API scaffold only; it does not yet provide the AG-UI protocol, a browser client, or a graphical workflow editor.

---

## Step-by-Step Instructions

### Option 1: Command Line

```bash
# 1. Clone or navigate to the repository
cd DNA-platform/prestigedevop-bioinformatic-workflow-platform

# 2. Restore dependencies (first time only)
dotnet restore

# 3. Build the solution (optional, dotnet run will build automatically)
dotnet build

# 4. Run the console demo
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

### Option 2: Visual Studio Code

```bash
# 1. Open the workspace folder
code DNA-platform/prestigedevop-bioinformatic-workflow-platform

# 2. Open terminal (Ctrl+`)

# 3. Run:
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

### Option 3: Visual Studio 2022

1. Open `DNAPlatform.slnx` in Visual Studio
2. Set **StartupProject** to `DNAPlatform.AgentWorkflowPatterns.ConsoleApp`
3. Press **F5** or **Ctrl+F5** to run
4. Output appears in Debug console

---

## What You'll See

When you run it, the console will display:

```
═══════════════════════════════════════════════════════════
  DNA Platform - Agent Workflow Patterns
  Low-Code/No-Code Bioinformatic Workflow Engine
═══════════════════════════════════════════════════════════

📋 Example 1: Simple Sequential Workflow
─────────────────────────────────────────
✓ Workflow completed with status: Completed
  Duration: 333ms

🤖 Example 2: Parallel Agent Workflow
─────────────────────────────────────
✓ Parallel workflow completed with status: Completed
  Parallel nodes executed: 2
  Total duration: 210ms

🔄 Example 3: Dynamic Workflow Generation
──────────────────────────────────────────
✓ Dynamic workflow completed with status: Completed
  Iterations completed: 1
```

---

## Troubleshooting

### "dotnet: command not found"
**Solution**: Install .NET 8.0 or higher from https://dotnet.microsoft.com/download

Check version:
```bash
dotnet --version
```

### "Project file not found"
**Solution**: Make sure you're in the correct directory:
```bash
# From the repository root, run:
cd src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp
dotnet run
```

### Build fails with errors
**Solution**: Clean and rebuild:
```bash
dotnet clean
dotnet build
```

### Slow first run
**Solution**: First run downloads NuGet packages. Subsequent runs are faster.

---

## Next Steps After Running

1. **Read the Docs**: Check out [DEVELOPMENT.md](./DEVELOPMENT.md) for examples
2. **Modify the Demo**: Edit [Program.cs](./src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp/Program.cs) to create your own workflow
3. **Create Custom Skills**: See [DEVELOPMENT.md#writing-custom-skills](./DEVELOPMENT.md#writing-custom-skills)
4. **Build Your Workflow**: Use the code examples in [QUICKREF.md](./QUICKREF.md)

---

## File Locations

| What | Path |
|------|------|
| **Console App** | `src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp/` |
| **Core Engine** | `src/03_AgentWorkflowPatterns/DNAPlatform.AgentFramework/` |
| **Documentation** | Root directory (`*.md` files) |
| **Solution File** | `DNAPlatform.slnx` |

---

## Common Commands

```bash
# Build only (don't run)
dotnet build

# Run with verbose output
dotnet run --verbosity diagnostic

# Run specific project
dotnet run --project src/03_AgentWorkflowPatterns/DNAPlatform.AgentWorkflowPatterns.ConsoleApp

# Clean build artifacts
dotnet clean

# Run tests (when available)
dotnet test

# Publish release build
dotnet publish -c Release
```

---

## That's It! 🚀

You're ready to explore and modify the DNA Platform. Happy workflows!
