#if PHOTON_VOICE_VIDEO_ENABLE
using Fusion.Addons.ScreenSharing;
using Photon.Voice;
using Photon.Voice.Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;


/***
 * 
 * ScreensharingReceiver manages the reception of screen sharing streams.
 * It watchs for new voice connections, with the VoiceClient.OnRemoteVoiceInfoAction callback.
 * Upon such a connection, it creates a video player with Platform.CreateVideoPlayerUnityTexture.
 * Then, when this video player is ready (OnVideoPlayerReady), it creates a material containing the video player texture, 
 * and pass it to the ScreenSharingScreen with EnablePlayback: the screen will then change its renderer material to this new one.
 * 
 ***/
public class ScreensharingReceiver : MonoBehaviour
{
    public FusionVoiceClient fusionVoiceClient;

    public ScreenSharingScreen defaultRemoteScreen;
    Dictionary<int, IVideoPlayer> videoPlayerByPlayerIds = new Dictionary<int, IVideoPlayer>();
    public Dictionary<int, ScreenSharingScreen> screenByPlayerIds = new Dictionary<int, ScreenSharingScreen>();
    public Dictionary<IVideoPlayer, object> userDataForPlayer = new Dictionary<IVideoPlayer, object>();
    private Photon.Voice.ILogger logger;

    // Set it to true if you target Oculus Quest.
    public bool useCustomQuestScreenShader = true;
    string customQuestScreenShaderName = "QuestVideoTextureExt3D";

    private bool voiceClientRegistrationDone = false;

    private void Start()
    {
        if (fusionVoiceClient == null)
        {
            Debug.Log("videoConnection not set: searching it");
            fusionVoiceClient = FindObjectOfType<FusionVoiceClient>(true);
        }

        RegisterVoiceClient();

        logger = new Photon.Voice.Unity.Logger();
        if (defaultRemoteScreen) defaultRemoteScreen.ToggleScreenVisibility(false);

    }


    private void RegisterVoiceClient()
    {
        if (voiceClientRegistrationDone == true || fusionVoiceClient == null || fusionVoiceClient.Client == null || fusionVoiceClient.VoiceClient == null)
            return;

        fusionVoiceClient.VoiceClient.OnRemoteVoiceInfoAction += OnRemoteVoiceInfoAction;
        voiceClientRegistrationDone = true;
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    void Cleanup()
    {
        foreach (var v in videoPlayerByPlayerIds)
        {
            if (v.Value != null) v.Value.Dispose();
        }
        videoPlayerByPlayerIds.Clear();
    }

    private void Update()
    {
        RegisterVoiceClient();
    }
    ScreenSharingScreen ScreenForPlayerId(int playerId)
    {
        if (screenByPlayerIds.ContainsKey(playerId))
        {
            return screenByPlayerIds[playerId];
        }
        return defaultRemoteScreen;
    }

    // Called when a video playing stream is detected
    private void OnRemoteVoiceInfoAction(int channelId, int playerId, byte voiceId, VoiceInfo voiceInfo, ref RemoteVoiceOptions options)
    {
        Debug.Log($"OnRemoteVoiceInfoAction {channelId} {playerId} {voiceId}");
        switch (voiceInfo.Codec)
        {
            case Codec.VideoVP8:
            case Codec.VideoVP9:
            case Codec.VideoH264:
                if (videoPlayerByPlayerIds.ContainsKey(playerId))
                {
                    Debug.LogError($"Error: This player {playerId} is already sending a stream");
                    return;
                }
                IVideoPlayer videoPlayer = Platform.CreateVideoPlayerUnityTexture(logger, voiceInfo, (player) => {
                    videoPlayerByPlayerIds.Add(playerId, player);
                    userDataForPlayer[player] = voiceInfo.UserData;

                    OnVideoPlayerReady(player);
                });

                Debug.Log("ScreenSharingReceiver.OnRemoteVoiceInfoAction: Decoder: " + videoPlayer.Decoder + " / UserData: " + voiceInfo.UserData);
                options.Decoder = videoPlayer.Decoder;


                options.OnRemoteVoiceRemoveAction += () =>
                {
                    Debug.Log($"OnRemoteVoiceRemoveAction playerId:{playerId} videoPlayer:{videoPlayer}");
                    videoPlayer.Dispose();
                    videoPlayerByPlayerIds.Remove(playerId);
                    userDataForPlayer.Remove(videoPlayer);
                    var screen = ScreenForPlayerId(playerId);
                    if (screen)
                    {
                        screen.DisablePlayback(videoPlayer);
                    }
                };

                break;
            default:
                Debug.Log($"Voice Info: {voiceInfo.Codec} {voiceInfo}");
                break;
        }
    }

    private void OnApplicationQuit()
    {
        Cleanup();
    }

    private void OnVideoPlayerReady(IVideoPlayer videoPlayer)
    {
        Debug.Log($"OnVideoPlayerReady videoPlayer");
        ScreenSharingScreen screen = null;
        foreach (var entry in videoPlayerByPlayerIds)
        {
            if (videoPlayer == entry.Value)
            {
                screen = ScreenForPlayerId(entry.Key);
            }
        }

        if (videoPlayer.PlatformView is Texture)
        {
            if (screen != null)
            {
                try
                {
                    var flip = videoPlayer.Flip;
                    var screenTexture = videoPlayer.PlatformView as Texture;

                    if (useCustomQuestScreenShader && Application.platform == RuntimePlatform.Android)
                    {
                        var shader = Resources.Load<Shader>(customQuestScreenShaderName);
                        if (shader == null)
                        {
                            throw new Exception("Shader resource " + customQuestScreenShaderName + " fails to load");
                        }
                        var material = new Material(shader);
                        material.SetTexture("_MainTex", screenTexture);
                        material.SetVector("_Flip", new Vector4(flip.IsHorizontal ? -1 : 1, flip.IsVertical ? -1 : 1, 0, 0));
                        screen.usingShaderRequiringMatrix = true;
                        screen.EnablePlayback(material, videoPlayer);
                    }
                    else
                    {
                        var material = Photon.Voice.Unity.VideoTexture.Shader3D.MakeMaterial(screenTexture, flip);
                        screen.EnablePlayback(material, videoPlayer);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Error while creating video material: " + e.Message);
                }
            }
        }
    }
}
#endif