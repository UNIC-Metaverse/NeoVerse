using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared.Desktop;
using Fusion.Addons.Avatar;

namespace Fusion.Samples.IndustriesComponents
{
    /// <summary>
    /// Hides the LOCAL player's networked hand meshes when running in Desktop mode.
    ///
    /// In desktop mode the hands exist only to host the teleport interactor ray, so the
    /// visible hand model is a distraction. Each NetworkHand has a
    /// NetworkHandRepresentationManager whose Update() shows/hides the local hand mesh based
    /// on its displayForLocalPlayer flag. This component simply sets that flag to false for
    /// the local rig in desktop mode.
    ///
    /// Scope of the effect:
    ///  - Local only: remote players still see this player's hands (avatar looks normal).
    ///  - Desktop only: VR is untouched, so VR users keep seeing their own hands.
    ///  - Visual only: colliders, grabbing and the teleport ray (a separate LineRenderer)
    ///    keep working.
    ///
    /// Place this on the network rig prefab root (MeetingRoomNetworkRig).
    /// </summary>
    public class DesktopHandHider : MonoBehaviour
    {
        NetworkRig networkRig;

        void Awake()
        {
            networkRig = GetComponent<NetworkRig>();
            if (networkRig == null) networkRig = GetComponentInParent<NetworkRig>();
        }

        void Update()
        {
            if (networkRig == null)
            {
                enabled = false;
                return;
            }

            // Wait until the rig is spawned and authority is known.
            if (networkRig.Object == null || !networkRig.Object.IsValid) return;

            if (networkRig.IsLocalNetworkRig && IsDesktopMode())
            {
                foreach (var manager in GetComponentsInChildren<NetworkHandRepresentationManager>(true))
                {
                    manager.displayForLocalPlayer = false;
                }
            }

            // Decision made (applied or not applicable) — stop polling.
            enabled = false;
        }

        /// <summary>
        /// Desktop mode = the active local hardware rig carries a DesktopController.
        /// Falls back to the RigMode preference written at startup by RigModeAutoDetector.
        /// </summary>
        static bool IsDesktopMode()
        {
            foreach (var rig in FindObjectsOfType<HardwareRig>())
            {
                if (rig.isActiveAndEnabled && rig.GetComponentInChildren<DesktopController>(true) != null)
                {
                    return true;
                }
            }
            return PlayerPrefs.GetString("RigMode") == "Desktop";
        }
    }
}
