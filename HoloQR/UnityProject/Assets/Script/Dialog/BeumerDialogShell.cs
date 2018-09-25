// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.UX.Buttons;
using UnityEngine;
#if UNITY_WSA && UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;

#endif

namespace HoloToolkit.UX.Dialog
{
    /// <summary>
    /// Dialog that approximates the look of a HoloLens shell dialog
    /// </summary>
    public class BeumerDialogShell : DialogShell
    {
        [SerializeField] private GameObject[] ButtonSet;

        /// <summary>
        /// Runs solver after Dialog is made to center it in view.
        /// </summary>
        /// TODO: Make it appear in middle.
        protected override void FinalizeLayout()
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

        /// <summary>
        /// Creates the buttons that are displayed on the dialog.
        /// </summary>
        protected override void GenerateButtons()
        {

            //Get List of ButtonTypes that should be created on Dialog
            List<DialogButtonType> buttonTypes = new List<DialogButtonType>();
            foreach (DialogButtonType buttonType in Enum.GetValues(typeof(DialogButtonType)))
            {
                if (buttonType == DialogButtonType.None)
                {
                    continue;
                }

                // If this button type flag is set
                if ((buttonType & result.Buttons) == buttonType)
                {
                    buttonTypes.Add(buttonType);
                }
            }
            Debug.Log(buttonTypes);

            ButtonSet = new GameObject[4];

            // TODO: Create new type of buttons that doesn't close the dialog but only change it.
            //Find all buttons on dialog bar one...
            List<DialogButton> buttonsOnDialogBar1 = GetAllDialogButtons("ButtonParent");
            //Find all buttons on dialog bar two...
            List<DialogButton> buttonsOnDialogBar2 = GetAllDialogButtons("ButtonParent2");


            //set desired buttons active and the rest inactive
            SetButtonsActiveStates(buttonsOnDialogBar1, buttonTypes.Count > 2 ? 2 : buttonTypes.Count);
            SetButtonsActiveStates(buttonsOnDialogBar2, buttonTypes.Count <= 2 ? 0 : buttonTypes.Count - 2);
            SetBGActiveStates(buttonTypes.Count);
            //set titles and types
            if (buttonTypes.Count > 0)
            {
                // If we have two buttons then do step 1, else 0
                int step = buttonTypes.Count >= 2 ? 1 : 0;
                int step2 = buttonTypes.Count == 4 ? 1 : 0;
                for (int i = 0; i < buttonTypes.Count; ++i)
                {
                    if (i < 2)
                    {
                        ButtonSet[i] = buttonsOnDialogBar1[i + step].gameObject;
                        buttonsOnDialogBar1[i + step].SetTitle(buttonTypes[i].ToString());
                        buttonsOnDialogBar1[i + step].ButtonTypeEnum = buttonTypes[i];
                    }
                    else
                    {
                        ButtonSet[i] = buttonsOnDialogBar2[i - 2 + step2].gameObject;
                        Debug.Log(buttonTypes[i].ToString());
                        buttonsOnDialogBar2[(i - 2) + step].SetTitle(buttonTypes[i].ToString());
                        buttonsOnDialogBar2[(i - 2) + step].ButtonTypeEnum = buttonTypes[i];
                    }
                }
            }
        }

        private void SetButtonsActiveStates(List<DialogButton> buttons, int count)
        {
            for (int i = 0; i < buttons.Count; ++i)
            {
                var flag1 = (count == 1) && (i == 0);
                var flag2 = (count == 2) && (i > 0);
                buttons[i].ParentDialog = this;
                buttons[i].gameObject.SetActive(flag1 || flag2);
            }
        }

        private void SetBGActiveStates(int count)
        {
            bool findBg1 = count == 0;
            bool findBg2 = count < 3;

            if (findBg1 || findBg2)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name == "BackPlateButton" && findBg1)
                    {
                        child.gameObject.SetActive(false);
                    }
                    else if (child.name == "BackPlateButton2" && findBg2)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }


        private List<DialogButton> GetAllDialogButtons(String barName)
        {
            List<DialogButton> buttonsOnDialog = new List<DialogButton>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == barName)
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

        /// <summary>
        /// Function to destroy the Dialog.
        /// </summary>
        public void DismissDialog()
        {
            state = DialogState.InputReceived;
        }
    }
}