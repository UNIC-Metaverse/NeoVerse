//using TMPro;
//using UnityEngine;

//public class ChatUI : MonoBehaviour
//{
//    [SerializeField] TMP_Text chatLog;
//    [SerializeField] TMP_InputField input;

//    string currentChannel = "global";

//    void Start()
//    {
//        PhotonChatManager.Instance.OnChannelMessage += OnMessage;
//    }

//    void OnDestroy()
//    {
//        PhotonChatManager.Instance.OnChannelMessage -= OnMessage;
//    }

//    void OnMessage(string channel, string message)
//    {
//        if (channel != currentChannel) return;
//        chatLog.text += "\n" + message;
//    }

//    public void Send()
//    {
//        if (string.IsNullOrWhiteSpace(input.text)) return;

//        PhotonChatManager.Instance.SendToChannel(currentChannel, input.text);
//        input.text = "";
//    }
//}
