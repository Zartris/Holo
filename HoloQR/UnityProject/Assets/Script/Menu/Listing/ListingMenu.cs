using System;
using System.Collections;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Receivers;
using UnityEngine;

namespace Script.Menu.Listing
{
    public abstract class ListingMenu : InteractionReceiver
    {
        /// <summary>
        /// Where the instantiated buttons will be placed
        /// </summary>
        [SerializeField] protected Transform buttonParent;

        protected ListingMenuResult result;

        protected MenuState state = MenuState.Uninitialized;

        public MenuState State
        {
            get { return state; }
            set { state = value; }
        }

        /// <summary>
        /// Called after user has clicked a button and the menu has finished closing
        /// </summary>
        public Action<ListingMenuResult> OnClosed;

        /// <summary>
        /// Can be used to monitor result instead of events
        /// </summary>
        public ListingMenuResult Result
        {
            get { return result; }
        }

        protected void Launch(ListingMenuResult newResult)
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
            FinalizeLayout(result);

            // Open menu
            State = MenuState.Opening;
            yield return StartCoroutine(OpenListingMenu());
            State = MenuState.WaitingForInput;
            // Wait for input
            while (State == MenuState.WaitingForInput)
            {
                UpdateListingMenu();
                yield return null;
            }

            // Close menu
            State = MenuState.Closing;
            yield return StartCoroutine(CloseListingMenu());
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
        protected virtual IEnumerator OpenListingMenu()
        {
            yield break;
        }

        /// <summary>
        /// Closes the menu - state must be set to Closed afterwards
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator CloseListingMenu()
        {
            yield break;
        }

        /// <summary>
        /// Perform any updates (animation, tagalong, etc) here
        /// This will be called every frame while waiting for input
        /// </summary>
        protected virtual void UpdateListingMenu()
        {
            return;
        }

        /// <summary>
        /// Generates buttons - Must parent them under buttonParent!
        /// </summary>
        /// <param name="listingMenuResult"></param>
        protected abstract void GenerateButtons(ListingMenuResult listingMenuResult);

        /// <summary>
        /// Lays out the buttons on the menu
        /// Eg using an ObjectCollection
        /// </summary>
        /// <param name="listingMenuResult"></param>
        protected abstract void FinalizeLayout(ListingMenuResult listingMenuResult);

        /// <summary>
        /// Instantiates a menu and passes it a result
        /// </summary>
        /// <param name="listingMenuPrefab"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static ListingMenu Open(GameObject listingMenuPrefab, ListingMenuResult result)
        {
            GameObject listingMenuGO = GameObject.Instantiate(listingMenuPrefab) as GameObject;
            ListingMenu LMenu = listingMenuGO.GetComponent<ListingMenu>();
            var solverHandler = LMenu.GetComponent<SolverHandler>();
            solverHandler.AdditionalOffset = new Vector3(0,-0.05f,0);
            LMenu.Launch(result);
            return LMenu;
        }
    }
}