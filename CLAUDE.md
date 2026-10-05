# CLAUDE.md

Project context for Claude Code sessions. Keep this file updated after every completed part.

## Start here (new session)
1. Read this whole file, then `PROJECT_STATUS.md` (where we are and what's next) and `AI_USAGE.md` (the log you must keep appending to).
2. **Expected state as of 2026-10-05:**
   - The Unity 2022.3.62f2 project exists in this folder with the package imported.
   - It was created from the 2D URP template and has since been switched to the **Built-in pipeline**. The build target is **Android**, portrait only.
   - Received so far: the Core Mechanics overview, the Level JSON note, and **Part 1: Scene Setup**, which is **complete**.
   - Scene: `Assets/_Project/Scenes/Game.unity`.
   - Scripts: `Core/CameraFitMath`, `View/CameraFitter`, `View/SafeAreaPanel`.
   - **Part 2: Core Gameplay has been received** (see Spec). The user wants it done **slowly, stage by stage**, keeping strictly to the guidelines. The stage plan and open questions are in `PROJECT_STATUS.md`.
3. **What happens next:**
   - The user will send the assignment spec **in parts**.
   - For each part: help the user design the solution (suggest approaches), and let **the user implement it** (see "Learning mode" below). Then update all three docs before saying the part is done.
   - Don't jump ahead or invent mechanics that aren't in the spec. Ask when something is ambiguous.
4. **Earlier session:**
   - Docs were set up from `C:\Users\ecohen\source\repos`.
   - `source\repos\MyProject` (C/SDL3) is unrelated, so ignore it.

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

## Learning mode (from the user, highest priority)
The user wants to implement most of this project themselves to learn from it.
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

### Facts about the assets that matter for layout (checked 2026-10-04)
- `BG_Full.png` is 1125×2436 (aspect 0.46, iPhone X). **The conveyor track and the sand play area are baked into the background.** So gameplay objects (conveyor path, board) must stay locked to the BG image's coordinates, and the BG can't be stretched or cropped independently of them.
- In the BG, measured as a fraction of image height from the top: the track spans about 13%–60%, and the sand area about 18%–55%. The track covers about 3%–97% of the width. Below the track it's plain grass (holders and queue go there). Above it, trees.
- `MOCK.png` is 440×958, the same aspect as the BG:
  - The holder row sits at about 63–69% of the height: 5 tiles, each about 13% of the width.
  - `Conveyor_start` sits on the bottom-left corner of the track.
- `Conveyor_start.png` is 269×330. `Holder.png` is 162×161.
- **Render pipeline issue:**
  - The user created the project with the **2D URP template**: Renderer2D, URP 14.0.12, and Default Behavior Mode set to 2D.
  - `MobileSingleColorGlossy.shader` is a **Built-in pipeline shader** (CGPROGRAM, `LightMode=ForwardBase`). It renders pink under URP.
  - Claude recommended switching the project to Built-in.
- Default orientation is still Auto Rotation (`defaultScreenOrientation: 4`). It must become Portrait.
- The `.unitypackage` was moved into `Assets/`. It should go back to the project root.

## Architecture
_Proposed for Part 1, not yet confirmed by the user. Logic is plain C#, and the view is MonoBehaviours._
- `BoardModel`: color-index grid plus "exposed cell" queries. Cells are removed logically when a bullet is fired, so two shooters can never target the same cell. The visual removal happens when the bullet hits.
- `ConveyorModel`: a closed path parameterized by distance, with a fixed number of slots (5 if the "5/5" counter is the capacity). A shooter is placed only into a free slot at the entry point, so overlap can't happen by construction.
- `ShooterQueue` (columns, only the top one is pickable) and `Holder` (a fixed-size array).
- `GameController`: placement rules, loopback, and win/lose.
- View: board rendering (a single texture or pooled sprites), pooled bullets and pooled shooters.

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
  - **`LevelValidator.Validate` is the user's to implement.**
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

## Board model (Part 2, stage 3; the user implements it)
- **Point 1, decided 2026-10-05.** `BoardModel` is pure C# (no UnityEngine). It's built from `LevelData` and has the same coordinates: x right, y up, (0,0) at the bottom-left.
  - **A1, cell state:** a single `sbyte[]` of colors, copied from the level. **Firing clears the cell to `EmptyCell` immediately.** That logical removal is the reservation, so there's no separate "targeted" state. The view removes the sprite later, when the bullet lands.
  - **B1, counting:** only `RemainingCells` (an int, decremented on fire), and `IsCleared => RemainingCells == 0` in O(1). There are no per-color counts.
    - ⚠️ `IsCleared` becomes true when the **last shot is fired**, not when it lands. The win flow (stage 8) must wait for bullets in flight and pop animations.
  - **C1, notifications:** no events. Queries return values. The shooting system gets the x,y of the taken cell, fires the bullet, and tells `BoardView` to pop it on hit.
  - **`Reset(LevelData)`** reuses arrays when the size matches. Restarting allocates nothing.
- **Point 2, exposed cells (recommended options; the user said "move on" on 2026-10-05):**
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
  - `BoardTester` is a temporary helper: keys 1/2/3 load a level, Space fires a random valid shot, R restarts.

## Queue (Part 2, stage 4, decided 2026-10-05)
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

## Performance conventions
- No per-frame heap allocations in `Update` or other hot paths. Use object pooling and preallocated buffers/arrays.
- Avoid boxing:
  - No enums or structs passed as `object` or non-generic interfaces.
  - No `Dictionary<Enum,…>` without a custom comparer.
  - No string concatenation per frame.
- No LINQ, closures or `foreach` over interfaces in update loops. Cache component references, and use no `GetComponent`/`Find` at runtime.
- The conveyor reserves a slot before a shooter moves, so overlap is impossible by construction.
- Keep game logic separate from presentation (MonoBehaviours), so the logic can be tested.
