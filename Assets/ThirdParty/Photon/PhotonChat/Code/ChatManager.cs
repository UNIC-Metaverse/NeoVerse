using ExitGames.Client.Photon;
using Photon.Chat;
using UnityEngine;

public class ChatManager : MonoBehaviour, IChatClientListener
{
    [Header("Photon Chat")]
    [SerializeField] private string chatAppId;
    [SerializeField] private string appVersion = "1.0";
    [SerializeField] private string region = "EU"; // "EU", "US", "ASIA" per Photon docs

    public ChatClient Client { get; private set; }
    public bool IsConnected => Client != null && Client.CanChat;

    public System.Action<string, string> OnChannelMessage; // (channel, formattedMessage)
    public System.Action<string> OnStatus;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void Connect(string userId)
    {
        if (Client != null) return;

        Client = new ChatClient(this);
        Client.ChatRegion = region;

        var auth = new AuthenticationValues(userId);
        Client.Connect(chatAppId, appVersion, auth);
    }

    void Update()
    {
        Client?.Service(); // REQUIRED: pumps callbacks
    }

    public void JoinChannel(string channel, int history = 50)
    {
        if (!IsConnected) return;
        Client.Subscribe(channel, history);
    }

    public void SendToChannel(string channel, string message)
    {
        if (!IsConnected) return;
        if (string.IsNullOrWhiteSpace(message)) return;

        message = message.Trim();
        if (message.Length > 200) message = message.Substring(0, 200);

        Client.PublishMessage(channel, message);
    }

    // ===== IChatClientListener =====

    public void DebugReturn(DebugLevel level, string message) =>
        OnStatus?.Invoke($"[{level}] {message}");

    public void OnChatStateChange(ChatState state) =>
        OnStatus?.Invoke($"Chat state: {state}");

    public void OnConnected()
    {
        OnStatus?.Invoke("Chat connected");
        // optional: auto-join a lobby channel
        // JoinChannel("global");
    }

    public void OnDisconnected() => OnStatus?.Invoke("Chat disconnected");

    public void OnGetMessages(string channelName, string[] senders, object[] messages)
    {
        for (int i = 0; i < senders.Length; i++)
        {
            string line = $"{senders[i]}: {messages[i]}";
            OnChannelMessage?.Invoke(channelName, line);
        }
    }

    public void OnSubscribed(string[] channels, bool[] results) =>
        OnStatus?.Invoke("Subscribed: " + string.Join(", ", channels));

    public void OnUnsubscribed(string[] channels) { }

    public void OnPrivateMessage(string sender, object message, string channelName) { }
    public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
    public void OnUserSubscribed(string channel, string user) { }
    public void OnUserUnsubscribed(string channel, string user) { }
}
