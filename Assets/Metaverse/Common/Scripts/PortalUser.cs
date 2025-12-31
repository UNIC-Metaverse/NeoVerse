using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalUser : MonoBehaviour
{
    NetworkRig networkRig;

    // Start is called before the first frame update
    void Start()
    {
        networkRig = GetComponent<NetworkRig>();
    }

    private void OnDestroy()
    {
        // inform all portals in case the user was on a portal to stop the teleport animation
        foreach (var spaceLoader in FindObjectsOfType<SpaceLoader>())
        {
            spaceLoader.OnPortalUserDetroy(networkRig);
        }
    }
}
