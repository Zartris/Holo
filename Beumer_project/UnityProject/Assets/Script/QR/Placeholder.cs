﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class Placeholder : MonoBehaviour
{
    public Transform textMeshObject;
    private DateTime resetTime;
    private GameObject dialogManager;

    private void Start()
    {
        this.textMesh = this.textMeshObject.GetComponent<TextMesh>();
        dialogManager = GameObject.Find("/Managers/DialogManager");
        this.OnReset();
        resetTime = DateTime.Now;
        Debug.Log("Start called");
    }

    public void OnScan()
    {
        // This is used to prevent from resetting and instant run again.
        DateTime now = DateTime.Now;
        if (resetTime.AddSeconds(1.0) >= now)
        {
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
                        this.textMesh.text = result ?? "not found";
                        if(result)
                        {
                            var createDialog = dialogManager.GetComponent<CreateDialog>();
                            createDialog.CreateDialogElement(result, "Very long text, okay not so creative but it is building up to a nice and long text. Soooo What did you have for breakfast??? Hmmm this most be long enough.");
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
        resetTime = DateTime.Now;
    }

    TextMesh textMesh;
}