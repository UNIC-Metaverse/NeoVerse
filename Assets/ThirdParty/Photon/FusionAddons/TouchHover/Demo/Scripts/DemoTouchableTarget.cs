using Fusion.Addons.Touch;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoTouchableTarget : MonoBehaviour
{
    Touchable touchable;

    private void Awake()
    {
        touchable = GetComponent<Touchable>();
        touchable.onTouch.AddListener(OnTouch);
        touchable.onUnTouch.AddListener(OnUnTouch);
    }

    private void OnTouch()
    {
        Debug.LogError($"[{name}] OnTouch");
    }

    private void OnUnTouch()
    {
        Debug.LogError($"[{name}] OnUnTouch");
    }
}
