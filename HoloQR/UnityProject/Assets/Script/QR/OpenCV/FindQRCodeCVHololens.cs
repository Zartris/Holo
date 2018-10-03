using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using OpenCVForUnity.RectangleTrack;
using System.Threading;
using HoloToolkit.Unity.InputModule;
using OpenCVForUnity;
using Script.QR.OpenCV.Utils;
using Rect = OpenCVForUnity.Rect;

namespace HoloLensWithOpenCVForUnityExample
{
    /// <summary>
    /// HoloLens Face Detection Example
    /// An example of detecting face using OpenCVForUnity on Hololens.
    /// Referring to https://github.com/Itseez/opencv/blob/master/modules/objdetect/src/detection_based_tracker.cpp.
    /// </summary>
    [RequireComponent(typeof(HololensCameraStreamToMatHelper))]
    public class FindQRCodeCVHololens : MonoBehaviour
    {
        /// <summary>
        /// Determines if displays camera image.
        /// </summary>
        public bool displayCameraImage = false;

        /// <summary>
        /// The display camera image toggle.
        /// </summary>
        public Toggle displayCameraImageToggle;

        /// <summary>
        /// The min detection size ratio.
        /// </summary>
        public float minDetectionSizeRatio = 0.07f;

        /// <summary>
        /// The webcam texture to mat helper.
        /// </summary>
        HololensCameraStreamToMatHelper webCamTextureToMatHelper;

        /// <summary>
        /// The gray mat.
        /// </summary>
        Mat grayMat;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The quad renderer.
        /// </summary>
        Renderer quad_renderer;

        /// <summary>
        /// The detection result.
        /// </summary>
        MatOfRect detectionResult;

        /// <summary>
        /// The Qr detector
        /// </summary>
        private QrDetector detector;

        const int DBG = 1;

        public GameObject rightObject;
        public GameObject leftObject;
        public GameObject middleObject;

        private Texture2D rightTexture;
        private Texture2D leftTexture;
        private Texture2D middleTexture;

        private bool taskRunning = false;
        private bool workDone = false;
        private Mat _rawImage;

        // Creation of Intermediate 'Image' Objects required later
        private Mat gray;
        private Mat edges;
        private Mat traces;

        private Mat qr;
        private Mat qr_raw;
        private Mat qr_gray;
        private Mat qr_thres;

        // Variable used
        int mark, A, B, C, top, right, bottom, median1, median2, outlier, align, orientation;
        float AB, BC, CA, dist, slope, areat, arear, areab, large, padding;

        protected MixedRealityCameraManager camera;
        protected HoloToolkit.Unity.InputModule.Cursor cursor;
        protected InputManager input;


        Mat grayMat4Thread;

        readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
        System.Object sync = new System.Object();

        bool _isThreadRunning = false;

        bool isThreadRunning
        {
            get
            {
                lock (sync)
                    return _isThreadRunning;
            }
            set
            {
                lock (sync)
                    _isThreadRunning = value;
            }
        }

        RectangleTracker rectangleTracker;
        float coeffTrackingWindowSize = 2.0f;
        float coeffObjectSizeToTrack = 0.85f;
        Rect[] regionsWithResults = new Rect[0];
        List<Rect> detectedObjectsInRegions = new List<Rect>();
        List<Rect> resultObjects = new List<Rect>();

        bool _isDetecting = false;

        bool isDetecting
        {
            get
            {
                lock (sync)
                    return _isDetecting;
            }
            set
            {
                lock (sync)
                    _isDetecting = value;
            }
        }

        bool _hasUpdatedDetectionResult = false;

        bool hasUpdatedDetectionResult
        {
            get
            {
                lock (sync)
                    return _hasUpdatedDetectionResult;
            }
            set
            {
                lock (sync)
                    _hasUpdatedDetectionResult = value;
            }
        }

        // Use this for initialization
        protected void Start()
        {
            detector = new QrDetector();
            camera = FindObjectOfType<MixedRealityCameraManager>();
            cursor = FindObjectOfType<HoloToolkit.Unity.InputModule.Cursor>();
            input = FindObjectOfType<InputManager>();
            displayCameraImageToggle.isOn = displayCameraImage;

            webCamTextureToMatHelper = gameObject.GetComponent<HololensCameraStreamToMatHelper>();
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
             webCamTextureToMatHelper.frameMatAcquired += OnFrameMatAcquired;
            #endif
            webCamTextureToMatHelper.Initialize();

            rectangleTracker = new RectangleTracker();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper initialized event.
        /// </summary>
        public void OnWebCamTextureToMatHelperInitialized()
        {
            Debug.Log("OnWebCamTextureToMatHelperInitialized");

            Mat webCamTextureMat = webCamTextureToMatHelper.GetMat();

#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
// HololensCameraStream always returns image data in BGRA format.
            rightTexture =
 new Texture2D (webCamTextureMat.cols (), webCamTextureMat.rows (), TextureFormat.BGRA32, false);
            leftTexture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false
            middleTexture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
#else
            rightTexture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
            leftTexture = new Texture2D(webCamTextureMat.cols(), webCamTextureMat.rows(), TextureFormat.RGBA32, false);
//            rightTexture = new Texture2D(640, 480, TextureFormat.RGBA32, false);
//            leftTexture = new Texture2D(640, 480, TextureFormat.RGBA32, false);
            middleTexture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
#endif

            rightTexture.wrapMode = TextureWrapMode.Clamp;
            leftTexture.wrapMode = TextureWrapMode.Clamp;

            leftObject.GetComponent<Renderer>().material.mainTexture = leftTexture;
            rightObject.GetComponent<Renderer>().material.mainTexture = rightTexture;
            middleObject.GetComponent<Renderer>().material.mainTexture = middleTexture;
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " +
                      Screen.orientation);

            quad_renderer = gameObject.GetComponent<Renderer>() as Renderer;
            quad_renderer.sharedMaterial.SetTexture("_MainTex", rightTexture);
            quad_renderer.sharedMaterial.SetVector("_VignetteOffset", new Vector4(0, 0));

            Matrix4x4 projectionMatrix;
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            projectionMatrix = webCamTextureToMatHelper.GetProjectionMatrix ();
            quad_renderer.sharedMaterial.SetMatrix ("_CameraProjectionMatrix", projectionMatrix);
#else
            //This value is obtained from PhotoCapture's TryGetProjectionMatrix() method.I do not know whether this method is good.
            //Please see the discussion of this thread.Https://forums.hololens.com/discussion/782/live-stream-of-locatable-camera-webcam-in-unity
            projectionMatrix = Matrix4x4.identity;
            projectionMatrix.m00 = 2.31029f;
            projectionMatrix.m01 = 0.00000f;
            projectionMatrix.m02 = 0.09614f;
            projectionMatrix.m03 = 0.00000f;
            projectionMatrix.m10 = 0.00000f;
            projectionMatrix.m11 = 4.10427f;
            projectionMatrix.m12 = -0.06231f;
            projectionMatrix.m13 = 0.00000f;
            projectionMatrix.m20 = 0.00000f;
            projectionMatrix.m21 = 0.00000f;
            projectionMatrix.m22 = -1.00000f;
            projectionMatrix.m23 = 0.00000f;
            projectionMatrix.m30 = 0.00000f;
            projectionMatrix.m31 = 0.00000f;
            projectionMatrix.m32 = -1.00000f;
            projectionMatrix.m33 = 0.00000f;
            quad_renderer.sharedMaterial.SetMatrix("_CameraProjectionMatrix", projectionMatrix);
#endif

            quad_renderer.sharedMaterial.SetFloat("_VignetteScale", 0.0f);

            grayMat = new Mat(webCamTextureMat.rows(), webCamTextureMat.cols(), CvType.CV_8UC1);

            grayMat4Thread = new Mat();

            detectionResult = new MatOfRect();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper disposed event.
        /// </summary>
        public void OnWebCamTextureToMatHelperDisposed()
        {
            Debug.Log("OnWebCamTextureToMatHelperDisposed");

            StopThread();
            lock (sync)
            {
                ExecuteOnMainThread.Clear();
            }

            hasUpdatedDetectionResult = false;
            isDetecting = false;

            // if not null, then dispose.
            grayMat?.Dispose();
            grayMat4Thread?.Dispose();
            gray?.Dispose();
            edges?.Dispose();
            traces?.Dispose();
            qr?.Dispose();
            qr_thres?.Dispose();
            qr_gray?.Dispose();
            qr_raw?.Dispose();


            rectangleTracker.Reset();
        }

        /// <summary>
        /// Raises the web cam texture to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
        {
            Debug.Log("OnWebCamTextureToMatHelperErrorOccurred " + errorCode);
        }

#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
        public void OnFrameMatAcquired (Mat bgraMat, Matrix4x4 projectionMatrix, Matrix4x4 cameraToWorldMatrix)
        {
            Imgproc.cvtColor (bgraMat, grayMat, Imgproc.COLOR_BGRA2GRAY);
            Imgproc.equalizeHist (grayMat, grayMat);

            if (enableDetection && !isDetecting ) {

                isDetecting = true;

                grayMat.copyTo (grayMat4Thread);
                
                System.Threading.Tasks.Task.Run(() => {

                    isThreadRunning = true;
                    DetectObject ();
                    isThreadRunning = false;
                    OnDetectionDone ();
                });
            }
            

            if (!displayCameraImage) {
                // fill all black.
                Imgproc.rectangle (bgraMat, new Point (0, 0), new Point (bgraMat.width (), bgraMat.height ()), new Scalar (0, 0, 0, 0), -1);
            }


            Rect[] rects;
            if (!useSeparateDetection) {
                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    lock (rectangleTracker) {
                        rectangleTracker.UpdateTrackedObjects (detectionResult.toList ());
                    }
                }

                lock (rectangleTracker) {
                    rectangleTracker.GetObjects (resultObjects, true);
                }
                rects = resultObjects.ToArray ();

                for (int i = 0; i < rects.Length; i++) {
                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log ("detected face[" + i + "] " + rects [i]);
                    //}, true);

                    Imgproc.rectangle (bgraMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 0, 255, 255), 3);
                }

            }else {

                if (hasUpdatedDetectionResult) {
                    hasUpdatedDetectionResult = false;

                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get regionsWithResults were got from detectionResult");
                    //}, true);

                    lock (rectangleTracker) {
                        regionsWithResults = detectionResult.toArray ();
                    }

                    rects = regionsWithResults;
                    for (int i = 0; i < rects.Length; i++) {
                        Imgproc.rectangle (bgraMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (255, 0, 0, 255), 1);
                    }

                } else {
                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log("process: get regionsWithResults from previous positions");
                    //}, true);

                    lock (rectangleTracker) {
                        regionsWithResults = rectangleTracker.CreateCorrectionBySpeedOfRects ();
                    }

                    rects = regionsWithResults;
                    for (int i = 0; i < rects.Length; i++) {
                        Imgproc.rectangle (bgraMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 255, 0, 255), 1);
                    }
                }

                detectedObjectsInRegions.Clear ();
                if (regionsWithResults.Length > 0) {
                    int len = regionsWithResults.Length;
                    for (int i = 0; i < len; i++) {
                        DetectInRegion (grayMat, regionsWithResults [i], detectedObjectsInRegions);
                    }
                }                

                lock (rectangleTracker) {
                    rectangleTracker.UpdateTrackedObjects (detectedObjectsInRegions);
                    rectangleTracker.GetObjects (resultObjects, true);
                }

                rects = resultObjects.ToArray ();

                for (int i = 0; i < rects.Length; i++) {
                    //UnityEngine.WSA.Application.InvokeOnAppThread (() => {
                    //    Debug.Log ("detected face[" + i + "] " + rects [i]);
                    //}, true);

                    Imgproc.rectangle (bgraMat, new Point (rects [i].x, rects [i].y), new Point (rects [i].x + rects [i].width, rects [i].y + rects [i].height), new Scalar (0, 0, 255, 255), 3);
                }
            }


            UnityEngine.WSA.Application.InvokeOnAppThread(() => {

                if (!webCamTextureToMatHelper.IsPlaying ()) return;

                Utils.fastMatToTexture2D(bgraMat, texture);
                bgraMat.Dispose ();

                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix ("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn (3) - cameraToWorldMatrix.GetColumn (2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation =
 Quaternion.LookRotation (-cameraToWorldMatrix.GetColumn (2), cameraToWorldMatrix.GetColumn (1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;

            }, false);
        }

#else
        // Update is called once per frame
        void Update()
        {
            lock (sync)
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    ExecuteOnMainThread.Dequeue().Invoke();
                }
            }

            if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
            {
                Mat _rawImage = webCamTextureToMatHelper.GetMat();

                Imgproc.cvtColor(_rawImage, grayMat, Imgproc.COLOR_RGBA2GRAY);
                Imgproc.equalizeHist(grayMat, grayMat);

                if (!taskRunning)
                {
                    taskRunning = true;

                    grayMat.copyTo(grayMat4Thread);

                    StartThread(ThreadWorker);
                }

                if (!displayCameraImage)
                {
                    // fill all black.
                    Imgproc.rectangle(_rawImage, new Point(0, 0), new Point(_rawImage.width(), _rawImage.height()),
                        new Scalar(0, 0, 0, 0), -1);
                }

                Rect[] rects;
                if (workDone)
                {
                    workDone = false;
                    Utils.fastMatToTexture2D(_rawImage, rightTexture);
                    Utils.fastMatToTexture2D(traces, leftTexture);
                    Utils.fastMatToTexture2D(qr_raw, middleTexture);

                    // Add rects of QR:
                    regionsWithResults = detectionResult.toArray();

                    rects = regionsWithResults;
                    for (int i = 0; i < rects.Length; i++)
                    {
                        Imgproc.rectangle(_rawImage, new Point(rects[i].x, rects[i].y),
                            new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height),
                            new Scalar(0, 0, 255, 255), 1);
                    }
                }
                else
                {
                    //Debug.Log("process: get regionsWithResults from previous positions");
                    regionsWithResults = rectangleTracker.CreateCorrectionBySpeedOfRects();

                    rects = regionsWithResults;
                    for (int i = 0; i < rects.Length; i++)
                    {
                        Imgproc.rectangle(_rawImage, new Point(rects[i].x, rects[i].y),
                            new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height),
                            new Scalar(0, 255, 0, 255), 1);
                    }
                }

                detectedObjectsInRegions.Clear();
                if (regionsWithResults.Length > 0)
                {
                    int len = regionsWithResults.Length;
                    for (int i = 0; i < len; i++)
                    {
                        DetectInRegion(grayMat, regionsWithResults[i], detectedObjectsInRegions);
                    }
                }

                rectangleTracker.UpdateTrackedObjects(detectedObjectsInRegions);
                rectangleTracker.GetObjects(resultObjects, true);

                rects = resultObjects.ToArray();
                for (int i = 0; i < rects.Length; i++)
                {
                    //Debug.Log ("detected face[" + i + "] " + rects [i]);
                    Imgproc.rectangle(_rawImage, new Point(rects[i].x, rects[i].y),
                        new Point(rects[i].x + rects[i].width, rects[i].y + rects[i].height),
                        new Scalar(255, 0, 0, 255), 2);
                }

                Utils.fastMatToTexture2D(_rawImage, rightTexture);
            }

            if (webCamTextureToMatHelper.IsPlaying())
            {
                Matrix4x4 cameraToWorldMatrix = Camera.main.cameraToWorldMatrix;
                ;
                Matrix4x4 worldToCameraMatrix = cameraToWorldMatrix.inverse;

                quad_renderer.sharedMaterial.SetMatrix("_WorldToCameraMatrix", worldToCameraMatrix);

                // Position the canvas object slightly in front
                // of the real world web camera.
                Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
                position *= 1.2f;

                // Rotate the canvas object so that it faces the user.
                Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2),
                    cameraToWorldMatrix.GetColumn(1));

                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
            }
        }
#endif

        private void StartThread(Action action)
        {
#if UNITY_METRO && NETFX_CORE
            System.Threading.Tasks.Task.Run(() => action());
#elif UNITY_METRO
            action.BeginInvoke(ar => action.EndInvoke(ar), null);
#else
            ThreadPool.QueueUserWorkItem (_ => action());
#endif
        }

        private void StopThread()
        {
            if (!isThreadRunning)
                return;

            while (isThreadRunning)
            {
                //Wait threading stop
            }
        }

        private void ThreadWorker()
        {
            isThreadRunning = true;

            DetectObject();

            lock (sync)
            {
                if (ExecuteOnMainThread.Count == 0)
                {
                    ExecuteOnMainThread.Enqueue(() => { OnDetectionDone(); });
                }
            }

            isThreadRunning = false;
        }

        private void DetectObject()
        {
            MatOfRect objects = new MatOfRect();
            if (cascade4Thread != null)
                cascade4Thread.detectMultiScale(grayMat4Thread, objects, 1.1, 2,
                    Objdetect.CASCADE_SCALE_IMAGE, // TODO: objdetect.CV_HAAR_SCALE_IMAGE
                    new Size(grayMat4Thread.cols() * minDetectionSizeRatio,
                        grayMat4Thread.rows() * minDetectionSizeRatio), new Size());

            detectionResult = objects;
        }

        private void OnDetectionDone()
        {
            workDone = true;

            taskRunning = false;
        }

        private void DetectInRegion(Mat img, Rect r, List<Rect> detectedObjectsInRegions)
        {
            Rect r0 = new Rect(new Point(), img.size());
            Rect r1 = new Rect(r.x, r.y, r.width, r.height);
            Rect.inflate(r1, (int) ((r1.width * coeffTrackingWindowSize) - r1.width) / 2,
                (int) ((r1.height * coeffTrackingWindowSize) - r1.height) / 2);
            r1 = Rect.intersect(r0, r1);

            if ((r1.width <= 0) || (r1.height <= 0))
            {
                Debug.Log("DetectionBasedTracker::detectInRegion: Empty intersection");
                return;
            }

            int d = Math.Min(r.width, r.height);
            d = (int) Math.Round(d * coeffObjectSizeToTrack);

            MatOfRect tmpobjects = new MatOfRect();

            Mat img1 = new Mat(img, r1); //subimage for rectangle -- without data copying

            detector.detectQR(img1);
            CascadeClassifier cascade = new CascadeClassifier();
            cascade.detectMultiScale(img1, tmpobjects, 1.1, 2,
                0 | Objdetect.CASCADE_DO_CANNY_PRUNING | Objdetect.CASCADE_SCALE_IMAGE |
                Objdetect.CASCADE_FIND_BIGGEST_OBJECT, new Size(d, d), new Size());


            Rect[] tmpobjectsArray = tmpobjects.toArray();
            int len = tmpobjectsArray.Length;
            for (int i = 0; i < len; i++)
            {
                Rect tmp = tmpobjectsArray[i];
                Rect curres = new Rect(new Point(tmp.x + r1.x, tmp.y + r1.y), tmp.size());
                detectedObjectsInRegions.Add(curres);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
#if NETFX_CORE && !DISABLE_HOLOLENSCAMSTREAM_API
            webCamTextureToMatHelper.frameMatAcquired -= OnFrameMatAcquired;
#endif
            webCamTextureToMatHelper.Dispose();

            if (rectangleTracker != null)
                rectangleTracker.Dispose();
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            webCamTextureToMatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            webCamTextureToMatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            webCamTextureToMatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            webCamTextureToMatHelper.Initialize(null, webCamTextureToMatHelper.requestedWidth,
                webCamTextureToMatHelper.requestedHeight, !webCamTextureToMatHelper.requestedIsFrontFacing);
        }


        /// <summary>
        /// Raises the display camera image toggle value changed event.
        /// </summary>
        public void OnDisplayCameraImageToggleValueChanged()
        {
            if (displayCameraImageToggle.isOn)
            {
                displayCameraImage = true;
            }
            else
            {
                displayCameraImage = false;
            }
        }


    }
}