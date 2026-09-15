# Phase 3: Custom Skills System

> Part of DNA Platform - Bioinformatic Workflow Engine

## Overview

The Custom Skills System enables GUI-triggered backend execution of reusable components. Skills are self-contained units of logic that can be executed from the workflow designer, API, or programmatically.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Frontend (SvelteKit)                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Workflow Designer / Skill Browser                       │  │
│  │  - Drag-and-drop skill nodes                             │  │
│  │  - Configure skill inputs via GUI                        │  │
│  │  - Click "Execute" button                                │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                   │
│                              │ POST /api/skills/{id}/execute    │
│                              ▼                                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Backend API (.NET 8)                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SkillsController                                        │  │
│  │  - GET /api/skills (list all)                            │  │
│  │  - GET /api/skills/{id} (get details)                    │  │
│  │  - POST /api/skills/{id}/execute (execute skill)         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                   │
│                              ▼                                   │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SkillRegistry (Singleton)                               │  │
│  │  - Thread-safe skill storage                             │  │
│  │  - Auto-registers built-in skills                        │  │
│  │  - Executes skills with inputs                           │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Skills Included

### UI Components
| Skill ID | Name | Description | Icon |
|----------|------|-------------|------|
| `msgbox-alert` | Message Box Alert | Configurable alert with type dropdown | 💬 |


## Quick Start

### Execute a Skill via API

```bash
# List all skills
curl http://localhost:5254/api/skills

# Get skill details
curl http://localhost:5254/api/skills/msgbox-alert

# Execute MsgBoxAlertSkill
curl -X POST http://localhost:5254/api/skills/msgbox-alert/execute \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Hello from DNA Platform!",
    "msgType": "info",
    "title": "Greeting"
  }'
```

### Execute via Frontend

```typescript
import { api } from '$lib/services/api';

const result = await api.executeSkill('msgbox-alert', {
  message: 'Analysis complete!',
  msgType: 'success',
  title: 'Workflow Status'
});

console.log(result.data);
```

## MsgBoxAlertSkill Demo

The simplest skill demonstrating GUI-triggered backend execution:

### Input Parameters
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `message` | string | ✅ | - | Message content to display |
| `msgType` | string | ❌ | `info` | Alert type: `info`, `warning`, `error`, `question`, `success` |
| `title` | string | ❌ | `Alert` | Alert title |
| `duration` | int | ❌ | `0` | Auto-dismiss duration in ms |

### Example Response

```json
{
  "success": true,
  "skillId": "msgbox-alert",
  "data": {
    "alert_id": "a1b2c3d4-...",
    "message": "Workflow completed!",
    "type": "success",
    "icon": "✅",
    "color": "#10b981"
  },
  "executedAt": "2026-09-06T12:00:00.000Z",
  "durationMs": 3
}
```

## File Structure

```
src/06_Skills/
├── README.md                    # This file
├── SkillInterfaces.cs           # ISkill, SkillOutput, SkillMetadata
├── SkillRegistry.cs             # Skill registry manager
├── BuiltIn/
│   ├── MsgBoxAlertSkill.cs      # GUI demo skill
│   ├── DataTransformSkill.cs    # Data transformation
│   ├── ValidationSkill.cs       # Data validation
│   └── MergeResultsSkill.cs     # Result merging
└── Bioinformatics/
    ├── FastaLoaderSkill.cs      # FASTA file loader
    └── SequenceAlignerSkill.cs  # Sequence alignment
```

## Next Steps

Phase 3 status: **Complete** (with remaining items below).

Currently complete:
- Custom skills framework (`DNAPlatform.Skills`)
- Built-in + bioinformatics skills
- GUI-triggered skill execution
- Skill “Details” parameter editor
- Custom snippet model + snippet CRUD
- Snippet execute endpoint (hello-world .NET handler)
- “Create Custom Snippet” dialog on `/skills`
- Drag-and-drop designer integrated into `/workflows`
- Designer IO ports from snippet/node metadata
- Click node → inline inspect/edit panel
- Connected services status panel

Phase 3 remaining tasks:
- [ ] Full connection drawing (source port → target port → SVG edge)
- [ ] Workflow execution inside the canvas
- [ ] Export/import workflow JSON with runtime environment metadata
- [ ] Agent-backed nodes in workflows
- [ ] Real custom code execution path for polyglot kernel snippets (hello-world placeholder is live)

## License

MIT License - Part of DNA Platform
### Data Processing
| Skill ID | Name | Description | Icon |
|----------|------|-------------|------|
| `data-transform` | Data Transform | Normalize, standardize, filter data | 🔄 |
| `validation` | Validation | Validate data against rules | ✅ |
| `merge-results` | Merge Results | Combine multiple results | 🔀 |

### Bioinformatics
| Skill ID | Name | Description | Icon |
|----------|------|-------------|------|
| `fasta-loader` | FASTA Loader | Load and parse FASTA files | 🧬 |
| `sequence-aligner` | Sequence Aligner | Needleman-Wunsch alignment | 🔬 |