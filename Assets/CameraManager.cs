using OpenCVForUnity.ArucoModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CameraManager : MonoBehaviour
{
    /*
    public Image previewImage;

    // image of the jigsaw
    public Texture2D texture;

    Mat dst, dst_norm, dst_norm_scaled;
    public Slider slider, one, two, three;

    [SerializeField]
    private List<Piece> pieces = new List<Piece>();

    public void TakePicture()
    {
        if (NativeCamera.CheckPermission() != NativeCamera.Permission.Granted)
        {
            NativeCamera.RequestPermission();
        }

        if (!NativeCamera.DeviceHasCamera())
        {
            Debug.LogError("Device does not have camera, cannot continue");
        }

        // src: https://github.com/yasirkula/UnityNativeCamera
        NativeCamera.Permission permission = NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                texture = NativeCamera.LoadImageAtPath(path, -1, false);
                if (texture == null)
                {
                    Debug.Log("Couldn't load image");
                    return;
                }

                texture = Resize(texture, 1360, 1024);

                // src: https://answers.unity.com/questions/938516/convert-texture2d-to-sprite.html
                UnityEngine.Rect sizeRect = new UnityEngine.Rect(0, 0, texture.width, texture.height);
                Sprite sprite = Sprite.Create(texture, sizeRect, Vector2.zero, 1);

                previewImage.sprite = sprite;

                IdentifyMarkers();
            }
        }, -1, true, NativeCamera.PreferredCamera.Rear);

    }

    // src: https://stackoverflow.com/questions/56949217/how-to-resize-a-texture2d-using-height-and-width
    Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new UnityEngine.Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }

    void IdentifyMarkers()
    {
        // params and identification dictionary
        DetectorParameters detectorParams = DetectorParameters.create();
        Dictionary dictionary = Aruco.getPredefinedDictionary(Aruco.DICT_6X6_250);

        // define new mat with texture dimensions
        Mat mat = new Mat(texture.height, texture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(texture, mat);

        // declare out variables for detectMarkers
        Mat ids = new Mat();
        List<Mat> corners = new List<Mat>();
        List<Mat> rejectedCorners = new List<Mat>();

        // detect the markers
        Aruco.detectMarkers(mat, dictionary, corners, ids, detectorParams, rejectedCorners);
        // draw markers onto mat
        Aruco.drawDetectedMarkers(mat, corners, ids, new Scalar(0, 255, 0));
        //Aruco.drawDetectedMarkers(mat, rejectedCorners, ids, new Scalar(0, 255, 0));

        Core.flip(mat, mat, 0);

        AddProcessImage(mat);

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
            {
                coords.Add(one);
            }
            if (twoDist < oneDist && twoDist < threeDist && twoDist < fourDist)
            {
                coords.Add(two);
            }
            if (threeDist < oneDist && threeDist < twoDist && threeDist < fourDist)
            {
                coords.Add(three);
            }            
            if (fourDist < oneDist && fourDist < twoDist && fourDist < threeDist)
            {
                coords.Add(four);
            }
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
            coords[1] + new Point(10, -10),
            coords[0] - new Point(10, 10),
            coords[3] + new Point(10, 10),
            coords[2] + new Point(-10, 10)
        };

        Texture2D textureWithPointsOn = drawnMarkersTexture;

        Circle(textureWithPointsOn, (int)points[0].x, (int)points[0].y, 5, Color.green);
        Circle(textureWithPointsOn, (int)points[1].x, (int)points[1].y, 5, Color.green);
        Circle(textureWithPointsOn, (int)points[2].x, (int)points[2].y, 5, Color.green);
        Circle(textureWithPointsOn, (int)points[3].x, (int)points[3].y, 5, Color.green);

        Circle(textureWithPointsOn, 0, 0, 5, Color.red);
        Circle(textureWithPointsOn, mat.cols(), 0, 5, Color.red);
        Circle(textureWithPointsOn, 0, mat.rows(), 5, Color.red);
        Circle(textureWithPointsOn, mat.cols(), mat.rows(), 5, Color.red);

        textureWithPointsOn.Apply();

        SaveToFile(textureWithPointsOn, "_points");

        Mat src_mat = new Mat(4, 1, CvType.CV_32FC2);
        Mat dst_mat = new Mat(4, 1, CvType.CV_32FC2);
        dst_mat.put(0, 0, 0.0, 0.0, mat.cols(), 0.0, 0.0, mat.rows(), mat.cols(), mat.rows());
        src_mat.put(0, 0, points[0].x, points[0].y, points[1].x, points[1].y, points[2].x, points[2].y, points[3].x, points[3].y);

        Mat perspectiveTransform = Imgproc.getPerspectiveTransform(src_mat, dst_mat);

        Imgproc.warpPerspective(mat, mat, perspectiveTransform, new Size(mat.cols(), mat.rows()));

        Texture2D perspectiveShifted = new Texture2D(mat.width(), mat.height());

        Utils.matToTexture2D(mat, perspectiveShifted, true, 1);

        texture = perspectiveShifted;

        AddProcessImage(mat);

        DetectCorners();
    }

    public void DetectCorners()
    {
        //Mat imgMat = new Mat(texture.height, texture.width, CvType.CV_8UC3);
        //Utils.texture2DToMat(texture, imgMat);

       

        
        FindConnectedPieces(chromaKeyed);

        // find edges
        Mat edges = new Mat(chromaKeyed.height(), chromaKeyed.width(), CvType.CV_8UC1);
        Imgproc.Canny(chromaKeyed, edges, 100, 200);

        Mat cannyEdges = new Mat(chromaKeyed.height(), chromaKeyed.width(), CvType.CV_8UC3);
        for (int x = 0; x < edges.width(); ++x)
        {
            for (int y = 0; y < edges.height(); ++y)
            {
                if (edges.get(y, x)[0] != 0)
                {
                    Imgproc.circle(cannyEdges, new Point(x, y), 3, new Scalar(0, 255, 00));
                }
            }
        }
        AddProcessImage(cannyEdges);

        List<MatOfPoint> matOfPoints = new List<MatOfPoint>();
        Mat hierarchy = new Mat(texture.height, texture.width, CvType.CV_8UC3);

        Imgproc.findContours(chromaKeyed, matOfPoints, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);

        Mat convexHullOutput = new Mat(imgMat.height(), imgMat.width(), CvType.CV_8UC3);
        Core.flip(imgMat, convexHullOutput, 0);

        for (int i = 0; i < matOfPoints.Count; i++)
        {
            MatOfPoint points = matOfPoints[i];

            MatOfInt hullInt = new MatOfInt();
            Imgproc.convexHull(points, hullInt);

            List<Point> pointMatList = points.toList();
            List<int> hullIntList = hullInt.toList();
            List<Point> hullPointList = new List<Point>();

            for (int j = 0; j < hullInt.toList().Count; j++)
            {
                hullPointList.Add(pointMatList[hullIntList[j]]);
            }

            MatOfInt4 defects = new MatOfInt4();

            // find convexity defects, the points furthest from the convex hull
            // two consecutive defects is indicitive of a tab, one is likely to be a blank
            // and none is simply an edge piece
            Imgproc.convexityDefects(points, hullInt, defects);

            Mat tempMat = Converters.vector_Point2f_to_Mat(hullPointList);
            MatOfPoint2f pointsAsFloat = new MatOfPoint2f(tempMat);

            List<int> defectsIntList = defects.toList();
            List<Point> farDefects = new List<Point>();

            for (int x = 0; x < defectsIntList.Count / 4; x++)
            {
                // find far defects and create a circle at their locations
                // we will use these defects to determine tabs and blanks
                Point p = pointMatList[defectsIntList[4 * x + 2]];

                double val = Imgproc.pointPolygonTest(pointsAsFloat, p, true);
                if (val == 0) // if the point is on the contour, then ignore it.
                    continue;
                if (val < 30f) // if the point is really close to the counter, then ignore it
                    continue;

                farDefects.Add(p);

                Imgproc.circle(convexHullOutput, p, 5, new Scalar(0, 0, 255, 255), -1);
            }
            
            pieces.Add(new Piece(i, pointMatList.ToList()));

            MatOfPoint hullPointMat = new MatOfPoint();
            hullPointMat.fromList(hullPointList);

            List<MatOfPoint> hullPoints = new List<MatOfPoint>();
            hullPoints.Add(hullPointMat);

            Imgproc.drawContours(convexHullOutput, hullPoints, -1, new Scalar(255, 0, 0), 2);
            Imgproc.cvtColor(convexHullOutput, convexHullOutput, Imgproc.COLOR_BGR2RGB);
        }

        AddProcessImage(convexHullOutput);
    }

    public void FindConnectedPieces(Mat mat)
    {
        pieces.Clear();

        Mat src = new Mat();
        mat.copyTo(src);

        Mat labels = new Mat();
        Mat stats = new Mat();
        Mat centroids = new Mat();

        int count = Imgproc.connectedComponentsWithStats(src, labels, stats, centroids);
        Debug.Log(count);

        // determine drawing color
        List<Scalar> colors = new List<Scalar>(count);
        colors.Add(new Scalar(0, 0, 0));
        for (int i = 1; i < count; ++i)
        {
            colors.Add(new Scalar(UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255), UnityEngine.Random.Range(0, 255)));
        }

        Mat output = new Mat(src.size(), CvType.CV_8UC3);
        for (int i = 0; i < labels.rows(); ++i)
        {
            for (int j = 0; j < labels.cols(); ++j)
            {
                Scalar color = colors[(int)labels.get(i, j)[0]];

                output.put(i, j, color.val[0], color.val[1], color.val[2]);
            }
        }

        // draw rectangle
        for (int i = 1; i < count; ++i)
        {
            int x = (int)stats.get(i, Imgproc.CC_STAT_LEFT)[0];
            int y = (int)stats.get(i, Imgproc.CC_STAT_TOP)[0];
            int height = (int)stats.get(i, Imgproc.CC_STAT_HEIGHT)[0];
            int width = (int)stats.get(i, Imgproc.CC_STAT_WIDTH)[0];

            OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect(x, y, width, height);

            Imgproc.rectangle(output, rect.tl(), rect.br(), new Scalar(0, 255, 0), 2);
        }

        // draw centroids
        for (int i = 1; i < count; ++i)
        {

            int x = (int)centroids.get(i, 0)[0];
            int y = (int)centroids.get(i, 1)[0];

            Imgproc.circle(output, new Point(x, y), 3, new Scalar(255, 0, 0), -1);
        }

        // draw index of label
        for (int i = 1; i < count; ++i)
        {

            int x = (int)stats.get(i, Imgproc.CC_STAT_LEFT)[0];
            int y = (int)stats.get(i, Imgproc.CC_STAT_TOP)[0];

            Imgproc.putText(output, "" + i, new Point(x + 5, y + 15), Imgproc.FONT_HERSHEY_COMPLEX, 0.5, new Scalar(255, 255, 0), 2);
        }

        AddProcessImage(output);
    }

    public void AddProcessImage(Mat mat, bool save = true)
    {
        Texture2D texture2 = new Texture2D(mat.cols(), mat.rows(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(mat, texture2, false);

        Sprite sprite = Sprite.Create(texture2, new UnityEngine.Rect(0, 0, texture2.width, texture2.height), Vector2.zero, 1);

        ProcessSprites.instance.AddProcessSprite(sprite);
        ProcessSprites.instance.ChangeIndex(1);

        if (save)
        {
            SaveToFile(texture2);
        }
    }

    public void SaveToFile(Texture2D t)
    {
        SaveToFile(t, "");
    }

    public void SaveToFile(Texture2D t, string ext)
    {

//#if PLATFORM_ANDROID // code can't run on android, so just return
        //return;
//#endif

        byte[] bytes = t.EncodeToPNG();
        var dirPath = Application.dataPath + "/SaveImages/";
        Debug.Log(dirPath);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + DateTime.Now.ToFileTimeUtc() + ext + ".png", bytes);
    }
    */
}


/*
// Detecting corners
// Detector parameters
int blockSize = (int)one.value;//7;
int apertureSize = (int)two.value;//5;
double k = three.value;//0.01901326;

Imgproc.cornerHarris(chromaKeyed, dst, blockSize, apertureSize, k, Core.BORDER_DEFAULT);

// Normalizing
Core.normalize(dst, dst_norm, 0, 255, Core.NORM_MINMAX, CvType.CV_32FC1, new Mat ());
Core.convertScaleAbs(dst_norm, dst_norm_scaled);

// Draw the output
Mat mat = new Mat();
dst_norm_scaled.copyTo(mat);

// Drawing a circle around corners
for (int j = 0; j < dst_norm.rows(); j++)
{
    for (int i = 0; i < dst_norm.cols(); i++)
    {
        if ((int)dst_norm.get(j, i)[0] > (slider.value/100f))//70.15243)
        {
            Imgproc.circle(mat, new Point(i, j), 1, new Scalar(255, 0, 0), 1, 8, 0);

            foreach (Piece piece in pieces)
            {
                if (piece.rect.contains(i, j))
                {
                    // if this corner is within the bounds of the piece then add the corner
                    piece.AddCorner(new Point(i, j));
                    break; // no need to keep looking
                }
            }
        }
    }
}
*/

/*
public void GenerateLargestRectangleForPieces()
{
    foreach (Piece piece in pieces)
    {
        Mat output = new Mat(texture.height, texture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(texture, output, false);

        Mat points = Converters.vector_Point_to_Mat(piece.Corners());
        MatOfPoint matOfPoint = new MatOfPoint(points); 

        MatOfInt hullInt = new MatOfInt();
        Imgproc.convexHull(matOfPoint, hullInt);

        List<Point> pointMatList = matOfPoint.toList();
        List<int> hullIntList = hullInt.toList();
        List<Point> hullPointList = new List<Point>();

        for (int j = 0; j < hullInt.toList().Count; j++)
        {
            hullPointList.Add(pointMatList[hullIntList[j]]);
        }

        MatOfPoint hullPointMat = new MatOfPoint();
        hullPointMat.fromList(hullPointList);

        List<MatOfPoint> hullPoints = new List<MatOfPoint>();
        hullPoints.Add(hullPointMat);

        Imgproc.drawContours(output, hullPoints, -1, new Scalar(255, 0, 0), 2);

        Imgproc.cvtColor(output, output, Imgproc.COLOR_BGR2RGB);

        AddProcessImage(output);
    }
}*/