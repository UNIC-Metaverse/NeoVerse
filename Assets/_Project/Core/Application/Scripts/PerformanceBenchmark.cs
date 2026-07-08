#if NEOVERSE_BENCHMARK  // Add NEOVERSE_BENCHMARK to Player Settings > Scripting Define Symbols to compile the benchmark in; remove it to exclude it entirely.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Fusion.XR.Shared.Rig;

namespace Fusion.Samples.IndustriesComponents
{
    /// <summary>
    /// Cross-platform performance benchmark for comparing Desktop, tethered VR and Quest
    /// standalone. Self-bootstraps at runtime (no scene wiring), runs an identical timed
    /// "spin in place" sweep so every platform renders the same workload, samples consistent
    /// metrics, and writes a CSV to Application.persistentDataPath.
    ///
    /// Trigger:
    ///  - Android (Quest standalone): auto-starts a few seconds after load.
    ///  - Windows (desktop & tethered): press F8 to start.
    ///
    /// Metrics: frame time (mean/median/p95/p99, min/max, avg & 1% low FPS), CPU/GPU frame
    /// time (FrameTimingManager), main/render thread ms, draw calls / setpass / batches /
    /// triangles, GC per frame, peak reserved/system memory.
    ///
    /// Notes:
    ///  - Enable Player Settings > Other > "Frame Timing Stats" for CPU/GPU ms.
    ///  - Use DEVELOPMENT builds so the render-stat recorders (draw calls, tris) report data.
    ///  - FPS is NOT directly comparable across flat vs VR (VR is vsync-locked & stereo);
    ///    compare CPU/GPU frame time and headroom instead.
    /// </summary>
    public class PerformanceBenchmark : MonoBehaviour
    {
        const float WarmupSeconds = 6f;            // skip load spike; let avatars/connection settle
        const float DurationSeconds = 60f;         // sampling window
        const float YawTotalDegrees = 360f;        // rig rotates this much over the run
        const float DollyDistance = 0f;            // optional forward move (m) over the run; 0 = spin only
        const float AutoStartDelayAndroid = 12f;   // Quest auto-begins this long after load

        static PerformanceBenchmark _instance;

        enum Phase { Idle, Warmup, Running, Done }
        Phase _phase = Phase.Idle;
        float _phaseTimer;
        float _autoTimer;
        bool _autoStartArmed;

        HardwareRig _rig;
        Vector3 _startPos;
        Quaternion _startRot;
        float _yawApplied;

        readonly List<double> _frameMs = new List<double>(8192);
        double _sumCpu, _sumGpu; int _cpuGpuSamples;
        double _sumDraw, _sumSetPass, _sumTris, _sumVerts, _sumBatches; int _renderSamples;
        double _sumMain, _sumRender; int _threadSamples;
        long _peakReserved, _peakSystem; double _sumGCPerFrame; int _gcSamples;

        ProfilerRecorder _rMain, _rRender, _rDraw, _rSetPass, _rTris, _rVerts, _rBatches, _rGC, _rReserved, _rSystem;
        FrameTiming[] _ft = new FrameTiming[1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("[PerformanceBenchmark]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PerformanceBenchmark>();
        }

        void OnEnable()
        {
            _rMain = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            _rRender = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
            _rDraw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _rSetPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _rTris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            _rVerts = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            _rBatches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            _rGC = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _rReserved = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            _rSystem = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            _autoStartArmed = Application.platform == RuntimePlatform.Android;
        }

        void OnDisable()
        {
            _rMain.Dispose(); _rRender.Dispose(); _rDraw.Dispose(); _rSetPass.Dispose();
            _rTris.Dispose(); _rVerts.Dispose(); _rBatches.Dispose(); _rGC.Dispose();
            _rReserved.Dispose(); _rSystem.Dispose();
        }

        void Update()
        {
            switch (_phase)
            {
                case Phase.Idle:
                {
                    bool trigger = false;
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) trigger = true;
#endif
                    if (_autoStartArmed)
                    {
                        _autoTimer += Time.unscaledDeltaTime;
                        if (_autoTimer >= AutoStartDelayAndroid) trigger = true;
                    }
                    if (trigger) BeginWarmup();
                    break;
                }
                case Phase.Warmup:
                {
                    _phaseTimer += Time.unscaledDeltaTime;
                    if (_phaseTimer >= WarmupSeconds) BeginRunning();
                    break;
                }
                case Phase.Running:
                {
                    double dt = Time.unscaledDeltaTime;
                    _phaseTimer += (float)dt;
                    ApplyMotion();
                    _frameMs.Add(dt * 1000.0);
                    SampleProfilers();
                    SampleFrameTiming();
                    if (_phaseTimer >= DurationSeconds) Finish();
                    break;
                }
            }
        }

        void ApplyMotion()
        {
            if (_rig == null) return;
            float frac = Mathf.Clamp01(_phaseTimer / DurationSeconds);
            float targetYaw = YawTotalDegrees * frac;
            float dYaw = targetYaw - _yawApplied;
            _yawApplied = targetYaw;
            _rig.transform.RotateAround(_startPos, Vector3.up, dYaw);
            if (DollyDistance != 0f)
            {
                var fwd = _startRot * Vector3.forward; fwd.y = 0f; fwd.Normalize();
                _rig.transform.position = _startPos + fwd * (DollyDistance * frac);
            }
        }

        void BeginWarmup()
        {
            _rig = FindObjectOfType<HardwareRig>();
            if (_rig != null) { _startPos = _rig.transform.position; _startRot = _rig.transform.rotation; }
            _yawApplied = 0f;
            _frameMs.Clear();
            _sumCpu = _sumGpu = 0; _cpuGpuSamples = 0;
            _sumDraw = _sumSetPass = _sumTris = _sumVerts = _sumBatches = 0; _renderSamples = 0;
            _sumMain = _sumRender = 0; _threadSamples = 0;
            _peakReserved = _peakSystem = 0; _sumGCPerFrame = 0; _gcSamples = 0;
            _phaseTimer = 0f; _phase = Phase.Warmup;
            Debug.Log($"[PerformanceBenchmark] Warmup {WarmupSeconds}s (rig={(_rig ? _rig.name : "none")})");
        }

        void BeginRunning()
        {
            _phaseTimer = 0f; _phase = Phase.Running;
            Debug.Log($"[PerformanceBenchmark] RUNNING {DurationSeconds}s — keep the headset still.");
        }

        void SampleProfilers()
        {
            bool anyThread = false;
            if (_rMain.Valid) { _sumMain += _rMain.LastValue; anyThread = true; }
            if (_rRender.Valid) { _sumRender += _rRender.LastValue; anyThread = true; }
            if (anyThread) _threadSamples++;

            if (_rDraw.Valid)
            {
                _sumDraw += _rDraw.LastValue;
                _sumSetPass += _rSetPass.Valid ? _rSetPass.LastValue : 0;
                _sumTris += _rTris.Valid ? _rTris.LastValue : 0;
                _sumVerts += _rVerts.Valid ? _rVerts.LastValue : 0;
                _sumBatches += _rBatches.Valid ? _rBatches.LastValue : 0;
                _renderSamples++;
            }
            if (_rReserved.Valid) _peakReserved = Math.Max(_peakReserved, _rReserved.LastValue);
            if (_rSystem.Valid) _peakSystem = Math.Max(_peakSystem, _rSystem.LastValue);
            if (_rGC.Valid) { _sumGCPerFrame += _rGC.LastValue; _gcSamples++; }
        }

        void SampleFrameTiming()
        {
            FrameTimingManager.CaptureFrameTimings();
            uint n = FrameTimingManager.GetLatestTimings(1, _ft);
            if (n > 0)
            {
                _sumCpu += _ft[0].cpuFrameTime;
                _sumGpu += _ft[0].gpuFrameTime;
                _cpuGpuSamples++;
            }
        }

        void Finish()
        {
            _phase = Phase.Done;

            var sorted = new List<double>(_frameMs);
            sorted.Sort();
            int nn = sorted.Count;
            double mean = 0; foreach (var v in sorted) mean += v; if (nn > 0) mean /= nn;
            double median = Pct(sorted, 50), p95 = Pct(sorted, 95), p99 = Pct(sorted, 99);
            double min = nn > 0 ? sorted[0] : 0, max = nn > 0 ? sorted[nn - 1] : 0;
            double avgFps = mean > 0 ? 1000.0 / mean : 0;
            double lowFps = p99 > 0 ? 1000.0 / p99 : 0;
            double avgCpu = _cpuGpuSamples > 0 ? _sumCpu / _cpuGpuSamples : 0;
            double avgGpu = _cpuGpuSamples > 0 ? _sumGpu / _cpuGpuSamples : 0;
            double avgMain = _threadSamples > 0 ? _sumMain / _threadSamples / 1e6 : 0; // ns -> ms
            double avgRender = _threadSamples > 0 ? _sumRender / _threadSamples / 1e6 : 0;
            double avgDraw = _renderSamples > 0 ? _sumDraw / _renderSamples : 0;
            double avgSetPass = _renderSamples > 0 ? _sumSetPass / _renderSamples : 0;
            double avgBatches = _renderSamples > 0 ? _sumBatches / _renderSamples : 0;
            double avgTris = _renderSamples > 0 ? _sumTris / _renderSamples : 0;
            double avgGC = _gcSamples > 0 ? _sumGCPerFrame / _gcSamples : 0;

            var ci = CultureInfo.InvariantCulture;
            string plat = PlatformTag();
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string dir = Application.persistentDataPath;
            string sumPath = Path.Combine(dir, $"bench_{plat}_{ts}_summary.csv");
            string framePath = Path.Combine(dir, $"bench_{plat}_{ts}_frames.csv");

            var sb = new StringBuilder();
            sb.AppendLine("key,value");
            sb.AppendLine($"platform,{plat}");
            sb.AppendLine($"device,{SystemInfo.deviceModel}");
            sb.AppendLine($"gpu,{SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"graphicsAPI,{SystemInfo.graphicsDeviceType}");
            sb.AppendLine($"xrDevice,{XRSettings.loadedDeviceName}");
            sb.AppendLine($"vrActive,{XRSettings.isDeviceActive}");
            sb.AppendLine($"eyeTexRes,{XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}");
            sb.AppendLine($"screen,{Screen.width}x{Screen.height}");
            sb.AppendLine($"devBuild,{Debug.isDebugBuild}");
            sb.AppendLine($"targetFrameRate,{Application.targetFrameRate}");
            sb.AppendLine($"scene,{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            sb.AppendLine($"durationSec,{DurationSeconds.ToString(ci)}");
            sb.AppendLine($"frames,{nn}");
            sb.AppendLine($"frameMs_mean,{mean.ToString("F3", ci)}");
            sb.AppendLine($"frameMs_median,{median.ToString("F3", ci)}");
            sb.AppendLine($"frameMs_p95,{p95.ToString("F3", ci)}");
            sb.AppendLine($"frameMs_p99,{p99.ToString("F3", ci)}");
            sb.AppendLine($"frameMs_min,{min.ToString("F3", ci)}");
            sb.AppendLine($"frameMs_max,{max.ToString("F3", ci)}");
            sb.AppendLine($"fps_avg,{avgFps.ToString("F1", ci)}");
            sb.AppendLine($"fps_1pct_low,{lowFps.ToString("F1", ci)}");
            sb.AppendLine($"cpuFrameMs_avg,{avgCpu.ToString("F3", ci)}");
            sb.AppendLine($"gpuFrameMs_avg,{avgGpu.ToString("F3", ci)}");
            sb.AppendLine($"mainThreadMs_avg,{avgMain.ToString("F3", ci)}");
            sb.AppendLine($"renderThreadMs_avg,{avgRender.ToString("F3", ci)}");
            sb.AppendLine($"drawCalls_avg,{avgDraw.ToString("F0", ci)}");
            sb.AppendLine($"setPass_avg,{avgSetPass.ToString("F0", ci)}");
            sb.AppendLine($"batches_avg,{avgBatches.ToString("F0", ci)}");
            sb.AppendLine($"triangles_avg,{avgTris.ToString("F0", ci)}");
            sb.AppendLine($"gcPerFrameBytes_avg,{avgGC.ToString("F0", ci)}");
            sb.AppendLine($"peakReservedMB,{(_peakReserved / 1048576.0).ToString("F1", ci)}");
            sb.AppendLine($"peakSystemMB,{(_peakSystem / 1048576.0).ToString("F1", ci)}");

            try { File.WriteAllText(sumPath, sb.ToString()); }
            catch (Exception e) { Debug.LogError("[PerformanceBenchmark] write failed: " + e.Message); }

            var fb = new StringBuilder();
            fb.AppendLine("frameIndex,frameMs");
            for (int i = 0; i < _frameMs.Count; i++) fb.AppendLine($"{i},{_frameMs[i].ToString("F3", ci)}");
            try { File.WriteAllText(framePath, fb.ToString()); } catch { }

            Debug.Log($"[PerformanceBenchmark] DONE [{plat}] -> {sumPath}\n" +
                      $"mean {mean:F2}ms ({avgFps:F1} fps) | p99 {p99:F2}ms (1% low {lowFps:F1} fps) | " +
                      $"CPU {avgCpu:F2}ms / GPU {avgGpu:F2}ms | draws {avgDraw:F0} | tris {avgTris:F0} | peakMem {_peakReserved / 1048576.0:F0}MB");
        }

        static double Pct(List<double> sorted, double p)
        {
            if (sorted.Count == 0) return 0;
            double idx = (p / 100.0) * (sorted.Count - 1);
            int lo = (int)Math.Floor(idx), hi = (int)Math.Ceiling(idx);
            if (lo == hi) return sorted[lo];
            return sorted[lo] + (sorted[hi] - sorted[lo]) * (idx - lo);
        }

        static string PlatformTag()
        {
            if (Application.platform == RuntimePlatform.Android) return "quest_standalone";
            if (XRSettings.isDeviceActive) return "pc_vr_tethered";
            return "pc_desktop";
        }

        void OnGUI()
        {
            if (_phase == Phase.Idle) return;
            string msg = _phase == Phase.Warmup ? "BENCHMARK: warmup..."
                : _phase == Phase.Running ? $"BENCHMARK running {_phaseTimer:F0}/{DurationSeconds:F0}s"
                : "BENCHMARK done - CSV written";
            GUI.Label(new Rect(12, 12, 700, 32), msg);
        }
    }
}
#endif
