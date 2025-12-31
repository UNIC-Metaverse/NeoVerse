// Uncomment nextr line if a Photon video SDK version earlier than 2.52 is used
//#define VIDEOSDK_251 
#if U_WINDOW_CAPTURE_RECORDER_ENABLE
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define UWC_EMITTER_ENABLED
#endif
#endif
#if PHOTON_VOICE_VIDEO_ENABLE
using Photon.Voice;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using uWindowCapture;


/***
 * 
 * ScreenSharingEmitter uses Photon Voice channel to stream screen sharing images.
 * 
 * When the user wants to start a screen sharing using the UI buttons (see EmitterMenu), the ConnectScreenSharing() method is called.
 *  - ScreenSharingEmitter will first wait for the voice session initialization to be finished,
 *  - Then, it will also wait for the uWindowCapture initialization to be finished (the uWindowCaptureRecorder class implementing the IVideoRecorderPusher interface allows to collect frames for the PhotonVideo SDK),
 *  - When everything is ready, a transmission channel (a "voice") is created by calling VoiceClient.CreateLocalVoiceVideo on the FusionVoiceClient: from now on, the recorder will stream the desktop capture.
 *  - If an IEmitterListener is provided, it will be warn of the emission start with the OnStartEmitting callback.
 * 
 * The DisconnectScreenSharing() method is called when the user stops the screensharing. If an IEmitterListener is provided, it will be warn of the emission end with the OnStopEmitting callback.
 * 
 * Note: if not present in the scene, uWindowCaptureRecorder will create a uWindowCaptureHost (allowing to configure uWindowsCapture integration) that will be automatically configured by the ScreenSharingEmitter
 ***/
public class ScreenSharingEmitter : MonoBehaviour
{
    public interface IEmitterListener
    {
        public void OnStartEmitting(ScreenSharingEmitter emitter);
        public void OnStopEmitting(ScreenSharingEmitter emitter);

    }

    public bool startSharingOnVoiceConnectionAvailable = false;
#if UWC_EMITTER_ENABLED
    [HideInInspector]
    public uWindowCaptureRecorder screenRecorder;
    [HideInInspector]
    public uWindowCaptureHost captureHost;
#endif
    public UnityEngine.UI.Image previewImage;
    public Renderer previewRenderer;
    [Tooltip("A gameobject to display when not offline (could containt a UWC texture to display preview before emitting for instance)")]
    public UwcWindowTexture offlinePreview;
    public IEmitterListener listener;

#if VIDEOSDK_251
#else
    // Separate media in channels for better Photon transport performance
    public int videoChannel = 3;
#endif

    public enum Status
    {
        NotEmitting,
        WaitingVoiceConnection,
        WaitingScreenCaptureAvailability,
        Emitting,

    }
    public Status status = Status.NotEmitting;

    [System.Serializable]
    public struct ScreenSharingSettings
    {
        public Photon.Voice.Codec VideoCodec;
        public bool UseScreenshareResolution;
        public int VideoWidth;
        public int VideoHeight;
        public int VideoBitrate;
        public int AudioBitrate;
        public int VideoFPS;
        public int CaptureFPS;
        public int VideoKeyFrameInt;
        public int videoDelayFrames;
        // Split frames into fragments according to the size provided by the Transport
        public bool fragment;
        // Send data reliable
        public bool reliable;
    }
    [SerializeField]
    ScreenSharingSettings settings = new ScreenSharingSettings
    {
        VideoCodec = Photon.Voice.Codec.VideoVP8,
        UseScreenshareResolution = true,
        VideoWidth = 1920,
        VideoHeight = 1080,
        VideoBitrate = 10000000,
        AudioBitrate = 30000,
        VideoFPS = 3,
        CaptureFPS = 3,
        VideoKeyFrameInt = 180,
        videoDelayFrames = 0,
        reliable = false,
#if VIDEOSDK_251
#else
        fragment = false,
#endif
    };
    private Photon.Voice.ILogger logger;
    public FusionVoiceClient fusionVoiceClient;

    bool didVoiceConnectionJoined = false;
    LocalVoiceVideo localVoiceVideo;

    public bool screenSharingInProgress = false;

    object emitterUserData = null;

    int _desktopIndex = 0;
    public int DesktopIndex
    {
        get { return _desktopIndex; }
        set { 
                _desktopIndex = value;
                if(captureHost)
                {
                    captureHost.DesktopIndex = _desktopIndex;
                }
            }
    }

    private void Awake()
    {
        if (fusionVoiceClient == null)
        {
            Debug.LogError("ScreenSharingEmitter videoConnection not set: searching it");
            fusionVoiceClient = FindObjectOfType<FusionVoiceClient>(true);
        }

        if (previewRenderer)
            previewRenderer.enabled = false;

        if (previewImage)
            previewImage.enabled = false;

        if (offlinePreview)
            offlinePreview.gameObject.SetActive(true);
    }

    private void Start()
    {
        logger = new Photon.Voice.Unity.Logger();
    }

    private void Update()
    {
        if (!didVoiceConnectionJoined && fusionVoiceClient && fusionVoiceClient.ClientState == Photon.Realtime.ClientState.Joined)
        {
            didVoiceConnectionJoined = true;
            OnVoiceJoined();
        }
    }

    public void OnVoiceJoined()
    {
        if (!enabled) return;
        if (startSharingOnVoiceConnectionAvailable) ConnectScreenSharing();
    }

#if UWC_EMITTER_ENABLED
    void AddCameraScreensharing(object userData = null)
    {
        status = Status.WaitingScreenCaptureAvailability;
        emitterUserData = userData;

        if (screenRecorder == null)
        {
            screenRecorder = new Photon.Voice.Unity.uWindowCaptureRecorder(gameObject);
        }
        if (captureHost == null)
        {
            // If not present in the scene, uWindowCaptureRecorder will create a uWindowCaptureHost
            captureHost = GameObject.FindObjectOfType<Photon.Voice.Unity.uWindowCaptureHost>();
            captureHost.Type = global::uWindowCapture.WindowTextureType.Desktop;
        }
        if(captureHost != null)
        {
            captureHost.DesktopIndex = DesktopIndex;
        }
        if (screenRecorder != null)
        {
            screenRecorder.OnReady += UWCRecorderReady;
        }
    }

    private void UWCRecorderReady(uWindowCaptureRecorder uwcRecorder)
    {

        if (status == Status.Emitting)
        {
            return;
        }
        status = Status.Emitting;
        Debug.Log("UWCRecorderReady");
        if (listener != null) listener.OnStartEmitting(this);

        // Prepare encoder
        int width = settings.UseScreenshareResolution ? uwcRecorder.Width : settings.VideoWidth;
        int height = settings.UseScreenshareResolution ? uwcRecorder.Height : settings.VideoHeight;

        captureHost.encoderFPS = settings.CaptureFPS;
        var info = VoiceInfo.CreateVideo(settings.VideoCodec, settings.VideoBitrate, width, height, settings.VideoFPS, settings.VideoKeyFrameInt, emitterUserData);
        Debug.Log($"CreateVideo {settings.VideoCodec}, {settings.VideoBitrate}, {width}, {height}, {settings.VideoFPS}, {settings.VideoKeyFrameInt}");
        uwcRecorder.Encoder = Platform.CreateDefaultVideoEncoder(logger, info);

        // Prepare voice
#if VIDEOSDK_251
        localVoiceVideo = fusionVoiceClient.VoiceClient.CreateLocalVoiceVideo(info, uwcRecorder);
#else
        localVoiceVideo = fusionVoiceClient.VoiceClient.CreateLocalVoiceVideo(info, uwcRecorder, videoChannel);
        localVoiceVideo.Fragment = settings.fragment;
#endif
        localVoiceVideo.Encrypt = false;
        localVoiceVideo.Reliable = settings.reliable;

        if (previewRenderer)
        {
            previewRenderer.enabled = true;
            previewRenderer.material = Photon.Voice.Unity.VideoTexture.Shader3D.MakeMaterial(uwcRecorder.PlatformView as Texture, Flip.None);
        }
        if (previewImage)
        {
            previewImage.enabled = true;
            previewImage.material = Photon.Voice.Unity.VideoTexture.Shader3D.MakeMaterial(uwcRecorder.PlatformView as Texture, Flip.Vertical);
            previewImage.SetAllDirty();
        }
        if (offlinePreview != null)
        {
            offlinePreview.gameObject.SetActive(false);
        }

        fusionVoiceClient.VoiceClient.SetRemoteVoiceDelayFrames(settings.VideoCodec, settings.videoDelayFrames);
    }

    /// <summary>
    /// Change the screen captured by uWindowCapture
    /// </summary>
    /// <param name="desktopID">Screen id, starting at 0</param>
    public void SelectDesktop(int desktopID)
    {
        Debug.Log($"Desktop {desktopID} selected");
        DesktopIndex = desktopID;
        if (offlinePreview) offlinePreview.desktopIndex = desktopID;
    }

    public async void ConnectScreenSharing()
    {
        Debug.Log("ConnectScreenSharing...");
        status = Status.WaitingVoiceConnection;
        while (this != null && didVoiceConnectionJoined == false)
        {
            Debug.Log($"Screen sharing connection requested. Waiting for Photon voice connection ({(fusionVoiceClient ? fusionVoiceClient.ClientState : "")}) ...");
            await System.Threading.Tasks.Task.Delay(1000);
        }
        screenSharingInProgress = true;
        AddCameraScreensharing();
    }

    public void DisconnectScreenSharing()
    {
        Debug.Log("DisconnectScreenSharing...");

        status = Status.NotEmitting;
        screenSharingInProgress = false;
        if (localVoiceVideo != null)
        {
            localVoiceVideo.RemoveSelf();
            localVoiceVideo = null;
        }

        if(screenRecorder?.OnReady != null)
            screenRecorder.OnReady -= UWCRecorderReady;

        if (listener != null) listener.OnStopEmitting(this);

        if (screenRecorder != null)
        {
            screenRecorder.Dispose();
            screenRecorder = null;
        }
        if (previewRenderer)
        {
            previewRenderer.enabled = false;
        }
        if (previewImage) previewImage.enabled = false;
        if (offlinePreview)
        {
            offlinePreview.gameObject.SetActive(true);
        }
    }
#else

    public void SelectDesktop(int desktopID)
    {
        Debug.LogError("Only compatible with Windows build with U_WINDOW_CAPTURE_RECORDER_ENABLE");
    }

    public void ConnectScreenSharing()
    {
        Debug.LogError("Only compatible with Windows build with U_WINDOW_CAPTURE_RECORDER_ENABLE");
    }

    public void DisconnectScreenSharing()
    {
        Debug.LogError("Only compatible with Windows build with U_WINDOW_CAPTURE_RECORDER_ENABLE");
    }
#endif
}
#endif