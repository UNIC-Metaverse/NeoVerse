using Fusion.Addons.ExtendedRigSelectionAddon;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Desktop;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * 
 * MenuManager provides the functions to open/close the main menu and to quit the application.
 * It sets the keyboard binding key
 * 
 **/

namespace Fusion.Samples.IndustriesComponents
{
    public class MenuManager : ApplicationLifeCycleManager
    {
        public GameObject mainMenu;

        [SerializeField] private IRigSelection rigSelection;
        [SerializeField] private List<GameObject> UIItemsforDesktopRigOnly = new List<GameObject>();

        public bool IsMainMenuOpened => mainMenu.activeInHierarchy;
        public InputActionProperty menuAction;

        bool menuDisabled;
        bool uiItemsVisible = false;

        private void Awake()
        {
            if (rigSelection == null) rigSelection = FindObjectOfType<ExtendedRigSelection>(true);
            if (rigSelection == null) rigSelection = FindObjectOfType<RigSelection>(true);

            rigSelection.OnSelectRig.AddListener(OnSelectRig);
            if (rigSelection.IsRigSelected) OnSelectRig();
        }

        private void Start()
        {

            mainMenu.SetActive(false);
            if (menuAction.reference == null && menuAction.action.bindings.Count == 0)
            {
                menuAction.action.AddBinding("<Keyboard>/escape");
            }
            menuAction.action.Enable();
        }

        public void DisableMenu()
        {
            menuAction.action.Disable();
            menuDisabled = true;
            if (uiItemsVisible) ChangeUIItemsVisibility(false);
            if (IsMainMenuOpened) CloseMainMenu();
        }
        public void EnableMenu()
        {
            menuAction.action.Enable();
            menuDisabled = false;
            if (uiItemsVisible) ChangeUIItemsVisibility(true);
        }

        bool wasDisabledThisFrame = false;
        private void Update()
        {
            if (!menuDisabled && menuAction.action.WasPerformedThisFrame() && wasDisabledThisFrame == false)
            {
                if (IsMainMenuOpened)
                    CloseMainMenu();
                else
                    OpenMainMenu();
            }
            wasDisabledThisFrame = menuDisabled;
        }

        public void OpenMainMenu()
        {
            mainMenu.SetActive(true);
        }

        public void CloseMainMenu()
        {
            mainMenu.SetActive(false);
        }

        public void QuitApplication()
        {
            Debug.Log("User Exit Application with Main Menu");
            Application.Quit();
        }



        private void OnSelectRig()
        {
            if (rigSelection.IsVRRigSelected)
            {
                ChangeUIItemsVisibility(false);
                uiItemsVisible = false;
            }
            else
            {
                ChangeUIItemsVisibility(true);
                uiItemsVisible = true;
            }
        }

        private void ChangeUIItemsVisibility(bool shouldBeVisible)
        {
            foreach (GameObject item in UIItemsforDesktopRigOnly)
            {
                item.SetActive(shouldBeVisible);
            }
        }

        public override void ChangeMenuAuthorization(bool authorized)
        {
            if (authorized)
            {
                EnableMenu();
            }
            else
            {
                DisableMenu();
            }
        }
    }
}

