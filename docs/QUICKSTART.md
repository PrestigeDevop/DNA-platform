# DNA Platform - Quick Start 
 
## How to Run the GUI (Svelte DevUI) 
 
### Option 1: Run Everything (Easiest) 
 
From the main folder (`DNA-platform Cat`), run: 
 
``` 
run-all.bat 
``` 
 
This will: 
- Start the backend API on http://localhost:5254 
- Start the Svelte frontend on http://localhost:5173 
 
### Option 2: Run Separately 
 
**Backend only:** 
``` 
cd src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API
dotnet run --urls http://localhost:5254
``` 
 
**Frontend only (Svelte):** 
``` 
cd src\05_DevUI\DNAPlatform.DevUI.Web
npm install
npm run dev
``` 
 
### Access Points 
 
- Svelte Frontend: http://localhost:5173 
- Backend API: http://localhost:5254 
- Swagger Docs: http://localhost:5254/swagger 
- Health Check: http://localhost:5254/health 
 
### Prerequisites

- .NET 8.0+ SDK installed
- Node.js 20+ installed
- Git (optional, for version control)

### First Time Setup

``` 
# Clone repository (if not done)
git clone https://github.com/PrestigeDevop/DNA-platform.git
cd DNA-platform

# Restore .NET dependencies
dotnet restore

# Build the solution
dotnet build

# Install frontend dependencies
cd src\05_DevUI\DNAPlatform.DevUI.Web
npm install

# Run everything
cd ..\..\..
run-all.bat
``` 
 
### Troubleshooting

| Problem | Solution |
|---------|----------|
| Port 5254 already in use | Change port in `run-all.bat` or kill the process using it |
| Port 5173 already in use | Change port in `vite.config.ts` or kill the process using it |
| `npm install` fails | Clear cache: `npm cache clean --force` then retry |
| `dotnet run` fails | Run `dotnet restore` and `dotnet build` first |
| Frontend can't connect to backend | Ensure backend is running on port 5254 (check proxy in vite.config.ts) |

### Next Steps

1. Open http://localhost:5173 in your browser
2. Navigate to **Workflows** to create your first workflow
3. Check **Skills** to see available bioinformatic skills
4. Monitor **Executions** to track workflow progress
5. Manage **Agents** to configure AI agents

### Need More Help?
 ask
