using UnityEngine;
using Photon.Voice;
using Photon.Voice.Unity;

// Renders video from the remote stream on the screen
public class RemoteVoiceScreen : VoiceComponent
{
#if PHOTON_VOICE_VIDEO_ENABLE
    void Start()
    {
        var voiceConnection = FindObjectOfType<VoiceConnection>();
        var pm = Platform.CreatePreviewManagerUnityTexture(Logger);
        voiceConnection.Client.VoiceClient.OnRemoteVoiceInfoAction += (int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options) =>
        {
            if (voiceInfo.Codec == Codec.VideoH264)
            {
                var vp = Platform.CreateVideoPlayerUnityTexture(Logger, voiceInfo, (vpReady) =>
                {
                    pm.AddView(vpReady, vpReady);
                    pm.SetBounds(vpReady, 0, 0, Screen.width / 2, Screen.width * voiceInfo.Height / voiceInfo.Width / 2);
                });
                options.OnRemoteVoiceRemoveAction = () =>
                {
                    pm.RemoveView(vp);
                };
                options.Decoder = vp.Decoder;
                options.OnRemoteVoiceRemoveAction = () =>
                {
                    pm.RemoveView(vp);
                    vp.Dispose();
                };
            };
        };
    }
#endif
}
