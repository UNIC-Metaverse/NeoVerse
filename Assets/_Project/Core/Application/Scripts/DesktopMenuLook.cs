using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem.XR;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared.Desktop;

namespace Fusion.Samples.IndustriesComponents
{
    /// <summary>
    /// Configures mouse look for a menu / selection scene (e.g. AvatarSelection) depending on
    /// whether the app is running in Desktop or VR mode.
    ///
    /// Desktop:
    ///  - Disables the camera's TrackedPoseDriver. In desktop there is no headset, but the
    ///    driver still writes the camera pose every frame, fighting MouseCamera and producing
    ///    stuttered, non-functional rotation. Disabling it lets the mouse fully own the camera.
    ///  - Enables MouseCamera and (optionally) turns on always-on look (forceRotation).
    ///
    /// VR:
    ///  - Disables MouseCamera so it never fights the headset head-tracking.
    ///
    /// Place on the HardwareRig (alongside / above the MouseCamera).
    /// </summary>
    [DefaultExecutionOrder(100)] // after MouseCamera.Awake/Start have resolved the rig
    public class DesktopMenuLook : MonoBehaviour
    {
        [Tooltip("The MouseCamera to drive. Auto-found in children if left empty.")]
        public MouseCamera mouseCamera;

        [Tooltip("In desktop mode, enable always-on look (no need to hold the right mouse button).")]
        public bool alwaysOnLookInDesktop = true;

        void Start()
        {
            if (mouseCamera == null) mouseCamera = GetComponentInChildren<MouseCamera>(true);

            var rig = GetComponent<HardwareRig>();
            if (rig == null) rig = GetComponentInParent<HardwareRig>();

            bool desktop = IsDesktopMode();

            if (desktop)
            {
                // Stop XR head-tracking from overwriting the mouse-driven camera rotation.
                if (rig != null && rig.headset != null)
                {
                    var trackedPoseDriver = rig.headset.GetComponent<TrackedPoseDriver>();
                    if (trackedPoseDriver != null) trackedPoseDriver.enabled = false;
                }

                if (mouseCamera != null)
                {
                    mouseCamera.enabled = true;
                    if (alwaysOnLookInDesktop) mouseCamera.forceRotation = true;
                    mouseCamera.CaptureReferenceYaw();
                }
            }
            else
            {
                // VR: the headset drives the camera; mouse look must not interfere.
                if (mouseCamera != null) mouseCamera.enabled = false;
            }
        }

        /// <summary>
        /// Desktop = no active XR HMD. Mirrors the detection used elsewhere in the project.
        /// </summary>
        static bool IsDesktopMode()
        {
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager != null && manager.activeLoader != null) return false; // XR running => VR
            if (XRSettings.isDeviceActive) return false;
            return true;
        }
    }
}
