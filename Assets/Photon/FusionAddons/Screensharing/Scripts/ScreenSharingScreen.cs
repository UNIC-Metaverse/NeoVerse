using Photon.Voice;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.ScreenSharing
{
    /***
     * 
     * ScreenSharingScreen manages the screen sharing renderer visibility :
     * When a screensharing is in progress : 
     *          - the screen renderer is enabled and the material is set with the one provided by the ScreensharingEmitter
     *          - the material shader matrix is updated every frame (required for URP in VR)
     *          - the "notPlayingObject" game object is disabled
     *          
     * When the screensharing is stopped : 
     *          - the screen renderer is disabled and the material is restored with the initial one
     *          - the "notPlayingObject" game object is enabled according to the VisibilityBehaviour settings
     * 
     ***/
    public class ScreenSharingScreen : MonoBehaviour
    {
        public Renderer screenRenderer;
        public UnityEvent<bool> onScreensharingScreenVisibility = new UnityEvent<bool>();
        Material initialMaterial;
        public bool isRendering = false;
        // Needed for PhotonVoiceApi/GLES3/QuestVideoTextureExt3D shader
        public bool usingShaderRequiringMatrix = true;
        private IVideoPlayer currentVideoPlayer;
        [System.Flags]
        public enum VisibilityBehaviour
        {
            None = 0,
            HideScreenRendererWhenNotPlaying = 1,
            DisplayNotPlayingObjectWhenNotPlaying = 2
        }
        public VisibilityBehaviour visibilityBehaviour = VisibilityBehaviour.None;

        public GameObject notPlayingObject;

        private void Awake()
        {
            if (screenRenderer == null) screenRenderer = GetComponentInChildren<Renderer>();
            if (screenRenderer)
                initialMaterial = screenRenderer.material;
            if (notPlayingObject && (visibilityBehaviour & VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying) != VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying)
            {
                Debug.LogError("A notPlayingObject is set, but DisplayNotPlayingObjectWhenNotPlaying option is not choosen: the object won't be used");
            }
        }

        private void Update()
        {
            // Needed for the URP VR shader
            if (isRendering && usingShaderRequiringMatrix)
            {
                screenRenderer.material.SetMatrix("_localToWorldMatrix", screenRenderer.transform.localToWorldMatrix);
            }
        }

        public void EnablePlayback(Material videoMaterial, IVideoPlayer videoPlayer)
        {

            if (currentVideoPlayer != null)
            {
                Debug.Log($"Screen reused by another player {videoPlayer}. Note: make sure that the initial player is disposed by orchestration logic.");
            }
            else
                Debug.Log("Playback started on screen for videoPlayer " + videoPlayer);

            currentVideoPlayer = videoPlayer;
            ToggleScreenVisibility(true);
            screenRenderer.material = videoMaterial;
        }

        public void DisablePlayback(IVideoPlayer videoPlayer)
        {
            if (videoPlayer != currentVideoPlayer)
            {
                Debug.Log("Not stopping playback because videoPlayer hasbeen reused by another player");
                return;
            }
            else
                Debug.Log("Playback stopped for videoPlayer " + videoPlayer);

            currentVideoPlayer = null;
            ToggleScreenVisibility(false);
            screenRenderer.material = initialMaterial;
        }

        public virtual void ToggleScreenVisibility(bool ShouldScreenBeDisplayed)
        {
            isRendering = ShouldScreenBeDisplayed;
            if ((visibilityBehaviour & VisibilityBehaviour.HideScreenRendererWhenNotPlaying) == VisibilityBehaviour.HideScreenRendererWhenNotPlaying)
            {
                screenRenderer.enabled = ShouldScreenBeDisplayed;
            }
            if (notPlayingObject && (visibilityBehaviour & VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying) == VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying)
            {
                notPlayingObject.SetActive(!ShouldScreenBeDisplayed);
            }
            if (onScreensharingScreenVisibility != null) onScreensharingScreenVisibility.Invoke(ShouldScreenBeDisplayed);
        }
    }
}
