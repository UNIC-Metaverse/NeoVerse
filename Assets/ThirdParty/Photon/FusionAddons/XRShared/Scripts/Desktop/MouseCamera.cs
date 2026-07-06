using Fusion.XR.Shared.Rig;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Fusion.XR.Shared.Desktop
{
    /***
     *
     * MouseCamera computes the rotation of the head according to the mouse movements.
     *
     * CUSTOMISED (NeoVerse):
     *  1) The original implementation scaled the mouse delta by Time.deltaTime and by
     *     (referenceScreenWidth / Screen.width). A mouse delta is a displacement (already
     *     accumulated per frame by the input system), not a rate, so multiplying by
     *     Time.deltaTime made the sensitivity depend on framerate, and the screen-width term
     *     made it depend on resolution (stiff at high fullscreen res, fast in a small window).
     *     Both factors were removed so the look sensitivity is identical in the editor and in
     *     any build resolution / framerate. Tune feel with the `sensitivity` field.
     *  2) Added an optional yaw clamp (clampYaw / maxYawAngle) so the look can be limited to a
     *     cone around the starting orientation (useful for menu / selection scenes). Off by
     *     default, so free-look scenes are unaffected.
     *
     ***/
    public class MouseCamera : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        public InputActionProperty mouseXAction;
        public InputActionProperty mouseYAction;
#endif
        public bool forceRotation = false;

        public HardwareRig rig;
        [Header("Mouse point of view")]
        [Tooltip("Safety clamp on the maximum head turn applied in a single frame (degrees). Prevents huge jumps, e.g. when the window regains focus.")]
        public Vector2 maxMouseInput = new Vector2(40, 40);
        [Tooltip("Framerate-independent smoothing rate for the head rotation. 0 = instant / crisp. Higher = snappier; lower = smoother/laggier.")]
        public float maxHeadRotationSpeed = 30;
        [Tooltip("Look sensitivity (degrees of rotation per unit of mouse movement, before the internal scale). Increase for faster turning.")]
        public Vector2 sensitivity = new Vector2(15, 15);
        public float maxHeadAngle = 65;
        public float minHeadAngle = 65;

        [Header("Yaw clamp (optional)")]
        [Tooltip("If enabled, horizontal look is limited to +/- maxYawAngle around the orientation captured at startup. Useful for menu / avatar-selection scenes.")]
        public bool clampYaw = false;
        [Tooltip("Maximum horizontal look angle (degrees) to each side of the starting orientation.")]
        public float maxYawAngle = 45f;

        // Degrees of rotation per (sensitivity * mouse-count). Keeps the `sensitivity` numbers
        // in a friendly range while decoupling the raw pixel delta from the final angle.
        const float SensitivityScale = 0.05f;

        Vector3 rotation = Vector3.zero;
        float referenceYaw;
        bool referenceYawCaptured = false;

        Transform Head => rig == null ? null : rig.headset.transform;


        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (mouseXAction.action.bindings.Count == 0) mouseXAction.action.AddBinding("<Mouse>/delta/x");
            if (mouseYAction.action.bindings.Count == 0) mouseYAction.action.AddBinding("<Mouse>/delta/y");

            mouseXAction.action.Enable();
            mouseYAction.action.Enable();
#else
            Debug.LogError("Missing com.unity.inputsystem package");
#endif
            if (rig == null) rig = GetComponentInParent<HardwareRig>();
        }

        private void Start()
        {
            CaptureReferenceYaw();
        }

        /// <summary>
        /// Records the current head yaw as the centre of the yaw clamp cone.
        /// Call again if the rig is re-oriented and you want to re-centre the allowed range.
        /// </summary>
        public void CaptureReferenceYaw()
        {
            if (Head != null)
            {
                referenceYaw = Head.eulerAngles.y;
                referenceYawCaptured = true;
            }
        }


        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (forceRotation || Mouse.current.rightButton.isPressed)
            {
                // Raw per-frame mouse delta (pixels). No Time.deltaTime and no Screen.width
                // scaling => identical feel at any framerate or resolution.
                float dx = mouseXAction.action.ReadValue<float>() * sensitivity.x * SensitivityScale;
                float dy = mouseYAction.action.ReadValue<float>() * sensitivity.y * SensitivityScale;

                // Safety clamp against very large single-frame jumps.
                dx = Mathf.Clamp(dx, -maxMouseInput.x, maxMouseInput.x);
                dy = Mathf.Clamp(dy, -maxMouseInput.y, maxMouseInput.y);

                rotation.x = Head.eulerAngles.x - dy;
                rotation.y = Head.eulerAngles.y + dx;

                if (rotation.x > maxHeadAngle && rotation.x < (360 - minHeadAngle))
                {
                    if (Mathf.Abs(maxHeadAngle - rotation.x) < Mathf.Abs(rotation.x - (360 - minHeadAngle)))
                    {
                        rotation.x = maxHeadAngle;
                    }
                    else
                    {
                        rotation.x = -minHeadAngle;
                    }
                }
                else if (rotation.x < -minHeadAngle)
                {
                    rotation.x = -minHeadAngle;
                }

                // Optional horizontal clamp around the startup orientation.
                if (clampYaw)
                {
                    if (!referenceYawCaptured) CaptureReferenceYaw();
                    float yawDelta = Mathf.DeltaAngle(referenceYaw, rotation.y);
                    yawDelta = Mathf.Clamp(yawDelta, -maxYawAngle, maxYawAngle);
                    rotation.y = referenceYaw + yawDelta;
                }

                var targetRotation = Quaternion.Euler(rotation);
                if (maxHeadRotationSpeed > 0f)
                {
                    // Framerate-independent exponential smoothing.
                    float t = 1f - Mathf.Exp(-maxHeadRotationSpeed * Time.deltaTime);
                    Head.rotation = Quaternion.Slerp(Head.rotation, targetRotation, t);
                }
                else
                {
                    Head.rotation = targetRotation;
                }
            }
#endif
        }
    }
}
