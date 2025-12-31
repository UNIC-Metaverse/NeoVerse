#define PREVIEW_UNITY_TEXTURE // or default platform preview (for PC and MAC it's also Unity Texture)

#define TEST_VIDEO_TEXTURE_3D // requires preview to Unity texture and "PreviewLocal" and "PreviewRemote" objects in scene

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Voice;
using System;
using System.Linq;

namespace TestVideo
{
    [RequireComponent(typeof(Settings))]
    abstract public class Client : MonoBehaviour
    {
#if PHOTON_VOICE_VIDEO_ENABLE

        // Separate media in channels for better Photon transport performance
        enum Channel
        {
            Audio = 1,
            Video = 2,
            ScreenShare = 3,
        }

        private Settings settings;

        private IDeviceEnumerator micDevices;
        private IDeviceEnumerator camDevices;
        public IDeviceEnumerator MicDevices
        {
            get
            {
                if (micDevices == null)
                {
                    micDevices = Platform.CreateAudioInEnumerator(logger);
                }
                return micDevices;
            }
        }

        public IDeviceEnumerator CamDevices
        {
            get
            {
                if (camDevices == null)
                {
                    camDevices = Platform.CreateVideoInEnumerator(logger);
                }
                return camDevices;
            }
        }


        protected Photon.Voice.Unity.Logger logger = new Photon.Voice.Unity.Logger(LogLevel.Info);

        public IEnumerable<IVideoPlayer> VideoPlayers { get { return videoPlayers; } }
        public IEnumerable<IAudioOut<float>> AudioPlayers { get { return audioPlayers; } }
        public IEnumerable<IVideoRecorder> VideoRecorders => localVoices.Values.Select(x => x.videoRecorder).Where(x => x != null);
        public int AudioFramesLost { get; set; }

        HashSet<IVideoPlayer> videoPlayers = new HashSet<IVideoPlayer>();

        Dictionary<StreamType, Stream> localVoices = new Dictionary<StreamType, Stream>();
        public ILocalVoiceAudio LocalVoiceAudio
        {
            get
            {
                var x = localVoices.GetValueOrDefault(StreamType.Mic);
                if (x != null)
                {
                    return x as ILocalVoiceAudio;
                }
                return null;
            }

        }

        public IPreviewManager PreviewManager { get; private set; }

        abstract protected IVoiceTransport Transport { get; }
        abstract public VoiceClient VoiceClient { get; }
        abstract public bool IsConnected { get; }
        abstract public string StateStr { get; }
        abstract public int RoundTripTime { get; }
        abstract public long BytesOut { get; }
        abstract public long PacketsOut { get; }
        abstract public long BytesIn { get; }
        abstract public long PacketsIn { get; }
        abstract public void Connect();
        abstract public void Disconnect();

        public string[] StatsStr => new string[] {
                "LV: A=" + localVoices.Where(x => x.Value.audioSource != null).Count() + ", V=" + localVoices.Where(x => x.Value.videoRecorder != null).Count() + ", RV: A=" + audioPlayers.Count + ", V=" + videoPlayers.Count,
                "out: bytes, pkts: " + BytesOut + ", " + PacketsOut + " /sec: " + perSecStats[0] + ", " + perSecStats[1],
                "in: bytes, pkts: " + BytesIn + ", " + PacketsIn + " /sec: " + perSecStats[2] + ", " + perSecStats[3],
                "audio lost: " + AudioFramesLost + " per sec: " + this.perSecStats[4] + (LocalVoiceAudio == null ? "" : ", audio level: " + (LocalVoiceAudio.LevelMeter.CurrentAvgAmp).ToString("0.000")),
            };

        const int STATS_COUNT = 5;

        long[] prevStats = new long[STATS_COUNT];
        long[] perSecStats = new long[STATS_COUNT];
        void updateStats()
        {
            int i = 0;
            foreach (var s in new long[STATS_COUNT] { BytesOut, PacketsOut, BytesIn, PacketsIn, AudioFramesLost })
            {
                perSecStats[i] = s - prevStats[i];
                prevStats[i] = s;
                i++;
            }
        }

        List<IAudioOut<float>> audioPlayers = new List<IAudioOut<float>>();
        protected bool started;

        protected virtual void Awake()
        {
        }

        protected virtual IEnumerator Start()
        {
            // to limit fps to avoid video to texture render stall when ui enabled:
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;

#if PREVIEW_UNITY_TEXTURE
            PreviewManager = Platform.CreatePreviewManagerUnityTexture(logger);
#else
            PreviewManager = Platform.CreateDefaultPreviewManager(logger);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            }
#endif

            this.settings = GetComponent<Settings>();
            InvokeRepeating("updateStats", 1.0f, 1.0f);
            VoiceClient.OnRemoteVoiceInfoAction += (int channelId, int playerId, byte voiceId, VoiceInfo i, ref RemoteVoiceOptions options) =>
            {
                if (i.Codec.ToString().StartsWith("Audio"))
                {
                    var audioSource = gameObject.AddComponent<AudioSource>();
                    var pdc = Photon.Voice.Unity.UnityAudioOut.PlayDelayConfig.Default;
                    pdc.Low = settings.AudioJitterBufferMs;
                    pdc.High = settings.AudioJitterBufferMsHigh;
                    pdc.Max = settings.AudioJitterBufferMsMax;
                    var audioPlayer =
#if UNITY_WEBGL && !UNITY_EDITOR // allows non-WebGL workflow in Editor
                        new Photon.Voice.Unity.WebAudioAudioOut(pdc, 0, logger, "PhotonVoiceVideo: PhotonVoiceSpeaker:", true);
#else
                        new Photon.Voice.Unity.UnityAudioOut(audioSource, pdc, logger, "PhotonVoiceVideo: PhotonVoiceSpeaker:", true);
#endif
                    audioPlayer.Start(i.SamplingRate, i.Channels, i.FrameDurationSamples);
                    audioPlayers.Add(audioPlayer);
                    options.SetOutput((frame) =>
                    {
                        audioPlayer.Push(frame.Buf);
                    });
                    options.OnRemoteVoiceRemoveAction = () =>
                    {
                        audioPlayers.Remove(audioPlayer);
                        audioPlayer.Stop();
                        Destroy(audioSource);
                    };
                }
                else if (i.Codec.ToString().StartsWith("Video"))
                {
                    Action<IVideoPlayer> onReady = (vp) =>
                    {
                        PreviewManager.AddView(vp, vp);
#if TEST_VIDEO_TEXTURE_3D
                        if (vp.PlatformView is Texture)
                        {
                            var go = GameObject.Find("PreviewRemote");
                            if (go != null)
                            {
                                try
                                {
                                    go.GetComponent<Renderer>().material = Photon.Voice.Unity.VideoTexture.Shader3D.MakeMaterial(vp.PlatformView as Texture, vp.Flip);
                                }
                                catch (Exception e)
                                {
                                    Debug.LogErrorFormat("LBC: " + e.Message);
                                }
                            }
                        }
#endif
                    };

#if PREVIEW_UNITY_TEXTURE
                    IVideoPlayer videoPlayer = Platform.CreateVideoPlayerUnityTexture(logger, i, onReady);
#else
                        IVideoPlayer videoPlayer = Platform.CreateDefaultVideoPlayer(logger, i, onReady);
#endif
                    options.Decoder = videoPlayer.Decoder;
                    videoPlayers.Add(videoPlayer);

                    options.OnRemoteVoiceRemoveAction += () =>
                    {
                        videoPlayer.Dispose();
                        videoPlayers.Remove(videoPlayer);
                        PreviewManager.RemoveView(videoPlayer);
                    };
                }
                else
                {
                    Debug.LogErrorFormat("LBC: " + "unsupported codec " + i.Codec);
                }
            };

            Connect();

            Debug.LogFormat("LBC: init");
            started = true;

            yield break;
        }

        private void RemoveLocalVoice(Stream s)
        {
            s.localVoice?.RemoveSelf();
            s.audioSource?.Dispose();
            if (s.videoRecorder != null)
            {
                PreviewManager.RemoveView(s.videoRecorder);
            }
            s.videoRecorder?.Dispose();
        }

        protected void RemoveLocalVoices()
        {
            foreach (var v in localVoices.Values)
            {
                RemoveLocalVoice(v);
            }
            localVoices.Clear();
        }

        public AudioClip AudioClip;

        public enum StreamType
        {
            Mic,
            Camera,
            CameraLow,
            ScreenShare,
            WindowShare,
            WebGLScreenShare,
        }

        class Stream
        {
            public LocalVoice localVoice;
            public IVideoRecorder videoRecorder;
            public IAudioDesc audioSource;
        }

        static bool multiStreamSupported = new RuntimePlatform[]{
                RuntimePlatform.WindowsEditor,
                RuntimePlatform.WindowsPlayer,
                RuntimePlatform.OSXEditor,
                RuntimePlatform.OSXPlayer,
                RuntimePlatform.WebGLPlayer,
            }.Contains(Application.platform);

        static bool downscaleSupported = multiStreamSupported && Application.platform != RuntimePlatform.WebGLPlayer;

        private Stream CreateCamera(StreamType stream, Settings set, bool lowQuality)
        {
            if (lowQuality && !multiStreamSupported)
            {
                return null;
            }

            int bitraate = lowQuality ? set.VideoBitrate / 10 : set.VideoBitrate;
            int width = set.VideoWidth;
            int height = set.VideoHeight;

            // Downscale low bitrate video before encoding. High-res video stream must be created before low-res to avoid camera initialization with lower resolution.
            // Supported on Windows and Mac.
            // Keeping original sizes may preserve more details for slowly changing videos like in video chat.
            // if (lowQuality && downscaleSupported)
            // {
            //     width /= 4;
            //     height /= 4;
            // }

            var videoVoiceInfo = VoiceInfo.CreateVideo(set.VideoCodec, bitraate, width, height, set.VideoFPS, set.VideoKeyFrameInt);

            Stream s = new Stream();
            Action<IVideoRecorder> onReady = (vr) =>
            {
                PreviewManager.RemoveView(vr);

                if (vr.Error != null)
                {
                    Debug.LogErrorFormat("LBC: failed to start video recorder: " + vr.Error);
                    return;
                }

                s.localVoice = VoiceClient.CreateLocalVoiceVideo(videoVoiceInfo, vr, (int)Channel.Video);

                VoiceClient.SetRemoteVoiceDelayFrames(Codec.VideoH264, set.VideoDelay);
                VoiceClient.SetRemoteVoiceDelayFrames(Codec.VideoVP8, set.VideoDelay);

                PreviewManager.AddView(vr, vr);
#if TEST_VIDEO_TEXTURE_3D
                if (vr.PlatformView is Texture)
                {
                    var go = GameObject.Find("PreviewLocal");
                    if (go != null)
                    {
                        try
                        {
                            go.GetComponent<Renderer>().material = Photon.Voice.Unity.VideoTexture.Shader3D.MakeMaterial(vr.PlatformView as Texture, vr.Flip);
                        }
                        catch (Exception e)
                        {
                            Debug.LogErrorFormat("LBC: " + e.Message);
                        }
                    }
                }
#endif
            };

#if PREVIEW_UNITY_TEXTURE
            var r = Platform.CreateVideoRecorderUnityTexture(logger, videoVoiceInfo, set.CamDevice, onReady);
#else
            var r = Platform.CreateDefaultVideoRecorder(logger, videoVoiceInfo, set.CamDevice, onReady);
#endif
            s.videoRecorder = r;
            Debug.LogFormat("LBC: Video recorder created: {0}", r.GetType());
            return s;
        }

        Dictionary<StreamType, Func<Client, Settings, Stream>> streamCreate = new Dictionary<StreamType, Func<Client, Settings, Stream>>()
        {
            { StreamType.Mic, (client, set) => {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);

                var source = Platform.CreateDefaultAudioSource(client.logger, set.MicDevice, Settings.AudioMicSamplingRate, 1);
                VoiceInfo voiceInfo = VoiceInfo.CreateAudioOpus(Settings.AudioEncoderSamplingRate, source.Channels, OpusCodec.FrameDuration.Frame20ms, set.AudioBitrate);
                var lv = client.VoiceClient.CreateLocalVoiceAudioFromSource(voiceInfo, source, AudioSampleType.Source, (int)Channel.Audio);

                //var source = new AudioClipWrapper(this.AudioClip);
                //VoiceInfo voiceInfo = VoiceInfo.CreateAudioOpus(Settings.AudioEncoderSamplingRate, this.AudioClip.frequency, OpusCodec.FrameDuration.Frame20ms, set.AudioBitrate);
                //var lv = VoiceClient.CreateLocalVoiceAudioFromSource(voiceInfo, source, AudioSampleType.Source, (int)Channel.Audio);

                Debug.LogFormat("LBC: Audio source created: {0}", source.GetType());
                return new Stream() { localVoice = lv, audioSource = source};
            } },

            { StreamType.Camera, (client, set) => client.CreateCamera(StreamType.Camera, set, false) },

            { StreamType.CameraLow, (client, set) => client.CreateCamera(StreamType.CameraLow, set, true) },

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && U_WINDOW_CAPTURE_RECORDER_ENABLE
            { StreamType.WindowShare, (client, set) => {
                Stream s = new Stream();
                var wc = new Photon.Voice.Unity.uWindowCaptureRecorder(client.gameObject);
                wc.OnReady = (wc) =>
                {
                    var info = VoiceInfo.CreateVideo(Photon.Voice.Codec.VideoVP8, 10000000, wc.Width, wc.Height, 30, 30);
                    wc.Encoder = Platform.CreateDefaultVideoEncoder(client.logger, info);

                    client.PreviewManager.RemoveView(wc);

                    client.PreviewManager.AddView(wc, wc);
                    s.videoRecorder = wc;

                    if (s.localVoice != null)
                    {
                        s.localVoice.RemoveSelf();
                    }
                    s.localVoice = client.VoiceClient.CreateLocalVoiceVideo(info, wc, (int)Channel.ScreenShare);
                    s.localVoice.Reliable = true;
                };
                return s;
            } },
#endif
#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            { StreamType.WebGLScreenShare, (client, set) => {
                var videoVoiceInfo = VoiceInfo.CreateVideo(set.VideoCodec, set.VideoBitrate, set.VideoWidth, set.VideoHeight, set.VideoFPS, set.VideoKeyFrameInt);

                Stream s = new Stream();
                // onReady may be called multiple times while the encoder input changes, store the voice in a var to update it
                Action<IVideoRecorder> onReady = (vr) =>
                {

                    client.PreviewManager.RemoveView(vr);
                    if (s.localVoice != null)
                    {
                        s.localVoice.RemoveSelf();
                    }

                    if (vr.Error != null)
                    {
                        Debug.LogErrorFormat("LBC: failed to start video recorder: " + vr.Error);
                        return;
                    }

                    client.PreviewManager.AddView(vr, vr);

                    videoVoiceInfo.Width = vr.Width;
                    videoVoiceInfo.Height = vr.Height;

                    s.localVoice = client.VoiceClient.CreateLocalVoiceVideo(videoVoiceInfo, vr, (int)Channel.Video);
                };

                var r = new Photon.Voice.Unity.WebCodecsScreenRecorderUnityTexture(client.logger, videoVoiceInfo, set.WebGLScreenShareSizeMode, onReady);
                s.videoRecorder = r;
                Debug.LogFormat("LBC: Video recorder created: {0}", r.GetType());
                return s;
            } },
#endif
            { StreamType.ScreenShare, (client, set) => {
                var cam = FindObjectOfType<Camera>();
                var sr = cam.gameObject.GetComponent<Photon.Voice.Unity.ScreenRecorder>();
                if (sr == null)
                {
                    Debug.LogFormat("LBC: Adding ScreenRecorder component to Camera object");
                    sr = cam.gameObject.AddComponent<Photon.Voice.Unity.ScreenRecorder>();
                }
                var info = VoiceInfo.CreateVideo(set.VideoCodec, set.VideoBitrate, sr.Width, sr.Height, set.VideoFPS, set.VideoKeyFrameInt);
                sr.SetEncoder(Platform.CreateDefaultVideoEncoder(client.logger, info));
                sr.OnReady = (srReady) => client.PreviewManager.AddView(srReady, srReady);
                return new Stream() { localVoice = client.VoiceClient.CreateLocalVoiceVideo(info, sr, (int)Channel.ScreenShare), videoRecorder = sr};
            } },
        };

        protected void CreateLocalVoices()
        {
            RemoveLocalVoices();
            var set = GetComponent<Settings>();
            ToggleLocalVoice(Client.StreamType.Mic, set.AudioEnable);
            ToggleLocalVoice(Client.StreamType.Camera, set.VideoEnable);
            ToggleLocalVoice(Client.StreamType.CameraLow, set.VideoLowEnable);
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN) && U_WINDOW_CAPTURE_RECORDER_ENABLE
            ToggleLocalVoice(Client.StreamType.WindowShare, set.WindowShareEnable);
#endif
#if UNITY_WEBGL && UNITY_2021_2_OR_NEWER && !UNITY_EDITOR // requires ES6
            ToggleLocalVoice(Client.StreamType.WebGLScreenShare, set.WebGLScreenShareEnable);
#endif
            ToggleLocalVoice(Client.StreamType.ScreenShare, set.ScreenShareEnable);
        }

        public void ToggleLocalVoice(StreamType stream, bool x)
        {
            var set = GetComponent<Settings>();
            var v = localVoices.GetValueOrDefault(stream);
            if (v != null)
            {
                RemoveLocalVoice(v);
                localVoices.Remove(stream);
            }
            if (x)
            {
                v = streamCreate[stream](this, set);
                if (v != null)
                {
                    localVoices[stream] = v;
                }
            }
            else
            {
                localVoices.Remove(stream);
            }
        }
        bool ec = false;
        // Update is called once per frame
        protected virtual void Update()
        {
            if (!started)
                return;
            if (ec != settings.VideoEcho)
            {
                ec = settings.VideoEcho;
            }
            foreach (var v in localVoices)
            {
                if (v.Value.localVoice is LocalVoiceVideo)
                {
                    v.Value.localVoice.DebugEchoMode = settings.VideoEcho;
                    v.Value.localVoice.TransmitEnabled = !settings.VideoMute;
                    v.Value.localVoice.Reliable = settings.VideoReliable;
                    v.Value.localVoice.Encrypt = settings.VideoEncrypt;
                    v.Value.localVoice.Fragment = settings.VideoFragment;
                    v.Value.localVoice.FEC = settings.VideoFEC;
                }
                else if (v.Value.localVoice != null)
                {
                    v.Value.localVoice.DebugEchoMode = settings.AudioEcho;
                    v.Value.localVoice.TransmitEnabled = !settings.AudioMute;
                    v.Value.localVoice.Reliable = settings.AudioReliable;
                    v.Value.localVoice.Encrypt = settings.AudioEncrypt;
                    v.Value.localVoice.Fragment = settings.AudioFragment;
                }
            }

            foreach (var p in audioPlayers)
            {
                p.Service();
            }
        }

        protected void OnApplicationQuit()
        {
            RemoveLocalVoices();
            if (Transport != null)
            {
                Disconnect();
            }
        }
#endif
    }
}