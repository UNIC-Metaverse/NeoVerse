using Fusion.Addons.Avatar;
using UnityEngine;
using UnityEngine.UI;


namespace Photon.Chat.DemoChat
{
    [RequireComponent(typeof(ChatNewGui))]
    public class NamePickNewGui : MonoBehaviour
    {
        private const string UserNamePlayerPref = "NamePickUserName";

        public ChatNewGui chatNewComponent;

        public TMPro.TMP_InputField idInput;

        public void Start()
        {
            this.chatNewComponent = FindObjectOfType<ChatNewGui>();
            this.idInput.interactable = false;

            string prefsName = PlayerPrefs.GetString(UserInfo.SETTINGS_USERNAME);
            Debug.Log("NamePickNewGui: Start() found PlayerPref name: " + prefsName);
            if (!string.IsNullOrEmpty(prefsName))
            {
                this.idInput.text = prefsName;
            }
        }


        // new UI will fire "EndEdit" event also when loosing focus. So check "enter" key and only then StartChat.
        public void EndEditOnEnter()
        {
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                this.StartChat();
            }
        }

        public void StartChat()
        {
            ChatNewGui chatNewComponent = FindObjectOfType<ChatNewGui>();
            chatNewComponent.UserName = this.idInput.text.Trim();
            chatNewComponent.Connect();
            this.enabled = false;

            PlayerPrefs.SetString(NamePickNewGui.UserNamePlayerPref, chatNewComponent.UserName);
        }
    }
}