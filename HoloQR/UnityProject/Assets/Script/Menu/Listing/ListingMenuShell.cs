// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Buttons;
using HoloToolkit.Unity.Collections;
using HoloToolkit.UX.Buttons;
using Script.Menu;
using Script.Menu.Listing;
using UnityEngine;
#if UNITY_WSA && UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;

#endif

namespace HoloToolkit.UX.Dialog
{
    /// <summary>
    /// Dialog that approximates the look of a HoloLens shell dialog
    /// </summary>
    public class ListingMenuShell : ListingMenu
    {
        [SerializeField] private GameObject[] twoButtonSet;

        private DialogButton buttonPressed;

        /// <summary>
        /// Creates the buttons that are displayed on the dialog.
        /// </summary>
        protected override void GenerateButtons(ListingMenuResult listingMenuResult)
        {
            AddButtonsToMenu(listingMenuResult.Buttons);

            /**
            //Find all buttons on dialog...
            List<DialogButton> buttonsOnDialog = GetAllDialogButtons();

            //set desired buttons active and the rest inactive
            SetButtonsActiveStates(buttonsOnDialog, buttonTypes.Count);

            //set titles and types
            if (buttonTypes.Count > 0)
            {
                // If we have two buttons then do step 1, else 0
                int step = buttonTypes.Count == 2 ? 1 : 0;
                for (int i = 0; i < buttonTypes.Count; ++i)
                {
                    twoButtonSet[i] = buttonsOnDialog[i + step].gameObject;
                    buttonsOnDialog[i + step].SetTitle(buttonTypes[i].ToString());
                    buttonsOnDialog[i + step].ButtonTypeEnum = buttonTypes[i];
                }
            }
            **/
        }

        private void SetButtonsActiveStates(List<DialogButton> buttons, int count)
        {
            for (int i = 0; i < buttons.Count; ++i)
            {
                var flag1 = (count == 1) && (i == 0);
                var flag2 = (count == 2) && (i > 0);
                //buttons[i].ParentDialog = this;
                buttons[i].gameObject.SetActive(flag1 || flag2);
            }
        }

        private List<DialogButton> GetAllDialogButtons()
        {
            List<DialogButton> buttonsOnDialog = new List<DialogButton>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "ButtonParent")
                {
                    for (int childIndex = 0; childIndex < child.transform.childCount; ++childIndex)
                    {
                        Transform t = child.transform.GetChild(childIndex);
                        if (t != null)
                        {
                            DialogButton button = t.GetComponent<DialogButton>();
                            if (button != null)
                            {
                                buttonsOnDialog.Add(button);
                            }
                        }
                    }
                }
            }

            return buttonsOnDialog;
        }

        private void AddButtonsToMenu(List<GameObject> buttons)
        {
            List<DialogButton> buttonsOnDialog = new List<DialogButton>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == "ButtonParent")
                {
                    var objectCollection = child.GetComponent<ObjectCollection>();

                    objectCollection.Rows = buttons.Count;
                    var collectionNodes = new List<CollectionNode>();
                    for (int j = 0; j < buttons.Count; j++)
                    {
                        GameObject buttonGO = buttons[j];
                        var component = buttonGO.GetComponent<Button>();
                        
                        setPositionAndScale(child, component.gameObject, j);
                        var collectionNode = new CollectionNode();
                        collectionNode.transform = buttonGO.transform;
                        collectionNode.Name = buttonGO.name;
                        collectionNodes.Add(collectionNode);
                    }

                    objectCollection.NodeList = collectionNodes;

                    /**
                    for (int childIndex = 0; childIndex < child.transform.childCount; ++childIndex)
                    {
                        Transform t = child.transform.GetChild(childIndex);
                        if (t != null)
                        {
                            DialogButton button = t.GetComponent<DialogButton>();
                            if (button != null)
                            {
                                buttonsOnDialog.Add(button);
                            }
                        }
                    }
                    **/
                }
            }
        }

        /// <summary>
        /// Function to destroy the Dialog.
        /// </summary>
        public void DismissMenu()
        {
            state = MenuState.InputReceived;
        }

        protected override void FinalizeLayout(ListingMenuResult listingMenuResult)
        {
            SolverConstantViewSize solver = GetComponent<SolverConstantViewSize>();

#if UNITY_WSA && UNITY_2017_2_OR_NEWER
            // Optimize the content for immersive headset
            if (HolographicSettings.IsDisplayOpaque)
            {
                solver.TargetViewPercentV = 0.35f;
            }
#else
            solver.TargetViewPercentV = 0.35f;
            #endif
        }

        private void setPositionAndScale(Transform parent, GameObject button, int number)
        {
            button.transform.parent = parent;
            button.transform.localPosition = new Vector3(0, 0.04f + (number * 0.071f), -0.01f);
            button.transform.localScale = new Vector3(1.25f, 0.6f, 0.5f);
        }
    }
}