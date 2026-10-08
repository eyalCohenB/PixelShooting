# Project Status

_Last updated: 2026-10-08_

## Current step (2026-10-08; resume here)
**Part 2, stage 5 (Conveyor): DONE.** The user play-tested it in Unity and it was committed. **Next: stage 6 (Shooting).** Start it by briefly presenting 2-3 decisions, then guide the user (or implement, if they say so).

### Stage 5 summary (who wrote what)
1. **`ConveyorPath`** (Core), written by the user:
   - Claude's review found `this.` shadowing and a negative width.
   - A sharp rectangle; `Evaluate` clamps.
2. **`ConveyorModel`** (Core):
   - The user wrote the first version (a waiting list at the start).
   - Claude redesigned it to **"wait in place"** after finding two overlap holes:
     - stacking at the start
     - a finishing rider meeting a new rider at the corner
   - **How it works now:**
     - A tap reserves a place, and the shooter waits in its queue/holder spot until `CanEnter`.
     - `Enter` adds the rider at `-approach` (the hop is part of the ride).
     - The gate checks the rider ahead and the one behind.
     - `LapLength` lets the lap end early: riders get off on the left side, above the entrance (`lapEndGap` in the tester).
   - `Rider` was renamed `ConveyorRider` (it clashed with Unity's global `Rider` namespace).
3. **`ConveyorView`** (View):
   - Written by the user and reviewed.
   - It positions riders, faces them toward the board (the normal to the left of the travel direction), and sets the `free/5` label with allocation-free `SetText`.
   - It skips riders still hopping on.
4. **Skateboards**, written by Claude:
   - `SkateboardPool` (prewarm 8; it also holds the pose under the shooter) and `SkateboardView`.
   - A board is a child of its shooter while riding. On exit it scales out where it is and returns to the pool.
   - The user made the prefab and tuned the scale.
5. **`GameplayTester` wiring**, written by Claude:
   - A FIFO of pending taps (columns). `StartPendingHops` runs while `CanEnter`, then `Update` → `Sync`.
   - Shooters that exit hop to `ConveyorStart` and are released. **This is a placeholder: stage 7 hops them to the holder.**
   - There's a gizmo for the path and the lap end, and `ClearConveyor` runs on restart.

- **Known placeholders/notes:**
  - The exit hop goes to the 5/5 box. Stage 6 needs `ConveyorModel.RemoveAt(i)` for shooters that run out of bullets mid-lap. `Shooter.Location` is still not set on exit (stage 7).
  - **Path values** (tester Inspector): left -4.75, right 4.75, bottom -1.77, top 8.26, lapEndGap 1.6, speed 5, minSpacing 1.2, hopTime 0.4.
- **Tests:** `cd Tools/CoreTests && dotnet run` → Level 14, Board 17, Queue 7, Conveyor 33 checks, all passing.
- **Git:** `master`, one commit per stage.

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
- [x] **5. Conveyor:** path, capacity 5, no overlap (wait in place + two-sided gate), lap tracking, the `free/5` label, skateboards. Done on 2026-10-08.
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
