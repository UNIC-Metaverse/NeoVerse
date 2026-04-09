using Fusion;
using Fusion.Addons.VirtualKeyboard;
using Fusion.Addons.VirtualKeyboard.Touch;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing;
using Fusion.XRShared.GrabbableMagnet;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class StickyNoteText : NetworkBehaviour
{
    [Networked]
    public NetworkString<_512> StickyNote_Text { get; set; }

    [SerializeField] private TouchableTMPInputField touchableTMPInputField;
    [SerializeField] private MagnetPoint magnetPoint;
    [SerializeField] private float disableStickyNoteColliderAfterSnapDuration = 0.3f;
    ChangeDetector changeDetector;

    [SerializeField] private Grabbable grabbable;
    [SerializeField] private InputActionProperty looseFocusAction;
    private List<ApplicationLifeCycleManager> applicationManagers;

    bool disabledApplicationMenus = false;

    private void Awake()
    {
        if (touchableTMPInputField == null)
            touchableTMPInputField = GetComponentInChildren<TouchableTMPInputField>();
        if (touchableTMPInputField == null)
            Debug.LogError("TouchableTMPInputField not found");

        if (magnetPoint == null)
            magnetPoint = GetComponentInChildren<MagnetPoint>();
        if (magnetPoint == null)
            Debug.LogError("MagnetPoint not found");

        touchableTMPInputField.onFocusChange.AddListener(OnTextInputFocusChange);


        if (grabbable == null)
            grabbable = GetComponent<Grabbable>();

        if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode)
        {
            grabbable.onGrab.AddListener(OnStickyNoteGrab);
            grabbable.onUngrab.AddListener(OnStickyNoteUnGrab);
        }

        if (looseFocusAction.reference == null && looseFocusAction.action.bindings.Count == 0)
        {
            looseFocusAction.action.AddBinding($"<Keyboard>/escape");
        }
        looseFocusAction.action.Enable();

        applicationManagers = new List<ApplicationLifeCycleManager>(FindObjectsOfType<ApplicationLifeCycleManager>(true));
    }

    float maxGrabDurationForTouch = 0.3f;
    float startGrabTime = 0f;

    private void OnStickyNoteUnGrab()
    {
        touchableTMPInputField.canReceiveFocus = true;

        if (Time.time > (startGrabTime + maxGrabDurationForTouch))
        {
            // Grab
            touchableTMPInputField.HasFocus = false;
        }
        else
        {
            // Touch
            TakeFocus();
        }
    }

    private void OnStickyNoteGrab()
    {
        startGrabTime = Time.time;
        touchableTMPInputField.canReceiveFocus = false;
        touchableTMPInputField.HasFocus = false;
    }
    
    private void OnTextInputFocusChange()
    {
        if (Object && Object.HasStateAuthority == false && touchableTMPInputField.HasFocus)
        {
            Object.RequestStateAuthority();
        }
    }

    public override void Spawned()
    {
        changeDetector = GetChangeDetector(ChangeDetector.Source.SnapshotFrom);
        TextChanged();
    }

    public void TakeFocus()
    {
        if (Object.HasStateAuthority)
        {
            // Request the focus when the local player spawn a new StickyNote
            if (touchableTMPInputField)
            {
                touchableTMPInputField.HasFocus = true;
            }
            if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode)
            {
                disabledApplicationMenus = true;
                foreach (var applicationManager in applicationManagers)
                    applicationManager.ChangeMenuAuthorization(false);
            }
        }
    }

    void LooseFocus()
    {
        touchableTMPInputField.HasFocus = false;
        if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode)
        {
            disabledApplicationMenus = false;
            foreach (var applicationManager in applicationManagers)
                applicationManager.ChangeMenuAuthorization(true);
        }
    }

    private void OnEnable()
    {
        touchableTMPInputField.onTextChange.AddListener(OnInputFieldTextChange);
        magnetPoint.onSnapToMagnet.AddListener(OnMagnetPointSnapp);
    }

    private void OnDisable()
    {
        touchableTMPInputField.onTextChange.RemoveListener(OnInputFieldTextChange);
        magnetPoint.onSnapToMagnet.RemoveListener(OnMagnetPointSnapp);
    }


    private void OnInputFieldTextChange()
    {
        if (Object && Object.HasStateAuthority)
        {
            StickyNote_Text = touchableTMPInputField.Text;
        }
    }

    private void OnMagnetPointSnapp()
    {
        if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode)
        {
            return;
        }
        touchableTMPInputField.HasFocus = false;
        touchableTMPInputField.canReceiveFocus = false;
        Invoke("RestoreCanReceiveFocus", disableStickyNoteColliderAfterSnapDuration);

    }

    private void RestoreCanReceiveFocus()
    {
        if (touchableTMPInputField)
            touchableTMPInputField.canReceiveFocus = true;
    }

    bool TryDetectTextChange()
    {
        foreach (var changedVar in changeDetector.DetectChanges(this))
        {
            if (changedVar == nameof(StickyNote_Text))
            {
                return true;
            }
        }
        return false;
    }

    public override void Render()
    {
        base.Render();
        // Check if the Text changed
        if (TryDetectTextChange())
        {
            TextChanged();
        }

        if(touchableTMPInputField && touchableTMPInputField.HasFocus && Object.HasStateAuthority == false)
        {
            touchableTMPInputField.HasFocus = false;
        }

        if (KeyboardFocusManager.Instance && KeyboardFocusManager.Instance.IsInDesktopMode & looseFocusAction.action.WasPerformedThisFrame())
        {
            // Warning: if the event system has "Send navigation events" checked, after pressing escape, other post-it might get selected
            LooseFocus();
        }

        if (touchableTMPInputField && touchableTMPInputField.HasFocus == false && disabledApplicationMenus)
        {
            // The focus has been lost due to other components (right click, ...): we reactivate the menu we had disabled
            LooseFocus();
        }
    }

    void TextChanged()
    {
        if (Object.HasStateAuthority == false)
        {
            // update the local text
            touchableTMPInputField.Text = StickyNote_Text.ToString();
        }
    }
}
