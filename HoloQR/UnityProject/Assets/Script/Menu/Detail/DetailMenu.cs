using System;
using System.Collections;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Receivers;
using UnityEngine;

namespace Script.Menu.Listing
{
    public abstract class DetailMenu : InteractionReceiver
    {
        /// <summary>
        /// Where the instantiated buttons will be placed
        /// </summary>
        [SerializeField] protected Transform buttonParent;

        [SerializeField] protected int maxCharsPerLineSmall = 35;
        [SerializeField] protected int maxCharsPerLineLarge = 75;

        [SerializeField] protected TextMesh titleText;

        public GameObject textPrefab;
        protected DetailMenuResult result;

        protected MenuState state = MenuState.Uninitialized;

        public MenuState State
        {
            get { return state; }
            set { state = value; }
        }

        /// <summary>
        /// Called after user has clicked a button and the menu has finished closing
        /// </summary>
        public Action<DetailMenuResult> OnClosed;

        /// <summary>
        /// Can be used to monitor result instead of events
        /// </summary>
        public DetailMenuResult Result
        {
            get { return result; }
        }

        protected void Launch(DetailMenuResult newResult)
        {
            if (State != MenuState.Uninitialized)
            {
                return;
            }

            result = newResult;
            StartCoroutine(RunListingMenuOverTime());
        }

        /// <summary>
        /// Opens menu, waits for input, then closes
        /// </summary>
        /// <returns></returns>
        protected IEnumerator RunListingMenuOverTime()
        {
            // Create our buttons and set up our message
            GenerateButtons(result);
            SetContent(result.ButtonContent[result.Buttons[0]], result.ButtonNames[result.Buttons[0]]);
            FinalizeLayout(result);

            // Open menu
            State = MenuState.Opening;
            yield return StartCoroutine(OpenDetailMenu());
            State = MenuState.WaitingForInput;
            // Wait for input
            while (State == MenuState.WaitingForInput)
            {
                UpdateDetailMenu();
                yield return null;
            }

            // Close menu
            State = MenuState.Closing;
            yield return StartCoroutine(CloseDetailMenu());
            State = MenuState.Closed;
            // Callback
            if (OnClosed != null)
            {
                OnClosed(result);
            }

            // Wait a moment to give scripts a chance to respond
            yield return null;
            // Destroy ourselves
            GameObject.Destroy(gameObject);
            yield break;
        }

        /// <summary>
        /// Opens the menu - state must be set to WaitingForInput afterwards
        /// Overridden in inherited class.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator OpenDetailMenu()
        {
            yield break;
        }

        /// <summary>
        /// Closes the menu - state must be set to Closed afterwards
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator CloseDetailMenu()
        {
            yield break;
        }

        /// <summary>
        /// Perform any updates (animation, tagalong, etc) here
        /// This will be called every frame while waiting for input
        /// </summary>
        protected virtual void UpdateDetailMenu()
        {
            return;
        }

        /// <summary>
        /// Generates buttons - Must parent them under buttonParent!
        /// </summary>
        /// <param name="listingMenuResult"></param>
        protected abstract void GenerateButtons(DetailMenuResult listingMenuResult);
        /// <summary>
        /// Set the title and message using the result
        /// Eg using TextMesh components 
        /// </summary>
        protected abstract void SetContent(MessageBoardContent content, String titleString);
        /// <summary>
        /// Lays out the buttons on the menu
        /// Eg using an ObjectCollection
        /// </summary>
        /// <param name="listingMenuResult"></param>
        protected abstract void FinalizeLayout(DetailMenuResult listingMenuResult);

        /// <summary>
        /// Instantiates a menu and passes it a result
        /// </summary>
        /// <param name="listingMenuPrefab"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static DetailMenu Open(GameObject detailMenuPrefab, DetailMenuResult result)
        {
            GameObject detailMenuGO = GameObject.Instantiate(detailMenuPrefab) as GameObject;
            DetailMenu DMenu = detailMenuGO.GetComponent<DetailMenu>();
            var solverHandler = DMenu.GetComponent<SolverHandler>();
            solverHandler.AdditionalOffset = new Vector3(0,-0.05f,0);
            DMenu.Launch(result);
            return DMenu;
        }
    }
}