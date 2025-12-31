using System;
using UnityEngine;
using Photon.Voice;
using Photon.Voice.Unity;

// Captures video from the default camera and transmits it via VoiceConnection instance found in the scene.
// Optionally applies captured video preview to the object texture.
public class LocalVideoObject : VoiceComponent
{
#if PHOTON_VOICE_VIDEO_ENABLE
    public bool Preview;
    public bool DebugEcho;
    IVideoRecorder r;
    LocalVoiceVideo v;
    void Start()
    {
        var voiceConnection = FindObjectOfType<VoiceConnection>();
        var videoVoiceInfo = VoiceInfo.CreateVideo(Codec.VideoH264, 100000, 640, 480, 30, 30);
        Action<IVideoRecorder> onReady = (vr) =>
        {
            if (vr.Error != null)
            {
                Debug.LogErrorFormat("LocalVideoObject: failed to start video recorder: " + vr.Error);
                return;
            }
            if (Preview)
            {
                GetComponent<Renderer>().material = VideoTexture.Shader3D.MakeMaterial(vr.PlatformView as Texture, vr.Flip);
            }
        };
        r = Platform.CreateVideoRecorderUnityTexture(Logger, videoVoiceInfo, DeviceInfo.Default, onReady);
        Debug.LogFormat("LocalVideoObject: Video recorder created: {0}", r.GetType());
        v = voiceConnection.Client.VoiceClient.CreateLocalVoiceVideo(videoVoiceInfo, r, VoiceConnection.ChannelVideo);
        v.DebugEchoMode = DebugEcho;
    }

    private void OnDestroy()
    {
        v.RemoveSelf();
        r.Dispose();
    }
#endif
}
