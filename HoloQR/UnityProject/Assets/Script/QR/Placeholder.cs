using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.WSA.Input;

public class Placeholder : MonoBehaviour
{
    public Transform TextMeshObject;
    private DateTime _resetTime;

    // it is used but only on the Hololens.
    [UsedImplicitly] private GameObject dialogManager;

    private void Start()
    {
        this.textMesh = this.TextMeshObject.GetComponent<TextMesh>();
        dialogManager = GameObject.Find("/Managers/DialogManager");
        this.OnReset();
        _resetTime = DateTime.Now;
        Debug.Log("Start called");
    }

    public void OnScan()
    {
        // This is used to prevent from resetting and instant run again.
        DateTime now = DateTime.Now;
        if (_resetTime.AddSeconds(1.0) >= now)
        {
            Debug.Log("resetTime:<" +_resetTime+">, now:<" +now+">");
            return;
        }

        // Update text:
        this.textMesh.text = "scanning for 30s";
        Debug.Log("OnScan called");
        // This is only executed if it is not in unity editor, since we cannot find first cam.
#if !UNITY_EDITOR
        MediaFrameQrProcessing.Wrappers.ZXingQrCodeScanner.ScanFirstCameraForQrCode(
            result =>
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        Debug.Log("1. Invoked");                        
                        
                        if(result != null)
                        {
                            Debug.Log("2. Result found");
                            this.textMesh.text = "";
                            var createDialog = dialogManager.GetComponent<CreateDialog>();
                            createDialog.CreateLoadingElement(result);
                        } else 
                        {
                            Debug.Log("2. Result not found");
                            this.textMesh.text = "not found";
                        }
                        // This is only executed when 
                        // LoadUri();
                    },
                    false);
            },
            TimeSpan.FromSeconds(30));
#endif
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

    public void OnRun()
    {
        this.textMesh.text = "running forever";

#if !UNITY_EDITOR
        MediaFrameQrProcessing.Wrappers.ZXingQrCodeScanner.ScanFirstCameraForQrCode(
            result =>
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(
                    () => { this.textMesh.text = $"Got result {result} at {DateTime.Now}"; },
                    false);
            },
            null);
#endif
    }

    public void OnReset()
    {
        this.textMesh.text = "say scan or run to start";
        _resetTime = DateTime.Now;
    }

    TextMesh textMesh;
}