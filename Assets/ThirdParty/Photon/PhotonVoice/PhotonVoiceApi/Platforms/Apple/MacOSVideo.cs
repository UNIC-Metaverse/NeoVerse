#if PHOTON_VOICE_VIDEO_ENABLE
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Photon.Voice.MacOS
{
    public class VideoEncoder : IEncoderDirectImage
    {
        const string lib_name = "Video";
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Video_CreateEncoder(int hostID, OutCallbackDelegate callback, int codec, int width, int height, int bitrate, int fps, int keyFrameInt);
        [DllImport(lib_name)]
        private static extern int Photon_Video_Encode(IntPtr handle, IntPtr buf, int width, int height);
        [DllImport(lib_name)]
        private static extern void Photon_Video_DestroyEncoder(IntPtr handle);

        public delegate void OutCallbackDelegate(int instanceID, IntPtr buf, int size, int flags);

        IntPtr handle;
        bool disposed;

        ILogger logger;
        int instanceID;

        private static Dictionary<int, VideoEncoder> handles = new Dictionary<int, VideoEncoder>();
        static int instanceCnt;

        // ref to delegate preventing it from GC'ing
        OutCallbackDelegate outCallbackDelegate;

        public VideoEncoder(ILogger logger, VoiceInfo info)
        {
            this.logger = logger;
            this.instanceID = instanceCnt;
            instanceCnt++;
            lock (handles)
            {
                handles.Add(this.instanceID, this);
            }
            outCallbackDelegate = new OutCallbackDelegate(staticOutCallback);
            var handle = Photon_Video_CreateEncoder(this.instanceID, outCallbackDelegate, 0, info.Width, info.Height, info.Bitrate, info.FPS, info.KeyFrameInt);
            lock (this)
            {
                this.handle = handle;
            }
            if (this.handle == IntPtr.Zero)
            {
                Error = "Native Encoder creation error";
            }
        }

        public string Error { get; private set; }

        [MonoPInvokeCallbackAttribute(typeof(OutCallbackDelegate))]
        static void staticOutCallback(int instanceID, IntPtr buf, int len, int flags)
        {
            bool ok;
            VideoEncoder instance;
            lock (handles)
            {
                ok = handles.TryGetValue(instanceID, out instance);
            }
            if (ok)
            {
                instance.outCallback(buf, len, flags);
            }
        }

        byte[] bufManaged = new byte[0];
        void outCallback(IntPtr buf, int len, int flags0)
        {
            FrameFlags flags = (FrameFlags)flags0;
            if (bufManaged.Length < len)
            {
                bufManaged = new byte[len];
            }
            Marshal.Copy(buf, bufManaged, 0, len);
            Output(new ArraySegment<byte>(bufManaged, 0, len), flags);
        }

        public ImageFormat ImageFormat { get { return ImageFormat.BGRA; } }

        public Action<ArraySegment<byte>, FrameFlags> Output { set; get; }

        private static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(new byte[] { });
        public ArraySegment<byte> DequeueOutput(out FrameFlags flags)
        {
            flags = 0;
            return EmptyBuffer;
        }

        public void Input(ImageBufferNative imBuf)
        {
            if (Error != null)
            {
                return;
            }
            if (disposed)
            {
                return;
            }
            if (Output == null)
            {
                Error = "Output action is not set";
                logger.Log(LogLevel.Error, "[PV] [VE] " + Error);
                return;
            }

            var err = Photon_Video_Encode(handle, imBuf.Planes[0], imBuf.Info.Width, imBuf.Info.Height);
            if (err != 0)
            {
                // the error may be not critical, do not set Error
                // Error = "Native Encoder encoding error " + err;
                logger.Log(LogLevel.Error, "[PV] [VE] Photon_Video_Encode error: " + err);
            }
        }

        public void EndOfStream()
        {
        }

        public I GetPlatformAPI<I>() where I : class
        {
            return null;
        }

        public void Dispose()
        {
            lock (handles)
            {
                handles.Remove(instanceID);
            }
            lock (this)
            {
                if (handle != IntPtr.Zero)
                {
                    Photon_Video_DestroyEncoder(handle);
                    handle = IntPtr.Zero;
                }
                disposed = true;
            }
        }
    }

    public class VideoDecoder : IDecoderDirect<ImageBufferNative>
    {
        const string lib_name = "Video";
        [DllImport(lib_name)]
        private static extern IntPtr Photon_Video_CreateDecoder(int hostID, OutCallbackDelegate callback);
        [DllImport(lib_name)]
        private static extern int Photon_Video_Decode(IntPtr handle, IntPtr data, int size, int flags);
        [DllImport(lib_name)]
        private static extern void Photon_Video_DestroyDecoder(IntPtr handle);

        public delegate void OutCallbackDelegate(int instanceID, IntPtr buf, int width, int height, int bytesPerRow);

        bool ready;
        ILogger logger;
        VoiceInfo info;
        IntPtr handle;
        int instanceID;

        private static Dictionary<int, VideoDecoder> handles = new Dictionary<int, VideoDecoder>();
        static int instanceCnt;

        // ref to delegate preventing it from GC'ing
        OutCallbackDelegate outCallbackDelegate;

        public VideoDecoder(ILogger logger, VoiceInfo info)
        {
            this.logger = logger;
            this.info = info;

            this.instanceID = instanceCnt;
            instanceCnt++;
            lock (handles)
            {
                handles.Add(this.instanceID, this);
            }
            outCallbackDelegate = new OutCallbackDelegate(staticOutCallback);
            var handle = Photon_Video_CreateDecoder(this.instanceID, outCallbackDelegate);
            lock (this)
            {
                this.handle = handle;
            }
            if (this.handle == IntPtr.Zero)
            {
                Error = "Native Decoder creation error";
            }
        }

        public string Error { get; private set; }
        public Action<ImageBufferNative> Output { get; set; }

        private Flip flip = Flip.None;
        public void Open(VoiceInfo info)
        {
            ready = true;
            logger.Log(LogLevel.Info, "[PV] [VD] " + info.Codec + " initialized");
        }

        public void Input(ref FrameBuffer buf)
        {
            if (Error != null)
            {
                return;
            }
            if (!ready)
            {
                return;
            }
            if (buf.Array == null)
            {
                return;
            }
            if (Output == null)
            {
                Error = "Output action is not set";
                logger.Log(LogLevel.Error, "[PV] [VD] " + Error);
                return;
            }

            var err = Photon_Video_Decode(handle, buf.Ptr, buf.Length, 0);
            if (err != 0)
            {
                // the error may be not critical, do not set Error
                // Error = "Native Decoder decoding error " + err;
                logger.Log(LogLevel.Error, "[PV] [VD] Photon_Video_Decode error: " + err);
            }
        }

        [MonoPInvokeCallbackAttribute(typeof(OutCallbackDelegate))]
        static void staticOutCallback(int instanceID, IntPtr buf, int width, int height, int bytesPerRow)
        {
            bool ok;
            VideoDecoder instance;
            lock (handles)
            {
                ok = handles.TryGetValue(instanceID, out instance);
            }
            if (ok)
            {
                instance.outCallback(buf, width, height, bytesPerRow);
            }
        }

        void outCallback(IntPtr inBuf, int width, int height, int bytesPerRow)
        {
            Output(new ImageBufferNative(inBuf, width, height, width * 4, ImageFormat.BGRA));
        }

        public void Dispose()
        {
            lock (this)
            {
                if (handle != IntPtr.Zero)
                {
                    Photon_Video_DestroyDecoder(handle);
                    handle = IntPtr.Zero;
                }
                ready = false;
            }
        }
    }
}
#endif
