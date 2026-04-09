using Fusion.Addons.Spaces;
using Fusion.Addons.VirtualKeyboard.Touch;
using Fusion.Samples.IndustriesComponents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/***
 *
 * SpaceSettingsMenu manages the UI used for the Space selection in the application menu.
 * It displays the group Id saved in user preference when UI is enabled.
 * It provides methods to :
 *      - generate a random private group ID,
 *      - join or leave a private group ID.
 * Texts are updated according to the actions made by the user.
 * 
 ***/
public class SpaceSettingsMenu : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI currentGroupStatusTMP;
    [SerializeField] private TextMeshProUGUI groupExplanationTMP;

    [SerializeField] private SpaceRoom spaceRoom;
    public TouchableTMPInputField touchableTMPInputField;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonTMP;

    private const string HOW_TO_JOIN_PRIVATE_GROUP = "Enter a private meeting room ID (or click on the <b>Generate</b> button).<br>Then click on the <b>Join</b> button below to enter a private room.<br>Share this private room ID with your friends to be together in the samme room.";
    private const string HOW_TO_JOIN_PUBLIC_GROUP = "Click on the <b>Leave</b> button below to leave your private room.<br>Then you will join the public room.";
    private const string SETTINGS_GROUPID = "SPACES_NAVIGATION_GROUPID";
    private const string BUTTON_GENERATE_ID = "Generate a private room ID";
    private const string BUTTON_JOIN_PRIVATE_GROUP = "Join this private meeting room";
    private const string BUTTON_LEAVE_PRIVATE_GROUP = "Leave this private room";

    [SerializeField] private string groupId = "";

    bool hasJoinedPrivateGroup = false;

    bool HasGroupId => groupId != null && groupId != "";
    bool IsValidPrivateGroupId => SpaceRoom.IsValidPrivateGroupId(groupId);
    private void Awake()
    {
        if (!touchableTMPInputField)
            touchableTMPInputField = GetComponentInChildren<TouchableTMPInputField>();

        if (touchableTMPInputField)
        {
            touchableTMPInputField.onFocusChange.AddListener(OnFocusChange);
            touchableTMPInputField.onTextChange.AddListener(OnTextChange);
        }
        if (!button)
            Debug.LogError("button has not been set !");
        else
            buttonTMP = button.GetComponentInChildren<TextMeshProUGUI>();

        button.onClick.AddListener(OnClick);

        if (!spaceRoom) {
            var managers = Managers.FindInstance();
            spaceRoom = managers.GetComponentInChildren<SpaceRoom>();
        }
        if (!spaceRoom)
        {
            spaceRoom = FindObjectOfType<SpaceRoom>();
        }
        if (!spaceRoom)
            Debug.LogError("Space has not been set !");
     }

    private void OnEnable()
    {
        groupId = PlayerPrefs.GetString(SETTINGS_GROUPID);

        if (groupId != null)
            touchableTMPInputField.Text = groupId;

        CheckWhichTextToDisplay();
        UpdateButtonConfig();
    }

    #region TouchableTMPInputField callbacks
    void OnFocusChange()
    {
        UpdateButtonConfig();
    }

    void OnTextChange()
    {
        groupId = touchableTMPInputField.Text;
        UpdateButtonConfig();
    }

    #endregion
    private void CheckWhichTextToDisplay()
    {
        if (!HasGroupId)
        {
            groupExplanationTMP.text = HOW_TO_JOIN_PRIVATE_GROUP;
            currentGroupStatusTMP.text = "PUBLIC";
            hasJoinedPrivateGroup = false;
        }
        else
        {
            groupExplanationTMP.text = HOW_TO_JOIN_PUBLIC_GROUP;
            currentGroupStatusTMP.text = "PRIVATE";
            hasJoinedPrivateGroup = true;
            touchableTMPInputField.inputfield.interactable = false;
        }
    }

    void OnClick()
    {
        if (hasJoinedPrivateGroup)
        {
            QuitPrivateGroup();
        }
        else
        {
            if (!IsValidPrivateGroupId)
            {
                GeneratePrivateGroupID();
            }
            else
            {
                JoinPrivateGroup();
            }
        }
    }

    private void UpdateButtonConfig()
    {
        if (hasJoinedPrivateGroup)
        {
            buttonTMP.text = BUTTON_LEAVE_PRIVATE_GROUP;
        }
        else
        {
            if (!IsValidPrivateGroupId)
            {
                buttonTMP.text = BUTTON_GENERATE_ID;
            }
            else
            {
                buttonTMP.text = BUTTON_JOIN_PRIVATE_GROUP;
            }
        }
    }

    private void GeneratePrivateGroupID()
    {
        int randomNumber = new System.Random().Next(1000, 9999);
        touchableTMPInputField.Text = randomNumber.ToString();
    }

    public void JoinPrivateGroup()
    {
        Debug.Log($"Group ID change to :{groupId}!");
        spaceRoom.ChangeGroupId(groupId);
    }
    public void QuitPrivateGroup()
    {
        groupId = "";
        Debug.Log($"Group ID is reset!");
        spaceRoom.ChangeGroupId("");
    }
}
