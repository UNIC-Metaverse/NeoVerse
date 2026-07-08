# NeoVerse

NeoVerse is a multi-user, networked XR "metaverse" application built in Unity. A single
codebase targets three deployment modes:

- **Desktop (flat)** — Windows, mouse/keyboard.
- **Tethered PC-VR** — Meta Quest 3 driven from a PC over Quest Link (OpenXR).
- **Standalone VR** — a native Android build running on the Meta Quest 3.

## Tech stack

- **Unity 2022.3.62f2 (LTS)**, Universal Render Pipeline (URP), OpenXR + XR Interaction Toolkit
- **Photon Fusion** (shared mode) for multiplayer and **Photon Voice** for voice chat
- **Convai** embodied conversational NPCs; **Ready Player Me** avatars
- **Android Build Support (IL2CPP)** module is required for Quest builds

## First-time setup — credentials

API keys are **not** committed to the repository. After cloning, recreate them from the
templates before building, or Photon/Convai will fail to connect. See
[`docs/CREDENTIALS.md`](docs/CREDENTIALS.md). The three files are:

- `Assets/_Project/Shared/Resources/ConvaiAPIKey.asset`
- `Assets/ThirdParty/Photon/Fusion/Resources/PhotonAppSettings.asset`
- `Assets/ThirdParty/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset`

## Running & controls

At launch, `RigModeAutoDetector` checks for an active XR headset: if one is present the VR
rig loads, otherwise the desktop rig loads.

- **Startup override** (hold while the app starts): **Numpad 1** forces Desktop, **Numpad 2** forces VR.
- **Desktop controls:** `WASD` move, `Q`/`E` turn the body, **hold right mouse button** to look around. Mouse-look sensitivity is resolution/framerate-independent — tune it via `sensitivity` on the `MouseCamera` component.

## Build targets

- **Desktop / tethered VR:** one Windows Standalone (x86_64) build serves both; the rig is chosen at runtime.
- **Quest 3 standalone:** Android build — IL2CPP, ARM64, Vulkan, OpenXR with the Meta Quest feature. Sideload with SideQuest or `adb install`.

Scenes build in this order: `AvatarSelection` → `UNIC` → `UnicReceptionArea` → `MeetingRoom`.

### Two-checkout workflow (recommended)

To avoid the slow asset re-import when switching platforms, the project is worked on as two
checkouts, each pinned to one platform with its own `Library` cache:

- `NeoVerse/` (branch `main`) → Windows target (desktop / tethered).
- `NeoVerse-Quest/` (git worktree, branch `android`) → Android target (standalone).

Make content/code changes on `main` and merge `main → android`. Remember the gitignored
credential assets must be copied into each checkout.

## Performance benchmarking

An in-engine benchmark (`PerformanceBenchmark.cs`) runs an identical, deterministic
"spin-in-place" sweep of the current scene and writes consistent per-run metrics to CSV on
every platform, so Desktop / Tethered / Standalone can be compared directly.

It is **off by default** and gated behind a scripting define symbol so it is never compiled
into normal or production builds.

### Turning it on

1. **Project Settings → Player → Other Settings → Scripting Define Symbols.**
2. Add `NEOVERSE_BENCHMARK`. These symbols are **per build target**, so add it on the
   *Standalone* tab (covers desktop + tethered) and/or the *Android* tab (covers Quest).
3. Also tick **Frame Timing Stats** (same Player Settings page) so CPU/GPU frame time is
   captured, and build as a **Development Build** so the draw-call / triangle counters report data.
4. Press **Apply**, let Unity recompile, then build.

### Running a benchmark

- **Desktop / tethered (Windows):** press **F8** to start.
- **Quest standalone:** it **auto-starts ~12 s** after the scene loads (no keyboard needed).
- Each run is a 6 s warm-up (discarded) followed by a 60 s sampling sweep. An on-screen
  label shows progress; the console logs a `[PerformanceBenchmark] DONE` line with the file path.

### Getting the results

CSVs are written to `Application.persistentDataPath`:

- **Desktop / tethered:** `C:\Users\<you>\AppData\LocalLow\UNIC-Metaverse\Neoverse\`
- **Quest:** `/sdcard/Android/data/com.UNICMetaverse.Neoverse/files/` — pull with
  `adb pull /sdcard/Android/data/com.UNICMetaverse.Neoverse/files/ .\quest-bench`

Two files per run: `bench_<platform>_<timestamp>_summary.csv` (aggregates) and
`..._frames.csv` (per-frame series). Metrics include frame-time mean/median/p95/p99/max,
average and 1%-low FPS, CPU/GPU frame time, draw calls, triangles, per-frame GC allocation,
and peak memory. Paste the summary CSV values into the comparison spreadsheet template to
build the cross-platform table.

### Turning it off

Remove `NEOVERSE_BENCHMARK` from Scripting Define Symbols and rebuild. The entire benchmark
is excluded from compilation — no auto-start, no listener, zero overhead.

## Custom scripts

Under `Assets/_Project/Core/Application/Scripts/`:

- `RigModeAutoDetector.cs` — selects the VR or desktop rig from actual headset presence at startup; Numpad 1 / Numpad 2 startup overrides.
- `DesktopHandHider.cs` — hides the local player's hand meshes in desktop mode (on the `MeetingRoomNetworkRig` prefab); VR and remote players are unaffected.
- `DesktopMenuLook.cs` — mouse-look for the AvatarSelection menu in desktop mode; disables the camera's `TrackedPoseDriver` (which otherwise fights the mouse) and clamps look to ±45°.
- `PerformanceBenchmark.cs` — the benchmark harness described above (guarded by `NEOVERSE_BENCHMARK`).

Customised third-party script:

- `Assets/ThirdParty/Photon/FusionAddons/XRShared/Scripts/Desktop/MouseCamera.cs` — desktop mouse-look made resolution/framerate-independent, with an optional yaw clamp (used by the AvatarSelection menu). Marked with a `CUSTOMISED (NeoVerse)` comment.

## Notes

- **Screen sharing** (Photon Screensharing addon / `uWindowCapture`) is **Windows-desktop only**; it is compiled out / a no-op on Android, so it does not function on the Quest standalone build.
