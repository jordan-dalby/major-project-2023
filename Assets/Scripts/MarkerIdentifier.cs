using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarkerIdentifier
{

    //private Texture2D texture;
    private Mat mat;

    public MarkerIdentifier(Mat inputMat)
    {
        mat = inputMat;
    }

    public void IdentifyMarkers()
    {
        // identification dictionary
        Dictionary dictionary = Objdetect.getPredefinedDictionary((int)Objdetect.DICT_6X6_250);
        // create a new instance of the aruco detector and pass the dictionary we want to use
        ArucoDetector arucoDetector = new ArucoDetector(dictionary);

        // declare out variables for detectMarkers
        Mat ids = new Mat();
        // create a list for the corners of the ArUco markers
        List<Mat> corners = new List<Mat>();
        // create a list of rejected corners
        List<Mat> rejectedCorners = new List<Mat>();

        // detect the markers using the previously created instance
        arucoDetector.detectMarkers(mat, corners, ids, rejectedCorners);
        // draw markers onto mat
        Objdetect.drawDetectedMarkers(mat, corners, ids, new Scalar(0, 255, 0));

        Core.flip(mat, mat, 0);

        ProcessSprites.instance.AddProcessImage(mat, "MarkerIdentifier");

        Texture2D drawnMarkersTexture = new Texture2D(mat.width(), mat.height());
        Utils.matToTexture2D(mat, drawnMarkersTexture, true, 0);

        if (corners.Count < 4)
        {
            Debug.LogWarning("Couldn't perspective correct image, not enough ArUco markers found");
            return;
        }
        else if (corners.Count > 4)
        {
            Debug.LogWarning("More than 4 ArUco markers found, results may not be as expected");
        }
        else
        {
            Debug.Log("4 ArUco markers found");
        }

        Point middle = new Point(1360 / 2, 1024 / 2);

        // loop the corners, pick 0,0 index of mat, corners is not always presorted
        List<Point> coords = new List<Point>();
        foreach (Mat m in corners)
        {
            // find the closest corner to the centre so we can nicely crop the final perspective warped image to contain only the pieces
            Point one = new Point(m.get(0, 0)[0], m.get(0, 0)[1]);
            Point two = new Point(m.get(0, 1)[0], m.get(0, 1)[1]);
            Point three = new Point(m.get(0, 2)[0], m.get(0, 2)[1]);
            Point four = new Point(m.get(0, 3)[0], m.get(0, 3)[1]);

            float oneDist = Mathf.Sqrt(Mathf.Pow(Mathf.Abs((float)middle.x - (float)one.x), 2) + Mathf.Pow(Mathf.Abs((float)middle.y - (float)one.y), 2));
            float twoDist = Mathf.Sqrt(Mathf.Pow(Mathf.Abs((float)middle.x - (float)two.x), 2) + Mathf.Pow(Mathf.Abs((float)middle.y - (float)two.y), 2));
            float threeDist = Mathf.Sqrt(Mathf.Pow(Mathf.Abs((float)middle.x - (float)three.x), 2) + Mathf.Pow(Mathf.Abs((float)middle.y - (float)three.y), 2));
            float fourDist = Mathf.Sqrt(Mathf.Pow(Mathf.Abs((float)middle.x - (float)four.x), 2) + Mathf.Pow(Mathf.Abs((float)middle.y - (float)four.y), 2));

            if (oneDist < twoDist && oneDist < threeDist && oneDist < fourDist)
                coords.Add(one);

            if (twoDist < oneDist && twoDist < threeDist && twoDist < fourDist)
                coords.Add(two);

            if (threeDist < oneDist && threeDist < twoDist && threeDist < fourDist)
                coords.Add(three);

            if (fourDist < oneDist && fourDist < twoDist && fourDist < threeDist)
                coords.Add(four);
        }

        // sort coords into BL BR TL TR
        coords = coords.OrderByDescending(y => y.y).ToList();
        // first two values should be the bottom, second two should be the top
        // now we just have to filter each one for left and right
        Point temp;
        if (coords[0].x > coords[1].x)
        {
            // if the first item is larger then it's actually bottom right, and we need to swap
            temp = coords[1];
            coords[1] = coords[0];
            coords[0] = temp;
            // if the first x is greater than the second x, swap them, so we get BL BR
        }

        if (coords[2].x > coords[3].x)
        {
            // if the first top is larger than the second top then swap them
            temp = coords[3];
            coords[3] = coords[2];
            coords[2] = temp;
        }
        // 0BL 1BR 2TL 3TR

        List<Point> points = new List<Point>()
        {
            coords[1] + new Point(5, -5),
            coords[0] - new Point(5, 5),
            coords[3] + new Point(5, 5),
            coords[2] + new Point(-5, 5)
        };

        Imgproc.circle(mat, new Point(points[0].x, points[0].y), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(points[1].x, points[1].y), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(points[2].x, points[2].y), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(points[3].x, points[3].y), 5, new Scalar(0, 255, 0));

        Imgproc.circle(mat, new Point(0, 0), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(mat.cols(), 0), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(0, mat.rows()), 5, new Scalar(0, 255, 0));
        Imgproc.circle(mat, new Point(mat.cols(), mat.rows()), 5, new Scalar(0, 255, 0));

        // find dimensions of destination mat by calculating the difference between each corner
        // and selecting the largest value from width and height respectively
        double widthBottom = Mathf.Abs((float)(points[0].x - points[1].x));
        double widthTop = Mathf.Abs((float)(points[2].x - points[3].x));
        double width = widthBottom > widthTop ? widthBottom : widthTop;

        double heightLeft = Mathf.Abs((float)(points[2].y - points[0].y));
        double heightRight = Mathf.Abs((float)(points[1].y - points[3].y));
        double height = heightLeft > heightRight ? heightLeft : heightRight;

        // markers are a fixed distance between one another, so we can declare them here
        double knownWidth = 3780 / 2; // 3245
        double knownHeight = 2620 / 2; // 2219

        Mat src_mat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dst_mat = new Mat(4, 1, CvType.CV_32FC2);
        dst_mat.put(0, 0, 0.0, 0.0, knownWidth, 0.0, 0.0, knownHeight, knownWidth, knownHeight);
        src_mat.put(0, 0, points[0].x, points[0].y, points[1].x, points[1].y, points[2].x, points[2].y, points[3].x, points[3].y);

        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src_mat, dst_mat);

        Imgproc.warpPerspective(mat, mat, perspectiveTransform, new Size(knownWidth, knownHeight));

        Texture2D perspectiveShifted = new Texture2D(mat.width(), mat.height());

        Utils.matToTexture2D(mat, perspectiveShifted, true, 1);

        ProcessSprites.instance.AddProcessImage(mat, "MarkerIdentifier2");
    }

}