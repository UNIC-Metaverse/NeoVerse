using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.XR.Shared.Rig;
using Fusion.XR.Shared.Grabbing;


[DefaultExecutionOrder(DesktopGrabbing.EXECUTION_ORDER)]
public class DesktopGrabbing : NetworkBehaviour
{
    public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 10;
    public bool lookForwardWhileGrabbed = true;
    public Transform forwardTransform;

    RigInfo rigInfo;
    NetworkGrabbable networkGrabbable;
    private void Awake()
    {
        networkGrabbable = GetComponent<NetworkGrabbable>();
    }

    public override void Render()
    {
        base.Render();
        if (rigInfo == null)
        {
            rigInfo = RigInfo.FindRigInfo(Runner);
        }
        if (forwardTransform == null) forwardTransform = networkGrabbable.transform;
        if (rigInfo == null || rigInfo.localHardwareRigKind != RigInfo.RigKind.Desktop) return;
        if (networkGrabbable.IsGrabbed == false) return;

        if (lookForwardWhileGrabbed)
        {
            var targetRotation = Quaternion.LookRotation(rigInfo.localHardwareRig.headset.transform.forward, rigInfo.localHardwareRig.headset.transform.up);
            var it = networkGrabbable.transform;

            var localForwardRotation = Quaternion.Inverse(it.rotation) * forwardTransform.rotation;
            it.rotation = targetRotation * Quaternion.Inverse(localForwardRotation);
        }
    }
}
