# Project Status

_Last updated: 2026-10-05_

## Current step (end of day 1, 2026-10-05; resume here)
**Part 2, stage 5 (Conveyor): decisions made (A/A/A), no code yet.**
- **Next action:** ask the user "Should I implement stage 5, or do you want to write part of it?", then build it using the sketch in `CLAUDE.md` → Conveyor:
  1. `ConveyorPath` and `ConveyorModel` (Core), with a `ConveyorTests` suite in `Tools/CoreTests`.
  2. `ConveyorView`, skateboard pool and `CapacityLabel` updates (View).
  3. Wire it into `GameplayTester`. The user does the Unity setup (skateboard prefab etc.) and play-tests.
- After that: stage 6 (Shooting), 7 (Holder), 8 (Game flow), 9 (Polish/Profiler).
- **Git:** `master`, one commit per stage. Last stage commit: `856615b` "Shooter queue with pooled shooter views". End-of-day doc updates and `Tools/CoreTests` are uncommitted unless the user said to commit.
- **Tests:** `cd Tools/CoreTests && dotnet run` → Level 14, Board 17, Queue 7 checks, all passing.

## Done
- [x] Setup: rubric, documentation rules, asset inventory, learning mode
- [x] Core Mechanics overview and Level JSON note received and clarified
- [x] **Part 1: Scene Setup, complete (2026-10-05)**
  - Pipeline switched from 2D URP to Built-in, because the package shader is Built-in only.
  - Android build target, Portrait only, sorting layers Background/Board/Gameplay/FX.
  - `Assets/_Project/` folder structure. Scene: `_Project/Scenes/Game.unity`.
  - World layout under the BG: 5 holders (x = ±3.76, ±1.88, 0; y = -4.16), `ConveyorStart` with `CapacityLabel` (TMP), and the anchors `Conveyor`, `Queue` (y -6.5) and `BoardArea` (y 3.26).
  - `CameraFitter` (content rect -5.6, -10, 11.2, 19.4, fit to the safe area) and `HUDCanvas` with `SafeArea` / `SafeAreaPanel`.
  - Tested in the Device Simulator on an iPhone with a notch, a 16:9 phone and an iPad.
  - `.gitignore` added and `git init` run.

## Part 2 stage plan (one stage at a time: design → decide → implement → review → docs)
- [x] **0. Spec breakdown:** questions answered on 2026-10-05
- [x] **1. Tweening:** **DOTween** chosen, installed (modules: UI, TMP, Sprite), and the smoke test passed. Riding the conveyor and bullet flight are **not** tweens; the models drive them. **To do in stage 8:** call `DOTween.Init(...).SetCapacity(...)` in a bootstrap.
- [x] **2. Level data:** JSON schema, DTO, loader, validation (bullets = cells), 2–3 sample levels
- [x] **3. Board:** `BoardModel` (grid, exposed-cell queries) and `BoardView` (cells scaled to fill `BoardArea`)
- [x] **4. Queue:** `QueueModel` (only the top shooter is selectable, the rest shift up) and its view and input
- [ ] **5. Conveyor:** path, capacity 5, no overlap, lap tracking, the `free/5` label, skateboards. **Decided A/A/A, not built.**
- [ ] **6. Shooting:** firing points per row/column, matching the exposed cell, removing the cell on fire, pooled bullets, destroying the shooter when its bullets run out
- [ ] **7. Holder:** 5 slots, a shooter returns after its lap, shooters can be selected from the holder, the 6th arrival loses
- [ ] **8. Game flow:** state machine, win and lose, restart, HUD popup
- [ ] **9. Polish and verification:** animation pass, Profiler (GC alloc = 0 in gameplay), edge-case playtests, docs

## Stage 0 answers (the user, 2026-10-05)
1. **Lose:** the moment a **6th shooter arrives** at the full holder. This is the spec's rule, and it replaces the user's earlier "no one can shoot" rule.
2. "Select from the holder, not only from the conveyor" was a typo for "**queue**".
3. **Label:** shows **free/5** (5/5 when the conveyor is empty).
4. **Conveyor full:** tapping a shooter plays a **small shake** and nothing else happens.
5. **Lap:**
   - Shooters enter at `ConveyorStart` and go **counter-clockwise**.
   - The lap ends at the end of the conveyor, and the shooter then **hops to the holder**.
   - **Confirmed:** from the bottom-left corner they go right along the bottom, up the right side, left along the top, and down the left side, ending back at the bottom-left. The bottom side is the first one a shooter passes, so its first shots go **up** into the columns.
6. **Bullets = cells per color**, and in total. The loader rejects levels that fail this, so every level can be finished.
7. **Empty cells** are allowed in the grid.
8. **Holder:** shooters **keep their slots**. There's no sliding to fill gaps.
9. **Skateboard:** used only on the conveyor. It's removed when the shooter moves to the holder or is destroyed, and goes back to a pool.

## Resolved by Part 2
- Holder: **5 slots**, fixed.
- A shooter with 0 bullets **is destroyed** when it runs out, before finishing the lap.
- The JSON defines the order and number of the queue columns.
- Win: the board and all shooters are cleared. **Total bullets = total cells**, always.
- Grid cells scale to fill the grid area. The conveyor has a constant size, whatever the grid size.
