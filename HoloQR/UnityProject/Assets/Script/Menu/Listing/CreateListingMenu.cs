using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HoloToolkit.Unity.Buttons;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.UX.Dialog;
using HoloToolkit.UX.Progress;
using Script.Loading;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Script.Menu.Listing
{
    public class CreateListingMenu : MonoBehaviour, IInputClickHandler
    {
        public ListingMenu ListingMenuPrefab;
        public GameObject buttonPrefab;
        private ListingMenu menu;
        private ListingMenuResult _result;

        protected bool launchedMenu = false;


        #region IInputClickHandler

        public void OnInputClicked(InputClickedEventData eventData)
        {
            CreateListingElement();
        }

        #endregion IInputClickHandler

        public void CreateListingElement(ListingMenuResult result)
        {
            // TEST create_dialog:
            if (launchedMenu)
                return;
            launchedMenu = true;

            StartCoroutine(LaunchListingMenuOverTime(result));
        }

        public void CreateListingElement()
        {
            if (launchedMenu)
                return;
            launchedMenu = true;
            var result = new ListingMenuResult();
            List<GameObject> buttons = new List<GameObject>();
            // MOG TIME, this should be given from homepage. 
            List<Uri> links = new List<Uri>();
            List<string> names = new List<string>();
            links.Add(new Uri("http://www.google.com"));
            links.Add(new Uri("http://www.Bing.com"));
            links.Add(new Uri("http://www.duckduckgo.com"));
            links.Add(new Uri("https://www.wolframalpha.com/"));

            names.Add("Edoc - element type");
            names.Add("Edoc - element location");
            names.Add("Edoc - element parts");
            names.Add("Edoc - element instructions");
            // end of mog code::

            for (int i = 0; i < 4; i++)
            {
                // Create object
                var buttonGO = GameObject.Instantiate(buttonPrefab) as GameObject;
                // set click method
                var button = buttonGO.GetComponent<Button>();
                button.OnButtonClicked += OnButtonClicked;
                

                // set text:
                CompoundButtonText compoundButtonText = buttonGO.GetComponent<CompoundButtonText>();
                if (compoundButtonText)
                {
                    // position of text:
                    compoundButtonText.OverrideAnchor = true;
                    compoundButtonText.OverrideOffset = false;
                    compoundButtonText.Anchor = TextAnchor.MiddleCenter; 

                    // text
                    compoundButtonText.Text = names[i];
                    // compoundButtonText.OverrideSize = true;
                    // compoundButtonText.Size = 10;
                }

                var TextElement = buttonGO.transform.GetChild(2);
                TextElement.localScale = new Vector3(0.002180442f, 0.01f,0.002180442f);

                // set icon:
                CompoundButtonIcon compoundButtonIcon = buttonGO.GetComponent<CompoundButtonIcon>();
                if (compoundButtonIcon)
                {
                    compoundButtonIcon.DisableIcon = true;
                }

                // add to list:
                buttons.Add(buttonGO);
            }

            result.createListingMenuResult(buttons,links, names);
            // made it local to use it on OnClick method.
            _result = result;
            StartCoroutine(LaunchListingMenuOverTime(_result));
        }

        /// <summary>
        /// event handler that runs when button is clicked.
        /// Dismisses the parent dialog.
        /// </summary>
        /// <param name="obj"></param>
        public void OnButtonClicked(GameObject obj)
        {
            if (launchedMenu)
            {
                // todo: use link!
                var uri = _result.ButtonLink[obj];
                LoadUri(uri);
                menu.State = MenuState.Closed;
            }
        }

        /// <summary>
        /// Handles the loading of the Uri
        /// </summary>
        /// <param name="uri"></param>
        private async void LoadUri(Uri uri)
        {
            // The URI to launch
            if(uri == null)
            {
                uri = new Uri(@"http://www.bing.com");
            }

#if ENABLE_WINMD_SUPPORT
        // Launch the URI
        var success = await Windows.System.Launcher.LaunchUriAsync(uri);
#endif
        }
        /// <summary>
        /// Setter Method to set the Text at the top of the Dialog.
        /// </summary>
        /// <param name="title"></param>
        public void SetTitle(string title)
        {
            CompoundButtonText compoundButtonText = GetComponent<CompoundButtonText>();
            if (compoundButtonText)
            {
                compoundButtonText.Text = title;
            }
        }

        protected IEnumerator LaunchListingMenuOverTime(ListingMenuResult result)
        {
            menu = ListingMenu.Open(ListingMenuPrefab.gameObject, result);
            menu.OnClosed += OnClosed;

            // Wait for dialog to close

            while (menu.State != MenuState.Closed)
            {
                yield return null;
            }

            launchedMenu = false;
            yield break;
        }

        protected void OnClosed(ListingMenuResult result)
        {
        }
    }
}