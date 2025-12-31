using Fusion;
using Fusion.Addons.DesktopFocusAddon;
using Fusion.Addons.Drawing;
using Fusion.Addons.Touch;
using Fusion.XR.Shared.Grabbing;
using Fusion.XR.Shared.Rig;

using UnityEngine;
using UnityEngine.InputSystem;


[DefaultExecutionOrder(ScreenDrawing.EXECUTION_ORDER)]

public class ScreenDrawing : NetworkBehaviour
{
    public const int EXECUTION_ORDER = NetworkGrabbable.EXECUTION_ORDER + 5;

    RigInfo rigInfo;
    [SerializeField] 
    DesktopFocus desktopFocus;
    [SerializeField]
    Board board;
    [SerializeField]
    Collider focusToggleCollider;
    [SerializeField]
    Transform orientationTransform;
    Drawer drawer;
    [Header("Layers")]
    // Layer of the surfaces used for drawing (and in parralele of which grabbed objects move)
    public LayerMask layerForDrawingStart;
    // Layer valid for interaction (including drawing)
    public LayerMask layerForInteraction;
    bool focusColliderDisabled = false;
    Camera focusCamera;

    [Header("Camera configuration")]
    public bool useOrthographicProjection = true;
    public float orthoCameraMargin = 0.01f;
    bool initialFocusCameraOrtho = false;
    float initialFocusCameraOrthoSize = 0;


    private void Awake()
    {
        if (layerForDrawingStart == 0) layerForDrawingStart = ~0;
        if (layerForInteraction == 0) layerForInteraction = ~0;
        if (rigInfo == null) rigInfo = RigInfo.FindRigInfo(allowSceneSearch: true);
        if (board == null) board = GetComponentInParent<Board>();
        if(focusToggleCollider == null) focusToggleCollider = GetComponent<Collider>();
        if (orientationTransform == null && board) orientationTransform = board.transform;
        if (orientationTransform == null) orientationTransform = transform;
        if (desktopFocus == null) desktopFocus = board.GetComponentInChildren<DesktopFocus>();
        if (desktopFocus)
        {
            desktopFocus.closeOnClick = false;
        }
    }

    public override void Spawned()
    {
        base.Spawned();
    }

    public void ScreenTouch()
    {
        if(desktopFocus.hasFocus)
        {
            return;
        }
        desktopFocus.ActivateFocus();
        focusToggleCollider.enabled = false;
        focusColliderDisabled = true;
        focusCamera = desktopFocus.GetComponentInChildren<Camera>();
        if (!focusCamera) return;

        if (!useOrthographicProjection) return;
        initialFocusCameraOrtho = focusCamera.orthographic;
        initialFocusCameraOrthoSize = focusCamera.orthographicSize;
        focusCamera.orthographic = true;
        var boardScale = board.transform.lossyScale;
        var screenRatio = (float)Screen.width / (float)Screen.height;
        var boardRatio = boardScale.x / boardScale.y;
        var targetHeight = board.transform.lossyScale.y;
        if (boardRatio > screenRatio)
        {
            targetHeight *= boardRatio / screenRatio;
        }
        focusCamera.orthographicSize = orthoCameraMargin + targetHeight / 2f;
    }

    void OnFocusdisabled()
    {
        if (useOrthographicProjection && focusCamera)
        {
            focusCamera.orthographic = initialFocusCameraOrtho;
            if (initialFocusCameraOrtho)
            {
                focusCamera.orthographicSize = initialFocusCameraOrthoSize;
            }
        }
        focusToggleCollider.enabled = true;
        focusColliderDisabled = false;
        focusCamera = null;

    }


    private void Update()
    {
        if(focusColliderDisabled && desktopFocus.hasFocus == false)
        {
            OnFocusdisabled();
        }
    }

    public override void FixedUpdateNetwork()
    {
        InitDrawer();

        // Restore the normal hand position if a grab has stopped
        if (handPositionRestoreRequired && (grabbed == null || grabbed.IsGrabbed == false))
        {
            rigInfo.localHardwareRig.leftHand.transform.localPosition = defaultHandLocalPosition;
            handPositionRestoreRequired = false;
            grabbed = null;
        }

        if (!focusCamera) return;
        var drawingRay = focusCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (isDrawing)
        {
            if (Physics.Raycast(drawingRay, out var hit, 3f, layerForDrawingStart))
            {
                MoveTipToScreen(hit.point, true);
            }
        }
    }

    void InitDrawer()
    {
        if (drawer == null && rigInfo && rigInfo.localNetworkedRig)
        {
            drawer = rigInfo.localNetworkedRig.GetComponentInChildren<Drawer>();
        }
    }

    bool isDrawing;
    bool firstPressChecked = false;
    NetworkGrabbable grabbed;
    Vector3 defaultHandLocalPosition;
    bool handPositionRestoreRequired = false;
    float grabAltiude;

    public override void Render()
    {
        InitDrawer();
        base.Render();

        if (desktopFocus.hasFocus && focusCamera && Mouse.current.leftButton.isPressed)
        {
            var drawingRay = focusCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!firstPressChecked)
            {
                // First focus left click: drawing, grabbing or touching something ? (no filter on the raycast)
                firstPressChecked = true;
                if (Physics.Raycast(drawingRay, out var firstHit, 3f, layerForInteraction))
                {
                    // Drawing if the layer if the object hit is the expected one for drawing
                    isDrawing = layerForDrawingStart == (layerForDrawingStart | (1 << firstHit.collider.gameObject.layer));

                    // Grabbing if the hit object contains a NetworkGrabbable
                    if (!isDrawing)
                    {
                        grabbed = firstHit.collider.GetComponentInParent<NetworkGrabbable>();
                        if (grabbed)
                        {
                            // Hit a grabbable object, putting the hand at the hit position and starts the grabbing
                            defaultHandLocalPosition = rigInfo.localHardwareRig.leftHand.transform.localPosition;
                            rigInfo.localHardwareRig.leftHand.isGrabbing = true;
                            grabAltiude = orientationTransform.InverseTransformPoint(firstHit.point).z;
                            if (useOrthographicProjection)
                            {
                                MoveGrabbingHand(drawingRay);
                            } 
                            else
                            {
                                // Without an orthographic projection, we are not sure the firstHit position and the projection determined in MoveGrabbingHand are aligned.
                                // So to be sure the object is grabbed, we simply put the hand on the hit point (the object will jump a bit once grabbed)
                                rigInfo.localHardwareRig.leftHand.transform.position = firstHit.point;
                                rigInfo.localNetworkedRig.leftHand.transform.position = firstHit.point;
                            }
                        }
                    }

                    // Touching if the hit object contains a Touchable
                    if (!isDrawing && grabbed == null)
                    {
                        var touchable = firstHit.collider.GetComponentInParent<Touchable>();
                        if (touchable)
                        {
                            // Hit a touchable
                            touchable.OnTouch();
                        }
                    }
                }
            }

            if (isDrawing)
            {
                // While drawing, we limit the raycast to hit the drawing surface
                if (Physics.Raycast(drawingRay, out var hit, 3f, layerForDrawingStart))
                {
                    MoveTipToScreen(hit.point);
                }
            }
            else if (grabbed && grabbed.IsGrabbed)
            {
                MoveGrabbingHand(drawingRay);
            }            
        }
        else if (firstPressChecked)
        {
            firstPressChecked = false;

            if (isDrawing)
            {
                ReleaseTip();
                isDrawing = false;
            }

            if (grabbed)
            {
                rigInfo.localHardwareRig.leftHand.isGrabbing = false;
                handPositionRestoreRequired = true;
            }
        }
    }

    void MoveGrabbingHand(Ray drawingRay)
    {
        // While grabbed (we wait for the grabbing confirmation, moving the hand to move the grabbed object
        if (Physics.Raycast(drawingRay, out var hit, 3f, layerForDrawingStart))
        {
            // Move the hand to the appropriate place, and activate grabbing on a hand
            var localHitPosition = orientationTransform.InverseTransformPoint(hit.point);
            var loclaHandPosition = localHitPosition + Vector3.forward * grabAltiude;
            var handPosition = orientationTransform.TransformPoint(loclaHandPosition);
            rigInfo.localHardwareRig.leftHand.transform.position = handPosition;
            rigInfo.localNetworkedRig.leftHand.transform.position = handPosition;
        }
    }
                

    void ReleaseTip()
    {
        if (drawer == null) return;

        drawer.projectionBoard = null;
        drawer.forceUse = false;
    }

    void MoveTipToScreen(Vector3 newTipPosition, bool moveActualObject = false)
    {
        if (drawer == null) return;

        drawer.forceUse = true;
        drawer.projectionBoard = board;

        var localTipRotation = Quaternion.Inverse(drawer.transform.rotation) * drawer.tip.rotation;

        drawer.transform.rotation = orientationTransform.rotation * Quaternion.Inverse(localTipRotation);

        if (moveActualObject == false)
        {
            drawer.grabbable.transform.position = newTipPosition - drawer.tip.position + drawer.grabbable.transform.position;
        }
        else
        {
            drawer.transform.position = newTipPosition - drawer.tip.position + drawer.transform.position;
        }
    }
}
