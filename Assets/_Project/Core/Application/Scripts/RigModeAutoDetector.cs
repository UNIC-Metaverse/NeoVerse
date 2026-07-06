using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Fusion.Addons.ExtendedRigSelectionAddon;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fusion.Samples.IndustriesComponents
{
    /// <summary>
    /// Detects whether an XR headset is actually available at launch and writes the
    /// "RigMode" preference accordingly, BEFORE ExtendedRigSelection reads it.
    ///
    /// ExtendedRigSelection is configured in SelectedByUserPref mode, which only replays
    /// the last-used rig from PlayerPrefs. That meant a stale "VR" preference caused the
    /// VR rig to load even with no headset attached (no keyboard locomotion => "no movement
    /// in desktop mode"). This component makes headset presence the source of truth.
    ///
    /// A startup override lets you force a rig for testing: hold the Force-Desktop key
    /// (default Numpad 1) or Force-VR key (default Numpad 2) while entering Play mode /
    /// launching. The override is only sampled once, at Awake, because rig selection happens
    /// once at startup (the chosen rig is what connects to the session).
    ///
    /// Runs with a very low execution order so it sets the preference before
    /// ExtendedRigSelection (execution order 0) evaluates it in Awake.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class RigModeAutoDetector : MonoBehaviour
    {
        [Header("Rig kind names (must match ExtendedRigSelection)")]
        [Tooltip("Rig kind name saved when a headset IS detected.")]
        public string vrRigKindName = "VR";

        [Tooltip("Rig kind name saved when NO headset is detected.")]
        public string desktopRigKindName = "Desktop";

        [Header("Detection")]
        [Tooltip("If XR auto-init has not resolved yet, attempt a synchronous loader init to get a definitive answer.")]
        public bool attemptSynchronousInit = true;

        [Header("Startup override (for testing)")]
        [Tooltip("If enabled, holding one of the override keys at launch forces that rig, bypassing headset detection.")]
        public bool enableStartupOverride = true;

#if ENABLE_INPUT_SYSTEM
        [Tooltip("Hold this key at launch to force Desktop mode.")]
        public Key forceDesktopKey = Key.Numpad1;

        [Tooltip("Hold this key at launch to force VR mode.")]
        public Key forceVRKey = Key.Numpad2;
#else
        [Tooltip("Hold this key at launch to force Desktop mode.")]
        public KeyCode forceDesktopKey = KeyCode.Keypad1;

        [Tooltip("Hold this key at launch to force VR mode.")]
        public KeyCode forceVRKey = KeyCode.Keypad2;
#endif

        [Header("Debug")]
        [Tooltip("Log the detection / override result to the console.")]
        public bool verbose = true;

        void Awake()
        {
            string chosen;
            string reason;

            if (enableStartupOverride && IsKeyHeld(forceDesktopKey))
            {
                chosen = desktopRigKindName;
                reason = "startup override (Force-Desktop key held)";
            }
            else if (enableStartupOverride && IsKeyHeld(forceVRKey))
            {
                chosen = vrRigKindName;
                reason = "startup override (Force-VR key held)";
            }
            else
            {
                bool headsetPresent = IsHeadsetPresent();
                chosen = headsetPresent ? vrRigKindName : desktopRigKindName;
                reason = headsetPresent ? "headset detected" : "no headset detected";
            }

            ExtendedRigSelection.SavePreference(chosen);

            if (verbose)
            {
                Debug.Log($"[RigModeAutoDetector] RigMode set to '{chosen}' ({reason}).");
            }
        }

#if ENABLE_INPUT_SYSTEM
        static bool IsKeyHeld(Key key)
        {
            if (key == Key.None) return false;
            var kb = Keyboard.current;
            return kb != null && kb[key].isPressed;
        }
#else
        static bool IsKeyHeld(KeyCode key)
        {
            if (key == KeyCode.None) return false;
            return Input.GetKey(key);
        }
#endif

        /// <summary>
        /// Returns true when an XR HMD is actually usable.
        /// Primary signal: the XR Management active loader (OpenXR only starts when an HMD is present).
        /// Fallback: an InputDevice with the HeadMounted characteristic.
        /// </summary>
        bool IsHeadsetPresent()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance != null
                ? XRGeneralSettings.Instance.Manager
                : null;

            if (manager != null)
            {
                // If "Initialize XR on Startup" has not resolved (or is disabled), force a
                // synchronous attempt so we get a deterministic answer this frame.
                if (manager.activeLoader == null && attemptSynchronousInit)
                {
                    manager.InitializeLoaderSync();
                }

                if (manager.activeLoader != null)
                {
                    // A loader only becomes active when a headset actually started up.
                    manager.StartSubsystems();
                    return true;
                }
            }

            // Fallback for setups not driven by XR Management.
            if (XRSettings.isDeviceActive && !string.IsNullOrEmpty(XRSettings.loadedDeviceName))
            {
                return true;
            }

            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return device.isValid;
        }
    }
}
