Docs root: E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system

# Mandatory project standards

The `.cursor/rules/` files below are mandatory standards for all Unity work, not optional guidance. They override convenience, speed of generation, and consistency with older code. A deviation is allowed only through the exception process in `unity-core.mdc` section 7, with the justification recorded.

Documentation, plans, comments, and recorded technical debt do not satisfy these standards. Only migrated, verified implementation does. Every report must state compliance status using the four statuses in `unity-core.mdc` section 3.

| Path | Contents | When to read |
| --- | --- | --- |
| `.cursor/rules/unity-core.mdc` | Mandatory standards: asset references, existing violations, compliance, serialization, boundaries, validation, exceptions, principles | Always |
| `.cursor/rules/unity-workflow.mdc` | Mandatory process: inspection, planning, scope, implementation, testing, reporting | Always |
| `.cursor/rules/unity-source-control.mdc` | Mandatory source control: when commits are allowed, what each commit contains, protecting uncommitted work, backups | Always |
| `.cursor/rules/unity-ui-toolkit.mdc` | UI Toolkit: UXML and USS authoring, static vs dynamic UI, lifecycle, reusable components, UI validation | Any UI Toolkit or UI presentation task |
| `.cursor/rules/advisor.mdc` | Advisor stance: accuracy over agreement, confidence tags | Always |
| `AGENTS.md` | Project-wide agent instructions and documentation index | Every relevant session |
| `GDD.md` | Game design and gameplay rules | When designing or implementing gameplay |
| `TECHNICAL_DESIGN.md` | Architecture, dependencies and system ownership | When designing or changing systems |
| `ROADMAP.md` | Milestones, priorities and task status | When planning or choosing work |
| `TEST_STRATEGY.md` | Testing requirements and definition of done | When implementing or verifying features |
| `PROJECT_CONTEXT.md` | Current state, known issues and next steps | At session start, or when context is needed |
| `FEATURE_SPECS\*.md` | Detailed requirements for individual features | When working on the relevant feature |
| `DECISIONS\*.md` | Architectural decisions and rationale | When making related technical decisions |
| [Second Life](https://app.notion.com/p/Second-Life-408b3d478d4f80708da5db5176b7d3c3) | Long-form design the other agent edits | When a task affects gameplay, world rules, or the toolchain |
| [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7) | Questions and later answers | When filing or answering an inquiry that would change what gets built |

`<docs>` is the docs root above. Every bare `.md` name in this file is under it.

# Project Instructions

Process, scope, testing, and reporting are `unity-workflow.mdc`, which loads every turn. Do not restate it here. What follows is only what that file does not carry.

## Reading rules

- `PROJECT_CONTEXT.md` at session start.
- `GDD.md` when a task affects gameplay or game design.
- `TECHNICAL_DESIGN.md` when a task affects architecture or system boundaries.
- The relevant feature spec before implementing a substantial feature.
- Notion Story Bible and Systems & Mechanics Reference when a task affects gameplay or world rules. Confirmed text is design intent; provisional and open text is not a requirement.
- Notion Development Tools page when a task affects the toolchain.

## Documentation duties

- Update the relevant feature specification when requirements change.
- Record significant architectural decisions in `DECISIONS\`.
- Keep `ROADMAP.md` and `PROJECT_CONTEXT.md` consistent with actual progress.

## Two doc homes

Both copies are intentional. The other agent edits Notion and cannot be assumed to reach the Unity repo. Do not delete either copy. Do not paste a whole Notion page into the repo on every change.

| Home | Who edits it | What it holds |
| --- | --- | --- |
| [Second Life](https://app.notion.com/p/Second-Life-408b3d478d4f80708da5db5176b7d3c3) | The Notion agent | Long-form design under this hub, except [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7). The pages named in this file are not a closed list. |
| `<docs>` | This agent | Implementation-facing GDD, technical design, roadmap, tests, project context, feature specs, decisions |
| [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7) | Either agent | Questions and later answers. Not a design document |

- When the user says to review the Notion docs, or asks for the gaps, connections, consistency, or inquiries in them, read every page in the connected Notion workspace except AI to AI and the pages under it. Open the Second Life System hub and follow every child and nested page except that branch. Search the workspace so an unlinked page is still read. Story Bible, Systems & Mechanics Reference, and Development Tools are not the whole set. Characters & Factions, its profiles, and any later page are included.
- A Notion recommendation is not adopted in the Unity project until this agent records it in the repo technical design or a decision file.
- When the copies disagree, name both sources. Do not silently pick one. Post the conflict on AI to AI.
- Post on AI to AI only when a missing decision would change what gets built. Do not wait in the same session for the answer. Do not invent the answer.
- When answering an inquiry: read the live pages named in its Read before answering property on the inquiry table, answer from those pages, and say so if they changed after the row was written. When filing one: fill that property with a Notion page link for each page, and name the section the question uses. Page links or section names go in the property only — never a typed title, a phrase like "the docs", or a list in the row body.
- A confirmed rule is working canon, not untouchable. Filing and answering both follow the how-to's Challenging working canon test, and the reply is labelled Keep, Reconsider, or Replace. Do not keep a rule because an earlier reply agreed with it. The template is [How to use this page](https://app.notion.com/p/408b3d478d4f80a5809ad7f1ef4bde8a).
- After an answer changes a rule the project must follow, update the matching repo file. After a code change changes a rule, update the repo file and post a note on AI to AI so the Notion agent can update the long-form page.
