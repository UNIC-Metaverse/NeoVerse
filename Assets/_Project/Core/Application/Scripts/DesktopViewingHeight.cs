using UnityEngine;
using UnityEngine.XR;

namespace Fusion.Samples.IndustriesComponents
{
    /// <summary>
    /// Desktop viewing height: Standing or Seated.
    ///
    /// Desktop users are normally sat at a monitor, so a standing eye height can feel wrong.
    /// This stores the choice in PlayerPrefs and applies it to the desktop rig's camera at
    /// startup, mirroring the "RigMode" preference pattern used by RigModeAutoDetector and
    /// ExtendedRigSelection - so the choice can be made once in AvatarSelection and is then
    /// picked up by every space the player loads afterwards.
    ///
    /// Only the eye height changes; the rig still stands on the ground (see NavMeshGroundClamp),
    /// so the networked avatar's feet stay planted and remote players see the player at the
    /// right place.
    ///
    /// VR is deliberately untouched: there the headset's tracked pose supplies the real height
    /// and XROrigin zeroes the Camera Offset in Floor tracking mode, so writing a camera height
    /// would either be discarded or fight the tracking. Apply() no-ops when a headset is active.
    ///
    /// Wiring the AvatarSelection UI: put this component on the menu object and hook a Toggle's
    /// OnValueChanged to SetSeated(bool), or two buttons to SetSeated() / SetStanding(). With no
    /// camera to move it just records the preference.
    /// </summary>
    public class DesktopViewingHeight : MonoBehaviour
    {
        public const string ViewingHeightPrefKey = "ViewingHeight";
        public const string SeatedValue = "Seated";
        public const string StandingValue = "Standing";

        [Header("Target")]
        [Tooltip("Camera to move. If empty, resolves 'Camera Offset/Main Camera' under this object at Awake.")]
        public Transform cameraTransform;

        [Header("Eye heights (metres above the rig)")]
        [Tooltip("Eye height when Standing. 1.74 gives roughly a 1.85 m person.")]
        public float standingEyeHeight = 1.74f;

        [Tooltip("Eye height when Seated, i.e. sat in a chair rather than a shorter person.")]
        public float seatedEyeHeight = 1.2f;

        [Header("Debug")]
        public bool verbose = true;

        /// <summary>True when the player last chose Seated. Defaults to Standing.</summary>
        public static bool IsSeated => PlayerPrefs.GetString(ViewingHeightPrefKey, StandingValue) == SeatedValue;

        /// <summary>Records the choice so later scenes pick it up. Does not move anything itself.</summary>
        public static void SavePreference(bool seated)
        {
            PlayerPrefs.SetString(ViewingHeightPrefKey, seated ? SeatedValue : StandingValue);
            PlayerPrefs.Save();
        }

        // UnityEvent-friendly entry points for the AvatarSelection UI.
        public void SetSeated(bool seated)
        {
            SavePreference(seated);
            Apply();
        }

        public void SetSeated() => SetSeated(true);
        public void SetStanding() => SetSeated(false);

        void Awake()
        {
            if (cameraTransform == null)
            {
                var offset = transform.Find("Camera Offset");
                if (offset != null) cameraTransform = offset.Find("Main Camera");
            }

            Apply();
        }

        /// <summary>Writes the current preference to the camera. Safe to call at any time.</summary>
        public void Apply()
        {
            if (cameraTransform == null) return;

            // In VR the tracked pose owns the camera - never author a height over it.
            if (XRSettings.isDeviceActive) return;

            var seated = IsSeated;
            var eyeHeight = seated ? seatedEyeHeight : standingEyeHeight;

            var localPosition = cameraTransform.localPosition;
            localPosition.y = eyeHeight;
            cameraTransform.localPosition = localPosition;

            if (verbose)
            {
                Debug.Log($"[DesktopViewingHeight] {(seated ? SeatedValue : StandingValue)} => eye height {eyeHeight:F2} m.");
            }
        }
    }
}
