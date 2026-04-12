using Fusion.Addons.Spaces;
using Fusion.Addons.VirtualKeyboard.Touch;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/***
 *
 * AvatarSelectionSpaceSetting manages the UI used for the Space selection in the avatar selection scene.
 * It displays the group Id saved in user preference when UI is enabled.
 * It provides methods to generate a random group ID and reset the group ID.
 * Texts are updated according to the actions made by the user.
 * 
 ***/
public class AvatarSelectionSpaceSetting : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI groupExplanationTMP;
    [SerializeField] private TextMeshProUGUI groupIDInputFieldAreaText;
    [SerializeField] private TouchableTMPInputField touchableTMPInputField;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonTMP;

    private const string HOW_TO_SET_PRIVATE_GROUP = "Enter a private meeting room ID (or click on the <b>Generate</b> button).<br>Share this private room ID with your friends to be together in the same room.<br>Reset the room ID to use the public room.";
    private const string BUTTON_GENERATE_ID = "Generate a private meeting room ID";
    private const string BUTTON_RESET_ID = "Reset the meeting room ID";

    [SerializeField] private string groupId = "";

    [SerializeField] private GameObject connectionPanel;
    public UnityEvent onGroupIDChange;

    bool IsPrivateIdProvided => IsGroupIDValid() && touchableTMPInputField.Text != "";

    private void Awake()
    {
        if (!touchableTMPInputField)
            touchableTMPInputField = GetComponentInChildren<TouchableTMPInputField>();

        if (touchableTMPInputField)
        {
            touchableTMPInputField.onFocusChange.AddListener(OnFocusChange);
            touchableTMPInputField.onTextChange.AddListener(OnTextChange);
        }

        if (!groupExplanationTMP)
            Debug.LogError("groupExplanationTMP has not been set !");

        if (!groupIDInputFieldAreaText)
            Debug.LogError("groupIDInputFieldAreaText has not been set !");

        if (!button)
            Debug.LogError("button has not been set !");
        else
            buttonTMP = button.GetComponentInChildren<TextMeshProUGUI>();

        if (!connectionPanel)
            Debug.LogError("connectionPanel has not been set !");    
    }

    #region TouchableTMPInputField callbacks
    void OnFocusChange()
    {
        UpdateButtonConfig();
    }

    void OnTextChange()
    {
        UpdateButtonConfig();
        SaveGroupID();
    }
    #endregion

    #region Button callback
    void OnClick()
    {
        if (IsPrivateIdProvided)
        {
            ResetPrivateGroup();
        }
        else
        {
            GeneratePrivateGroupID();
        }
    }
    #endregion

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
        groupId = PlayerPrefs.GetString(SpaceRoom.SETTINGS_GROUPID);

        if (groupId != null)
        {
            touchableTMPInputField.Text = groupId;
        }

        groupExplanationTMP.text = HOW_TO_SET_PRIVATE_GROUP;
        UpdateButtonConfig();
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
    }

    private void UpdateButtonConfig()
    {
        if (IsPrivateIdProvided)
        {
            buttonTMP.text = BUTTON_RESET_ID;
        }
        else
        {
            buttonTMP.text = BUTTON_GENERATE_ID;
        }
    }

    private void GeneratePrivateGroupID()
    {
        onGroupIDChange.Invoke();
        int randomNumber = new System.Random().Next(1000, 9999);
        touchableTMPInputField.Text = randomNumber.ToString();
        SaveGroupID();
        
    }
    private void ResetPrivateGroup()
    {
        onGroupIDChange.Invoke();
        touchableTMPInputField.Text = "";
        SaveGroupID();
    }

    public void SaveGroupID()
    {
        if(!IsGroupIDValid()) return;
        PlayerPrefs.SetString(SpaceRoom.SETTINGS_GROUPID, touchableTMPInputField.Text);
    }

    private bool IsGroupIDValid()
    {
        var groupId = touchableTMPInputField.Text;
        bool isValidPrivateGroupId = SpaceRoom.IsValidPrivateGroupId(groupId);
        bool isValidPublicGroupId = SpaceRoom.IsValidPublicGroupId(groupId);
        bool isValiGroupId = isValidPrivateGroupId || isValidPublicGroupId;
        if (!isValiGroupId)
        {
            groupIDInputFieldAreaText.color = Color.red;
            connectionPanel.SetActive(false);
            return false;
        }
        else
        {
            groupIDInputFieldAreaText.color = Color.black;
            connectionPanel.SetActive(true);
            return true;
        }
    }
}
