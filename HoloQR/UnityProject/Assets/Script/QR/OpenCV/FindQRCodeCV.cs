using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenCVForUnity;
using UnityEngine;
using Rect = OpenCVForUnity.Rect;

public class FindQRCodeCV : MonoBehaviour
{
    const int CV_QR_NORTH = 0;
    const int CV_QR_EAST = 1;
    const int CV_QR_SOUTH = 2;
    const int CV_QR_WEST = 3;

    // This is const we can change to fit better:
    const int FRAME_UPDATE_COUNTER = 10;
    const int DBG = 1;

    public GameObject rightObject;
    public GameObject leftObject;
    public GameObject middleObject;

    private Texture2D rightTexture;
    private Texture2D leftTexture;
    private Texture2D middleTexture;

    private bool _setupError = false;
    private int _frameCounter = 0;
    private VideoCapture _capture;
    private bool taskRunning = false;
    private bool workDone = false;
    private bool found = false;
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

    // Use this for initialization
    void Start()
    {
        rightTexture = new Texture2D(640, 480, TextureFormat.RGBA32, false);
        leftTexture = new Texture2D(640, 480, TextureFormat.RGBA32, false);
        middleTexture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
        leftObject.GetComponent<Renderer>().material.mainTexture = leftTexture;
        rightObject.GetComponent<Renderer>().material.mainTexture = rightTexture;
        middleObject.GetComponent<Renderer>().material.mainTexture = middleTexture;
        
        // Get first cam in hololens
        _capture = new VideoCapture(0);


        _rawImage = new Mat();
        // Checking for faults
        if (!_capture.isOpened())
        {
            Debug.Log(" ERR: Unable find input Video source.");
            _setupError = true;
        }

        if (!_capture.read(_rawImage))
        {
            Debug.Log(" ERR: Unable find input Video source.");
            _setupError = true;
        }

        gray = new Mat(_rawImage.size(), CvType.makeType(_rawImage.depth(), 1)); // To hold Grayscale Image
        edges = new Mat(_rawImage.size(), CvType.makeType(_rawImage.depth(), 1)); // To hold Grayscale Image
        traces = new Mat(_rawImage.size(), CvType.CV_8UC3); // For Debug Visuals
    }

    // Update is called once per frame
    void Update()
    {
        if (ShouldBail())
        {
            return;
        }

        if (workDone)
        {
            if (found)
            {
                Imgproc.cvtColor(_rawImage,_rawImage, Imgproc.COLOR_BGRA2RGBA);
                Utils.matToTexture2D(_rawImage, rightTexture);
                Utils.matToTexture2D(traces, leftTexture);
                Utils.matToTexture2D(qr_raw, middleTexture);
            }
            else
            {
                Utils.matToTexture2D(_rawImage, rightTexture);
            }

            workDone = false;
            taskRunning = false;
        }

        if (taskRunning)
        {
            return;
        }

        taskRunning = true;
//        workDone = notAsync();
        AsyncImgProcessing(
            result =>
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                    {
                        found = result;
                        workDone = true;
                    },
                    false);
            });


        //        imshow("Image", _rawImage);
        //        imshow("Traces", traces);
        //        imshow("QR code", qr_thres);
    }

    public async Task AsyncImgProcessing(
        Action<bool> resultCallback = null
    )
    {
        // Note: the natural thing to do here is what I used to do which is to create the
        // MediaCapture inside of a using block.
        // Problem is, that seemed to cause a situation where I could get a crash (AV) in
        //
        // Windows.Media.dll!Windows::Media::Capture::Frame::MediaFrameReader::CompletePendingStopOperation
        //
        // Which seemed to be related to stopping/disposing the MediaFrameReader and then
        // disposing the media capture immediately after.
        // 
        // Right now, I've promoted the media capture to a member variable and held it around
        // and instead of creating/disposing an instance each time one instance is kept
        // indefinitely.
        // It's not what I wanted...
        await Task.Run(
            async () =>
            {
                //        traces = new Scalar(0, 0, 0);
                // create Mat with 100x100 all zeros
                qr_raw = new Mat(100, 100, CvType.CV_8UC3, Scalar.all(0));
                qr = new Mat(100, 100, CvType.CV_8UC3, Scalar.all(0));
                qr_gray = new Mat(100, 100, CvType.CV_8UC1, Scalar.all(0));
                qr_thres = new Mat(100, 100, CvType.CV_8UC1, Scalar.all(0));


                Imgproc.cvtColor(_rawImage, gray,
                    Imgproc.COLOR_RGB2GRAY); // Convert Image captured from Image Input to GrayScale	
                Imgproc.Canny(gray, edges, 100, 200, 3,
                    true); // Apply Canny edge detection on the gray image ( Not sure true)


                // Find contours with hierarchy:
                List<MatOfPoint> contours = new List<MatOfPoint>();
                Mat hierarchy = new Mat();
                Imgproc.findContours(edges, contours, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);


                mark = 0; // Reset all detected marker count for this frame

                // Get Moments for all Contours and the mass centers
                List<Moments> mu = new List<Moments>();
                List<Point> mc = new List<Point>();

                for (int i = 0; i < contours.Count; i++)
                {
                    mu.Add(Imgproc.moments(contours[i], false));
                    mc.Add(new Point(mu[i].m10 / mu[i].m00, mu[i].m01 / mu[i].m00));
                }


                // Start processing the contour 
                // Find Three repeatedly enclosed contours A,B,C
                // NOTE: 1. Contour enclosing other contours is assumed to be the three Alignment markings of the QR code.
                // 2. Alternately, the Ratio of areas of the "concentric" squares can also be used for identifying base Alignment markers.
                // The below demonstrates the first method
                MatOfPoint2f pointsseq = new MatOfPoint2f(); //used to save the approximated sides of each contour
                for (int i = 0; i < contours.Count; i++)
                {
                    //Find the approximated polygon of the contour we are examining

                    Imgproc.approxPolyDP(new MatOfPoint2f(contours[i].toArray()), pointsseq,
                        Imgproc.arcLength(new MatOfPoint2f(contours[i].toArray()), true) * 0.02, true);
                    if (pointsseq.toList().Count == 4) // only quadrilaterals contours are examined
                    {
                        int k = i;
                        int c = 0;

                        // Checking if child is the inner contour
                        // 4 channels (id of next, previous, child, and parent contour)
                        while (hierarchy.get(0, k)[2] != -1.0)
                        {
                            k = Convert.ToInt32(hierarchy.get(0, k)[2]);
                            c = c + 1;
                        }

                        if (hierarchy.get(0, k)[2] != -1.0)
                        {
                            c = c + 1;
                        }

                        if (c >= 5)
                        {
                            if (mark == 0) A = i;
                            else if (mark == 1) B = i; // i.e., A is already found, assign current contour to B
                            else if (mark == 2) C = i; // i.e., A and B are already found, assign current contour to C
                            mark = mark + 1;
                        }
                    }
                }

                if (mark >= 3) // Ensure we have (atleast 3; namely A,B,C) 'Alignment Markers' discovered
                {
                    // We have found the 3 markers for the QR code; Now we need to determine which of them are 'top', 'right' and 'bottom' markers

                    // Determining the 'top' marker
                    // Vertex of the triangle NOT involved in the longest side is the 'outlier'

                    AB = cv_distance(mc[A], mc[B]);
                    BC = cv_distance(mc[B], mc[C]);
                    CA = cv_distance(mc[C], mc[A]);

                    if (AB > BC && AB > CA)
                    {
                        outlier = C;
                        median1 = A;
                        median2 = B;
                    }
                    else if (CA > AB && CA > BC)
                    {
                        outlier = B;
                        median1 = A;
                        median2 = C;
                    }
                    else if (BC > AB && BC > CA)
                    {
                        outlier = A;
                        median1 = B;
                        median2 = C;
                    }

                    top = outlier; // The obvious choice

                    dist = cv_lineEquation(mc[median1], mc[median2],
                        mc[outlier]); // Get the Perpendicular distance of the outlier from the longest side	
                    Tuple<float,Int16> res;
                    res= cv_lineSlope(mc[median1], mc[median2]); // Also calculate the slope of the longest side
                    slope = res.Item1;
                    align = res.Item2;
                    // Now that we have the orientation of the line formed median1 & median2 and we also have the position of the outlier w.r.t. the line
                    // Determine the 'right' and 'bottom' markers

                    if (align == 0)
                    {
                        bottom = median1;
                        right = median2;
                    }
                    else if (slope < 0 && dist < 0) // Orientation - North
                    {
                        bottom = median1;
                        right = median2;
                        orientation = CV_QR_NORTH;
                    }
                    else if (slope > 0 && dist < 0) // Orientation - East
                    {
                        right = median1;
                        bottom = median2;
                        orientation = CV_QR_EAST;
                    }
                    else if (slope < 0 && dist > 0) // Orientation - South			
                    {
                        right = median1;
                        bottom = median2;
                        orientation = CV_QR_SOUTH;
                    }

                    else if (slope > 0 && dist > 0) // Orientation - West
                    {
                        bottom = median1;
                        right = median2;
                        orientation = CV_QR_WEST;
                    }


                    // To ensure any unintended values do not sneak up when QR code is not present
                    float area_top, area_right, area_bottom;

                    if (top < contours.Count && right < contours.Count && bottom < contours.Count &&
                        Imgproc.contourArea(contours[top]) > 10 && Imgproc.contourArea(contours[right]) > 10 &&
                        Imgproc.contourArea(contours[bottom]) > 10)
                    {
                        List<Point> L = new List<Point>();
                        List<Point> M = new List<Point>();
                        List<Point> O = new List<Point>();
                        List<Point> tempL = new List<Point>();
                        List<Point> tempM = new List<Point>();
                        List<Point> tempO = new List<Point>();
                        Point N = new Point();
                        Mat src_mat =
                            new Mat(4, 1,
                                CvType.CV_32FC2); // src - Source Points basically the 4 end co-ordinates of the overlay image
                        Mat dst_mat =
                            new Mat(4, 1, CvType.CV_32FC2); // dst - Destination Points to transform overlay image	

                        Mat warp_matrix;

                        cv_getVertices(contours, top, slope, tempL);
                        cv_getVertices(contours, right, slope, tempM);
                        cv_getVertices(contours, bottom, slope, tempO);

                        cv_updateCornerOr(orientation, tempL,
                            L); // Re-arrange marker corners w.r.t orientation of the QR code
                        cv_updateCornerOr(orientation, tempM,
                            M); // Re-arrange marker corners w.r.t orientation of the QR code
                        cv_updateCornerOr(orientation, tempO,
                            O); // Re-arrange marker corners w.r.t orientation of the QR code

                        N = getIntersectionPoint(M[1], M[2], O[3], O[2]);


                        src_mat.put(0, 0, L[0].x, L[0].y);
                        src_mat.put(1, 0, M[1].x, M[1].y);
                        src_mat.put(2, 0, N.x, N.y);
                        src_mat.put(3, 0, O[3].x, O[3].y);

                        dst_mat.put(0, 0, 0, 0);
                        dst_mat.put(1, 0, qr.cols(), 0);
                        dst_mat.put(2, 0, qr.cols(), qr.rows());
                        dst_mat.put(3, 0, 0, qr.rows());

                        if (src_mat.rows() == 4 && dst_mat.rows() == 4
                        ) // Failsafe for WarpMatrix Calculation to have only 4 Points with src and dst
                        {
                            // TODO::: here we need to apply some help here. The output is way to big i think!.
                            warp_matrix = Imgproc.getPerspectiveTransform(src_mat, dst_mat);
                            Imgproc.warpPerspective(_rawImage, qr_raw, warp_matrix,
                                new Size(Convert.ToDouble(qr.cols()), Convert.ToDouble(qr.rows())));
                            Core.copyMakeBorder(qr_raw, qr, 10, 10, 10, 10, Core.BORDER_CONSTANT,
                                new Scalar(1, 1, 1));

                            Imgproc.cvtColor(qr, qr_gray, Imgproc.COLOR_RGB2GRAY);
                            Imgproc.threshold(qr_gray, qr_thres, 127, 255, Imgproc.THRESH_BINARY);

                            //threshold(qr_gray, qr_thres, 0, 255, CV_THRESH_OTSU);
                            //for( int d=0 ; d < 4 ; d++){	src.pop_back(); dst.pop_back(); }
                        }

                        //Draw contours on the image
                        Imgproc.drawContours(_rawImage, contours, top, new Scalar(255, 200, 0), 2, 8, hierarchy, 0,
                            new Point());
                        Imgproc.drawContours(_rawImage, contours, right, new Scalar(0, 0, 255), 2, 8, hierarchy, 0,
                            new Point());
                        Imgproc.drawContours(_rawImage, contours, bottom, new Scalar(255, 0, 100), 2, 8, hierarchy, 0,
                            new Point());

                        // Insert Debug instructions here
                        if (DBG == 1)
                        {
                            // Debug Prints
                            // Visualizations for ease of understanding
                            if (slope > 5)
                                Imgproc.circle(traces, new Point(10, 20), 5, new Scalar(0, 0, 255), -1, 8, 0);
                            else if (slope < -5)
                                Imgproc.circle(traces, new Point(10, 20), 5, new Scalar(255, 255, 255), -1, 8, 0);

                            // Draw contours on Trace image for analysis	
                            Imgproc.drawContours(traces, contours, top, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                                new Point());
                            Imgproc.drawContours(traces, contours, right, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                                new Point());
                            Imgproc.drawContours(traces, contours, bottom, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                                new Point());

                            // Draw points (4 corners) on Trace image for each Identification marker	
                            Imgproc.circle(traces, L[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, L[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, L[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                            Imgproc.circle(traces, L[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                            Imgproc.circle(traces, M[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, M[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, M[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                            Imgproc.circle(traces, M[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                            Imgproc.circle(traces, O[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, O[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                            Imgproc.circle(traces, O[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                            Imgproc.circle(traces, O[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                            // Draw point of the estimated 4th Corner of (entire) QR Code
                            Imgproc.circle(traces, N, 2, new Scalar(255, 255, 255), -1, 8, 0);

                            // Draw the lines used for estimating the 4th Corner of QR Code
                            Imgproc.line(traces, M[1], N, new Scalar(0, 0, 255), 1, 8, 0);
                            Imgproc.line(traces, O[3], N, new Scalar(0, 0, 255), 1, 8, 0);


                            //                    // Show the Orientation of the QR Code wrt to 2D Image Space
                            //                    int fontFace = Core.FONT_HERSHEY_PLAIN;
                            //
                            //                    if (orientation == CV_QR_NORTH)
                            //                    {
                            //                        putText(traces, "NORTH", Point(20, 30), fontFace, 1, Scalar(0, 255, 0), 1, 8);
                            //                    }
                            //                    else if (orientation == CV_QR_EAST)
                            //                    {
                            //                        putText(traces, "EAST", Point(20, 30), fontFace, 1, Scalar(0, 255, 0), 1, 8);
                            //                    }
                            //                    else if (orientation == CV_QR_SOUTH)
                            //                    {
                            //                        putText(traces, "SOUTH", Point(20, 30), fontFace, 1, Scalar(0, 255, 0), 1, 8);
                            //                    }
                            //                    else if (orientation == CV_QR_WEST)
                            //                    {
                            //                        putText(traces, "WEST", Point(20, 30), fontFace, 1, Scalar(0, 255, 0), 1, 8);
                            //                    }

                            // Debug Prints
                        }
                    }

                    resultCallback(true);
                }
                else
                {
                    resultCallback(false);
                }
            }
        );
    }

    private bool notAsync()
    {
        Debug.Log("Something! MORE!");
        //        traces = new Scalar(0, 0, 0);
        // create Mat with 100x100 all zeros
        qr_raw = new Mat(100, 100, CvType.CV_8UC3, Scalar.all(0));
        qr = new Mat(100, 100, CvType.CV_8UC3, Scalar.all(0));
        qr_gray = new Mat(100, 100, CvType.CV_8UC1, Scalar.all(0));
        qr_thres = new Mat(100, 100, CvType.CV_8UC1, Scalar.all(0));

        Debug.Log("2");
        Imgproc.cvtColor(_rawImage, gray,
            Imgproc.COLOR_RGB2GRAY); // Convert Image captured from Image Input to GrayScale	
        Imgproc.Canny(gray, edges, 100, 200, 3,
            true); // Apply Canny edge detection on the gray image ( Not sure true)


        // Find contours with hierarchy:
        List<MatOfPoint> contours = new List<MatOfPoint>();
        Mat hierarchy = new Mat();
        Imgproc.findContours(edges, contours, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);

        Debug.Log("3");
        mark = 0; // Reset all detected marker count for this frame

        // Get Moments for all Contours and the mass centers
        List<Moments> mu = new List<Moments>();
        List<Point> mc = new List<Point>();

        for (int i = 0; i < contours.Count; i++)
        {
            mu.Add(Imgproc.moments(contours[i], false));
            mc.Add(new Point(mu[i].m10 / mu[i].m00, mu[i].m01 / mu[i].m00));
        }

        Debug.Log("4");
        // Start processing the contour 
        // Find Three repeatedly enclosed contours A,B,C
        // NOTE: 1. Contour enclosing other contours is assumed to be the three Alignment markings of the QR code.
        // 2. Alternately, the Ratio of areas of the "concentric" squares can also be used for identifying base Alignment markers.
        // The below demonstrates the first method
        MatOfPoint2f pointsseq = new MatOfPoint2f(); //used to save the approximated sides of each contour
        for (int i = 0; i < contours.Count; i++)
        {
            //Find the approximated polygon of the contour we are examining

            Imgproc.approxPolyDP(new MatOfPoint2f(contours[i].toArray()), pointsseq,
                Imgproc.arcLength(new MatOfPoint2f(contours[i].toArray()), true) * 0.02, true);
            if (pointsseq.toList().Count == 4) // only quadrilaterals contours are examined
            {
                int k = i;
                int c = 0;

                // Checking if child is the inner contour
                while (hierarchy.get(0, k)[3] != -1.0)
                {
                    k = Convert.ToInt32(hierarchy.get(0, k)[3]);
                    c = c + 1;
                }

                if (hierarchy.get(0, k)[3] != -1.0)
                {
                    c = c + 1;
                }

                if (c >= 5)
                {
                    if (mark == 0) A = i;
                    else if (mark == 1) B = i; // i.e., A is already found, assign current contour to B
                    else if (mark == 2) C = i; // i.e., A and B are already found, assign current contour to C
                    mark = mark + 1;
                }
            }
        }

        Debug.Log("5");
        if (mark >= 3) // Ensure we have (atleast 3; namely A,B,C) 'Alignment Markers' discovered
        {
            Debug.Log("6: in mark");
            // We have found the 3 markers for the QR code; Now we need to determine which of them are 'top', 'right' and 'bottom' markers

            // Determining the 'top' marker
            // Vertex of the triangle NOT involved in the longest side is the 'outlier'

            AB = cv_distance(mc[A], mc[B]);
            BC = cv_distance(mc[B], mc[C]);
            CA = cv_distance(mc[C], mc[A]);

            if (AB > BC && AB > CA)
            {
                outlier = C;
                median1 = A;
                median2 = B;
            }
            else if (CA > AB && CA > BC)
            {
                outlier = B;
                median1 = A;
                median2 = C;
            }
            else if (BC > AB && BC > CA)
            {
                outlier = A;
                median1 = B;
                median2 = C;
            }

            top = outlier; // The obvious choice

            dist = cv_lineEquation(mc[median1], mc[median2],
                mc[outlier]); // Get the Perpendicular distance of the outlier from the longest side			
            Tuple<float,Int16> res;
            res = cv_lineSlope(mc[median1], mc[median2]); // Also calculate the slope of the longest side
            slope = res.Item1;
            align = res.Item2;
            // Now that we have the orientation of the line formed median1 & median2 and we also have the position of the outlier w.r.t. the line
            // Determine the 'right' and 'bottom' markers

            if (align == 0)
            {
                bottom = median1;
                right = median2;
            }
            else if (slope < 0 && dist < 0) // Orientation - North
            {
                bottom = median1;
                right = median2;
                orientation = CV_QR_NORTH;
            }
            else if (slope > 0 && dist < 0) // Orientation - East
            {
                right = median1;
                bottom = median2;
                orientation = CV_QR_EAST;
            }
            else if (slope < 0 && dist > 0) // Orientation - South			
            {
                right = median1;
                bottom = median2;
                orientation = CV_QR_SOUTH;
            }

            else if (slope > 0 && dist > 0) // Orientation - West
            {
                bottom = median1;
                right = median2;
                orientation = CV_QR_WEST;
            }


            // To ensure any unintended values do not sneak up when QR code is not present
            float area_top, area_right, area_bottom;

            if (top < contours.Count && right < contours.Count && bottom < contours.Count &&
                Imgproc.contourArea(contours[top]) > 10 && Imgproc.contourArea(contours[right]) > 10 &&
                Imgproc.contourArea(contours[bottom]) > 10)
            {
                List<Point> L = new List<Point>();
                List<Point> M = new List<Point>();
                List<Point> O = new List<Point>();
                List<Point> tempL = new List<Point>();
                List<Point> tempM = new List<Point>();
                List<Point> tempO = new List<Point>();
                Point N = new Point();
                Mat src_mat =
                    new Mat(4, 1,
                        CvType.CV_32FC2); // src - Source Points basically the 4 end co-ordinates of the overlay image
                Mat dst_mat =
                    new Mat(4, 1, CvType.CV_32FC2); // dst - Destination Points to transform overlay image	

                Mat warp_matrix;

                cv_getVertices(contours, top, slope, tempL);
                cv_getVertices(contours, right, slope, tempM);
                cv_getVertices(contours, bottom, slope, tempO);

                cv_updateCornerOr(orientation, tempL,
                    L); // Re-arrange marker corners w.r.t orientation of the QR code
                cv_updateCornerOr(orientation, tempM,
                    M); // Re-arrange marker corners w.r.t orientation of the QR code
                cv_updateCornerOr(orientation, tempO,
                    O); // Re-arrange marker corners w.r.t orientation of the QR code

                N = getIntersectionPoint(M[1], M[2], O[3], O[2]);


                src_mat.put(0, 0, L[0].x, L[0].y);
                src_mat.put(1, 0, M[1].x, M[1].y);
                src_mat.put(2, 0, N.x, N.y);
                src_mat.put(3, 0, O[3].x, O[3].y);

                dst_mat.put(0, 0, 0, 0);
                dst_mat.put(1, 0, qr.cols(), 0);
                dst_mat.put(2, 0, qr.cols(), qr.rows());
                dst_mat.put(3, 0, 0, qr.rows());

                if (src_mat.rows() == 4 && dst_mat.rows() == 4
                ) // Failsafe for WarpMatrix Calculation to have only 4 Points with src and dst
                {
                    warp_matrix = Imgproc.getPerspectiveTransform(src_mat, dst_mat);
                    Imgproc.warpPerspective(_rawImage, qr_raw, warp_matrix,
                        new Size(Convert.ToDouble(qr.cols()), Convert.ToDouble(qr.rows())));
                    Core.copyMakeBorder(qr_raw, qr, 10, 10, 10, 10, Core.BORDER_CONSTANT,
                        new Scalar(255, 255, 255));

                    Imgproc.cvtColor(qr, qr_gray, Imgproc.COLOR_RGB2GRAY);
                    Imgproc.threshold(qr_gray, qr_thres, 127, 255, Imgproc.THRESH_BINARY);

                    //threshold(qr_gray, qr_thres, 0, 255, CV_THRESH_OTSU);
                    //for( int d=0 ; d < 4 ; d++){	src.pop_back(); dst.pop_back(); }
                }

                //Draw contours on the image
                Imgproc.drawContours(_rawImage, contours, top, new Scalar(255, 200, 0), 2, 8, hierarchy, 0,
                    new Point());
                Imgproc.drawContours(_rawImage, contours, right, new Scalar(0, 0, 255), 2, 8, hierarchy, 0,
                    new Point());
                Imgproc.drawContours(_rawImage, contours, bottom, new Scalar(255, 0, 100), 2, 8, hierarchy, 0,
                    new Point());
                Debug.Log("4");
                // Insert Debug instructions here
                if (DBG == 1)
                {
                    // Debug Prints
                    // Visualizations for ease of understanding
                    if (slope > 5)
                        Imgproc.circle(traces, new Point(10, 20), 5, new Scalar(0, 0, 255), -1, 8, 0);
                    else if (slope < -5)
                        Imgproc.circle(traces, new Point(10, 20), 5, new Scalar(255, 255, 255), -1, 8, 0);

                    // Draw contours on Trace image for analysis	
                    Imgproc.drawContours(traces, contours, top, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                        new Point());
                    Imgproc.drawContours(traces, contours, right, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                        new Point());
                    Imgproc.drawContours(traces, contours, bottom, new Scalar(255, 0, 100), 1, 8, hierarchy, 0,
                        new Point());

                    // Draw points (4 corners) on Trace image for each Identification marker	
                    Imgproc.circle(traces, L[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, L[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, L[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                    Imgproc.circle(traces, L[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                    Imgproc.circle(traces, M[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, M[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, M[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                    Imgproc.circle(traces, M[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                    Imgproc.circle(traces, O[0], 2, new Scalar(255, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, O[1], 2, new Scalar(0, 255, 0), -1, 8, 0);
                    Imgproc.circle(traces, O[2], 2, new Scalar(0, 0, 255), -1, 8, 0);
                    Imgproc.circle(traces, O[3], 2, new Scalar(128, 128, 128), -1, 8, 0);

                    // Draw point of the estimated 4th Corner of (entire) QR Code
                    Imgproc.circle(traces, N, 2, new Scalar(255, 255, 255), -1, 8, 0);

                    // Draw the lines used for estimating the 4th Corner of QR Code
                    Imgproc.line(traces, M[1], N, new Scalar(0, 0, 255), 1, 8, 0);
                    Imgproc.line(traces, O[3], N, new Scalar(0, 0, 255), 1, 8, 0);
                }
            }

            Debug.Log("10");
        }
        else
        {
            Debug.Log("11");
        }

        Debug.Log("end");
        return true;
    }


    /// <summary>
    /// Releases all resource.
    /// </summary>
    private void Dispose()
    {
        
        if ( _capture != null)
        {
            _capture.release();
            _capture.Dispose();
            _capture = null;
        }

    }

    /// <summary>
    /// Raises the destroy event.
    /// </summary>
    void OnDestroy()
    {
        Dispose();
    }

    // =============================== Helping methods ================================
    /// <summary>
    /// Check if everything is alright.
    /// </summary>
    /// <param name="image">This is the raw image we grabbed if we return false.</param>
    private bool ShouldBail()
    {
        if (_setupError)
        {
            return true;
        }

        // don't update each frame
//        if (_frameCounter < FRAME_UPDATE_COUNTER)
//        {
//            _frameCounter++;
//            return true;
//        }

        _frameCounter = 0;

        if (!CheckVideoCapture())
        {
            return true;
        }

        if (!_capture.read(_rawImage))
        {
            Debug.Log(" ERR: Unable find input Video source.");
            _setupError = true;
            return true;
        }

        return false;
    }


    private bool CheckVideoCapture()
    {
        if(_capture == null)
        {
            _capture = new VideoCapture(0);
        }
        if (!_capture.isOpened())
        {
            _capture = new VideoCapture(0);
            // Checking for faults
            if (!_capture.isOpened())
            {
                _setupError = true;
                return false;
            }
        }

        return true;
    }

    // Routines used in update

    // Function: Routine to get Distance between two points
    // Description: Given 2 points, the function returns the distance

    float cv_distance(Point P, Point Q)
    {
        return (float) Math.Sqrt(Math.Pow(Math.Abs(P.x - Q.x), 2) + Math.Pow(Math.Abs(P.y - Q.y), 2));
    }


    // Function: Perpendicular Distance of a Point J from line formed by Points L and M; Equation of the line ax+by+c=0
    // Description: Given 3 points, the function derives the line quation of the first two points,
    //	  calculates and returns the perpendicular distance of the the 3rd point from this line.

    float cv_lineEquation(Point L, Point M, Point J)
    {
        float a, b, c, pdist;

        a = (float) -((M.y - L.y) / (M.x - L.x));
        b = 1.0f;
        c = (float) ((((M.y - L.y) / (M.x - L.x)) * L.x) - L.y);

        // Now that we have a, b, c from the equation ax + by + c, time to substitute (x,y) by values from the Point J

        pdist = (float) ((a * J.x + (b * J.y) + c) / Math.Sqrt((a * a) + (b * b)));
        return pdist;
    }

    // Function: Slope of a line by two Points L and M on it; Slope of line, S = (x1 -x2) / (y1- y2)
    // Description: Function returns the slope of the line formed by given 2 points, the alignement flag
    //	  indicates the line is vertical and the slope is infinity.
    Tuple<float, Int16> cv_lineSlope(Point L, Point M)
    {
        
        float dx, dy;
        dx = (float) (M.x - L.x);
        dy = (float) (M.y - L.y);

        if (dy != 0)
        {
            return new Tuple<float, Int16>((dy / dx), 1);
        }
        else // Make sure we are not dividing by zero; so use 'alignement' flag
        {
            return new Tuple<float, Int16>(0.0f, 0);
        }
    }


    // Function: Routine to calculate 4 Corners of the Marker in Image Space using Region partitioning
    // Theory: OpenCV Contours stores all points that describe it and these points lie the perimeter of the polygon.
    //	The below function chooses the farthest points of the polygon since they form the vertices of that polygon,
    //	exactly the points we are looking for. To choose the farthest point, the polygon is divided/partitioned into
    //	4 regions equal regions using bounding box. Distance algorithm is applied between the centre of bounding box
    //	every contour point in that region, the farthest point is deemed as the vertex of that region. Calculating
    //	for all 4 regions we obtain the 4 corners of the polygon ( - quadrilateral).
    // tl = top left,
    // br = button right
    // w = middle between A and B
    // x = middle between B and C
    void cv_getVertices(List<MatOfPoint> contours, int c_id, float slope, List<Point> quad)
    {
        Rect box = Imgproc.boundingRect(contours[c_id]);
        Point A = box.tl();
        Point B = new Point(box.br().x, box.tl().y);
        Point C = box.br();
        Point D = new Point(box.tl().x, box.br().y);
        Point W = new Point((A.x + B.x) / 2, A.y);
        Point X = new Point(B.x, (B.y + C.y) / 2);
        Point Y = new Point((C.x + D.x) / 2, C.y);
        Point Z = new Point(D.x, (D.y + A.y) / 2);

        Point M0 = new Point();
        Point M1 = new Point();
        Point M2 = new Point();
        Point M3 = new Point();

        float[] dmax = new float[4];

        dmax[0] = 0.0f;
        dmax[1] = 0.0f;
        dmax[2] = 0.0f;
        dmax[3] = 0.0f;

        float pd1 = 0.0f;
        float pd2 = 0.0f;

        if (slope > 5 || slope < -5)
        {
            for (int i = 0; i < contours[c_id].toList().Count; i++)
            {
                pd1 = cv_lineEquation(C, A, contours[c_id].toList()[i]); // Position of point w.r.t the diagonal AC 
                pd2 = cv_lineEquation(B, D, contours[c_id].toList()[i]); // Position of point w.r.t the diagonal BD

                if ((pd1 >= 0.0) && (pd2 > 0.0))
                {
                    cv_updateCorner(contours[c_id].toList()[i], W, dmax[1], M1);
                }
                else if ((pd1 > 0.0) && (pd2 <= 0.0))
                {
                    cv_updateCorner(contours[c_id].toList()[i], X, dmax[2], M2);
                }
                else if ((pd1 <= 0.0) && (pd2 < 0.0))
                {
                    cv_updateCorner(contours[c_id].toList()[i], Y, dmax[3], M3);
                }
                else if ((pd1 < 0.0) && (pd2 >= 0.0))
                {
                    cv_updateCorner(contours[c_id].toList()[i], Z, dmax[0], M0);
                }
                else
                    continue;
            }
        }
        else
        {
            float halfx = Convert.ToInt32((A.x + B.x) / 2);
            float halfy = Convert.ToInt32((A.y + D.y) / 2);
            // Cannot use tuples because unity only supports c# v.6
            Tuple<float,Point> result;
            for (int i = 0; i < contours[c_id].toList().Count; i++)
            {
                if ((contours[c_id].toList()[i].x < halfx) && (contours[c_id].toList()[i].y <= halfy))
                {
                    result = cv_updateCorner(contours[c_id].toList()[i], C, dmax[2], M0);
                    dmax[2] = result.Item1;
                    M0 = result.Item2;
                }
                else if ((contours[c_id].toList()[i].x >= halfx) && (contours[c_id].toList()[i].y < halfy))
                {
                    result = cv_updateCorner(contours[c_id].toList()[i], D, dmax[3], M1);
                    dmax[3] = result.Item1;
                    M1 = result.Item2;
                }
                else if ((contours[c_id].toList()[i].x > halfx) && (contours[c_id].toList()[i].y >= halfy))
                {
                    result = cv_updateCorner(contours[c_id].toList()[i], A, dmax[0], M2);
                    dmax[0] = result.Item1;
                    M2 = result.Item2;
                }
                else if ((contours[c_id].toList()[i].x <= halfx) && (contours[c_id].toList()[i].y > halfy))
                {
                    result = cv_updateCorner(contours[c_id].toList()[i], B, dmax[1], M3);
                    dmax[1] = result.Item1;
                    M3 = result.Item2;
                }
            }
        }

        quad.Add(M0);
        quad.Add(M1);
        quad.Add(M2);
        quad.Add(M3);
    }

    // Function: Compare a point if it more far than previously recorded farthest distance
    // Description: Farthest Point detection using reference point and baseline distance
    Tuple<float,Point> cv_updateCorner(Point P, Point p, float baseline, Point corner)
    {
        float temp_dist;
        temp_dist = cv_distance(P, p);

        if (temp_dist > baseline)
        {
            baseline = temp_dist; // The farthest distance is the new baseline
            corner = P; // P is now the farthest point
        }

        return new Tuple<float, Point>(baseline,corner);
    }

    // Function: Sequence the Corners wrt to the orientation of the QR Code
    void cv_updateCornerOr(int orientation, List<Point> IN, List<Point> OUT)
    {
        Point M0 = new Point();
        Point M1 = new Point();
        Point M2 = new Point();
        Point M3 = new Point();
        if (orientation == CV_QR_NORTH)
        {
            M0 = IN[0];
            M1 = IN[1];
            M2 = IN[2];
            M3 = IN[3];
        }
        else if (orientation == CV_QR_EAST)
        {
            M0 = IN[1];
            M1 = IN[2];
            M2 = IN[3];
            M3 = IN[0];
        }
        else if (orientation == CV_QR_SOUTH)
        {
            M0 = IN[2];
            M1 = IN[3];
            M2 = IN[0];
            M3 = IN[1];
        }
        else if (orientation == CV_QR_WEST)
        {
            M0 = IN[3];
            M1 = IN[0];
            M2 = IN[1];
            M3 = IN[2];
        }

        OUT.Add(M0);
        OUT.Add(M1);
        OUT.Add(M2);
        OUT.Add(M3);
    }

    // Function: Get the Intersection Point of the lines formed by sets of two points
    Point getIntersectionPoint(Point a1, Point a2, Point b1, Point b2)
    {
        Point p = a1;
        Point q = b1;
        Point r = new Point(a2.x - a1.x, a2.y - a1.y);
        Point s = new Point(b2.x - b1.x, b2.y - b1.y);

        float t = cross(q - p, s) / cross(r, s);

        return p + t * r;
    }

    float cross(Point v1, Point v2)
    {
        return (float) (v1.x * v2.y - v1.y * v2.x);
    }
}