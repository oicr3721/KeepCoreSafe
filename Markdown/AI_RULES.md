# AI_RULES.md

# AI Development Rules

## Core Goal

When multiple implementations work, prefer the one that is:

* Easier to understand
* Easier to inspect in the Unity Editor
* Easier to configure
* Easier to debug
* Easier to reuse
* Easier to maintain

Do not optimize only for "making the current task work."

---
## Before Writing Code

Before writing code:

1. Read `GAME_DESIGN.md`.
2. Read `GAME_RULES.md`.
3. Read `ARCHITECTURE.md`.
4. Inspect the current project state and the existing implementation relevant to the task.
5. Treat the current files on disk and the current Unity Scene/Prefab state as the source of truth.

### Preserve Current Project State

The project may have been modified manually in the Unity Editor since the previous task.

Before making changes, always inspect the current state of the relevant files, Scenes, and Prefabs.

Do not rely on assumptions about the project's previous state or on what was implemented in an earlier task.

Do not overwrite, revert, or reconstruct Scene/Prefab data based on an older version of the project.

Preserve existing Inspector values, object positions, hierarchy changes, references, and other serialized Editor changes unless the current task explicitly requires changing them.

Only modify the parts of the project that are necessary for the current task.

If an existing value or structure is unrelated to the current task, leave it unchanged.
---

## 1. Editor-Friendly Architecture

The Unity Editor is a first-class development tool.

A developer should be able to select a GameObject or Prefab and understand:

* What it does
* What Components it uses
* What it depends on
* Which settings control it

Prefer explicit and visible dependencies over hidden runtime behavior.

The project should remain understandable even after many future Codex changes.

---

## 2. Component Composition

Use Unity's Component-based architecture.

Separate independent responsibilities into focused Components when this improves clarity or reuse.

For example:

```text
ShopOfferButton
├── Button
├── ShopOfferView
├── ShopOfferMotion
├── LocalizedText
└── TooltipTrigger
```

Do not create a Component simply to split code into more files.

Avoid both:

* Giant Components with many unrelated responsibilities
* Excessive fragmentation into tiny Components

Prefer clear and meaningful responsibility boundaries.

---

## 3. Prefab First

Reusable objects should generally be Prefabs.

If a Component is always required by an object, attach it to the Prefab instead of adding it during runtime.

For example:

```text
ShopOfferButton.prefab
├── Button
├── View
├── Motion
├── LocalizedText
└── ...
```

Runtime code should generally provide dynamic data and initialize state, not reconstruct predefined object structures.

`AddComponent<T>()` is allowed when a Component is genuinely dynamic or runtime-only.

Do not make developers manually assemble normal reusable objects at runtime.

---

## 4. Explicit References

Do not use GameObject names or hierarchy paths as normal dependencies.

Avoid:

```csharp
GameObject.Find("Buttons");
transform.Find("Content/Buttons");
```

Prefer explicit Inspector references:

```csharp
[SerializeField] private SomeComponent target;
```

Use `GetComponent<T>()` when retrieving a Component from the same GameObject is appropriate.

External dependencies should be visible in the Inspector whenever practical.

Required references should be validated and produce a clear error when missing.

---

## 5. Responsibility Separation

Keep responsibilities separated.

For example:

```text
Gameplay
→ Gameplay rules and runtime state

View
→ Visual representation

Motion
→ Animation and movement

LocalizedText
→ Localization of its own TMP_Text

Manager
→ High-level system coordination
```

Gameplay systems should not depend on the internal hierarchy of UI objects.

UI Components should not become owners of gameplay rules.

Visual state should not become the source of truth for gameplay state.

---

## 6. Managers

Managers are allowed for genuine system-level responsibilities.

Examples include:

* Audio management
* Localization management
* Shop/gameplay coordination
* Game state management

However, Managers should coordinate systems rather than micromanage every GameObject, Button, TMP_Text, or animation.

Avoid creating a giant Manager that becomes responsible for unrelated systems.

---

## 7. Localization

Text objects that require translation should use the `LocalizedText` Component.

Prefer:

```text
GameObject
├── TMP_Text
└── LocalizedText
```

over a large UI Manager containing references to many TMP_Text objects and localization keys.

`LocalizedText` should primarily handle:

* Localization key
* Localized text lookup
* Updating its own TMP_Text
* Responding to language changes

When migrating to `LocalizedText`, remove obsolete localization fields, references, and duplicate localization logic.

Do not maintain two competing localization architectures.

---

## 8. Runtime Data vs Object Structure

Separate reusable object structure from runtime data.

Prefab:

```text
Defines what the object is.
```

Runtime data:

```text
Defines what this particular instance currently represents.
```

For example, a Shop Offer Prefab should contain its UI structure, Components, animations, and references, while price/effect/state can be supplied as runtime data.

Do not rebuild object structure merely to provide different runtime data.

---

## 9. Reuse Before Creating

Before creating a new:

* Component
* Manager
* Utility
* UI system
* Animation system
* Localization system

search the existing project for an appropriate implementation.

Prefer extending an existing system when it already owns the responsibility.

Do not introduce duplicate systems without a clear architectural reason.

---

## 10. Refactoring

When replacing an existing implementation:

1. Analyze current usage.
2. Design the new structure.
3. Migrate references and behavior.
4. Verify the result.
5. Remove obsolete implementation.

Do not simply add a new system on top of an obsolete one.

After migration, remove obsolete:

* Fields
* Methods
* Classes
* Manager responsibilities
* Runtime initialization
* Duplicate systems
* Unused references

Do not leave dead architecture "just in case."

---

## 11. Avoid Unnecessary Abstraction

This is a gameplay prototype.

Prefer the simplest implementation that remains maintainable.

Avoid:

* Unnecessary interfaces
* Excessive inheritance
* Premature abstractions
* Unnecessary generic frameworks
* Hidden global state
* Complex architectures without a real need

However:

> **Simple does not mean hardcoded or careless.**

Do not sacrifice maintainability merely to reduce code.

---

## 12. Preserve Existing Behavior

Refactoring should not unintentionally change gameplay behavior.

Unless explicitly requested:

* Preserve gameplay rules.
* Preserve existing player flow.
* Preserve existing data.
* Preserve existing functionality.

Keep architectural changes separate from intentional gameplay changes.

---

## 13. Runtime and Scene Setup

If an object can be configured in the Unity Editor, prefer configuring it in the Scene or Prefab.

Do not unnecessarily create and wire predefined objects during runtime.

Prefer:

```text
Prefab
↓
Instantiate
↓
Provide runtime data
↓
Initialize
```

over:

```text
Create GameObject
↓
Add Components
↓
Build hierarchy
↓
Find references
↓
Wire everything at runtime
```

Runtime construction is acceptable when the object is genuinely procedural, temporary, or dynamic.

---

## 14. Documentation Synchronization

Keep Markdown documentation synchronized with the current project state.

When a task changes:

* Gameplay rules
* System architecture
* Component responsibilities
* Important project structure
* Task scope or status

update the relevant Markdown documents accordingly.

Keep `GAME_DESIGN.md`, `GAME_RULES.md`, `ARCHITECTURE.md`, and `TASKS.md` consistent with the current implementation.

Do not update documentation for purely internal code changes that do not affect documented behavior or architecture.

Do not leave obsolete or contradictory documentation after refactoring.

---

## 16. Completion Checklist

Before considering a task complete:

* [ ] Design documents were read and followed.
* [ ] Existing implementation was inspected first.
* [ ] Existing systems were reused where appropriate.
* [ ] No unnecessary `GameObject.Find` or `Transform.Find` was introduced.
* [ ] External dependencies are visible in the Inspector.
* [ ] Reusable objects use Prefabs where appropriate.
* [ ] Required Components are configured on Prefabs.
* [ ] Responsibilities are clearly separated.
* [ ] No unnecessary Manager or abstraction was introduced.
* [ ] Obsolete implementation was removed after migration.
* [ ] Relevant Scenes and Prefabs were checked.
* [ ] Existing behavior was preserved unless explicitly changed.
* [ ] Relevant Markdown documentation was reviewed after meaningful design or architecture changes.
* [ ] Outdated documentation was updated or removed.
* [ ] GAME_DESIGN.md / GAME_RULES.md / ARCHITECTURE.md / TASKS.md remain consistent with the current implementation.

---


## Final Principle

Always optimize for:

> **A project that Codex can safely modify repeatedly without degrading its architecture, and that a human developer can understand immediately from the Unity Editor.**

When two implementations work equally well, prefer the one that makes the object's **structure, dependencies, and responsibilities more explicit and easier to inspect.**
