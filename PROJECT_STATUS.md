# Project Status

_Last updated: 2026-10-08_

## Current step (2026-10-08; resume here)
**Part 2, stage 7 (Holder): DONE**, play-tested by the user ("works great") and committed. **Next: stage 8 (Game flow).** Start it by briefly presenting 2-3 decisions, then ask "you or me?".

### Stage 7 summary (decisions 1A, 2A, 3A by the user; all code by Claude at the user's request)
- **`Core/Holder/HolderModel`:**
  - 5 fixed slots, no sliding.
  - `TryAdd(shooter, out slot)` gives the leftmost free slot and returns **false = 6th arrival = lose**.
  - `Take(slot)` frees a slot. Both set `Shooter.Location`.
- **1A:** the slot is assigned the moment the rider exits its lap; the hop is only animation.
- **2A:** the slot is freed only when the holder shooter actually leaves (gate open, hop starts), so nothing can land on a waiting shooter.
- **3A:** on a loss, the tester logs "LOST", shakes the 6th shooter and freezes until R/1/2/3. **Stage 8 replaces this with the popup.**
- **`View/HolderView`:**
  - Slot transforms (`Holder_0..4`) and the view in each slot.
  - `TryPickSlot` is distance math against `pickRadius`, plus a gizmo.
  - `Place`, `TakeView`, `Shake` and `Clear`.
- **Tester:**
  - Pending taps are a FIFO of `PendingTap {FromHolder, Index}` (a queue column or a holder slot). Holder taps can't be doubled.
  - `ReturnToHolder` replaces the old placeholder hop to the `5/5` box. The `conveyorStart` field was removed.
- **Tests:** a Holder suite with 11 checks.

### Stage 6 summary (decisions 1A, 2, 3A by the user; all code by Claude at the user's request)

### Stage 6 summary (decisions 1A, 2, 3A by the user; all code by Claude at the user's request)
- **`Core/Shooting/FiringPoints`:**
  - Per level, one point per line per side, at the cell centres, built in lap order (so already sorted).
  - The distance on each side comes from the cell centre's x (bottom/top) or y (right/left).
  - Each rider keeps `NextPoint`, the index of its next point.
- **`Core/Shooting/ShootingSystem.Fire(Shot[])`:**
  - For each rider, it processes every point it has passed (and that comes before `LapLength`): `board.TryFire` with the shooter's color, then `BulletsLeft--`.
  - On the last bullet: `Location = Destroyed` and `conveyor.RemoveAt(i)`.
  - Writes `Shot {Shooter, X, Y, Distance, ShooterEmptied}` into a caller buffer. If the buffer is full, the rest fire next frame.
- **`ConveyorModel` changes:**
  - `Update` is split into `Advance` + `CollectExits`.
  - **Frame order:** Advance → Fire → CollectExits, so points just before the lap end still fire.
  - New: `RemoveAt`, `RiderRef` (ref return) and `ConveyorRider.NextPoint`.
- **`ConveyorPath`:** got `Left`/`Right`/`Bottom`/`Top` getters.
- **`View/BulletSystem`:**
  - Pooled sprites (FX layer) moved in one update loop, with a swap-remove array.
  - When a bullet lands, its cell pops (`BoardView.Pop`).
  - `InFlight` is for the win check in stage 8.
- **Tester:**
  - `ShowShot` fires the bullet from `path.Evaluate(shot.Distance)` to the cell. An emptied shooter is removed from `ConveyorView` (which drops its board) and shrinks away.
  - `BuildFiringPoints` runs after `BoardView.Build`.
  - The Space random-shot key was removed (it broke bullets = cells).
- **Known:**
  - A dying shooter snaps to its base rotation (polish, stage 9).
  - On big grids, the bottom row(s) can be below the lap end on the left side, so they can't be shot from the left (only a limitation; they're still reachable from below).
  - The win check must wait for `bullets.InFlight == 0` (stage 8).

### Stage 5 summary (who wrote what)

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
  - The exit hop goes to the 5/5 box. `Shooter.Location` is still not set on exit (stage 7).
  - **Path values** (tester Inspector): left -4.75, right 4.75, bottom -1.77, top 8.26, lapEndGap 1.6, speed 5, minSpacing 1.2, hopTime 0.4.
- **Tests:** `cd Tools/CoreTests && dotnet run` → Level 14, Board 17, Queue 7, Conveyor 33, Shooting 11, Holder 11 checks, all passing.
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
- [x] **6. Shooting** (done 2026-10-08): firing points per row/column, matching the exposed cell, removing the cell on fire, pooled bullets, destroying the shooter when its bullets run out
- [x] **7. Holder** (done 2026-10-08): 5 slots, a shooter returns after its lap, shooters can be selected from the holder, the 6th arrival loses
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
