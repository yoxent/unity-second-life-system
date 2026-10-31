Docs root: E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system

| Path | Contents | When to read |
| --- | --- | --- |
| `.cursor/rules/*.mdc` | Reusable instructions and scoped coding constraints | Automatically, according to rule configuration |
| `AGENTS.md` | Project-wide agent instructions and documentation index | Every relevant agent session |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\GDD.md` | Game design and gameplay rules | When designing or implementing gameplay |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\TECHNICAL_DESIGN.md` | Architecture, dependencies and system ownership | When designing or changing systems |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\ROADMAP.md` | Milestones, priorities and task status | When planning or choosing work |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\TEST_STRATEGY.md` | Testing requirements and definition of done | When implementing or verifying features |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\PROJECT_CONTEXT.md` | Current state, known issues and next steps | At session start or when context is needed |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\FEATURE_SPECS\*.md` | Detailed requirements for individual features | When working on the relevant feature |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\DECISIONS\*.md` | Important architectural decisions and rationale | When making related technical decisions |
| [Second Life](https://app.notion.com/p/Second-Life-408b3d478d4f80708da5db5176b7d3c3) | Long-form design the other agent edits | When a task affects gameplay, world rules, or the toolchain |
| [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7) | Questions and later answers | When filing or answering an inquiry that would change what gets built |

# Project Instructions

## Before Working
- Inspect the existing Unity project and relevant code before proposing changes.
- Read E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\PROJECT_CONTEXT.md for the current project state.
- Read E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\GDD.md when a task affects gameplay or game design.
- Read the Notion Story Bible and Systems & Mechanics Reference when a task affects gameplay or world rules. Confirmed text is design intent. Provisional and open text is not a requirement.
- Read E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\TECHNICAL_DESIGN.md when a task affects architecture or system boundaries.
- Read the Notion Development Tools page when a task affects the toolchain. A recommendation there is not adopted until it is recorded in the repo technical design or a decision file.
- Read the relevant feature specification before implementing a substantial feature.

## Implementation
- Follow the applicable rules in .cursor/rules/.
- Prefer incremental changes over broad rewrites.
- Do not invent gameplay requirements or silently change approved decisions.
- Do not modify unrelated systems or assets without justification.
- Ask for clarification when a missing decision materially affects the implementation.

## Verification
- Follow E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\TEST_STRATEGY.md.
- Stay inside the current ROADMAP.md gate. Do not start the next gate until its exit criteria are met.
- A passing EditMode suite does not mean a scene flow was played. A class existing does not mean the feature works.
- Report what was changed, what was tested, and what remains unverified.
- Never claim that Unity runtime behavior was verified unless it was actually tested.

## Documentation
- Update the relevant feature specification when requirements change.
- Record significant architectural decisions in E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system\DECISIONS\.
- Keep ROADMAP.md and PROJECT_CONTEXT.md consistent with actual progress.

## Two doc homes

Both copies are intentional. The other agent can edit Notion and cannot be assumed to reach the Unity repo. Do not delete either copy, and do not paste a whole Notion page into the repo on every change.

| Home | Who edits it | What it holds |
| --- | --- | --- |
| [Second Life](https://app.notion.com/p/Second-Life-408b3d478d4f80708da5db5176b7d3c3) | The Notion agent | Long-form design under this hub, except [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7). The pages named in this file are not a closed list. |
| `E:\Projects\Docs\unity-second-life-system\Unity\unity-second-life-system` | This agent | Implementation-facing GDD, technical design, roadmap, tests, project context, feature specs, and decisions |
| [AI to AI](https://app.notion.com/p/AI-to-AI-408b3d478d4f80a99d8dd105c882f6d7) | Either agent | Questions and later answers. Not a design document |

- Read the Notion pages when a task affects gameplay, world rules, or the toolchain. Confirmed text is design intent. Provisional and open text is not a requirement.
- When the user says to review the Notion docs, or asks for the gaps, connections, consistency, or inquiries in those docs, read every page in the connected Notion workspace except AI to AI and the pages under it. Open the Second Life System hub and follow every child and nested page except that branch. Search the workspace so a page that is not linked on the hub is still read. Story Bible, Systems & Mechanics Reference, and Development Tools are not the whole set. Characters & Factions, its profiles, and any later page are included.
- A Notion recommendation is not adopted in the Unity project until this agent records it in the repo technical design or a decision file.
- When the copies disagree, name both sources. Do not silently pick one. Post the conflict on AI to AI.
- Post on AI to AI only when a missing decision would change what gets built. Do not wait in the same session for the answer. Do not invent the answer.
- When answering an inquiry, read the live pages named in its Read before answering property before answering. That property is on the inquiry table. Answer from those pages. If they changed after the row was written, say so. When filing an inquiry, fill that property with a Notion page link for each of those pages, and name the section the question uses. A typed title or page name is not enough. A phrase like "the docs" is not enough. Do not write that list in the row body. The property is the only copy. A confirmed rule is working canon, not untouchable. Filing and answering both follow the how-to's Challenging working canon test. Label the reply Keep, Reconsider, or Replace. Do not keep a rule because an earlier reply agreed with it. The how-to on that page is the template: [How to use this page](https://app.notion.com/p/408b3d478d4f80a5809ad7f1ef4bde8a).
- After an answer changes a rule the project must follow, update the matching repo file. After a code change changes a rule, update the repo file and post a note on AI to AI so the Notion agent can update the long-form page.
