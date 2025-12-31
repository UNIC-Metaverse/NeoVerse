using Photon.Realtime;
using ExitGames.Client.Photon;
using Transport = Photon.Voice.LoadBalancingTransport2;
using Photon.Voice;
using System.Collections;

namespace TestVideo
{
    public class LBClient : Client
    {
#if PHOTON_VOICE_VIDEO_ENABLE
        LoadBalancingTransport lbt = new LoadBalancingTransport2();
        public override VoiceClient VoiceClient => lbt.VoiceClient;
        protected override IVoiceTransport Transport => lbt;
        public override bool IsConnected => lbt.State != ClientState.PeerCreated && lbt.State != ClientState.Disconnected;
        public override string StateStr => lbt.State + (lbt.CurrentRoom != null ? (" " + lbt.CurrentRoom.Name + ", plrs: " + lbt.CurrentRoom.PlayerCount) : "");
        public override int RoundTripTime => lbt.LoadBalancingPeer.RoundTripTime;
        public override long BytesOut => lbt.LoadBalancingPeer.BytesOut;
        public override long PacketsOut => lbt.LoadBalancingPeer.TrafficStatsOutgoing.TotalPacketCount;
        public override long BytesIn => lbt.LoadBalancingPeer.BytesIn;
        public override long PacketsIn => lbt.LoadBalancingPeer.TrafficStatsIncoming.TotalPacketCount;

        public override void Connect()
        {
            var settings = GetComponent<Settings>();
            lbt.ConnectToRegionMaster(settings.Region);
        }

        public override void Disconnect()
        {
            lbt.Disconnect();
        }

        protected override IEnumerator Start()
        {
            lbt.LoadBalancingPeer.DebugOut = DebugLevel.INFO;
            lbt.LoadBalancingPeer.TrafficStatsEnabled = true;
            var settings = GetComponent<Settings>();
            lbt.AppId = settings.AppId;
            lbt.AppVersion = settings.AppVersion;
            lbt.StateChanged += (ClientState stateOld, ClientState s) =>
            {
                logger.Log(LogLevel.Info, $"LBC: state: {s}");
                switch (s)
                {
                    case ClientState.ConnectedToMasterServer:
                        lbt.OpJoinRandomOrCreateRoom(null, new EnterRoomParams() { RoomName = Settings.RoomName });
                        break;
                    case ClientState.Joined:
                        // recreate voices to update from settings possibly changed by user in lobby
                        CreateLocalVoices();
                        break;
                    case ClientState.Disconnected:
                        RemoveLocalVoices();
                        break;
                }
            };

            return base.Start();
        }

        protected override void Update()
        {
            base.Update();
            if (!started)
                return;
            lbt.Service();
        }
#endif
    }
}