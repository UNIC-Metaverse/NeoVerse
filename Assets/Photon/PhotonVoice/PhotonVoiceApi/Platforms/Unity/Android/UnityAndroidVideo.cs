#if PHOTON_VOICE_VIDEO_ENABLE
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Voice.Unity
{
    // depends on Unity's AndroidJavaProxy
    public static class AndroidVideoGkPluginEventIssuer
    {
        const int GlPluginEventID = 1000;
        static AndroidVideoGkPluginEventIssuerMB mb;
        static GameObject go;
        static ILogger logger;

        public static void Start(ILogger logger_)
        {
            logger = logger_;
            if (go == null)
            {
                go = new GameObject("[PV] AndroidVideoGkPluginEventIssuer"); ;
                mb = go.AddComponent<AndroidVideoGkPluginEventIssuerMB>();
                var nc = new AndroidJavaObject("com.exitgames.photon.video.NativeCallback");
                IntPtr pluginEventPtr = (IntPtr)nc.Call<long>("getNativeFunctionPointer", GlPluginEventID);
                mb.SetPluginEventPtr(pluginEventPtr, GlPluginEventID);
                logger.Log(LogLevel.Info, "[PV] [UAVPEI] AndroidVideoGkPluginEventIssuer started");
            }
        }

        public static void Stop()
        {
            UnityEngine.Object.Destroy(mb);
            if (go)
            {
                UnityEngine.Object.Destroy(go);
                go = null;
            }
            logger.Log(LogLevel.Info, "[PV] [UAVPEI] AndroidVideoGkPluginEventIssuer stopped");
        }
    }

    public class AndroidVideoGkPluginEventIssuerMB : MonoBehaviour
    {
        private IntPtr pluginEventPtr;
        private int eventID;
        public void SetPluginEventPtr(IntPtr pluginEventPtr, int eventID)
        {
            this.pluginEventPtr = pluginEventPtr;
            this.eventID = eventID;
        }
        public void Update()
        {
            if (pluginEventPtr != IntPtr.Zero)
            {
                GL.IssuePluginEvent(pluginEventPtr, eventID);
            }
        }
    }

    public class AndroidVideoEncoder : IEncoder
    {
        static class BufferFlag
        {
            public const int BUFFER_FLAG_CODEC_CONFIG = 2;
            public const int BUFFER_FLAG_END_OF_STREAM = 4;
            public const int BUFFER_FLAG_KEY_FRAME = 1;
            public const int BUFFER_FLAG_PARTIAL_FRAME = 8;
        }

        class DataCallback : AndroidJavaProxy
        {
            Action<ArraySegment<byte>, FrameFlags> callback;
            public DataCallback() : base("com.exitgames.photon.video.Encoder$DataCallback") { }
            public void SetCallback(Action<ArraySegment<byte>, FrameFlags> callback)
            {
                this.callback = callback;
            }

            byte[] ubuf = new byte[0];
            // ByteArray on Java side
            public void onData(AndroidJavaObject arrayObj, int bufferFlags)
            {
                if (callback != null)
                {
                    FrameFlags flags = 0;
                    if ((bufferFlags & BufferFlag.BUFFER_FLAG_CODEC_CONFIG) != 0)
                    {
                        flags |= FrameFlags.Config;
                    }
                    if ((bufferFlags & BufferFlag.BUFFER_FLAG_KEY_FRAME) != 0)
                    {
                        flags |= FrameFlags.KeyFrame;
                    }
                    if ((bufferFlags & BufferFlag.BUFFER_FLAG_PARTIAL_FRAME) != 0)
                    {
                        flags |= FrameFlags.PartialFrame;
                    }
                    if ((bufferFlags & BufferFlag.BUFFER_FLAG_END_OF_STREAM) != 0)
                    {
                        flags |= FrameFlags.EndOfStream;
                    }

                    AndroidJavaObject byteArray = arrayObj.Get<AndroidJavaObject>("buffer");
                    // we need to copy sbyte[] to byte[] if we want to avoid obsolete warninigs (see comment in AndroidVideoDecoder.Input)
                    var buf = AndroidJNIHelper.ConvertFromJNIArray<sbyte[]>(byteArray.GetRawObject());
                    if (ubuf.Length < buf.Length)
                    {
                        ubuf = new byte[buf.Length];
                    }

                    // Throws "ArgumentException: Object must be an array of primitives." in some Unity versions.
                    // See https://issuetracker.unity3d.com/issues/android-sbyte-type-is-considered-to-be-not-primitive-when-compiling-il2cpp-code
                    // Upgrade or use inefficient per byte copy:
                    // for (int i = 0; i < buf.Length; i++) ubuf[i] = (byte)buf[i];
                    Buffer.BlockCopy(buf, 0, ubuf, 0, buf.Length);

                    byteArray.Dispose();
                    arrayObj.Dispose();

                    this.callback(new ArraySegment<byte>(ubuf, 0, buf.Length), flags);
                }
            }
        }

        private AndroidJavaObject encoder;
        protected ILogger logger;
        protected VoiceInfo info;
        protected AndroidJavaObject activity;

        public AndroidVideoEncoder(ILogger logger, VoiceInfo info)
        {
            this.logger = logger;
            this.info = info;
            AndroidJavaClass app = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = app.GetStatic<AndroidJavaObject>("currentActivity");
        }

        public void Start(int width, int height)
        {
            Width = width;
            Height = height;
            encoder = new AndroidJavaObject("com.exitgames.photon.video.Encoder");
            logger.Log(LogLevel.Info, "[PV] [UAVE] Unity.AndroidVideoEncoder: AndroidJavaObjects created, actual video size = " + Width + "x" + Height);
            int res = encoder.Call<int>("start", info.Codec.ToString(), Width, Height, info.Bitrate, info.FPS, info.KeyFrameInt, this.callback);
            if (res != 0)
            {
                switch (res)
                {
                    case 1: Error = "Unsupported codec " + info.Codec; break;
                    default: Error = "Error " + res; break;
                }
                logger.Log(LogLevel.Error, "[PV] [UAVD] Unity.AndroidVideoEncoder: {0}", Error);
            }
        }

        public AndroidJavaObject Surface => encoder.Call<AndroidJavaObject>("getSurface");
        public int Width { get; private set; }
        public int Height { get; private set; }

        DataCallback callback = new DataCallback();

        public string Error { get; protected set; }

        public Action<ArraySegment<byte>, FrameFlags> Output
        {
            set
            {
                if (Error == null)
                {
                    this.callback.SetCallback(value);
                }
            }
        }

        private static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(new byte[] { });
        public ArraySegment<byte> DequeueOutput(out FrameFlags flags)
        {
            flags = 0;
            return EmptyBuffer;
        }
        public void EndOfStream()
        {
        }

        public I GetPlatformAPI<I>() where I : class
        {
            return null;
        }

        public virtual void Dispose()
        {
            Debug.Log("[PV] [UAVE] Dispose()");
            if (encoder != null)
            {
                encoder.Call("close");
            }
        }
    }

    abstract public class AndroidCameraVideoEncoder : AndroidVideoEncoder
    {
        protected class OnReadyCallback : AndroidJavaProxy
        {
            Action<AndroidJavaObject> callback;
            public OnReadyCallback(Action<AndroidJavaObject> callback) : base("com.exitgames.photon.video.Camera$OnReadyCallback")
            {
                this.callback = callback;
            }
            public void onReady(AndroidJavaObject camera)
            {
                if (callback != null)
                {
                    callback(camera);
                }
            }
        }

        public AndroidCameraVideoEncoder(ILogger logger, VoiceInfo info) : base(logger, info)
        {
        }

        protected void startEncoderAndCamera(AndroidJavaObject camera)
        {
            this.camera = camera;

            var videoSize = camera.Call<AndroidJavaObject>("getVideoSize");
            int width = videoSize.Call<int>("getWidth");
            int height = videoSize.Call<int>("getHeight");

            Start(width, height);

            camera.Call("start");
            surfId = new AndroidJavaObject("java.lang.Object");
            camera.Call("addSurface", surfId, Surface);
        }

        private AndroidJavaObject camera;
        private AndroidJavaObject surfId; // could use base.encoder here but it's private for clarity

        public override void Dispose()
        {
            if (camera != null)
            {
                camera.Call("removeSurface", surfId);
                camera.Call("close");
            }
            base.Dispose();
        }
    }

    // renders preview to SurfaceView
    public class AndroidCameraVideoEncoderSurfaceView : AndroidCameraVideoEncoder
    {
        public AndroidJavaObject Preview { get; private set; }

        public AndroidCameraVideoEncoderSurfaceView(ILogger logger, VoiceInfo info, string cameraID)
            : base(logger, info)
        {
            if (Error != null)
            {
                return;
            }
            var camera = new AndroidJavaObject("com.exitgames.photon.video.CameraSurfaceView", activity, cameraID, info.Width, info.Height, info.FPS);
            Preview = camera.Call<AndroidJavaObject>("getPreview");

            startEncoderAndCamera(camera);

            logger.Log(LogLevel.Info, "[PV] [UAVE] Unity.AndroidCameraVideoEncoderSurfaceView initialized");
        }
    }

    // renders preview to external texture
    public class AndroidCameraVideoEncoderTexture : AndroidCameraVideoEncoder
    {
        public Texture Preview { get; private set; }

        OnReadyCallback onReadyCallback;

        public AndroidCameraVideoEncoderTexture(ILogger logger, VoiceInfo info, string cameraID, Action<AndroidCameraVideoEncoderTexture> onReady)
            : base(logger, info)
        {
            if (Error != null)
            {
                return;
            }

            if (Application.platform != RuntimePlatform.Android ||
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 &&
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
            {
                Error = "Unsupported platform " + Application.platform + "/" + SystemInfo.graphicsDeviceType;
                logger.Log(LogLevel.Error, "[PV] [UAVE] Unity.AndroidCameraVideoEncoderTexture: {0}", Error);
                return;
            }

            this.onReadyCallback = new OnReadyCallback((cam) =>
            {
                var texID = cam.Call<int>("getPreview");
                if (texID != 0)
                {
                    Preview = Texture2D.CreateExternalTexture(info.Width, info.Height, TextureFormat.ARGB32, false, true, new IntPtr(texID));
                    logger.Log(LogLevel.Info, "[PV] [UAVE] camera getPreview: " + texID);
                }
                else
                {
                    Error = "Preview texture ID is 0";
                    logger.Log(LogLevel.Error, "[PV] [UAVE] camera getPreview error: {0}", Error);
                }
                onReady(this);
            });

            var camera = new AndroidJavaObject("com.exitgames.photon.video.CameraTexture", activity, cameraID, info.Width, info.Height, info.FPS, this.onReadyCallback);

            startEncoderAndCamera(camera);

            AndroidVideoGkPluginEventIssuer.Start(logger);

            logger.Log(LogLevel.Info, "[PV] [UAVE] Unity.AndroidCameraVideoEncoderTexture initialized");
        }
    }

    public class AndroidVideoDecoder : IDecoder
    {
        protected ILogger logger;
        protected VoiceInfo info;
        public int Width { get { return info.Width; } }
        public int Height { get { return info.Height; } }

        protected AndroidJavaObject decoder;

        public AndroidVideoDecoder(ILogger logger, VoiceInfo info)
        {
            this.logger = logger;
            this.info = info;
        }

        public void Open(VoiceInfo info)
        {
            if (Error != null)
            {
                return;
            }

            logger.Log(LogLevel.Info, "[PV] [UAVD] Open " + info);

            try
            {
                int res = decoder.Call<int>("start", info.Codec.ToString(), info.Width, info.Height);
                if (res != 0)
                {
                    switch (res)
                    {
                        case 1: Error = "Unsupported codec " + info.Codec; break;
                        default: Error = "Error " + res; break;
                    }
                    logger.Log(LogLevel.Error, "[PV] [UAVD] Unity.AndroidVideoDecoder: {0}", Error);
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
                logger.Log(LogLevel.Error, "[PV] [UAVD] Unity.AndroidVideoDecoder: {0}", Error);
                logger.Log(LogLevel.Error, "[PV] [UAVD] Unity.AndroidVideoDecoder: {0}", e.StackTrace);
            }
        }

        public string Error { get; protected set; }

        public void Input(ref FrameBuffer buf)
        {
            if (buf.Array == null)
            {
                return;
            }
            if (Error == null)
            {
                // passing buf.Array directly results in logging "AndroidJNIHelper.GetSignature: using Byte parameters is obsolete, use SByte parameters instead"
                // and "AndroidJNIHelper: converting Byte array is obsolete, use SByte array instead"
                // on each call
                // cast (sbyte[])(Array)buf.Array does not help,
                // so we need to copy the buffer if we want to avoid obsolete warninigs
                if (sbuf.Length < buf.Length)
                {
                    sbuf = new sbyte[buf.Length];
                }

                // Throws "ArgumentException: Object must be an array of primitives." in some Unity versions.
                // See https://issuetracker.unity3d.com/issues/android-sbyte-type-is-considered-to-be-not-primitive-when-compiling-il2cpp-code
                // Upgrade or use inefficient per byte copy:
                // for (int i = 0; i < buf.Length; i++) sbuf[i] = (sbyte)buf.Array[buf.Offset + i];
                Buffer.BlockCopy(buf.Array, buf.Offset, sbuf, 0, buf.Length);
                decoder.Call("decode", new object[] { sbuf, 0, buf.Length, (int)buf.Flags });
            }
        }

        sbyte[] sbuf = new sbyte[0];

        public void Dispose()
        {
            Debug.Log("[PV] [UAVD] Dispose()");
            decoder.Call("close");
        }
    }

    // renders preview to SurfaceView
    public class AndroidVideoDecoderSurfaceView : AndroidVideoDecoder
    {
        public AndroidJavaObject Preview { get; private set; }

        AndroidJavaClass app;
        AndroidJavaObject activity;
        public AndroidVideoDecoderSurfaceView(ILogger logger, VoiceInfo info)
            : base(logger, info)
        {
            app = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            activity = app.GetStatic<AndroidJavaObject>("currentActivity");

            decoder = new AndroidJavaObject("com.exitgames.photon.video.DecoderSurfaceView", activity);
            Preview = decoder.Call<AndroidJavaObject>("getPreview");
            logger.Log(LogLevel.Info, "[PV] [UAVD] Unity.AndroidVideoDecoderSurfaceView initialized");
        }
    }

    // renders preview to external texture
    public class AndroidVideoDecoderTexture : AndroidVideoDecoder
    {
        OnReadyCallback onReadyCallback;
        class OnReadyCallback : AndroidJavaProxy
        {
            Action callback;
            public OnReadyCallback(Action callback) : base("com.exitgames.photon.video.Decoder$OnReadyCallback")
            {
                this.callback = callback;
            }
            public void onReady()
            {
                if (callback != null)
                {
                    callback();
                }
            }
        }

        public Texture Preview { get; private set; }

        public AndroidVideoDecoderTexture(ILogger logger, VoiceInfo info, Action onReady)
            : base(logger, info)
        {
            if (Application.platform != RuntimePlatform.Android ||
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2 &&
                SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3)
            {
                Error = "Unsupported platform " + Application.platform + "/" + SystemInfo.graphicsDeviceType;
                logger.Log(LogLevel.Error, "[PV] [UAVD] Unity.AndroidVideoDecoderTexture: {0}", Error);
                return;
            }

            this.onReadyCallback = new OnReadyCallback(() =>
            {
                var texID = decoder.Call<int>("getPreview");
                if (texID != 0)
                {
                    Preview = Texture2D.CreateExternalTexture(info.Width, info.Height, TextureFormat.ARGB32, false, true, new IntPtr(texID));
                    logger.Log(LogLevel.Info, "[PV] [UAVD] decoder getPreview: " + texID);
                }
                else
                {
                    Error = "Preview texture ID is 0";
                    logger.Log(LogLevel.Error, "[PV] [UAVD] decoder getPreview error: {0}", Error);
                }
                onReady();
            });

            decoder = new AndroidJavaObject("com.exitgames.photon.video.DecoderTexture", this.onReadyCallback);

            AndroidVideoGkPluginEventIssuer.Start(logger);

            logger.Log(LogLevel.Info, "[PV] [UAVD] Unity.AndroidVideoDecoderTexture initialized");
        }
    }

    public class AndroidPreviewManagerSurfaceView : Photon.Voice.PreviewManager
    {
        public AndroidJavaObject ViewManager { get; private set; }

        public AndroidPreviewManagerSurfaceView(ILogger logger)
        {
            this.logger = logger;

            var app = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = app.GetStatic<AndroidJavaObject>("currentActivity");

            //var viewManagerClass = new AndroidJavaClass("com.exitgames.photon.video.ViewManager");
            //viewManager = viewManagerClass.GetStatic<AndroidJavaObject>("INSTANCE");
            ViewManager = new AndroidJavaObject("com.exitgames.photon.video.ViewManager", activity);
            logger.Log(LogLevel.Info, "[PV] [UAVM] AndroidPreviewManagerSurfaceView initialized");
        }

        override public void AddView(object id, IVideoPreview view)
        {
            ViewManager.Call("addView", view.PlatformView);
            base.AddView(id, view);
        }

        override public void RemoveView(object id)
        {
            if (views.ContainsKey(id))
            {
                ViewManager.Call("removeView", views[id].PlatformView);
            }
            else
            {
                logger.Log(LogLevel.Error, "[PV] [UAVM] RemoveView: id not found: " + id);
            }
            base.RemoveView(id);
        }

        override protected void Apply(ViewState v)
        {
            ViewManager.Call("setViewBounds", (AndroidJavaObject)v.PlatformView, v.x, v.y, v.w, v.h, 0);
            logger.Log(LogLevel.Info, "[PV] [UAVM] call setViewBounds {0} {1} {2} {3}", v.x, v.y, v.w, v.h);
        }

        ILogger logger;
    }

    public class AndroidVideoRecorderSurfaceView : IVideoRecorder
    {
        public IEncoder Encoder { get; private set; }
        public object PlatformView { get { return (Encoder as AndroidCameraVideoEncoderSurfaceView).Preview; } }
        public Rotation Rotation => Rotation.Rotate0;
        public Flip Flip => Flip.None;

        public int Width { get { return (Encoder as AndroidCameraVideoEncoderSurfaceView).Width; } }
        public int Height { get { return (Encoder as AndroidCameraVideoEncoderSurfaceView).Height; } }

        public string Error => Encoder.Error;

        public AndroidVideoRecorderSurfaceView(ILogger logger, VoiceInfo info, string cameraID, Action<IVideoRecorder> onReady)
        {
            Encoder = new AndroidCameraVideoEncoderSurfaceView(logger, info, cameraID);
            onReady(this);
        }

        public void Dispose()
        {
        }

    }

    public class AndroidVideoRecorderUnityTexture : IVideoRecorder
    {
        public IEncoder Encoder { get; protected set; }
        public Rotation Rotation => Rotation.Rotate0;
        public Flip Flip => Flip.None;
        public object PlatformView
        {
            get
            {
                return ((AndroidCameraVideoEncoderTexture)Encoder).Preview;
            }
        }

        public int Width { get { return ((AndroidCameraVideoEncoderTexture)Encoder).Width; } }
        public int Height { get { return ((AndroidCameraVideoEncoderTexture)Encoder).Height; } }

        public string Error => Encoder.Error;

        public AndroidVideoRecorderUnityTexture(ILogger logger, VoiceInfo info, string cameraID, Action<IVideoRecorder> onReady)
        {
            Encoder = new AndroidCameraVideoEncoderTexture(logger, info, cameraID, (e) => onReady(this)); ;
            if (Error != null)
            {
                onReady(this);
            }
        }

        public void Dispose()
        {
            if (Encoder != null)
            {
                Encoder.Dispose();
            }
        }
    }

    public class AndroidVideoPlayerUnityTexture : IVideoPlayer
    {
        public IDecoder Decoder { get; protected set; }
        public Rotation Rotation => Rotation.Rotate0;
        public Flip Flip => Flip.None;
        public object PlatformView
        {
            get
            {
                return ((AndroidVideoDecoderTexture)Decoder).Preview;
            }
        }

        public int Width { get { return ((AndroidVideoDecoderTexture)Decoder).Width; } }
        public int Height { get { return ((AndroidVideoDecoderTexture)Decoder).Height; } }

        public AndroidVideoPlayerUnityTexture(ILogger logger, VoiceInfo info, Action<IVideoPlayer> onReady)
        {
            Decoder = new AndroidVideoDecoderTexture(logger, info, () => onReady(this));
        }

        public void Dispose()
        {
            if (Decoder != null)
            {
                Decoder.Dispose();
            }
        }
    }

    /// <summary>Enumerates cameras available on device.
    /// </summary>
    public class AndroidVideoInEnumerator : DeviceEnumeratorBase
    {
        // android.hardware.camera2.CameraMetadata to FacingEnum
        static Dictionary<int, CameraFacing> androidToVoiceFacing = new Dictionary<int, CameraFacing>()
        {
            {0, CameraFacing.Front},     // LENS_FACING_FRONT
            {1, CameraFacing.Back},      // LENS_FACING_BACK
            {2, CameraFacing.Undef},     // LENS_FACING_EXTERNAL
        };

        public AndroidVideoInEnumerator(ILogger logger) : base(logger)
        {
            Refresh();
        }

        public override bool IsSupported => true;

        /// <summary>Refreshes the microphones list.
        /// </summary>
        public override void Refresh()
        {
            try
            {
                if (IsSupported)
                {
                    AndroidJavaClass app = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = app.GetStatic<AndroidJavaObject>("currentActivity");

                    AndroidJavaClass cameraClass = new AndroidJavaClass("com.exitgames.photon.video.Camera");
                    var cameraStatic = cameraClass.GetStatic<AndroidJavaObject>("Companion");

                    devices = new List<DeviceInfo>();
                    var javaArr = cameraStatic.Call<AndroidJavaObject>("getDevices", activity);
                    if (javaArr != null)
                    {
                        if (javaArr.GetRawObject().ToInt64() != 0) // Is another null check required? Taken from https://forum.unity.com/threads/passing-arrays-through-the-jni.91757/#post-1432511
                        {
                            String[] arr = AndroidJNIHelper.ConvertFromJNIArray<String[]>(javaArr.GetRawObject());
                            foreach (var id in arr)
                            {
                                var androidFacing = cameraStatic.Call<int>("getDeviceFacing", activity, id);
                                var facing = CameraFacing.Undef;
                                androidToVoiceFacing.TryGetValue(androidFacing, out facing);
                                DeviceInfo d = new DeviceInfo(id, id, new DeviceFeatures(facing));
                                devices.Add(d);
                            }
                            Error = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error = e.ToString();
                if (Error == null) // should never happen but since Error used as validity flag, make sure that it's not null
                {
                    Error = "Exception in AndroidVideoInEnumerator.Refresh()";
                }
            }

            if (OnReady != null)
            {
                OnReady();
            }
        }

        public override void Dispose()
        {
        }
    }
}
#endif