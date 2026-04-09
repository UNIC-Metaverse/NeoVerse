using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/***
 * 
 * LateUpdateFollow can be used on an object that must follow a target.
 * 
 ***/
public class LateUpdateFollow : MonoBehaviour
{
    public Transform target;

    private void LateUpdate()
    {
        Follow();
    }

    [ContextMenu("Follow")]
    public void Follow() { 
        if (!target) return;
        transform.SetPositionAndRotation(target.position, target.rotation);
    }
}
