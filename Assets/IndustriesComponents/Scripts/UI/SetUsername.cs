using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion.XR.Shared.Rig;
using UnityEngine.Events;
using System;
using Fusion.Samples.IndustriesComponents;
using Fusion;
using Fusion.Addons.Avatar;
using Fusion.Addons.VirtualKeyboard.Touch;

/***
 * 
 * SetUsername loads the username from the user's preferences and displays it on the avatar model.
 * Thanks to the listeners on the touchableTMPInputField, the username is updated when the buffer changes and saved in the UserInfo.
 * 
 ***/
public class SetUsername : MonoBehaviour
{
    public TouchableTMPInputField touchableTMPInputField;
    private string username;
    [SerializeField] private TextMeshProUGUI placeHolder;

    private const string PLACE_HOLDER_VR_KEYBOARD_CLOSE = "Touch here to<br>enter a username";
    private const string PLACE_HOLDER_VR_KEYBOARD_OPEN = "Use the keyboard<br>to enter a username";
    private const string PLACE_HOLDER_DESKTOP = "Click here to <br>enter a username";

    public bool updateUserInfo = false;

    [Header("Automatically set")]
    [SerializeField] private Managers managers;
    [SerializeField] private NetworkRunner runner;
    [SerializeField] private RigInfo rigInfo;
    [SerializeField] private UserInfo userInfo;

    bool IsInVr
    {
        get
        {
            if (rigInfo == null) rigInfo = RigInfo.FindRigInfo(allowSceneSearch: true);
            if (rigInfo == null)
                Debug.LogError("RigInfo not found");
            return rigInfo.localHardwareRigKind == RigInfo.RigKind.VR;
        }
    }

    private void Awake()
    {
        if (!placeHolder)
            placeHolder = GetComponentInChildren<TextMeshProUGUI>();

        if (!touchableTMPInputField)
            touchableTMPInputField = GetComponentInChildren<TouchableTMPInputField>();

        if (touchableTMPInputField)
        {
            touchableTMPInputField.keyboardValidationMode = TouchableTMPInputField.KeyboardInputValidationMode.RemoveNewLines;
        }
        else
        {
            Debug.LogError("Missing input field");
        }
    }


    private void OnEnable()
    {
        username = PlayerPrefs.GetString(UserInfo.SETTINGS_USERNAME);
        if (username != null)
            touchableTMPInputField.Text = username;

        touchableTMPInputField.onFocusChange.AddListener(OnFocusChange);
        touchableTMPInputField.onTextChange.AddListener(OnTextChange);
    }

    private void OnFocusChange()
    {
        UpdatePlaceHolderText();
    }

    private void OnTextChange()
    {
        UpdatePlaceHolderText();
        SaveUsername();
        if (updateUserInfo)
            UpdateUserInfo();
    }


    private void UpdateUserInfo()
    {
        if (userInfo == null)
            SetUserInfo();

        if (userInfo != null)
            userInfo.UserName = PlayerPrefs.GetString(UserInfo.SETTINGS_USERNAME);
    }

    void SetUserInfo()
    {
        managers = Managers.FindInstance();
        if (managers == null)
            Debug.LogError("Managers not found !");

        runner = managers.runner;
        if (runner == null)
            Debug.LogError("Runner not found !");
        else
            rigInfo = RigInfo.FindRigInfo(runner);

        if (rigInfo == null)
            Debug.LogError("RigInfo not found !");

        if (rigInfo.localNetworkedRig == null)
            Debug.LogError("localNetworkedRig not set !");
        else
            userInfo = rigInfo.localNetworkedRig.GetComponent<UserInfo>();
        if (userInfo == null)
            Debug.LogError("UserInfo not found !");
    }

    void UpdatePlaceHolderText()
    {
        if (touchableTMPInputField.Text != "")
        {
          placeHolder.text = "";
        }
        else
        {
            if (touchableTMPInputField.HasFocus)
            {
                if (IsInVr)
                {
                    placeHolder.text = PLACE_HOLDER_VR_KEYBOARD_OPEN;
                }
                else
                {
                    placeHolder.text = "";
                }
            }
            else
            {
                if (IsInVr)
                {
                    placeHolder.text = PLACE_HOLDER_VR_KEYBOARD_CLOSE;
                }
                else
                {
                    placeHolder.text = PLACE_HOLDER_DESKTOP;
                }
            }
        }
    }

    public void SaveUsername()
    {
        Debug.Log($"SaveUsername {touchableTMPInputField.Text}");
        PlayerPrefs.SetString(UserInfo.SETTINGS_USERNAME, touchableTMPInputField.Text);
        PlayerPrefs.Save();
    }
}
