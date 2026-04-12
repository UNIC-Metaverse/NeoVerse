using Fusion;
using Fusion.XR.Shared.Grabbing;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestoreGrabbableObjectPositionAfterTimeOut : NetworkBehaviour
{
    [SerializeField] private float timeOut;
    private float lastUnGrabTime = 0f;
    NetworkGrabbable networkGrabbable;

    [Networked]
    Vector3 InitialObjectPosition { get; set; }
    [Networked]
    Quaternion InitialObjectRotation { get; set; }
    [Networked]
    NetworkBool IsAtStartPosition { get; set; }


    // Start is called before the first frame update
    private void Awake()
    {
        networkGrabbable = GetComponent<NetworkGrabbable>();
        if (!networkGrabbable)
            Debug.LogError("NetworkGrabbable not found !");
    }

    public override void Spawned()
    {
        base.Spawned();

        if (networkGrabbable && networkGrabbable.Object && networkGrabbable.Object.HasStateAuthority)
        {
            InitialObjectPosition = transform.position;
            InitialObjectRotation = transform.rotation;
            IsAtStartPosition = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if has StateAuthority
        if (networkGrabbable && networkGrabbable.Object && networkGrabbable.Object.HasStateAuthority)
        {
            // Check if the object has moved
            if (networkGrabbable.IsGrabbed)
            {
                // reset last ungrab time
                lastUnGrabTime = 0f;
                if (IsAtStartPosition == true)
                    //Object has moved
                    IsAtStartPosition = false;
            }

            // Check if the object is ungrabbed and at initial position
            if (networkGrabbable.IsGrabbed == false && IsAtStartPosition == false)
            {
                // Check if object has just been ungrabbed
                if (lastUnGrabTime == 0f)
                {
                    //object just ungrabbed
                    lastUnGrabTime = Time.time;
                }
                // Check if the object has been ungrabbed for longer than the timer.
                else if (Time.time > (lastUnGrabTime + timeOut))
                {
                    // time to move object to initial position
                    transform.position = InitialObjectPosition;
                    transform.rotation = InitialObjectRotation;
                    IsAtStartPosition = true;
                    lastUnGrabTime = 0f;
                }
            }
        }
    }
}
