using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.WSA.Input;

public class Scanner : MonoBehaviour
{
    public Transform TextMeshObject;
    private DateTime _resetTime;
    private static bool _scanRunning = false;
    private static bool _dialogOpen = false;

    // it is used but only on the Hololens.
    [UsedImplicitly] private GameObject dialogManager;

    private void Start()
    {
        this.textMesh = this.TextMeshObject.GetComponent<TextMesh>();
        dialogManager = GameObject.Find("/Managers/DialogManager");
        this.OnReset();
        _resetTime = DateTime.Now;
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
        if(_scanRunning)
        {
            Debug.Log("A scan is already running");
            return;
        }
        if(_dialogOpen)
        {
            Debug.Log("A Dialog is open.");
            return;
        }

        _scanRunning = true;
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
                            _dialogOpen = true;
                            var createDialog = dialogManager.GetComponent<CreateDialog>();
                            createDialog.CreateLoadingElement(result);
                        } else 
                        {
                            Debug.Log("2. Result not found");
                            this.textMesh.text = "not found";
                        }
                        // This is only executed when 
                        // LoadUri();
                        Debug.Log("2.1 before test");
                        _scanRunning = false;
                        Debug.Log("2.2 after test");
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
    public void dialogClosed()
    {
        _dialogOpen = false;
        this.textMesh.text = "say scan or run to start";
    }
    public void OnReset()
    {
        _scanRunning = false;
        _dialogOpen = false;
        this.textMesh.text = "say scan or run to start";
        _resetTime = DateTime.Now;
    }

    TextMesh textMesh;
}