# CLAUDE.md

Project context for Claude Code sessions. Keep this file updated after every completed part.

## Start here (new session)
1. Read this whole file, then `PROJECT_STATUS.md` (current step and checklist) and `AI_USAGE.md` (the log you must keep appending to). Also read the memory index (`MEMORY.md`), which holds user preferences.
2. **Where we are (day 3, 2026-10-08):**
   - **Part 1 (Scene Setup): done.**
   - **Part 2 (Core Gameplay): COMPLETE** (stages 0-8; stage 9 polish was skipped by the user on 2026-10-08).
   - **Part 3 (Bonus, Loop Animation): COMPLETE** and committed (2026-10-10).
   - **Extras (2026-10-10):** `README.md` (architecture diagrams) and a hint button (`HintSolver`). The repo is prepared for GitHub (`*.unitypackage` ignored).
   - **Submission (2026-10-10):** **PixelShooting → Export Submission Package...** (`Assets/_Project/Editor/SubmissionExporter.cs`) builds `<name>_unity_home_exercise.unitypackage`. It includes the docs and the sorting layers; `SortingLayerSetup` recreates missing layers with the original IDs. The user verified it in a fresh project and is submitting.
   - **The first thing to do next session:** ask the user what's next (more spec, interview prep, or the optional cleanups in `PROJECT_STATUS.md`). For new work, keep the same flow: briefly present 2-3 decisions with a recommendation, and ask "you or me?" (on day 3 the user mostly said "you do it").
   - On day 3 the user delegated more again: "you do it" for the model redesign, the tester wiring and the skateboards. Always ask "you or me?" for each step.
3. **How the user wants to work** (updated on day 2; this overrides "Learning mode" below):
   - **From stage 5 on, the USER writes the gameplay code, and Claude guides.** The user isn't very experienced in C#/Unity and gets stuck on big tasks, but wants to be able to explain the code in the interview. So:
     - Split the work into small steps (one file or class each).
     - For each step, give a short "idea" explanation, a skeleton with numbered TODOs, and concrete hints (formulas, C# gotchas such as `this.` field shadowing or struct copies in a `List`).
     - When the user says "check X": review it and list the bugs with explanations and the fixed line. Let the user apply the fix.
     - Claude may write test-harness code (`Tools/CoreTests`, shim additions), run it, and report the results.
   - **Keep answers SHORT.** A few bullets and one recommendation. The user is tired of long messages.
   - **Go stage by stage.** For each stage: briefly present 2-3 decisions with a recommendation and let the user choose. Then guide the user through writing it, as described above. Implement it yourself only if the user explicitly says "just implement it".
   - For Core code: test in the .NET harness (`Tools/CoreTests`, run `dotnet run`). Then give the user short Unity setup steps and things to check.
   - After the user confirms it works in Unity: update the 3 docs, then commit **only when the user says "commit"**. Use one commit per stage and end messages with the Co-Authored-By line.
   - Don't suggest resizing sprites to match the mock exactly.
4. **Project facts a new session needs:**
   - Unity 2022.3.62f2, **Built-in render pipeline** (switched from 2D URP, because the package shader is Built-in only), Android, portrait only. Old Input Manager (`Input.GetMouseButtonDown`).
   - Scene: `Assets/_Project/Scenes/Game.unity`. Code: `Assets/_Project/Scripts/Core` (pure C#, namespace `PixelShooting.Core.*`) and `Scripts/View` (MonoBehaviours, `PixelShooting.View`).
   - Scene hierarchy (world space, BG at the origin, sprites at z=0, 3D shooters at z=-1, camera at z=-10):
     - `BG` (sorting layer Background), with children:
       - `Holders/Holder_0..4` at x = +/-3.76, +/-1.88, 0; y = -4.16
       - `ConveyorStart`, with child `CapacityLabel` (TMP)
       - `Conveyor` (empty anchor, at the origin)
       - `Queue` at y -6.5, with `QueueView`
       - `BoardArea` at y 3.26, with `BoardView` (area 7.8x7.8)
     - `Main Camera` (`CameraFitter`)
     - `Directional Light`
     - `HUDCanvas/SafeArea`
     - `EventSystem`
     - `ShooterPool` (`ShooterViewPool`)
     - A tester object with `GameplayTester`
   - Sorting layers: Background, Board, Gameplay, FX. DOTween is installed (`Assets/Plugins/Demigiant`), with recycling and safe mode on.
   - `GameController` (it was `GameplayTester`) runs the game. It owns the conveyor path settings and a path gizmo; editor-only keys 1/2/3 load a level and R restarts. `LevelLoadTester` is a leftover debug component.
   - Git: on `master`. One commit per finished stage (see `git log`).
5. **Earlier session note:** `C:\Users\ecohen\source\repos\MyProject` (C/SDL3) is unrelated, so ignore it.

## Project

**HomeExercise_PixelShooting**: a Unity (C#) home-assignment game built around **shooters** and a **conveyor**. The user sends the spec in parts, one at a time.

### Evaluation criteria (from the assignment)
- Sound software engineering principles, with documentation where needed
- Code quality
- AI utilization (the candidate will be asked how AI was used)
- Performance:
  - Execution time
  - Memory usage: GC pressure, boxing, allocations
- Smoothness: fluid gameplay and minimal bugs. Moving shooters onto the conveyor must never cause overlapping shooters.

## Working rules (from the user)
1. Keep this `CLAUDE.md` up to date as parts are completed.
2. Keep `AI_USAGE.md` up to date. It records how AI (Claude) was used on each part.
3. Keep `PROJECT_STATUS.md` up to date with the current step, what's done, and next steps.
4. Document code where it's needed, especially *how and why* key systems work (shooter logic, the conveyor system, etc.). The user will be interviewed and must be able to explain everything.

## Learning mode (original rules from day 1; "How the user wants to work" above takes priority now)
The user originally wanted to implement most of this project themselves to learn from it.
1. **Don't implement anything unless the user explicitly asks.** Don't write or edit code, scenes, prefabs or other project files on your own. Updating the three docs is the exception (rules 1–3 above).
2. **When asked how to approach something, suggest ways to implement it.** Give options with tradeoffs (performance, simplicity, how it fits the architecture) and a recommendation. Short snippets that illustrate an idea are fine. Don't hand over full implementations unless asked.
3. **When asked how to do something in Unity, give a detailed step-by-step guide.** Include exact menu paths (e.g. `GameObject > UI > Image`), Inspector fields and values, component names, where to put files, and how to check that it worked.
4. When the user asks for a review, point out issues and explain why. Let the user make the fix unless they ask you to.
5. **Update, 2026-10-05:** the user now often delegates implementation, both infrastructure and gameplay ("just implement it"). Present decisions briefly. When asked, write the code, test it and explain it. Keep answers short.

## Tech stack
- **Unity 2022.3.62f2** (required by the assignment), C#.
- The assignment ships as `HomeExercisePackage_PixelShooting.unitypackage` (in the project root).

## Provided assets (inside the .unitypackage → `Assets/PackageAssets/`)
```
Models/
  Shooter.fbx, ShooterMaterial.mat, ShooterStrokeMaterial.mat, ShooterGradMap.png
  MobileSingleColorGlossy.shader
  Skateboard/  Skateboard.fbx, SkateboardMat.mat, Skateboard_DiffSpec.tga, Skateboard_Norm.png
UI/
  BG_Full.png        Full background (portrait)
  Conveyor_start.png Conveyor entry point (shows a "5/5" capacity counter)
  Holder.png         Holder slot tile
  MOCK.png           Gameplay layout mockup
```

### Layout seen in `MOCK.png` (portrait)
- A rounded-square **conveyor track** loops around a central sandy **play area**. The play area is presumably where the pixel targets go.
- The **conveyor start** sits at the bottom-left of the track and shows a capacity counter (`5/5`).
- Under the track is a row of **5 holder slots**.
- Mechanics come from the spec parts (see "Spec" below). Don't invent anything beyond them.

## Spec (verbatim summaries of the parts received)
### Overview: Core Mechanics (received 2026-10-04; the assignment's intro, not a numbered part)
- **The Board:** a grid filled with a pixel-art image.
- **The Shooters:**
  - The player places shooters onto a conveyor by selecting them from the **queue** or from the **holder**.
  - From the queue, a shooter can be picked **only if it's at the top of its column**.
  - Once placed, a shooter shoots at **exposed cells matching its color**, limited by its **bullet count**.
- **The Loopback:** if a shooter doesn't use all its bullets (it clears its path and leaves the board), it **returns to the Holder**.
- **Lose condition:** the Holder has a finite capacity. If the board isn't cleared and the Holder fills up with returned shooters, the game is over.
- Implied, not stated: the win condition is clearing the board.
- **Clarifications from the user (2026-10-04):**
  - **Exposed cell:** the first non-empty cell in the row or column the shooter faces. The shooter fires only if that cell is its color. Otherwise it's blocked for that line.
  - **Movement:** exactly **one lap**. The shooter fires when it reaches the **middle of a row or column** and **keeps moving without stopping**.
  - **One color per shooter.**
  - **The "5/5"** is the conveyor capacity: the most shooters on the conveyor at once.
  - ~~**Lose:** every holder slot is filled and none of the holder's shooters can shoot anything.~~ **Replaced in Part 2** by the rule "a 6th shooter arrives at the holder".

### Level JSON Loading (received 2026-10-04)
- The JSON is "a simple 2D grid with varying size". Each level can have a different grid size, and each cell can have a different color.
- It also holds the **queue column count** and the **shooter descriptions** (color and bullet count).
- The exact schema isn't given. **We design it.**
- The user expects later parts to answer the remaining open questions. Keep them in `PROJECT_STATUS.md` and re-check them as each part arrives.

### Part 1: Scene Setup (received 2026-10-04)
- Set up the scene to match the mock (`MOCK.png`) and a **reference video**, using the package assets. Claude doesn't have the video, so ask the user what it shows.
- The scene must be **responsive** and support **SafeArea**.
- **Portrait only.**
- **What the reference video shows** (from the user):
  - The camera is top-down.
  - Shooters use the `Shooter` model and face the board.
  - The **queue is below the holders**, on the big grass area, laid out as **columns**. The count comes from the JSON (2 in the video).

### Part 2: Core Gameplay (received 2026-10-05). Implement the core gameplay and lifecycle.
- **Level JSON:**
  - A 2D grid whose size varies per level. Each cell has a color.
  - It also holds the queue column count and the shooter descriptions (color and bullet count).
  - **The order and number of queue columns are in the JSON.**
- **Queue:** only the **top** shooter of a column can be selected. Once it's selected, the shooters below it **move up**.
- **Grid:**
  - Built from the JSON.
  - Cells are **scaled dynamically** to fill the grid area: smaller grid dimensions mean larger cells.
- **Conveyor:**
  - **Constant size** whatever the grid size. It's the path selected shooters move along.
  - It has room for **5 shooters at a time**. The text on the conveyor entrance shows the **number of places available**.
  - Each shooter moved onto the conveyor is **assigned a skateboard** (`Skateboard.fbx`) to ride on.
- **Holder:**
  - A selected shooter that isn't destroyed by the end of the conveyor lap **moves to the holder** below the conveyor.
  - The holder has **5 slots**. **If a 6th shooter arrives, the player loses.**
  - The player can also select a shooter **from the holder**. The spec's "not only from the conveyor" was a typo for "queue"; the user confirmed.
- **Shooting:**
  - While riding the conveyor, a shooter shoots at **exposed cells matching its color**. Shooting a cell destroys it.
  - If the shooter **runs out of bullets before finishing a full lap, it is destroyed.**
- **Win:** all shooters **and** the board are cleared. **Total bullets must always equal the number of cells.**
- **Lose:** the holder overshoots, meaning a 6th shooter is moved to the holder.
  - This **replaces** the user's earlier "holder full and no holder shooter can shoot" rule; the user confirmed on 2026-10-05.
- **Animations:** basic smooth animations. Tweens are fine; it doesn't have to be 3D animation.
- **Clarifications from the user (2026-10-05):**
  - **Lose:** a 6th shooter **arriving** at the full holder. The spec rule is the only lose rule.
  - The spec's "from the conveyor" was a typo for "from the queue".
  - **Label:** shows `free/5`.
  - **Conveyor full:** tapping a shooter plays a small shake and nothing else happens.
  - **Lap:** shooters enter at `ConveyorStart` (bottom-left corner) and go **counter-clockwise**: right along the bottom, up the right side, left along the top, down the left side, and back to the bottom-left. The lap ends there, and the shooter then hops to the holder.
  - **Bullets = cells per color** (and in total). The loader validates this.
  - **Empty cells** are allowed.
  - **Holder:** shooters keep their slots; there's no compaction.
  - **Skateboard:** only on the conveyor. It's released to a pool when the shooter moves to the holder or is destroyed.

### Part 3: Bonus, Loop Animation (received 2026-10-08)
- Once **only 5 shooters are left** in total (conveyor + holder + queue), **input locks**.
- All remaining shooters that aren't on the conveyor **move to the conveyor smoothly**.
- The conveyor **speeds up** and quickly wins the level, "like in this animation" (a reference video Claude doesn't have, so ask the user what it shows).

### Facts about the assets that matter for layout (checked 2026-10-04)
- `BG_Full.png` is 1125×2436 (aspect 0.46, iPhone X). **The conveyor track and the sand play area are baked into the background.** So gameplay objects (conveyor path, board) must stay locked to the BG image's coordinates, and the BG can't be stretched or cropped independently of them.
- In the BG, measured as a fraction of image height from the top: the track spans about 13%–60%, and the sand area about 18%–55%. The track covers about 3%–97% of the width. Below the track it's plain grass (holders and queue go there). Above it, trees.
- `MOCK.png` is 440×958, the same aspect as the BG:
  - The holder row sits at about 63–69% of the height: 5 tiles, each about 13% of the width.
  - `Conveyor_start` sits on the bottom-left corner of the track.
- `Conveyor_start.png` is 269×330. `Holder.png` is 162×161.
- **Render pipeline (resolved):** the project was created from the 2D URP template, but `MobileSingleColorGlossy.shader` is Built-in only (CGPROGRAM, `ForwardBase`). The project was switched to **Built-in**, and the URP package was removed.
- **Resolved:** orientation is set to Portrait, and the `.unitypackage` is back in the project root.

## Architecture (actual, end of day 1)
- **Core (pure C#, testable outside Unity):**
  - `LevelData`: immutable, from JSON.
  - `BoardModel`: live cells and exposed-cell queries.
  - `Shooter` and `QueueModel`.
  - `ConveyorPath` and `ConveyorModel` (stage 5).
  - `FiringPoints` and `ShootingSystem` (stage 6).
  - `HolderModel` (stage 7).
  - Game flow lives in the View-side `GameController` (decision 1B, stage 8).
- **View (MonoBehaviours):**
  - `BoardView`, `QueueView`, `ShooterView` and `ShooterViewPool`, `CameraFitter`, `SafeAreaPanel`.
  - They read the models and animate with DOTween.
- **Flow:** controllers (currently `GameplayTester`) call the model first, then the view. For example, `queue.TakeTop(c)` and then `queueView.TakeTop(queue, c)`.
- Models don't raise events (decision C1). Callers use return values.

## Systems: how they work
### Camera fitting (Part 1; written by Claude at the user's request on 2026-10-05, because the user considers it boilerplate)
- **Goal:** a world-space content rectangle (track + holders + queue) is always fully visible inside `Screen.safeArea`, on any aspect ratio.
- **Files:**
  - `Scripts/Core/CameraFitMath.cs`: a pure static function, testable.
  - `Scripts/View/CameraFitter.cs`: a MonoBehaviour on `Main Camera`. It has a serialized `Rect` and draws it with `OnDrawGizmos`.
- **Math:**
  - `unitsPerPixel = max(cw/sw, ch/sh)` ("contain").
  - `orthoSize = unitsPerPixel * Screen.height / 2`.
  - `cameraXY = contentCenter - (safeCenter - screenCenter) * unitsPerPixel`.
- **When:** in `LateUpdate`, it compares screen width, height and `safeArea` to cached values and recalculates only when they change. Nothing is allocated.
- **Known limit:** on wide screens such as iPads, the visible width is greater than the BG's 11.25 units. The green camera background fills the sides.

### HUD safe area (Part 1; written by Claude on 2026-10-05)
- **File:** `Scripts/View/SafeAreaPanel.cs`, placed on a full-stretch `SafeArea` RectTransform directly under the HUD Canvas. All HUD elements go under it.
- **How it works:** it converts `Screen.safeArea` from pixels to normalized anchors (`safeArea.min / screenSize`, `safeArea.max / screenSize`) and sets the offsets to zero. Because it uses only anchors, it works with any Canvas Scaler settings.
- **When:** the same change check in `LateUpdate` as `CameraFitter`. Nothing is allocated.
- **Canvas:** Screen Space - Overlay. Canvas Scaler set to Scale With Screen Size, reference resolution 1125×2436, Match 0.5.

## Level data (Part 2, stage 2, decided 2026-10-05)
- **JSON schema** (read with `JsonUtility`):
  - `width` and `height`.
  - `palette`: hex strings.
  - `cells`: a flat `int[]` of size `width*height`. Each value is a palette index, and **-1 means empty**. The **top row comes first**, as it looks on screen; the loader flips it, because Unity's y axis points up.
  - `queues`: `[{ "shooters": [{ "color", "bullets" }] }]`. The order of `queues` is the left-to-right column order. In each column, the **first shooter is the top one**, the one you can select. The column count is `queues.length`.
- **Storage:**
  - `Assets/_Project/Levels/*.json` files are TextAssets.
  - They're referenced from a `LevelCatalog` ScriptableObject, whose list order is the level order.
  - No `Resources` folder and no string paths.
- **Pipeline:** DTO (`[Serializable]`, mirrors the JSON) → Loader + Validator (Core, pure C#) → `LevelData` (a `byte[]`/`sbyte` grid, a `Color32[]` palette, shooter specs per column). The rest of the game never sees the DTO.
- **The validator rejects a level** with a clear error naming the level and the problem when any of these fail:
  - dimensions > 0, and the cell count = w*h
  - every cell is -1 or a valid index, and every hex string parses
  - there's at least 1 queue
  - each shooter has a valid color and bullets > 0
  - **for every color, the bullets equal the cells** (which also covers the totals)
- **It warns about:** unused palette colors and empty columns.
- **Files** (`Scripts/Core/Levels/`, namespace `PixelShooting.Core.Levels`):
  - Written by Claude: `LevelDto`, `ShooterSpec` (readonly struct), `LevelData` (immutable; accessors `GetCell`, `GetColor`, `GetQueueLength`, `GetShooter`), `LevelLoader.TryLoad(json, name, out level, errors, warnings)`, and `LevelCatalog` (ScriptableObject, menu `PixelShooting/Level Catalog`).
  - `LevelValidator.Validate`: the user wrote the structural checks. Claude fixed the bugs and finished it.
- **`LevelData` model:**
  - `sbyte[]` cells, row-major, **y = 0 is the bottom row**.
  - `Color32[]` palette, at most 127 colors.
  - `ShooterSpec[][]` queues, where `[column][0]` is the top shooter.
  - Live counts are not stored here; `BoardModel` keeps its own.
- **Sample levels** (`Assets/_Project/Levels/`):
  - `Level01`: 4×4, 2 colors.
  - `Level02`: 6×6 with empty corners, 3 colors.
  - `Level03`: 10×10 concentric rings, 4 colors, 3 queues.
  - `Invalid_BulletMismatch`: red is 7 bullets against 8 cells. It **must** be rejected.

## Board model (Part 2, stage 3, done)
- **Point 1, decided 2026-10-05.** `BoardModel` is pure C# (no UnityEngine). It's built from `LevelData` and has the same coordinates: x right, y up, (0,0) at the bottom-left.
  - **A1, cell state:** a single `sbyte[]` of colors, copied from the level. **Firing clears the cell to `EmptyCell` immediately.** That logical removal is the reservation, so there's no separate "targeted" state. The view removes the sprite later, when the bullet lands.
  - **B1, counting:** only `RemainingCells` (an int, decremented on fire), and `IsCleared => RemainingCells == 0` in O(1). There are no per-color counts.
    - ⚠️ `IsCleared` becomes true when the **last shot is fired**, not when it lands. The win flow (stage 8) must wait for bullets in flight and pop animations.
  - **C1, notifications:** no events. Queries return values. The shooting system gets the x,y of the taken cell, fires the bullet, and tells `BoardView` to pop it on hit.
  - **`Reset(LevelData)`** reuses arrays when the size matches. Restarting allocates nothing.
- **Point 2, exposed cells (recommended options accepted):**
  - Each side (`enum BoardSide : byte { Bottom, Right, Top, Left }`) and each line stores a **depth**: how many steps in from that side the search starts. Rule: every cell before the depth is empty.
  - `TryGetExposed(side, line, out x, out y)` steps past empty cells **lazily**, stores the new depth, and returns false if it runs off the board. It only looks; it doesn't remove anything.
  - This covers holes, cells removed from other sides, and empty lines. The total work over a level is ≤ 4·W·H, so each query is O(1) amortized.
  - Depth to x,y: Bottom (line, d), Top (line, H-1-d), Left (d, line), Right (W-1-d, line).
- **Point 3, firing:** `TryFire(side, line, color, out x, out y)` looks up the exposed cell. If the color matches, it clears the cell and decrements `RemainingCells`. Otherwise it returns false (blocked or empty).
- **Implementation:** `Scripts/Core/Board/BoardModel.cs`.
  - The user started `Reset` and `GetCell`. Claude finished it on 2026-10-05 at the user's request.
  - It also has a public `LineCount(side)`.
  - `LevelData.CopyCellsTo(sbyte[])` was added so `Reset` can copy without allocating.
- **Point 4, `BoardView`** (`Scripts/View/BoardView.cs`, on `BoardArea`; written by Claude on 2026-10-05):
  - One pooled `SpriteRenderer` per cell: a white Square sprite tinted with the palette color, on the `Board` sorting layer.
  - `cellSize = min(areaW/W, areaH/H)`, with the area set to **7.8×7.8** in the scene (the user tuned it from 8.4). The grid is centered, and each sprite fills 90% of its slot.
  - `cellRenderers[y*W+x]` gives O(1) lookup. The pool only grows when a level needs more cells.
  - `Build(board, level)` runs after `Reset`.
  - `Pop(x,y)` is a DOTween scale to 0 with **no** `OnComplete`, so no closure. The next `Build` hides the cell.
  - `GetCellWorldPosition(x,y)` gives bullet targets and firing points.
  - (The old `BoardTester` was replaced by `GameplayTester` in stage 4.)

## Queue (Part 2, stage 4, done)
- **Runtime shooter:** a `Shooter` **class** (pure C#) with color, bullets left and location (Queue/Conveyor/Holder/Destroyed). The same object moves between containers. Shooters are created once per level.
- **`QueueModel`:**
  - Each column is a fixed `Shooter[]` plus a `top` index. Taking a shooter is `top++`: no shifting, no allocation.
  - "The shooters below move up" happens only in the view, as a tween.
- **Input:**
  - Plain math, no physics: convert the tap to world space, find the column by x, and check it's near the top shooter.
  - The holder will use the same approach.
- **Display:** only the first few rows of each column are shown (configurable, default 3). The rest appear as shooters move up.
- **Implemented by Claude (2026-10-05, the user said "just implement it")** in `Scripts/Core/Queue/`:
  - `Shooter`: `ColorIndex`, `BulletsLeft`, `Location`.
  - `QueueModel`: `Reset`, `IsEmpty`, `Remaining`, `PeekTop`, `TakeTop` (sets `Location` to Conveyor), and `GetAt(column, rowFromTop)`.
  - Tested in the harness: 7 checks pass.
- **View, written by Claude (2026-10-05)** in `Scripts/View/`:
  - `ShooterView`, on the prefab root. It tints with a `MaterialPropertyBlock` (shared materials, no copies), and the stroke material gets a darker shade. It has `Shake()` and a cached `ReleaseCallback`, so tweens can return it to the pool without allocating.
  - `ShooterViewPool`: prewarms (default 24), uses a `Stack`, and grows with a warning. The same view follows its shooter from the queue to the conveyor to the holder.
  - `QueueView`, on the `Queue` anchor:
    - Columns are centered, and rows go downward. Only `visibleRows` (3) are shown.
    - `TakeTop` must be called **after** `QueueModel.TakeTop`. It returns the top view, tweens the rest up, and scales in the next hidden shooter.
    - `TryPickColumn(worldPoint)` is a math hit-test.
  - `GameplayTester` replaces `BoardTester`: keys 1/2/3 load, R restarts, Space fires, S shakes, and a click takes a shooter, which jumps to `ConveyorStart`.

## Conveyor (Part 2, stage 5: DONE on 2026-10-08)
- **Files:**
  - `Core/Conveyor/ConveyorPath.cs` and `ConveyorModel.cs` (`ConveyorRider` struct).
  - `View/ConveyorView.cs`, `View/SkateboardPool.cs` and `View/SkateboardView.cs`. The wiring lives in `GameplayTester`.
- **Path (`ConveyorPath`, written by the user):**
  - A **sharp-cornered rectangle** on the track's centre line (left -4.75, right 4.75, bottom -1.77, top 8.26, set in the tester's Inspector).
  - Positions are a single number: the distance from the bottom-left corner, counter-clockwise.
  - `Evaluate(distance)` clamps to `[0, Length]`.
- **Lap end:**
  - `ConveyorModel.LapLength = path.Length - lapEndGap` (1.6). Riders get off on the **left side, just above the entrance** (the user's request), then hop away.
  - For now they hop to the `ConveyorStart` box and are released. **Stage 7 replaces that with the holder.**
- **No overlap, by construction ("wait in place", redesigned by Claude on day 3):**
  - **Same speed:** all riders move at the same constant speed, so the gaps between them never change once set.
  - **Reserve on tap:** a tap only reserves a place (`TryReserve`; the shooter shakes if full). The shooter **waits in its own spot** (queue top, later holder slot) until `CanEnter`, so nothing ever waits on the track.
  - **The hop is part of the ride:**
    - `Enter` adds the rider at `Distance = -approach`, where `approach = speed * hopTime`.
    - The DOTween hop takes `HopDuration`, so it lands exactly at distance 0. From there `ConveyorView.Sync` takes over and skips negative distances.
  - **Two-sided gate:**
    - the newest rider is ≥ `minSpacing` ahead of the new one
    - **and** the oldest rider (coming round behind the entrance) is ≥ `minSpacing` behind
    - The behind check is measured to the full path end, so it stays safe even with the shortened lap.
  - **Why a gate at all:** two taps 0.1 s apart would ride overlapped for the whole lap (gap = speed × Δt). The gate enforces a minimum time between entries of `minSpacing / speed`.
- **Model data:**
  - `ConveyorRider[5]` in entry order: `[0]` is the oldest and furthest along, and exits first.
  - Exits shift the array (5 items).
  - `reserved` counts riders plus pending taps. `Free = 5 - reserved` and `Pending = reserved - riderCount`.
  - `Update(dt, exited[])` writes exiting shooters into a caller-owned array: 0 allocations.
- **View:**
  - `ConveyorView` keeps a `Dictionary<Shooter, ShooterView>` (no boxing, since the key is a class).
  - It faces riders toward the board: `n = (-t.y, t.x)` (left of the travel direction), `angle = atan2(n) - 90`, applied on top of the prefab's base rotation. `Remove` resets the rotation.
  - The label uses `SetText("{0}/5", free)`, only when the value changes.
- **Skateboards:**
  - `SkateboardPool` prewarms 8, and also holds the board's local pose under the shooter.
  - While riding, a board is a **child of its shooter**, so it follows the shooter with no per-frame code. `Detach` keeps its world pose, scales it out, and releases it through a cached callback.
- **Tester (`GameplayTester`):**
  - A FIFO `int[]` of pending columns.
  - Each frame: while pending and `CanEnter`, it calls `TakeTop` + `Enter` + `Add` + `DOJump`. Then `Update` → `Sync`, and exits `HopOff`.
  - Restart calls `ClearConveyor`. Tapping a column more times than it has shooters is ignored.
- **Unity gotcha:** a type named `Rider` is hidden by a global `Rider` namespace in the Unity project (from the IDE package). Avoid that name.

## Shooting (Part 2, stage 6: DONE on 2026-10-08; all code by Claude, decisions by the user)
- **Files:** `Core/Shooting/FiringPoints.cs`, `Core/Shooting/ShootingSystem.cs` (namespace `PixelShooting.Core.Shooting`) and `View/BulletSystem.cs`.
- **Firing points (decision 1A):**
  - A sorted list per level: one point per column on the bottom and top sides, and one per row on the right and left sides.
  - Each point sits at the path distance where the rider lines up with that line's cell centre.
  - The build order follows the counter-clockwise lap, so the list is sorted with no sort call.
  - Each `ConveyorRider` keeps `NextPoint`. Per frame, a rider processes every point it has passed, so it can't skip a line on a slow frame. That's O(1) amortized and allocates nothing.
- **Firing (decision 2):**
  - `board.TryFire(side, line, color)`. A match removes the cell **logically at once** (this is the reservation, so two riders can never target the same cell), then `BulletsLeft--`.
  - Otherwise the line is blocked or empty: no shot.
  - One shot per line per lap.
- **Out of bullets (decision 3A):** the shooter is removed from the conveyor at once with `RemoveAt`, which frees its place. Its `Location` becomes `Destroyed`, and the view shrinks it away and drops its skateboard.
- **Frame order:** `conveyor.Advance(dt)` → `shooting.Fire(shots)` → `conveyor.CollectExits(exited)` → `conveyorView.Sync`.
- **Bullets:** a pooled sprite update loop (not DOTween). It flies at a constant speed from the firing point to the cell centre, then calls `BoardView.Pop`. The picture lags the logic by the flight time, and `InFlight` lets the win check wait for it.
- **Invariant (tested):** bullets left on all shooters == cells left on the board, at every frame of a full Level03 simulation.

## Holder (Part 2, stage 7: DONE on 2026-10-08; all code by Claude, decisions by the user)
- **Files:** `Core/Holder/HolderModel.cs` (namespace `PixelShooting.Core.Holder`) and `View/HolderView.cs` (on `Holders`, with the slots `Holder_0..4`).
- **Model:**
  - 5 fixed slots (`Shooter[5]`), so there's no sliding.
  - `TryAdd` puts a shooter in the leftmost free slot; **false means the 6th arrival, i.e. lose**.
  - `Take(slot)` frees a slot (it throws if the slot is empty). Both set `Shooter.Location`.
- **Timing rules (no overlap, no double booking):**
  - The slot is assigned the **moment the rider exits its lap**; the hop is only the animation. Two exits close together can't pick the same slot.
  - A holder slot is freed only when its shooter **actually leaves** (gate open, hop starts). Until then it waits in the slot, as in "wait in place".
- **Picking:** `HolderView.TryPickSlot` uses squared distance to each occupied slot (`pickRadius`). The tester checks the holder first, then the queue.
- **Pending taps:** a FIFO of `PendingTap {FromHolder, Index}`. A holder shooter can't be queued twice, and a column can't be tapped more times than it has shooters.
- **Lose (temporary, until stage 8):** the tester sets `lost`, logs it, shakes the 6th shooter and freezes taps and the conveyor until R/1/2/3.

## Game flow (Part 2, stage 8: DONE on 2026-10-08; all code by Claude, decisions by the user)
- **`View/GameController.cs`** (it was `GameplayTester`; decision 1B kept the flow in one MonoBehaviour):
  - It loads levels from `LevelCatalog`, handles taps (holder first, then queue) and runs the frame loop: pending hops → `Advance` → `Fire` → `CollectExits` → `Sync` → holder returns.
  - `GameState { Playing, Won, Lost }`.
- **Win:** `board.IsCleared`. Bullets = cells, so every shooter is used up at the same moment. The popup waits for `BulletSystem.InFlight == 0` plus a short delay, so the last pops can play.
- **Lose:** `HolderModel.TryAdd` fails, i.e. the 6th arrival. That shooter shakes, then the popup appears.
- **Level loading:**
  - It loads into a local `LevelData`, so a failed load never wipes the current level.
  - Invalid catalog entries are logged and skipped, and the level list wraps after the last one.
- **`View/GamePopup.cs`:**
  - Built and styled in code (`Init` from `GameController.Awake`), using `RoundedSprite`, a runtime 9-sliced rounded texture, because the package has no UI panel sprite.
  - The button listener is added once. **Next** loads the next level, **Retry** restarts.
  - It has no `Awake`, because it starts inactive.
- **DOTween:** `DOTween.Init(true, true, ErrorsOnly).SetCapacity(200, 50)` in `GameController.Awake`.
- **Bullet bubbles:**
  - `ShooterView` builds a badge in `Init` (FX layer, a TMP number). `Bind` sets the count, and `GameController.ShowShot` updates it.
  - `ConveyorView.Sync` calls `UpdateBubble` so it stays upright on turning riders.
- **Levels:** `Level01-03` are forgiving. `Level04` is "order matters" (a single 20-bullet red ring shooter buried in column 0), and random tapping loses there about 46% of the time.

## Loop finale (Part 3 bonus: DONE on 2026-10-10; all code by Claude, decisions by the user)
- **Trigger:**
  - `GameController.aliveShooters` counts the shooters not yet destroyed. It starts as the queue total and is decremented on `Shot.ShooterEmptied`.
  - When it **drops** to `loopShooterCount` (5), the finale starts. `loopArmed` requires a start count > 5, so small levels (e.g. `Level01`) never trigger it.
- **Finale:**
  - Taps are ignored, and the conveyor switches to `StartLooping(speed * loopSpeedMultiplier)` (an `int`, default 3; the speed jumps).
  - Every holder and queue shooter is added to the pending FIFO with a reservation, so they board **one by one through the normal gate**. With at most 5 alive (= capacity), every reservation fits.
- **Loop mode (`ConveyorModel`):**
  - No exits. At the path end a rider wraps (`Distance -= Length`, `NextPoint = 0`), so it fires a whole new lap, including the rows past the normal lap end (`FireLimit` = full path).
  - Because wrapping keeps the distances around the loop unchanged, it can't cause an overlap.
- **Gate (generalized for this):** `CanEnter` checks the circular gap from the new rider (at `-approach`) to **every** rider: ≥ `minSpacing` both ahead and behind. In normal mode this is the same as the old newest-ahead/oldest-behind check.
- **Speed change:** `approach` is a fixed distance, so `HopDuration` shrinks with speed. `ConveyorView` kills a rider's hop tween once when it lands (`ShooterView.OnTrack`), so a hop that was interrupted by the speed jump doesn't fight `Sync`.
- **Why it always wins:** bullets = cells per color. Every exposed cell has a color for which some looping shooter still has bullets, so each lap makes progress. This is checked by `LoopTests`, which plays whole random games into the finale (100% wins on Levels 2-4).

## Hint button (extra, 2026-10-10; all code by Claude at the user's request)
- **`Core/Hint/HintSolver.TryGetHint(...)`:**
  - It searches for a winning order using **sequential laps**: one shooter rides alone and fires by the `ShootingSystem` rules. Bullets left go to the leftmost holder slot; a 6th arrival is a dead branch.
  - **Starting state:** the live state, after riders finish their lap (from `NextPoint`) and pending taps run, in order.
  - **Search:** depth-first, with moves sorted by immediate hits (so the first dive usually wins), identical holder shooters de-duplicated, and a node budget (50k) with a greedy fallback.
  - **No effect on the game:** it works on `BoardModel.CopyFrom` snapshots and per-depth buffers that are kept between calls, so it allocates 0 bytes on repeat calls and takes about 0.3 ms on Level03.
- **`Core/Hint/TapSource`** (queue column or holder slot) is also the type of the pending-tap FIFO.
- **`GameController`:** the hint button (wired in the scene; styled by `UiStyle`, top-right) puts a pulsing ring under the suggested view (`QueueView.TopView` / `HolderView.GetView`) for `hintDuration` (4 s). Any tap clears it, and it's disabled in the finale and on the popup.
- **Why sequential laps:** any order that wins sequentially is a valid plan for the real game, and a lap is just a walk over the firing-point list, so the search is exact and cheap.

## Animation (Part 2, stage 1, decided 2026-10-05)
- **DOTween** handles one-off A→B animations:
  - queue shift-up
  - hops from the queue or holder onto the conveyor
  - the hop from the conveyor to the holder
  - the shake when the conveyor is full
  - cell pop and shooter death
  - the skateboard appearing and disappearing
- **Not tweened:**
  - Riding the conveyor: the conveyor model advances distance by `speed × dt`.
  - Bullet flight: a pooled update loop.
  - The model is the single source of truth for these, and it drives the firing logic.
- **GC rules for DOTween:**
  - Recycling is on, and the capacity is preallocated once at startup with `SetTweensCapacity`.
  - **No capturing lambdas** in `OnComplete` or `OnUpdate`. Use cached delegates or instance methods.
  - Kill tweens with `DOKill` before reusing a pooled object.
- **Logic stays independent of DOTween.** Only View scripts reference DOTween; Core never does.

## Testing
- **`Tools/CoreTests/`** is a .NET 10 console harness, outside `Assets/` so Unity ignores it. Run it with `dotnet run` in that folder.
  - It compiles the Core sources directly (`Levels`, `Board`, `Queue`, `Conveyor`, `Shooting`, `Holder`, `Hint`) against `UnityShim.cs`, which stands in for `Color`, `Color32`, `ColorUtility` and `JsonUtility`.
  - Suites: `LevelTests` (14 checks), `BoardTests` (17, including 42k random queries compared against brute force and 0-allocation checks), `QueueTests` (7), `ConveyorTests` (33: path, gate, two-sided gap, lap end, 20k-step random sim, 0 allocations), `ShootingTests` (11: points, lap order, blocking, lap-end cut-off, buffer overflow, Level03 sim with the bullets==cells invariant, 0 allocations), `HolderTests` (11), `Level04Tests` (plays whole games: Level04 is winnable via DFS and random tapping loses ≥1/3; it also reports Levels 1-3 never losing), `LoopTests` (10: loop wrap/speed/reset, circular-gap random sim, whole games where every finale wins), `HintTests` (9). Total: 117 checks.
  - The shim also has a minimal `Vector2` and `Mathf.Clamp`. Extend it when Core uses more of UnityEngine.
  - **When adding Core code, add a suite and add its folder to `CoreTests.csproj`.** (`.gitignore` ignores `*.csproj` but has an exception for `Tools/**`.)
- Unity is usually open while we work, so batch mode can't run. The user tests the View in Play mode.
- Shim difference: Unity's `TryParseHtmlString` also accepts named colors like "red". The shim doesn't.

## Performance conventions
- No per-frame heap allocations in `Update` or other hot paths. Use object pooling and preallocated buffers/arrays.
- Avoid boxing:
  - No enums or structs passed as `object` or non-generic interfaces.
  - No `Dictionary<Enum,…>` without a custom comparer.
  - No string concatenation per frame.
- No LINQ, closures or `foreach` over interfaces in update loops. Cache component references, and use no `GetComponent`/`Find` at runtime.
- The conveyor reserves a slot before a shooter moves, so overlap is impossible by construction.
- Keep game logic separate from presentation (MonoBehaviours), so the logic can be tested.
