using System;
using System.Collections;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.UX.Dialog;
using HoloToolkit.UX.Progress;
using Script.Loading;
using Script.Menu.Listing;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

public class CreateDialog : MonoBehaviour, IInputClickHandler
{
    [FormerlySerializedAs("_dialogPrefab")]
    public Dialog DialogPrefab;

    [Header("Dialog options")] public string Dialog2Title = "Two button dialog";

    [TextArea] public string Dialog2Message =
        "This is a message for dialog 2. Longer messages will be wrapped automatically. However you still need to be aware of overflow.";

    protected bool launchedDialog = false;


    #region IInputClickHandler

    public void OnInputClicked(InputClickedEventData eventData)
    {
        CreateLoadingElement("barcode in example.");

        // Disable everything else
    }

    #endregion IInputClickHandler

    public void CreateLoadingElement(String result)
    {
        if (ProgressIndicator.Instance.IsLoading)
        {
            return;
        }

        IndicatorStyleEnum indicatorStyle = IndicatorStyleEnum.AnimatedOrbs;
        ProgressStyleEnum progressStyle = ProgressStyleEnum.None;
        Loader loader = Object.FindObjectOfType<Loader>();
        loader.LeadInMessage = "loading from: " + result;
        loader.LaunchProgress(indicatorStyle, progressStyle);
        // Can create a fall back, so after 30 sec we launch a fail dialog.
        StartCoroutine(waitAndCreate(result));
    }

    private IEnumerator waitAndCreate(String result)
    {
        while (ProgressIndicator.Instance.IsLoading)
        {
            yield return null;
        }

        // MOG it
        {
            if ("Hello :)".Equals(result))
            {
                var m =
                    "Here is the element: 1SO63.DIC156, from here you can see the latest alarms occured, the Edoc instructions for the element or a 3d view of the element.";
                CreateDialogElement("Dynamic Discharge Element", m);
            }
            else if ("http://osxdaily.com".Equals(result))
            {
                var m =
                    "Here is the element: NKL62.LNA132, from here you can see the latest alarms occured, the Edoc instructions for the element or a 3d view of the element.";
                CreateDialogElement("Reciprocating Lift", m);
            }
            else
            {
                CreateDialogElement("Did not find one of the two test subjects", result);
            }
        }

        yield break;
    }

    public void CreateDialogElement(String title, String message)
    {
        Dialog2Title = title;
        Dialog2Message = message;
        CreateDialogElement();
    }

    public void CreateDialogElement()
    {
        // TEST create_dialog:
        if (launchedDialog)
            return;
        launchedDialog = true;

        // The buttons is order by enums so:
        // Lowest enum || 2. lowest
        // 2. highest  || highest

        DialogButtonType buttons = DialogButtonType.Close | DialogButtonType.EDoc | DialogButtonType.Alarms |
                                   DialogButtonType.Element3D;
        StartCoroutine(LaunchDialogOverTime(buttons, Dialog2Title, Dialog2Message));
    }

    private async void LoadUri()
    {
#if ENABLE_WINMD_SUPPORT
// The URI to launch
        var uriBing = new Uri(@"http://www.bing.com");

        // Launch the URI
        var success = await Windows.System.Launcher.LaunchUriAsync(uriBing);
#endif
    }

    protected IEnumerator LaunchDialogOverTime(DialogButtonType buttons, string title, string message)
    {
        // Disable all our buttons
        /**
        foreach (GameObject buttonGo in Interactibles)
        {
            buttonGo.SetActive(false);
        }

        Result.gameObject.SetActive(false);
        **/
        Debug.Log(buttons.ToString());
        DialogResult result = new DialogResult
        {
            Buttons = buttons,
            Title = title,
            Message = message
        };
        Dialog dialog = Dialog.Open(DialogPrefab.gameObject, result);
        dialog.OnClosed += OnClosed;

        // Wait for dialog to close

        while (dialog.State != DialogState.Closed)
        {
            yield return null;
        }

        // Enable all our buttons
        /**
        foreach (GameObject buttonGo in Interactibles)
        {
            buttonGo.SetActive(true);
        }
        
        Result.gameObject.SetActive(true);
        **/
        launchedDialog = false;
        yield break;
    }

    protected void OnClosed(DialogResult result)
    {
        var menuManager = GameObject.Find("/Managers/MenuManager");
        GameObject qrm = GameObject.Find("/Managers/QRManager");
        Scanner scanner = qrm.GetComponent<Scanner>();
        if (menuManager)
        {
                switch (result.Result)
                {
                    case DialogButtonType.Element3D:
                        break;
                    case DialogButtonType.Accept:
                        break;
                    case DialogButtonType.Alarms:
                        var detailCreator = menuManager.GetComponent<CreateDetailMenu>();
                        detailCreator.CreateDetailElement();
                        break;
                    case DialogButtonType.Cancel:
                        scanner.dialogClosed();
                        break;
                    case DialogButtonType.Close:
                        scanner.dialogClosed();
                    break;
                    case DialogButtonType.Confirm:
                        break;
                    case DialogButtonType.EDoc:
                        var listCreator = menuManager.GetComponent<CreateListingMenu>();
                        listCreator.CreateListingElement();
                        break;
                    case DialogButtonType.No:
                        scanner.dialogClosed();
                        break;
                    case DialogButtonType.None:
                        break;
                    case DialogButtonType.OK:
                        break;
                    case DialogButtonType.Yes:
                        break;
                    default:
                        break;
                }
        }
        else
        {
            //Some kind of error.
        }

        // Result.text = "Dialog result: " + result.Result.ToString();
    }
}