using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class Placeholder : MonoBehaviour
{
    public Transform textMeshObject;
    private DateTime resetTime;

    private void Start()
    {
        this.textMesh = this.textMeshObject.GetComponent<TextMesh>();
        this.OnReset();
        resetTime = DateTime.Now;
        Debug.Log("Start called");
    }

    public void OnScan()
    {
        // This is used to prevent from resetting and instant run again.
        DateTime now = DateTime.Now;
        if(resetTime.AddSeconds(1.0) >= now )
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
          }, 
          false);
        },
        TimeSpan.FromSeconds(30));
#endif
    }

    public void OnRun()
    {
        this.textMesh.text = "running forever";

#if !UNITY_EDITOR
    MediaFrameQrProcessing.Wrappers.ZXingQrCodeScanner.ScanFirstCameraForQrCode(
        result =>
        {
          UnityEngine.WSA.Application.InvokeOnAppThread(() =>
          {
            this.textMesh.text = $"Got result {result} at {DateTime.Now}";
          }, 
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