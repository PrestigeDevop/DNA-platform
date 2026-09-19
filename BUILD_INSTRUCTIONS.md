# Build & Test Instructions

> DNA Platform - Phase 3 Skills System

## Quick Start

### Option 1: Use Build Script (Recommended)

Double-click `build-skills.bat` in the project root folder.

Or run from command prompt:
```cmd
build-skills.bat
```

### Option 2: Manual Build

Open **PowerShell** in the project folder and run:

```powershell
# Build Skills library
dotnet build "src\06_Skills\DNAPlatform.Skills.csproj"

# Build API
dotnet build "src\05_DevUI\DNAPlatform.DevUI.API\DNAPlatform.DevUI.API.csproj"
```

### Option 3: Use Visual Studio Code

1. Open project folder in VS Code
2. Press `` Ctrl+` `` to open terminal
3. Run:
   ```bash
   dotnet build src/06_Skills/DNAPlatform.Skills.csproj
   dotnet build src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API.csproj
   ```

## Testing

### Test with Script

Double-click `test-skills.bat` to automatically:
1. Start the API server
2. Run test queries
3. Display results

### Test Manually

1. **Start the API:**
   ```cmd
   cd src\05_DevUI\DNAPlatform.DevUI.API
   dotnet run --urls http://localhost:5254
   ```

2. **Test in another terminal:**
   ```cmd
   # List all skills
   curl http://localhost:5254/api/skills

   # Execute MsgBoxAlertSkill
   curl -X POST http://localhost:5254/api/skills/msgbox-alert/execute ^
     -H "Content-Type: application/json" ^
     -d "{\"message\": \"Hello!\", \"msgType\": \"success\"}"
   ```

### Test in Browser

1. Start backend: `dotnet run --urls http://localhost:5254`
2. Start frontend: `cd src\05_DevUI\DNAPlatform.DevUI.Web && npm run dev`
3. Navigate to: `http://localhost:5173/skills`
4. Configure MsgBox inputs and click "Execute"

## Expected Results

### GET /api/skills
```json
{
  "count": 6,
  "skills": [
    {
      "skillId": "msgbox-alert",
      "name": "Message Box Alert",
      "category": "UI Components",
      "icon": "💬"
    },
    {
      "skillId": "data-transform",
      "name": "Data Transform",
      "category": "Data Processing",
      "icon": "🔄"
    },
    {
      "skillId": "validation",
      "name": "Validation",
      "category": "Data Processing",
      "icon": "✅"
    },
    {
      "skillId": "merge-results",
      "name": "Merge Results",
      "category": "Data Processing",
      "icon": "🔀"
    },
    {
      "skillId": "fasta-loader",
      "name": "FASTA Loader",
      "category": "Bioinformatics",
      "icon": "🧬"
    },
    {
      "skillId": "sequence-aligner",
      "name": "Sequence Aligner",
      "category": "Bioinformatics",
      "icon": "🔬"
    }
  ]
}
```

### POST /api/skills/msgbox-alert/execute
```json
{
  "success": true,
  "skillId": "msgbox-alert",
  "data": {
    "alert_id": "a1b2c3d4-...",
    "message": "Hello!",
    "type": "success",
    "title": "Alert",
    "icon": "✅",
    "color": "#10b981",
    "sound": "success.mp3",
    "timestamp": "2026-09-06T12:00:00.000Z"
  },
  "executedAt": "2026-09-06T12:00:00.000Z",
  "durationMs": 3
}
```

## Troubleshooting

### Issue: "Skill 'msgbox-alert' not found"
**Cause:** Project reference was missing
**Fix:** ✅ Already applied - Skills project is now referenced

### Issue: XML error in .csproj
**Cause:** Mismatched XML tags
**Fix:** ✅ Already applied - Tags now match

### Issue: Build fails due to path space
**Cause:** Folder name "DNA-platform Cat" has a space
**Fix:** Use the provided .bat scripts or PowerShell

### Issue: "Could not find project"
**Cause:** Wrong path
**Fix:** Make sure you're in the correct directory

## Files Created/Modified

### New Files
- ✅ `src/06_Skills/DNAPlatform.Skills.csproj`
- ✅ `src/06_Skills/SkillInterfaces.cs`
- ✅ `src/06_Skills/SkillRegistry.cs`
- ✅ `src/06_Skills/BuiltIn/MsgBoxAlertSkill.cs`
- ✅ `src/06_Skills/BuiltIn/DataTransformSkill.cs`
- ✅ `src/06_Skills/BuiltIn/ValidationSkill.cs`
- ✅ `src/06_Skills/BuiltIn/MergeResultsSkill.cs`
- ✅ `src/06_Skills/Bioinformatics/FastaLoaderSkill.cs`
- ✅ `src/06_Skills/Bioinformatics/SequenceAlignerSkill.cs`
- ✅ `src/06_Skills/README.md`
- ✅ `src/lib/components/MsgBoxDemo.svelte`
- ✅ `src/lib/components/MsgBoxResult.svelte`
- ✅ `build-skills.bat`
- ✅ `test-skills.bat`

### Modified Files
- ✅ `src/05_DevUI/DNAPlatform.DevUI.API/DNAPlatform.DevUI.API.csproj`
- ✅ `src/05_DevUI/DNAPlatform.DevUI.API/Program.cs`
- ✅ `src/05_DevUI/DNAPlatform.DevUI.API/Controllers/SkillsController.cs`
- ✅ `src/routes/skills/+page.svelte`

## Summary

All issues have been fixed:
1. ✅ Created Skills project file
2. ✅ Fixed XML tag mismatch
3. ✅ Added project reference to API
4. ✅ Updated namespaces
5. ✅ Created build/test scripts

**Next Step:** Run `build-skills.bat` to build and test!