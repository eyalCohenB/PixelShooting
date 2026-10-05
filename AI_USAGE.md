# How I Used AI (Claude Code) on This Assignment

The assignment was built with Claude Code (Claude Opus 5.5) as a pair programmer. This log records, for each part, what I asked, what the AI produced, the key decisions, and what I reviewed or changed myself.

## Overall approach
- I fed the assignment to Claude **part by part** instead of all at once, so each step could be designed, reviewed and verified separately.
- I set up working rules from the start: keep `CLAUDE.md`, this file and `PROJECT_STATUS.md` updated, and document how core systems work in the code.
- I gave Claude the grading rubric up front, so design choices were made with performance (GC, boxing, execution time) and smoothness in mind from the beginning.

## Log

### Setup (2026-10-04)
- **What I did:**
  - Shared the evaluation criteria and defined the documentation rules.
  - Pointed Claude at the project directory.
- **What the AI did:**
  - Inspected the provided `.unitypackage` without importing it and listed its assets.
  - Read the gameplay mockup (`MOCK.png`): a conveyor loop around a play area, a conveyor start with a capacity counter, and 5 holder slots.
  - Turned the rubric into concrete engineering conventions: no per-frame allocations, no boxing, and slot reservation on the conveyor to prevent overlap.
  - Created the three tracking documents.
- **What I corrected:** Claude first created the docs in the wrong folder. I pointed it to the right project directory, and it moved them.
- **Session handoff:**
  - I moved to a new Claude Code session started from the project folder.
  - Before that, I had Claude add a "Start here" section to `CLAUDE.md`, so the new session picks up the full context automatically. `CLAUDE.md` acts as persistent project memory.

### Switching to learning mode (2026-10-04)
- **What I decided:** I want to write most of the code myself so I learn from it. Claude works as a mentor, not as the implementer.
- **Rules I added to `CLAUDE.md`:**
  - Claude implements nothing unless I ask.
  - When I ask, Claude suggests ways to implement something, with tradeoffs.
  - Claude gives detailed step-by-step Unity guides when I ask how to do something in the editor.

### Part 1: Core Mechanics (2026-10-04, design in progress)
- **What I asked:** I gave Claude the Part 1 spec (board, shooters, queue, holder, loopback, lose condition).
- **What the AI produced:**
  - A summary of the spec in `CLAUDE.md`.
  - A list of ambiguities in the spec (open questions in `PROJECT_STATUS.md`).
  - A proposed logic/view architecture with options and tradeoffs. No code; I implement it.
- **Clarifying the spec:** Claude asked about the ambiguous rules before designing anything, and I answered them:
  - What "exposed" and "blocked" mean.
  - One lap, firing at the middle of each line without stopping.
  - One color per shooter, and the "5/5" is the conveyor capacity.
  - The exact lose condition.
- **Level JSON part:** I added the level-loading part. The JSON schema isn't specified, so we design it ourselves. Claude keeps the remaining questions open in `PROJECT_STATUS.md` until later parts answer them.
- **Key design decisions & alternatives considered:** _(fill in once confirmed)_
- **What I reviewed / changed / rejected:** _(fill in)_
- **Verification:** _(fill in)_

### Part 1: Scene Setup (2026-10-04 to 2026-10-05, complete)
- **What I asked:** I gave Claude the Scene Setup part: match the mock, responsive, SafeArea, portrait only.
- **What the AI did before suggesting anything:** it inspected the project I had created and the assets, and found problems I hadn't noticed:
  - I had used the **2D URP template**, but the provided shooter shader is **Built-in only**, so it would render pink.
  - The track is **baked into the background image**, so the gameplay layout has to be locked to the background's coordinates.
  - Orientation was still Auto Rotation.
- **Key design decisions & alternatives considered:**
  - World-space gameplay with an orthographic camera fitted to the safe area, versus doing everything in a UI Canvas.
  - A Canvas only for the HUD, with a SafeArea panel.
  - **Camera fitting:**
    - I chose a serialized `Rect` with a gizmo for the content area, over marker transforms or the BG bounds.
    - I chose to recalculate in `LateUpdate` only when the screen or safe area changes.
    - The fit math lives in a pure static class so it can be tested.
    - Claude walked me through the math with a worked example on a phone with a notch.
    - I decided this was boilerplate, not something I wanted to practise on, so I asked Claude to write `CameraFitMath` and `CameraFitter`. It explained them afterward.
    - From then on, infrastructure scripts are delegated to Claude. I write the gameplay systems myself.
  - **HUD safe area:** Claude wrote `SafeAreaPanel`. It converts `Screen.safeArea` into RectTransform anchors, with the same "recalculate only on change" pattern as the camera.
  - **Workflow note:** Claude also reviewed the scene from the saved `.unity` YAML. It caught that the BG was behind the camera, a non-uniform BG scale, a renamed camera, and a parent object scaled to store a size.
- **What I reviewed / changed / rejected:** _(fill in)_
- **Verification:**
  - I tested in the Device Simulator: a notched iPhone, a 16:9 phone and an iPad.
  - Claude did a final review of the saved scene YAML. It confirmed the hierarchy, sorting layers, Canvas settings and `CameraFitter` values, and that the earlier issues were fixed.

### Part 2: Core Gameplay (2026-10-05, in progress)
- **What I asked:**
  - I sent the whole Part 2 spec and asked to go through it slowly, stage by stage, keeping strictly to the guidelines.
- **What the AI did first, before any design:**
  - Restated every requirement.
  - Compared it with the earlier clarifications and found a **conflict**: the spec's lose rule (a 6th shooter arrives at the holder) differs from the rule I had described earlier.
  - Found a probable typo in the spec ("from the conveyor" instead of "from the queue").
  - Listed the remaining ambiguities.
  - Split Part 2 into 10 stages.
- **Stage 0 answers:**
  - I resolved the lose-rule conflict in favour of the spec (a 6th shooter arriving).
  - I confirmed the typo.
  - I chose the label format, shake feedback, counter-clockwise lap, per-color validation, empty cells allowed, fixed holder slots, and skateboards only on the conveyor.
- **Key design decisions & alternatives considered:**
  - **Stage 1, tweening:**
    - The options were DOTween, a custom zero-allocation tween runner, coroutines (rejected: they allocate per call) and Animator (rejected: positions are only known at runtime).
    - **I chose DOTween**, with the GC rules: recycling, preallocated capacity, no capturing closures.
    - Claude pointed out that riding the conveyor and bullet flight should **not** be tweens. The model drives them, so there's one source of truth.
    - I installed it by following Claude's step-by-step guide and verified it with a smoke test.
  - **Stage 2, level data:**
    - I accepted a flat-array JSON schema. `JsonUtility` can't read nested arrays, so that drove the format.
    - Palette indices for colors, -1 for empty cells, and the top row first in the file.
    - Levels are TextAssets in a `LevelCatalog` ScriptableObject. I rejected `Resources` (string paths) and `StreamingAssets` (async loading on Android).
    - The validator enforces bullets = cells per color, so every level can be finished.
    - We agreed on the `LevelData` shape together:
      - It's immutable, and restarting rebuilds the models from it.
      - Accessor methods instead of `IReadOnlyList`, to avoid boxed enumerators.
      - An `sbyte` grid with y = 0 at the bottom.
    - **Split:** Claude wrote the DTO, loader, catalog, `LevelData` and sample levels, including a deliberately broken one generated by a script that checks the counts. I write the validator.
    - **Validator:** I wrote the structural checks, then asked Claude to finish it. Claude found and explained bugs in my part:
      - I used `or` instead of `||`. `or` is a pattern combinator, so it doesn't compile here.
      - My `foreach` went over the queues instead of the shooters inside them.
      - `cells` and `shooters` weren't checked for null.
      - The error messages were generic.
      - `errors.Count > 0` also counted errors that were already in the list.
    - Claude then added the per-color cell and bullet counting.
    - **Verification:** Claude ran the Core level code outside Unity, in a .NET test harness with a small UnityEngine shim, on 14 cases. All passed. Claude also wrote a `LevelLoadTester` so I could check it in Unity. I ran it and confirmed the same results there.
- **What I reviewed / changed / rejected:** _(fill in)_
- **Verification:** _(fill in)_

  - **Stage 3, board model:**
    - I picked the options (A1, B1, C1, and the lazy depth approach).
    - I started `Reset`; Claude finished the class while I was on a break. Claude explained the bugs in my part:
      - `LevelData` has no public `Cells`.
      - `Clone()` allocates on every restart.
      - The cell count included empty cells.
      - `GetCell` used `y + x` instead of `y * Width + x`.
    - **Verification:** a randomized test compared it with a brute-force scan over 42k queries, plus an allocation check (0 bytes).

<!-- Template for each part:
### Part N: <title> (<date>)
- **What I asked:**
- **What the AI produced:**
- **Key design decisions & alternatives considered:**
- **What I reviewed / changed / rejected:**
- **Verification (tests, profiler, playtest):**
-->
