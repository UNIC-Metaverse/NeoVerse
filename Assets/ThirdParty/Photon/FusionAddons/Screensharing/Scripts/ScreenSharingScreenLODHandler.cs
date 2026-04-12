using Fusion.XR.Shared;
using UnityEngine;

namespace Fusion.Addons.ScreenSharing
{
    /**
     * The video playback texture does not support mip mapping.
     * This script captures the video renderer to store it in a mip mappable texture
     */
    public class ScreenSharingScreenLODHandler : MonoBehaviour
    {
        public ScreenSharingScreen screen;
        public Camera screenRenderTextureCamera;
        public float lowerResFPS = 1f;
        public RendererVisible lowResRendererVisible;
        public Renderer lowResRenderer;
        public float bias = 0.12f;
        float nextCapture = 0;
        bool isRendering = false;
        RenderTexture cameraTexture;
        [Header("Texture settings (replaced by screenRenderTextureCamera.targetTexture if any)")]
        [Tooltip("Leave it to 0,0 if you want to use the values in the screenRenderTextureCamera.targetTexture")]
        public Vector2 lowResResolution = new Vector2(0, 0);
        public int anisoLevel = 0;

        private void Awake()
        {
            if (lowResRendererVisible == null) lowResRendererVisible = GetComponentInChildren<RendererVisible>();
            if (lowResRenderer == null) lowResRenderer = lowResRendererVisible.GetComponent<Renderer>();
            if (screenRenderTextureCamera == null) screenRenderTextureCamera = GetComponentInChildren<Camera>();
            if (screen == null) screen = GetComponentInChildren<ScreenSharingScreen>();
            // Create camera texture
            RenderTextureFormat rtFormat = RenderTextureFormat.Default;
            if (screenRenderTextureCamera.targetTexture != null)
            {
                rtFormat = screenRenderTextureCamera.targetTexture.format;
                if (lowResResolution.x == 0) lowResResolution.x = screenRenderTextureCamera.targetTexture.width;
                if (lowResResolution.y == 0) lowResResolution.y = screenRenderTextureCamera.targetTexture.height;
                anisoLevel = screenRenderTextureCamera.targetTexture.anisoLevel;
            }
            cameraTexture = new RenderTexture((int)lowResResolution.x, (int)lowResResolution.y, 0, rtFormat);
            cameraTexture.useMipMap = true;
            cameraTexture.anisoLevel = screenRenderTextureCamera.targetTexture.anisoLevel;
            if (screenRenderTextureCamera.targetTexture != null)
            {
                cameraTexture.anisoLevel = anisoLevel;
            }
            screenRenderTextureCamera.targetTexture = cameraTexture;

            // Make sure the camera does not run automatically
            screenRenderTextureCamera.enabled = false;

            // Edit low res renderer material with the ouput texture form the camera
            lowResRenderer.material.mainTexture = cameraTexture;
            lowResRenderer.enabled = false;

            DetermineNextCapture();
            if (lowResRendererVisible && lowResRendererVisible.isVisible == false)
                Debug.Log("ScreenSharingScreenLODHandler Initial biais: " + screenRenderTextureCamera.targetTexture.mipMapBias);
        }

        private void Start()
        {
            screenRenderTextureCamera.cullingMask = 1 << screen.screenRenderer.gameObject.layer;
        }

        void DetermineNextCapture()
        {
            nextCapture = Time.time + 1f / lowerResFPS;
        }

        void Capture()
        {
            screenRenderTextureCamera.targetTexture.mipMapBias = bias;
            screenRenderTextureCamera.Render();
        }

        private void Update()
        {
            if (screen.isRendering != isRendering)
            {
                lowResRenderer.enabled = screen.isRendering;
                isRendering = screen.isRendering;
            }
            if (isRendering == false) return;

            if (Time.time > nextCapture)
            {
                if (lowResRendererVisible && lowResRendererVisible.isVisible == false)
                {
                    return;
                }
                Capture();
                DetermineNextCapture();
            }
        }
    }
}
