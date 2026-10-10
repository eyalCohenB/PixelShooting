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
    - **Board view:**
      - I chose pooled sprites over a single texture. Claude wrote `BoardView` and a `BoardTester` and explained them.
      - Pop uses no callback, to avoid closure allocations.
      - I play-tested it and tuned the area size to 7.8 so cells sit inside the sand.

  - **Stage 4, queue:**
    - I chose a `Shooter` class, an array with a `top` index per column, math-based tap picking, and 3 visible rows.
    - I said "just implement it", so Claude wrote `Shooter`, `QueueModel` (harness: 7 checks), `ShooterView`, `ShooterViewPool`, `QueueView` and `GameplayTester`.
    - I made the shooter prefab and play-tested it.
    - Claude caught that `Renderer.sharedMaterials` allocates on every call, and cached it.

  - **Stage 5, conveyor (designed only, day 1):**
    - I chose a computed rounded-rect path, the same speed for all riders plus an entry gap (so overlap can't happen by construction), and capacity reserved at tap time.
- **End of day 1, how the way I worked changed:**
  - I started in strict learning mode (I write, Claude mentors).
  - As the day went on I delegated more and more ("just implement it") and asked for shorter answers.
  - Claude kept explaining every piece it wrote, so I can still explain it in the interview.
  - Claude saved these preferences to its memory and to `CLAUDE.md`, so the next session continues the same way.
  - Claude moved its ad-hoc test harness into the repo (`Tools/CoreTests`), so the tests survive between sessions.

- **Day 2 (2026-10-06), stage 5 conveyor: I switched to writing the code myself.**
  - I wanted to write the gameplay code myself so I can explain it. But I'm not very experienced in C#/Unity, and I kept getting stuck.
  - So Claude now acts as a guide:
    - It splits each stage into small steps, one file each.
    - For each step it gives a short explanation of the idea, a skeleton with numbered TODOs, and concrete hints.
    - Then it reviews what I wrote.
  - **`ConveyorPath`:** I wrote it from the skeleton. Claude's review found 2 bugs and explained them:
    - `left = left;` assigned the parameter to itself, because the parameter hides the field. It needed `this.left`.
    - The width was negative (`left - right`).
  - Claude also suggested clamping the distance instead of logging an error and returning `Vector2.zero`, because the last frame of a lap can overshoot `Length`.
  - I chose sharp corners over rounded ones for simplicity.
  - **Verification:** Claude added `Vector2`/`Mathf` to the test shim and wrote a `ConveyorTests` suite (corners, the direction of each side, the full lap, clamping). 11/11 pass.
  - **Next:** I write `ConveyorModel` from Claude's skeleton (capacity reserved on tap, a waiting list, the entry gate, exits).

- **Day 3 (2026-10-08): stage 5 finished.**
  - **`ConveyorModel`:**
    - I wrote it from Claude's skeleton. Claude reviewed it (no logic bugs) and added harness tests: the gate, FIFO order, exits, and a 20k-step random simulation that checks gaps and allocations.
    - While planning the view, Claude found two overlap holes in our design:
      - Shooters landing while the gate was closed would stack at the start.
      - A rider finishing its lap could meet a new rider at the start corner, because the gate only looked ahead.
    - It proposed **"wait in place"**: a tap only reserves; the shooter stays in its spot until the gate opens; the hop counts as part of the ride (negative distance); and the gate checks both neighbours.
    - I asked why a wait is needed at all if taps happen at different times. Claude explained that the gap equals speed × time between taps, so two fast taps would ride overlapped for the whole lap. The gate just enforces a minimum time between entries.
    - I chose this design and asked Claude to implement it. The tests grew to 30 checks, all passing.
  - **`ConveyorView`:**
    - I wrote it from a skeleton. Claude's review found two compile errors (code outside the loop, and the type name) and two logic bugs (a missing `-90°` in the facing angle, and the base rotation being overwritten on every add).
    - A Unity-only compile error, "Rider is a namespace", came from a global `Rider` namespace in Unity. The struct was renamed `ConveyorRider`.
  - **Lap end:** I sketched on a screenshot where riders should get off (the left side, above the entrance) and where they hop to. Claude added `LapLength` to the model (+3 tests), and the exit hop plus a tuning gizmo to the tester.
  - **Wiring and skateboards:**
    - I asked Claude to write the `GameplayTester` wiring and the skateboard pool and view. Boards are children of their shooter while riding, and pooled with cached callbacks.
    - I made the skateboard prefab, tuned its scale and play-tested it. Everything works as expected.
  - **Verification:** the .NET harness (Conveyor 33/33, every suite passing) and a Unity play-test by me.

- **Day 3 (2026-10-08): stage 6, shooting.**
  - Claude presented 3 decisions. I picked:
    - precomputed firing points per level, with a "next point" index per rider
    - instant logical removal of the cell, with a pooled bullet that animates the hit
    - immediate removal from the conveyor when a shooter runs out of bullets
  - Claude also flagged a limitation of our shortened lap: on big grids, the bottom row can't be shot from the left side.
  - I asked Claude to write everything:
    - `FiringPoints`, `ShootingSystem`, the `ConveyorModel` split (`Advance`/`CollectExits` + `RemoveAt`), `BulletSystem`, and the tester wiring.
    - Claude also removed the old random-shot debug key, because it broke the bullets = cells rule.
  - **Verification:**
    - 11 new harness checks, including a full Level03 simulation that checks bullets left == cells left at every frame, and 0 allocations.
    - One check failed at first. It was a bug in the test (it didn't run long enough to reach the target), not in the code.
    - I play-tested it in Unity: "works like magic".

- **Day 3 (2026-10-08): stage 7, holder.**
  - Claude presented 3 timing decisions. I picked:
    - assign the slot when the rider exits (not when the hop lands)
    - free a slot only when its shooter actually leaves
    - log, shake and freeze on a loss until the stage 8 popup
  - I asked Claude to write it: `HolderModel` (11 harness checks), `HolderView` and the tester changes (pending taps can now come from the queue or the holder; the 5/5-box placeholder was replaced).
  - **Verification:** the harness passes, and I play-tested it in Unity: holder return, re-sending from the holder, and the loss on a 6th arrival.

- **Day 3 (2026-10-08): stage 8, game flow. Part 2 complete.**
  - **My decisions:**
    - I chose to keep the flow in one MonoBehaviour (B) rather than a testable Core `GameSession` (A), for speed.
    - I agreed to the win rule (board cleared, popup after the bullets land) and the popup design.
    - I decided to skip stage 9 (polish) and move on to Part 3.
  - **What Claude wrote:** `GameController` (renamed from the tester), `GamePopup` and the DOTween init.
  - **What I found in testing:**
    - After the last level, Next logged an error and didn't loop. Claude traced it to the catalog still containing the deliberately invalid test level, plus a bug where a failed load wiped the current level. Load now skips invalid entries and wraps.
    - I couldn't lose any level, so I asked for a level where order matters. Claude designed `Level04` and **proved it in the harness by simulating whole games**: winnable (a DFS finds an order), while random tapping loses about 46% of the time. The first design lost 0%, so Claude changed it and re-ran the simulation.
    - The popup looked bad, so Claude restyled it in code (rounded card from a runtime sprite, coloured ribbon, big button).
  - **Bullet bubble:** I asked for a bubble showing each shooter's bullets left. Claude added it to `ShooterView` (built in code; it stays upright on the conveyor).
  - **Verification:** the harness passes, including the Level04 suite. I play-tested win, lose, Next/Retry, wrapping and the bubbles.

- **Day 4 (2026-10-10): Part 3 bonus, loop finale.**
  - Claude restated the spec and asked 4 questions, since it hasn't seen the reference animation. My answers:
    - shooters board one by one
    - the speed jumps, with a configurable multiplier (default 3)
    - riders keep looping
    - it triggers only when the count drops to 5
  - Claude proposed reusing the existing pending/gate machinery for boarding, and a loop mode in `ConveyorModel` (wrap instead of exit).
  - Claude also noticed that wrapping breaks the front-to-back rider order the gate relied on, and generalized the gate to a circular-gap check against every rider (identical in normal mode).
  - I asked Claude to write it.
  - **Verification:**
    - 10 new harness checks, including whole random games run into the finale: every finale won (Levels 2-4).
    - Claude also explained why a win is guaranteed: bullets = cells per color, so some looping shooter always matches an exposed cell.
    - I play-tested it in Unity ("feels good").

- **Day 4 (2026-10-10): extras to make the project stand out.**
  - I asked Claude what would make the game stand out. It proposed, ranked against the grading criteria:
    - a performance proof and a README with diagrams
    - an image→level importer
    - a hint button using the solver
    - a Unity Test Runner integration
    - juice
  - I chose the README and the hint.
  - **README:** Claude wrote it for reviewers, with Mermaid architecture and frame diagrams. The test counts were taken from an actual harness run.
  - **Hint:**
    - Claude wrote `HintSolver` (DFS over sequential laps from the live state, on board snapshots) plus 9 harness checks: following the hints wins every level, and repeat calls allocate nothing.
    - Claude also wrote the button and ring wiring and a shared `UiStyle`.
  - **What went wrong:**
    - I didn't see the button. Claude checked the saved scene and found that the button I was supposed to add wasn't there yet.
    - Claude then added a fallback that built the button in code. I had done the manual setup in the meantime, so I asked it to revert the fallback, and it did.
  - **GitHub prep:** Claude checked the tracked files against `.gitignore`. It ignored and untracked the 10 MB assignment `.unitypackage`, whose contents are already imported under `Assets/PackageAssets`.

- **Day 4 (2026-10-10): submission package.**
  - The assignment asks for a `.unitypackage` named `<name>_unity_home_exercise`.
  - **Two gaps Claude flagged:**
    - A package only carries `Assets/`. The game's sorting layers live in `ProjectSettings`, and the docs live at the repo root.
    - Claude caught that scenes reference sorting layers **by ID**, so recreating them by name alone would still break the draw order.
  - **What Claude wrote:**
    - An Editor export menu that copies the docs in and also exports the sorting-layer and scene-list settings.
    - A safety-net editor script that recreates missing layers with the original IDs.
    - A README section on opening the package.
  - **My part:**
    - At first I couldn't find the package to import. It hadn't been exported yet, so Claude walked me through exporting first, then importing into a fresh project.
    - I exported it, imported it into a fresh Built-in project, and verified that it plays.

<!-- Template for each part:
### Part N: <title> (<date>)
- **What I asked:**
- **What the AI produced:**
- **Key design decisions & alternatives considered:**
- **What I reviewed / changed / rejected:**
- **Verification (tests, profiler, playtest):**
-->
