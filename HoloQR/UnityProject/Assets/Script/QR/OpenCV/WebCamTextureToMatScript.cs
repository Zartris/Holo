using System;
using UnityEngine;
using System.Collections;

#if UNITY_5_3 || UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using OpenCVForUnity;
using UnityEngine.Experimental.PlayerLoop;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// WebCamTextureToMat Example
    /// An example of converting a WebCamTexture image to OpenCV's Mat format.
    /// </summary>
    public class WebCamTextureToMatScript: MonoBehaviour
    {
        /// <summary>
        /// Set this to specify the name of the device to use.
        /// </summary>
        public string requestedDeviceName = null;

        /// <summary>
        /// Set the requested width of the camera device.
        /// </summary>
        public int requestedWidth = 640;
        
        /// <summary>
        /// Set the requested height of the camera device.
        /// </summary>
        public int requestedHeight = 480;
        
        /// <summary>
        /// Set the requested to using the front camera.
        /// </summary>
        public bool requestedIsFrontFacing = false;

        /// <summary>
        /// The webcam texture.
        /// </summary>
        WebCamTexture webCamTexture;

        /// <summary>
        /// The webcam device.
        /// </summary>
        WebCamDevice webCamDevice;

        /// <summary>
        /// The rgba mat.
        /// </summary>
        Mat rgbaMat;

        /// <summary>
        /// The colors.
        /// </summary>
        Color32[] colors;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// Indicates whether this instance is waiting for initialization to complete.
        /// </summary>
        bool isInitWaiting = false;

        /// <summary>
        /// Indicates whether this instance has been initialized.
        /// </summary>
        bool hasInitDone = false;
        /// <summary>
        /// What fps on quad
        /// </summary>
        public int frameCount = 0;
        // Use this for initialization
        void Start ()
        {
            Initialize ();
        }

        /// <summary>
        /// Initialize of web cam texture.
        /// </summary>
        private void Initialize ()
        {
            if (isInitWaiting)
                return;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture.
        /// </summary>
        /// <param name="deviceName">Device name.</param>
        /// <param name="requestedWidth">Requested width.</param>
        /// <param name="requestedHeight">Requested height.</param>
        /// <param name="requestedIsFrontFacing">If set to <c>true</c> requested to using the front camera.</param>
        private void Initialize (string deviceName, int requestedWidth, int requestedHeight, bool requestedIsFrontFacing)
        {
            if (isInitWaiting)
                return;

            this.requestedDeviceName = deviceName;
            this.requestedWidth = requestedWidth;
            this.requestedHeight = requestedHeight;
            this.requestedIsFrontFacing = requestedIsFrontFacing;

            StartCoroutine (_Initialize ());
        }

        /// <summary>
        /// Initialize of webcam texture by coroutine.
        /// </summary>
        private IEnumerator _Initialize ()
        {
            if (hasInitDone)
                Dispose ();

            isInitWaiting = true;

            if (!String.IsNullOrEmpty (requestedDeviceName)) {
                //Debug.Log ("deviceName is "+requestedDeviceName);
                webCamTexture = new WebCamTexture (requestedDeviceName, requestedWidth, requestedHeight);
            } else {
                //Debug.Log ("deviceName is null");
                // Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) {
                    if (WebCamTexture.devices [cameraIndex].isFrontFacing == requestedIsFrontFacing) {

                        //Debug.Log (cameraIndex + " name " + WebCamTexture.devices [cameraIndex].name + " isFrontFacing " + WebCamTexture.devices [cameraIndex].isFrontFacing);
                        webCamDevice = WebCamTexture.devices [cameraIndex];
                        webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);

                        break;
                    }
                }
            }

            if (webCamTexture == null) {
                if (WebCamTexture.devices.Length > 0) {
                    webCamDevice = WebCamTexture.devices [0];
                    webCamTexture = new WebCamTexture (webCamDevice.name, requestedWidth, requestedHeight);
                } else {
                    webCamTexture = new WebCamTexture (requestedWidth, requestedHeight);
                }
            }

            // Starts the camera.
            webCamTexture.Play ();

            while (true) {
                // If you want to use webcamTexture.width and webcamTexture.height on iOS, you have to wait until webcamTexture.didUpdateThisFrame == 1, otherwise these two values will be equal to 16. (http://forum.unity3d.com/threads/webcamtexture-and-error-0x0502.123922/).
                #if UNITY_IOS && !UNITY_EDITOR && (UNITY_4_6_3 || UNITY_4_6_4 || UNITY_5_0_0 || UNITY_5_0_1)
                if (webCamTexture.width > 16 && webCamTexture.height > 16) {
                #else
                if (webCamTexture.didUpdateThisFrame) {
                    #if UNITY_IOS && !UNITY_EDITOR && UNITY_5_2                                    
                    while (webCamTexture.width <= 16) {
                        webCamTexture.GetPixels32 ();
                        yield return new WaitForEndOfFrame ();
                    } 
                    #endif
                    #endif

                    Debug.Log ("name " + webCamTexture.name + " width " + webCamTexture.width + " height " + webCamTexture.height + " fps " + webCamTexture.requestedFPS);
                    Debug.Log ("videoRotationAngle " + webCamTexture.videoRotationAngle + " videoVerticallyMirrored " + webCamTexture.videoVerticallyMirrored + " isFrongFacing " + webCamDevice.isFrontFacing);

                    isInitWaiting = false;
                    hasInitDone = true;

                    OnInited ();

                    break;
                } else {
                    yield return 0;
                }
            }
        }

        /// <summary>
        /// Releases all resource.
        /// </summary>
        private void Dispose ()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null) {
                webCamTexture.Stop ();
                webCamTexture = null;
            }
            if (rgbaMat != null) {
                rgbaMat.Dispose ();
                rgbaMat = null;
            }
        }

        /// <summary>
        /// Initialize completion handler of the webcam texture.
        /// </summary>
        private void OnInited ()
        {
            if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
                colors = new Color32[webCamTexture.width * webCamTexture.height];
            if (texture == null || texture.width != webCamTexture.width || texture.height != webCamTexture.height)
                texture = new Texture2D (webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);

            rgbaMat = new Mat (webCamTexture.height, webCamTexture.width, CvType.CV_8UC4);

            gameObject.GetComponent<Renderer> ().material.mainTexture = texture;

            //gameObject.transform.localScale = new Vector3 (webCamTexture.width, webCamTexture.height, 1);
            Debug.Log ("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);


            float width = rgbaMat.width ();
            float height = rgbaMat.height ();

            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale) {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
            } else {
                Camera.main.orthographicSize = height / 2;
            }
        }

        // Update is called once per frame
        void Update ()
        {
            // four times per sec
            if(frameCount<10)
            {
                frameCount++;
                return;
            }

            frameCount = 0;
            if (hasInitDone && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame) {
                Utils.webCamTextureToMat (webCamTexture, rgbaMat, colors);

                Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Utils.matToTexture2D (rgbaMat, texture, colors);
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy ()
        {
            Dispose ();
        }
    }
}