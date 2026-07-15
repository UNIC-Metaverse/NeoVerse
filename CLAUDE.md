# CLAUDE.md — NeoVerse working notes

Agent context for this repo. User-facing setup/how-to lives in `README.md`; this file is the
working memory (decisions, rationale, conventions, open items). Keep it updated as work lands.

## Project

NeoVerse is a multi-user, networked XR "metaverse" (Unity **2022.3.62f2** LTS, URP, OpenXR +
XR Interaction Toolkit, **Photon Fusion** shared mode + Photon Voice, **Convai** NPCs, Ready
Player Me avatars). One codebase, three deployment modes: **desktop (flat)**, **tethered
PC-VR** (Quest 3 over Link), **standalone Quest 3** (Android / IL2CPP / ARM64 / Vulkan).

Build scenes (in order): `AvatarSelection` → `UNIC` → `UnicReceptionArea` → `MeetingRoom`.
`UNIC` is the heaviest scene (~1,190 objects) and the one used for perf testing.

## Working conventions

- Prefer editing over rewriting; keep changes minimal and platform-guarded.
- Third-party scripts we modify are marked with a `CUSTOMISED (NeoVerse)` comment so package
  updates don't silently revert them. Re-apply after any Fusion/addon update.
- Handle platform differences with `#if UNITY_ANDROID` / runtime checks in one codebase where
  possible, rather than divergent branches.

## Two-checkout workflow (avoid slow platform switching)

- `NeoVerse/` — branch `main` — pinned to **Windows Standalone** (desktop + tethered).
- `NeoVerse-Quest/` — git **worktree**, branch `android` — pinned to **Android** (Quest).
- Each keeps its own `Library` cache, so switching platforms never re-imports. Make
  content/code changes on `main`, then `git merge main` in the worktree.
- Both run as separate Unity editors simultaneously. When both are open, the Unity MCP sees
  two instances — always `unity_select_instance` / pass the right `port` before acting
  (main and quest are on different ports; confirm with `unity_list_instances`).

## Custom scripts (`Assets/_Project/Core/Application/Scripts/`)

- **`RigModeAutoDetector.cs`** — at startup, detects a real XR headset (XR active loader /
  `InputDevices`) and writes the `RigMode` PlayerPref so `ExtendedRigSelection`
  (SelectedByUserPref) loads VR vs desktop correctly. Fixes the original bug where a stale
  `"VR"` pref loaded the headset rig with no HMD (no movement). Startup overrides: **Numpad 1**
  = force Desktop, **Numpad 2** = force VR. Lives on the `ExtendedRigSelection` object in `UNIC`.
- **`DesktopHandHider.cs`** — on the `MeetingRoomNetworkRig` prefab. For the local rig in
  desktop mode only, sets `NetworkHandRepresentationManager.displayForLocalPlayer = false`,
  hiding the distracting local hand meshes. VR and remote players unaffected; teleport ray
  (separate LineRenderer) still works.
- **`DesktopMenuLook.cs`** — on the `AvatarSelection` `HardwareRig`. In desktop mode disables
  the camera's `TrackedPoseDriver` (which otherwise fights mouse-look and caused stutter) and
  enables `MouseCamera`; in VR it disables `MouseCamera`. Look is clamped to ±45°.
- **`PerformanceBenchmark.cs`** — cross-platform benchmark harness, **gated behind the
  `NEOVERSE_BENCHMARK` scripting define** (off by default). Self-bootstraps; F8 on
  desktop/tethered, auto-start ~12 s on Quest; 6 s warm-up + 60 s spin sweep; writes
  `bench_<platform>_<ts>_summary.csv` + `_frames.csv` to `persistentDataPath`. See README
  "Performance benchmarking" for full enable/collect steps.

## Customised third-party scripts

- **`.../XRShared/Scripts/Desktop/MouseCamera.cs`** — desktop mouse-look made
  resolution/framerate-independent (removed the `Time.deltaTime` and `1920/Screen.width`
  factors on the mouse delta; framerate-correct exponential smoothing). Added optional yaw
  clamp (`clampYaw` / `maxYawAngle`), used by `DesktopMenuLook`. Default `sensitivity` 15.
- **`.../FusionAddons/Screensharing/Scripts/ScreenSharingEmitter.cs`** — wrapped the
  `captureHost` use in `DesktopIndex` in `#if UWC_EMITTER_ENABLED` so the **Android build
  compiles** (screen sharing is Windows-desktop only; `uWindowCapture` is a Win-only native
  plugin and a no-op on Quest).
- **`.../FusionAddons/XRShared/Scripts/Utils/RandomizeStartPosition.cs`** — `FindStartPosition`
  now uses a local `effectiveRadius` instead of `randomRadius` directly; under the
  **`NEOVERSE_BENCHMARK`** define it forces `effectiveRadius = 0` so benchmark runs always spawn
  at the exact `startCenterPosition` (deterministic, repeatable). Normal builds use the
  inspector `randomRadius`. UNIC `DesktopRig` radius is **5** (spread on the MAIN PLAZA NavMesh,
  `useNavMesh` on); VR `HardwareRig` stays radius 1, `useNavMesh` off.

## Credentials (never commit)

Convai/Photon keys are gitignored. Templates in `docs/credential-templates/`; setup in
`docs/CREDENTIALS.md`. The three assets: `ConvaiAPIKey.asset`, `PhotonAppSettings.asset`,
`PhotonServerSettings.asset`. Each checkout needs its own copies (git won't carry them).
Quest package id: `com.UNICMetaverse.Neoverse`.

## Open items / TODO

- [ ] **Finish merging `main` into the `NeoVerse-Quest` (android) worktree.** The android
      worktree's uncommitted state was snapshotted to commit `81cb79e` (nothing lost), then a
      `git merge main` was attempted and **aborted** because it conflicts:
      - `UNIC.unity` — the git text-merge **duplicates the `Convai NPC Amelia` root** (main and
        android each carry Amelia as a *different* PrefabInstance fileID → merged scene ends up
        with two Amelias + a one-line `SceneRoots.m_Roots` conflict). Do **not** hand-merge the
        YAML. Resolve in the **Quest Unity editor**: likely take `main`'s `UNIC.unity`
        (`git checkout --theirs`) then re-apply android's ~60-line local scene edit from
        `81cb79e` if it was deliberate, ensuring only **one** Amelia remains.
      - `PerformanceBenchmark.cs` + `.meta` — add/add. Take **main**'s (`--theirs`): its `.cs`
        is the same code with richer comments, and its `.meta` GUID (`39394052…`) should win so
        both checkouts share one GUID (android's stray GUID was `64ceec44…`).
      - `ProjectSettings.asset` auto-merged (verify platform bits didn't get crossed).
      Consider configuring **UnityYAMLMerge** (smart merge) before retrying.
- [ ] **Push** when ready: `main` is at `ec70d79` (unpushed), android worktree at `81cb79e`
      (unpushed).
- [ ] **Rotate the Convai API key and Photon App IDs** — the repo is public and was forked, so
      the previously-committed keys are exposed; untracking only stops future leaks. (User was
      handling this.)
- [ ] **Standalone GC-hitch optimisation** — Quest 3 holds ~72 Hz on average but shows
      intermittent frame-time excursions (p99 ≈15.5 ms, max ≈126 ms, 1% low ≈64 fps) i.e.
      perceptible judder. Not geometry-bound (fewer draws/tris than the smooth tethered run);
      cause is per-frame managed allocation (~4.3 KB/frame) → periodic GC. First moves: enable
      **Incremental GC** (Player Settings) and profile GC Alloc over USB to kill per-frame
      allocations (suspects: Convai gRPC/strings, Photon serialisation, `Update()` allocs).
- [ ] Optional: real Quest GPU-time via **OVR Metrics Tool** (FrameTimingManager reports 0 on
      Vulkan; desktop D3D11 GPU time is also unreliable — both excluded from the perf report).

## Notes / gotchas

- **FMOD was removed** (was ~291 MB, unused): no `.bank` files, no code references, and 0
  build-scene dependencies — audio runs through Unity `AudioSource` + AudioMixer via
  `SoundManager`. Photon Voice ships an optional FMOD integration (`PhotonVoice/Code/FMOD`,
  `PhotonVoiceApi/Platforms/FMOD`) whose asmdefs are gated on **`PHOTON_VOICE_FMOD_ENABLE`**;
  that define had to be removed from **Standalone** *before* deleting the plugin or the desktop
  build breaks (Android never had it — the Quest build already compiled without it). Those
  Photon integration folders are left in place but are inert with the define off. Stale
  `PHOTON_VOICE_FMOD_*` defines still sit on **non-targeted** platform groups (PS4/Switch/
  WebGL/iOS/tvOS/…) — harmless, and unreachable via the `PlayerSettings` API because those
  platform modules aren't installed. Re-adding FMOD means restoring the define first.
- Active Input Handling is **Both**; the project runs on the **new Input System** (Convai's
  `ConvaiInputManager` uses the new path via `#elif`; legacy code is dormant). Switching to
  New-only is safe but not required.
- Perf benchmarking needs **Development builds** (draw-call/geometry counters) and **Frame
  Timing Stats** enabled (Player Settings) for CPU/GPU ms.
- Results comparison spreadsheet + LNCS paper write-up were produced outside the repo (in the
  Cowork outputs); re-create or import if needed.
