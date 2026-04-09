using Fusion.Addons.Touch.UI;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Locomotion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Fusion.Addons.Touch
{
    /***
     * 
     * BeamToucher simulates a touch when the player uses the beam and presses the trigger button
     * It is used to interact with Touchable objects or UI elements (button & sliders)
     * 
     ***/
    public class BeamToucher : MonoBehaviour
    {
        [System.Flags]
        public enum TouchableComponents
        {
            Touchable = 1,
            UITouchButton = 2,
            TouchableSlider = 4
        }
        public TouchableComponents touchableComponents = TouchableComponents.UITouchButton | TouchableComponents.TouchableSlider;
        public bool ShouldTouchUITouchButton => (touchableComponents & TouchableComponents.UITouchButton) != 0;
        public bool ShouldTouchTouchable => (touchableComponents & TouchableComponents.Touchable) != 0;
        public bool ShouldTouchTouchableSlider => (touchableComponents & TouchableComponents.TouchableSlider) != 0;

        public enum TouchMode
        {
            WasPressedThisFrame,
            WasReleasedThisFrame
        }
        public TouchMode touchMode = TouchMode.WasPressedThisFrame;

        RayBeamer beamer;

        Collider latestHitCollider;
        // Cache attributes, to limit the number of GetComponent calls
        UITouchButton uiTouchButton;
        Touchable touchable;
        TouchableSlider slider;
        bool noUITouchableFound = false;
        bool noTouchableFound = false;
        bool noSliderFound = false;

        public InputActionProperty useAction;

        bool continuousSliderTouch = false;

        private void Awake()
        {
            beamer = GetComponentInChildren<RayBeamer>();
            beamer.onHitEnter.AddListener(OnHitEnter);
            beamer.onHitExit.AddListener(OnHitExit);
            beamer.onRelease.AddListener(OnRelease);
        }

        private void Start()
        {
            useAction.EnableWithDefaultXRBindings(side: beamer.hand.side, new List<string> { "trigger" });
        }

        #region RayBeamer callbacks
        private void OnRelease(Collider collider, Vector3 hitPoint)
        {
            ResetColliderInfo();
        }

        private void OnHitExit(Collider collider, Vector3 hitPoint)
        {
            ResetColliderInfo();
        }

        private void OnHitEnter(Collider collider, Vector3 hitPoint)
        {
            if (latestHitCollider != collider)
            {
                ResetColliderInfo();
            }
            latestHitCollider = collider;
        }
        #endregion

        void ResetColliderInfo()
        {
            latestHitCollider = null;
            uiTouchButton = null;
            touchable = null;
            slider = null;
            noUITouchableFound = false;
            noTouchableFound = false;
            noSliderFound = false;
        }

        private void Update()
        {
            if (!latestHitCollider) return;
            bool used = touchMode == TouchMode.WasPressedThisFrame ? useAction.action.WasPressedThisFrame() : useAction.action.WasReleasedThisFrame();
            if (used)
            {
                if (ShouldTouchTouchable && noTouchableFound == false)
                {
                    if (!uiTouchButton) uiTouchButton = latestHitCollider.GetComponentInParent<UITouchButton>();

                    // if an UITouchButton exist, we don't check for its touchable (as it has a touchable). This way, if beam touching UITouchButton is not authorized, it won't be triggered due to its touchable
                    if (!uiTouchButton)
                    {
                        if (!touchable) touchable = latestHitCollider.GetComponentInParent<Touchable>();

                        if (touchable)
                            touchable.TryInstantTouch();
                        else
                            noTouchableFound = true;
                    }
                }

                if (ShouldTouchUITouchButton && noUITouchableFound == false)
                {
                    if (!uiTouchButton) uiTouchButton = latestHitCollider.GetComponentInParent<UITouchButton>();
                    if (uiTouchButton)
                        uiTouchButton.touchable.TryInstantTouch();
                    else
                        noUITouchableFound = true;
                }
            }

            // For slider, we check continuously (no need that the hit was this frame), to be able to move slider continuously)
            if (noSliderFound == false && useAction.action.IsPressed())
            {
                if (ShouldTouchTouchableSlider)
                {
                    bool isANewSliderTouch = continuousSliderTouch == false;

                    continuousSliderTouch = true;

                    if (!slider) slider = latestHitCollider.GetComponentInParent<TouchableSlider>();
                    if (slider)
                        slider.MoveSliderToPosition(beamer.lastHit, instantMove: isANewSliderTouch);
                    else
                        noSliderFound = true;
                }
            }
            else
            {
                continuousSliderTouch = false;
            }
        }
    }
}