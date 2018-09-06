using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.Receivers;
using HoloToolkit.Unity.UX;
using HoloToolkit.UX.Dialog;
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
        // TEST create_dialog:
        if (launchedDialog)
            return;
        launchedDialog = true;
        CreateDialogElement();

        // Disable everything else
    }

    #endregion IInputClickHandler

    public void CreateLoadingElement()
    {
        /** FOR LOADING LATER!
        while (_dataNotReceived)
        {
            
        }
        **/
    }

    public void CreateDialogElement()
    {
        // The buttons is order by enums so:
        // Lowest enum || 2. lowest
        // 2. highest  || highest
        DialogButtonType buttons = DialogButtonType.Cancel | DialogButtonType.Close; //| DialogButtonType.Confirm | DialogButtonType.Accept;
        StartCoroutine(LaunchDialogOverTime(buttons, Dialog2Title, Dialog2Message));
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