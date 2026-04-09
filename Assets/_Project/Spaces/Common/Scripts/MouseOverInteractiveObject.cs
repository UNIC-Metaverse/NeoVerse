using Fusion.XR.Shared.Desktop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * For desktop user, add an overlay over the object when the pointer pass upon it
 */
public class MouseOverInteractiveObject : MonoBehaviour, IMouseTeleportHover
{

    [SerializeField] private DesktopController desktopController;
    [SerializeField] private MeshRenderer meshRenderer;

    bool objectHovered = false;
    public Collider objectHoverCollider;

    MouseTeleport mouseTeleport; 

    private void Awake()
    {
        if (desktopController == null) desktopController = FindObjectOfType<DesktopController>(true);
        mouseTeleport = desktopController.GetComponentInChildren<MouseTeleport>();
        mouseTeleport.RegisterMouseTeleportHover(this);
        meshRenderer.enabled = false;
    }

    private void OnDestroy()
    {
        mouseTeleport.UnregisterMouseTeleportHover(this);
    }

    #region IMouseTeleportHover
    public void OnHoverHit(RaycastHit hit)
    {
        if (objectHovered == false && hit.collider == objectHoverCollider)
        {
            objectHovered = true;
            meshRenderer.enabled = true;
        }

        if (hit.collider != objectHoverCollider)
        {
            DisableOverlay();
        }
    }

    public void OnNoHover()
    {
        DisableOverlay();
    }
    #endregion

    void DisableOverlay()
    {
        if (objectHovered)
        {
            objectHovered = false;
            meshRenderer.enabled = false;
        }
    }

    private void Update()
    {
        if(objectHovered && mouseTeleport.enabled == false)
        {
            DisableOverlay();
        }
    }
}

