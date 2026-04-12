using UnityEngine;
using Photon.Voice;
using Photon.Voice.Unity;

// Applies video from the remote stream to the object's texture
public class RemoteVideoObject : VoiceComponent
{
#if PHOTON_VOICE_VIDEO_ENABLE
    void Start()
    {
        var voiceConnection = FindObjectOfType<VoiceConnection>();
        voiceConnection.Client.VoiceClient.OnRemoteVoiceInfoAction += (int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options) =>
        {
            if (voiceInfo.Codec == Codec.VideoH264)
            {
                var vp = Platform.CreateVideoPlayerUnityTexture(Logger, voiceInfo, (vpReady) =>
                {
                    GetComponent<Renderer>().material = VideoTexture.Shader3D.MakeMaterial(vpReady.PlatformView as Texture, vpReady.Flip);
                });
                options.Decoder = vp.Decoder;
                options.OnRemoteVoiceRemoveAction = () =>
                {
                    vp.Dispose();
                };
            }
        };
    }
#endif
}
