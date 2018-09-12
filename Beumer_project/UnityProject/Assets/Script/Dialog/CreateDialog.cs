using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.UX;
using HoloToolkit.UX.Dialog;
using HoloToolkit.UX.Progress;
using UnityEngine;

public class CreateDialog : MonoBehaviour, IInputClickHandler
{
    public Dialog _dialogPrefab;
    private bool _dataNotReceived = true;

    [Header("Dialog options")] public string Dialog2Title = "Two button dialog";
    [TextArea] public string Dialog2Message =
        "This is a message for dialog 2. Longer messages will be wrapped automatically. However you still need to be aware of overflow.";

    protected bool launchedDialog = false;


    #region IInputClickHandler

    public void OnInputClicked(InputClickedEventData eventData)
    {
        CreateDialogElement();

        // Disable everything else
    }

    #endregion IInputClickHandler

    public void CreateLoadingElement()
    {

        ProgressIndicator.Instance.Open(
            ProgressIndicator.IndicatorStyleEnum.None,
            ProgressIndicator.ProgressStyleEnum.None,
            ProgressIndicator.MessageStyleEnum.Visible,
            LeadInMessage);

        StartCoroutine(LoadOverTime(LoadTextMessage));

        /** FOR LOADING LATER!
        while (_dataNotReceived)
        {
            
        }
        **/
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

        DialogButtonType buttons = DialogButtonType.Cancel | DialogButtonType.Close | DialogButtonType.Confirm | DialogButtonType.Accept;
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

        Dialog dialog = Dialog.Open(_dialogPrefab.gameObject, buttons, title, message);
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
        // Result.text = "Dialog result: " + result.Result.ToString();
    }
}