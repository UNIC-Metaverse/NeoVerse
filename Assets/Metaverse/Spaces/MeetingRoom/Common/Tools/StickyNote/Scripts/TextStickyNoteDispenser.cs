using Fusion.Addons.VirtualKeyboard;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextStickyNoteDispenser : StickyNoteDispenser
{
    protected override GameObject Spawn()
    {
        var spawnedObject = base.Spawn();
        var stickyNoteText = spawnedObject.GetComponentInChildren<StickyNoteText>();

        if (stickyNoteText != null)
        {
            // Take the focus in VR only
            if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode == false)
                stickyNoteText.TakeFocus();
        }

        return spawnedObject;
    }
}
