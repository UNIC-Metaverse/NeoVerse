using Fusion.Samples.IndustriesComponents;
using Fusion.XR.Shared.Locomotion;
using Fusion.XR.Shared.Rig;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuDistanceManager : MonoBehaviour
{
    [SerializeField] HardwareRig rig;
    [SerializeField] GameObject uiRoot;

    AvatarCustomizer avatarCustomizer;
    float startTime;
    bool firstPlaced = false;
    private float uiCameraOffet = 0.3f;

    [SerializeField] float delayBeforeDisplayingPanel = 2;
    [SerializeField] float distanceToUser = 0.55f;
    [SerializeField] float minAngleTomoveUI = 90f;
    [SerializeField] float minHeightDiffTomoveUI = 0.35f;
    [SerializeField] float UIMoveSpeed = 2f;

    private bool UIRotationInProgress = false;  // flag when rotation is in progress to avoid stopping as soon as minAngleTomoveUI is reached
    private bool UIMoveDistanceInProgress = false;
    private bool UIMoveHeightInProgress = false;


    private void Awake()
    {
        if (avatarCustomizer == null)
            avatarCustomizer = GetComponent<AvatarCustomizer>();

        if (rig == null)
            rig = FindObjectOfType<HardwareRig>();

        if (uiRoot == null) uiRoot = gameObject;
    }


    private void Update()
    {
        // do nothing in desktop mode or when is UI has been moved
        if (!avatarCustomizer.defaultVRMode) return;

        // wait before moving the UI in front of the user
        if ((Time.time - startTime) > delayBeforeDisplayingPanel)
        {
            firstPlaced = true;
            CheckIfUIIsInFrontOfUser();
        }
        else
        {
            PlaceCanvasInFrontOfUser();
        }
    }

    private void CheckIfUIIsInFrontOfUser()
    {
        if (avatarCustomizer.defaultVRMode)
        {
            CheckAngleBetweenHeadAndUI();
            CheckHeightDifferenceBetweenHeadAndUI();
            CheckDistanceBetweenHeadAndUI();
        }
    }

    private void CheckDistanceBetweenHeadAndUI()
    {
        var canvasDistance = Mathf.Abs(Vector3.Distance(transform.position, rig.headset.transform.position));
        if ((canvasDistance < distanceToUser * 0.6f) || (canvasDistance > 2 * distanceToUser) || UIMoveDistanceInProgress)
        {
            UIMoveDistanceInProgress = true;
            PlaceCanvasInFrontOfUser();
        }
        if (UIMoveDistanceInProgress && ((canvasDistance > distanceToUser) && (canvasDistance < distanceToUser * 1.1)) || ((canvasDistance < distanceToUser) && (canvasDistance > distanceToUser * 0.9)))
        {
            UIMoveDistanceInProgress = false;
        }
    }

    private void CheckHeightDifferenceBetweenHeadAndUI()
    {
        var headPosition = rig.headset.transform.position.y - uiCameraOffet;
        float UIHeightDifference = Mathf.Abs(transform.position.y - headPosition);

        if ((UIHeightDifference > minHeightDiffTomoveUI) || UIMoveHeightInProgress)
        {
            UIMoveHeightInProgress = true;
            PlaceCanvasInFrontOfUser();
        }
        if (UIMoveHeightInProgress && UIHeightDifference < 0.05f)
        {
            UIMoveHeightInProgress = false;
        }
    }

    private void CheckAngleBetweenHeadAndUI()
    {
        var headDirection = rig.headset.transform.forward;
        headDirection.y = 0;
        headDirection = headDirection.normalized;
        var screenDirection = transform.forward;
        screenDirection.y = 0;
        float angle = Mathf.Abs(Vector3.Angle(headDirection, screenDirection));

        if ((angle > minAngleTomoveUI) || UIRotationInProgress)
        {
            UIRotationInProgress = true;
            PlaceCanvasInFrontOfUser();
        }

        // Stop the rotation when target angle is almost reached
        if (UIRotationInProgress && angle < 5f)
        {
            UIRotationInProgress = false;
        }
    }

    // If VR Mode is enabled, the UI is moved in front of the user
    void PlaceCanvasInFrontOfUser()
    {
        if (avatarCustomizer.defaultVRMode)
        {
            var forward = rig.headset.transform.forward;
            forward = new Vector3(forward.x, 0, forward.z);

            // compute UI rotation
            Quaternion targetRot = Quaternion.LookRotation(forward);
            targetRot = Quaternion.Euler(0, targetRot.eulerAngles.y, 0);
            if (!firstPlaced)
                uiRoot.transform.rotation = targetRot;
            else
                uiRoot.transform.rotation = Quaternion.Lerp(uiRoot.transform.rotation, targetRot, UIMoveSpeed * Time.deltaTime);

            // compute UI position
            var canvasPosition = rig.headset.transform.position + distanceToUser * forward;
            var targetPosition = new Vector3(canvasPosition.x, rig.headset.transform.position.y - uiCameraOffet, canvasPosition.z);
            var newPosition = Vector3.Lerp(uiRoot.transform.position, targetPosition, UIMoveSpeed * Time.deltaTime);
            if (!firstPlaced)
            {
                uiRoot.transform.position = targetPosition;
            }
            else
            {
                uiRoot.transform.position = newPosition;
            }

        }
    }
}
