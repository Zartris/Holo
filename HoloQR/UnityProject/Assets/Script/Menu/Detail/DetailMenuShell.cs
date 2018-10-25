// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using HoloLensWithOpenCVForUnityExample;
using HoloToolkit.Unity;
using HoloToolkit.Unity.Buttons;
using HoloToolkit.Unity.Collections;
using HoloToolkit.UX.Buttons;
using Script.Menu;
using Script.Menu.Listing;
using Script.Utils;
using UnityEngine;
#if UNITY_WSA && UNITY_2017_2_OR_NEWER
using UnityEngine.XR.WSA;

#endif

namespace Script.Menu.Listing
{
    /// <summary>
    /// Dialog that approximates the look of a HoloLens shell dialog
    /// </summary>
    public class DetailMenuShell : DetailMenu
    {
        private DialogButton buttonPressed;

        private DetailMenuResult _result;
        /// <summary>
        /// Creates the buttons that are displayed on the dialog.
        /// </summary>
        protected override void GenerateButtons(DetailMenuResult detailMenuResult)
        {
            _result = detailMenuResult;
            AddButtonsToMenu(detailMenuResult.Buttons);
        }

        private void AddButtonsToMenu(List<GameObject> buttons)
        {
            List<DialogButton> buttonsOnDialog = new List<DialogButton>();
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform c1 = transform.GetChild(i);
                if (c1.name == "Menu")
                {
                    var menu = c1.GetComponent<Transform>();
                    for (int j = 0; j < menu.childCount; j++)
                    {
                        Transform c2 = menu.GetChild(j);
                        if(c2.name =="ReturnField")
                        {
                            var returnField = c2.GetComponent<Transform>();
                            for (int k = 0; k < returnField.childCount; k++)
                            {
                                var c3 = returnField.GetChild(k);
                                if(c3.name == "ReturnButton")
                                {
                                    var returnButton = c3.gameObject.GetComponent<Button>();
                                    returnButton.OnButtonClicked += returnMethod;
                                }
                            }
                        } else if(c2.name =="MenuFan")
                        {
                            var menuFan = c2.GetComponent<Transform>();
                            for (int l = 0; l < menuFan.childCount; l++)
                            {
                                Transform c3 = menuFan.GetChild(l);
                                if (c3.name == "ButtonParent")
                                {

                                    // move current button (it is only for show)
                                    List<GameObject> buttonAnchors = new List<GameObject>();
                                    foreach (Transform child in c3.transform)
                                    {
                                        buttonAnchors.Add(child.gameObject);
                                    }

                                    var objectCollection = c3.GetComponent<ObjectCollection>();

                                    objectCollection.Rows = buttons.Count;
                                    var collectionNodes = new List<CollectionNode>();
                                    for (int k = 0; k < buttons.Count; k++)
                                    {
                                        GameObject buttonGO = buttons[k];
                                        var component = buttonGO.GetComponent<Button>();
                                        component.OnButtonClicked += OnButtonClicked;

                                        setPositionAndScale(c3, component.gameObject, k);
                                        var collectionNode = new CollectionNode();
                                        collectionNode.transform = buttonGO.transform;
                                        collectionNode.Name = buttonGO.name;
                                        collectionNodes.Add(collectionNode);
                                    }

                                    objectCollection.NodeList = collectionNodes;
                                    foreach (GameObject anchor in buttonAnchors)
                                    {
                                        Destroy(anchor);
                                    }
                                }
                                else if (c3.name == "BackPlateButton")
                                {
                                    c3.localScale = new Vector3((buttons.Count * 0.12f + (buttons.Count - 1) * 0.03f), c3.localScale.y, c3.localScale.z);
                                    c3.localPosition = new Vector3(c3.localPosition.x + (((buttons.Count - 1) * 0.12f + (buttons.Count - 1) * 0.03f) / 2), c3.localPosition.y, c3.localPosition.z);
                                }
                            }
                            
                        }

                        
                    }
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

        protected override void FinalizeLayout(DetailMenuResult listingMenuResult)
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
        /// event handler that runs when button is clicked.
        /// Dismisses the parent dialog.
        /// </summary>
        /// <param name="obj"></param>
        public void OnButtonClicked(GameObject obj)
        {
                var content = _result.ButtonContent[obj];
                SetContent(content, _result.ButtonNames[obj]);
                state = MenuState.WaitingForInput;
//                menu.State = MenuState.Closed;
        }

        public void returnMethod(GameObject obj)
        {
            state = MenuState.InputReceived;
            GameObject qrm = GameObject.Find("/Managers/QRManager");
            Scanner scanner = qrm.GetComponent<Scanner>();
            scanner.dialogClosed();
        }

        private void setPositionAndScale(Transform parent, GameObject button, int number)
        {
            button.transform.parent = parent;
            button.transform.localPosition = new Vector3(-0.0954f+(number* (0.12f+0.03f)), 0, 0);
            button.transform.localScale = new Vector3(0.12f, 0.08f, 1);

            foreach (Transform child in button.transform)
            {
                if (child.name == "UIButtonSquareIcon")
                {
                    child.localScale = new Vector3(12, 12, 1);
                }
            }
        }

        private void addTextToGo(GameObject go, String text, bool largeField)
        {
            // removing img since we cannot have img and text in one.
            RemoveMeshFilterAndMeshRenderer(go);
            var transform = go.GetComponent<Transform>();
            foreach (Transform child in transform)
            {
                if(child.name == "MenuText")
                {
                    Destroy(child.gameObject);
                }
            }

            // Create object
            var textGO = GameObject.Instantiate(textPrefab) as GameObject;
            textGO.name = "MenuText";
            textGO.transform.parent = transform;
            textGO.transform.localPosition = new Vector3(0,0,0);
            textGO.transform.localRotation = Quaternion.identity;
            textGO.transform.localScale = new Vector3(0.00475f, 0.00535f, 0);
            var MaxCharsPerLine = maxCharsPerLineSmall;
            if (largeField)
            {
                textGO.transform.localScale = new Vector3(0.00215f, 0.00525f, 0);
                MaxCharsPerLine = maxCharsPerLineLarge;
            }
            textGO.GetComponent<TextMesh>().text = TextUtils.WordWrap(text, MaxCharsPerLine); ;
        }

        private void RemoveTextFromGo(GameObject go)
        {
            var transform = go.GetComponent<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name == "MenuText")
                {
                    Destroy(child.gameObject);
                }
            }
        }

        protected override void SetContent(MessageBoardContent content, String titleString)
        {
            foreach (Transform child in transform)
            {
                if(child != null && child.name == "MessageBoard")
                {
                    for (int i = 0; i < child.childCount; i++)
                    {
                        Transform c1 = child.GetChild(i);
                        if (c1.name == "TitleText")
                        {
                            titleText = c1.GetComponent<TextMesh>();
                            titleText.text = titleString;
                        }
                        else if (c1.name == "WorkArea")
                        {
                            var go = c1.gameObject;
                            if(content.workAreaContent == MessageBoardContent.Content.NONE)
                            {
                                RemoveMeshFilterAndMeshRenderer(go);
                                RemoveTextFromGo(go);
                            }
                            else if(content.workAreaContent == MessageBoardContent.Content.IMG)
                            {
                                AddMeshFilterAndMeshRenderer(go,content, content.workAreaImageString);
                            } else if(content.workAreaContent == MessageBoardContent.Content.TEXT)
                            {
                                addTextToGo(go, content.workAreaText, true);
                            }

                            for (int j = 0; j < c1.childCount; j++)
                            {
                                var c2 = c1.GetChild(j);
                                if(c2.name == "LowerArea")
                                {
                                    go = c2.gameObject;
                                    if(content.lowerAreaContent == MessageBoardContent.Content.NONE)
                                    {
                                        RemoveMeshFilterAndMeshRenderer(go);
                                        RemoveTextFromGo(go);
                                    }
                                    else if (content.lowerAreaContent == MessageBoardContent.Content.IMG)
                                    {
                                        AddMeshFilterAndMeshRenderer(go, content, content.lowerAreaImageString);
                                    }
                                    else if (content.lowerAreaContent == MessageBoardContent.Content.TEXT)
                                    {
                                        addTextToGo(go, content.lowerAreaText, true);
                                    }

                                    for (int k = 0; k < c2.childCount; k++)
                                    {
                                        var c3 = c2.GetChild(k);
                                        if (c3.name == "LowerLeftArea")
                                        {
                                            go = c3.gameObject;
                                            if(content.lowerLeftAreaContent == MessageBoardContent.Content.NONE)
                                            {
                                                RemoveMeshFilterAndMeshRenderer(go);
                                                RemoveTextFromGo(go);
                                            }
                                            else if (content.lowerLeftAreaContent == MessageBoardContent.Content.IMG)
                                            {
                                                AddMeshFilterAndMeshRenderer(go, content, content.lowerLeftAreaImageString);
                                            }
                                            else if (content.lowerLeftAreaContent == MessageBoardContent.Content.TEXT)
                                            {
                                                addTextToGo(go, content.lowerLeftAreaText, false);
                                            }
                                        }
                                        else if (c3.name == "LowerRightArea")
                                        {
                                            go = c3.gameObject;
                                            if(content.lowerRightAreaContent == MessageBoardContent.Content.NONE)
                                            {
                                                RemoveMeshFilterAndMeshRenderer(go);
                                                RemoveTextFromGo(go);
                                            }
                                            else if (content.lowerRightAreaContent == MessageBoardContent.Content.IMG)
                                            {
                                                AddMeshFilterAndMeshRenderer(go, content, content.lowerRightAreaImageString);
                                            }
                                            else if (content.lowerRightAreaContent == MessageBoardContent.Content.TEXT)
                                            {
                                                addTextToGo(go, content.lowerRightAreaText, false);
                                            }
                                        }
                                    }
                                }
                                else if(c2.name =="TopArea")
                                {
                                    go = c2.gameObject;
                                    if(content.topAreaContent == MessageBoardContent.Content.NONE)
                                    {
                                        RemoveMeshFilterAndMeshRenderer(go);
                                        RemoveTextFromGo(go);
                                    }
                                    else if (content.topAreaContent == MessageBoardContent.Content.IMG)
                                    {
                                        AddMeshFilterAndMeshRenderer(go, content, content.topAreaImageString);
                                    }
                                    else if (content.topAreaContent == MessageBoardContent.Content.TEXT)
                                    {
                                        addTextToGo(go, content.topAreaText, true);
                                    }

                                    for (int k = 0; k < c2.childCount; k++)
                                    {
                                        var c3 = c2.GetChild(k);
                                        if(c3.name =="TopLeftArea")
                                        {
                                            go = c3.gameObject;
                                            if(content.topLeftAreaContent == MessageBoardContent.Content.NONE)
                                            {
                                                RemoveMeshFilterAndMeshRenderer(go);
                                                RemoveTextFromGo(go);
                                            }
                                            else if (content.topLeftAreaContent == MessageBoardContent.Content.IMG)
                                            {
                                                AddMeshFilterAndMeshRenderer(go, content, content.topLeftAreaImageString);
                                            }
                                            else if (content.topLeftAreaContent == MessageBoardContent.Content.TEXT)
                                            {
                                                addTextToGo(go, content.topLeftAreaText, false);
                                            }
                                        } else if(c3.name =="TopRightArea")
                                        {
                                            go = c3.gameObject;
                                            if(content.topRightAreaContent == MessageBoardContent.Content.NONE)
                                            {
                                                RemoveMeshFilterAndMeshRenderer(go);
                                                RemoveTextFromGo(go);
                                            }
                                            else if (content.topRightAreaContent == MessageBoardContent.Content.IMG)
                                            {
                                                AddMeshFilterAndMeshRenderer(go, content, content.topRightAreaImageString);
                                            }
                                            else if (content.topRightAreaContent == MessageBoardContent.Content.TEXT)
                                            {
                                                addTextToGo(go, content.topRightAreaText, false);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddMeshFilterAndMeshRenderer(GameObject go, MessageBoardContent content, string imgString)
        {
            // Remove text since we are adding img.
            RemoveTextFromGo(go);
            // We add a quad Mesh;
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var mf = quad.GetComponent<MeshFilter>();
            Mesh mesh = (Mesh) Instantiate(mf.sharedMesh);
            Destroy(quad);
            //Instantiate a new game object, and add mesh components so it's visible
            //We set the sharedMesh to the mesh we extracted from the prefab and the material 
            //of the MeshRenderer component
            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = go.AddComponent<MeshFilter>();
            }
            
            meshFilter.sharedMesh = mesh;
            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
            }
            var mat = new Material(Shader.Find("Sprites/Default"));
            meshRenderer.material = mat;
            meshRenderer.material.mainTexture = content.LoadImageStringToTexture2D(imgString);
        }

        public void RemoveMeshFilterAndMeshRenderer(GameObject go)
        {
            if (go.GetComponent<MeshFilter>() != null)
            {
                Destroy(go.GetComponent<MeshFilter>());
            }
            if (go.GetComponent<MeshRenderer>() != null)
            {
                Destroy(go.GetComponent<MeshRenderer>());
            }
        }
    }
}