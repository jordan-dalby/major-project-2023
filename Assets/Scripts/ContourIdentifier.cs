using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using UnityEngine;


// REPORT WRITING TIP: Talk about Contour identification modes, which ones work and which ones don't
// https://docs.opencv.org/4.x/d3/dc0/group__imgproc__shape.html#ga4303f45752694956374734a03c54d5ff
/// <summary>
/// Class for identifying contours in an image
/// </summary>
public class ContourIdentifier
{

    /// <summary>
    /// The mat to identify the contours on
    /// </summary>
    private Mat mat;
    /// <summary>
    /// The piece found when identifying contours
    /// </summary>
    private Piece pieceFound;

    /// <summary>
    /// The id to give to the next piece
    /// </summary>
    private int id;

    /// <summary>
    /// Process the contour
    /// </summary>
    /// <param name="inputMat">The piece mat</param>
    /// <param name="contour">The contour of the piece</param>
    /// <param name="piece">The piece</param>
    public void ProcessContour(Mat inputMat, MatOfPoint contour, Piece piece = null)
    {
        pieceFound = null;

        mat = inputMat;
        (MatOfPoint, Mat) newContour = PreprocessContour(contour);
        if (newContour.Item1 == null)
            return;

        PostprocessContour(newContour.Item1, newContour.Item2, piece);
    }

    /// <summary>
    /// Find convex hull and the contours of the piece
    /// </summary>
    /// <param name="contour">The input contour</param>
    /// <returns>A new contour and a cropped piece mat</returns>
    public (MatOfPoint, Mat) PreprocessContour(MatOfPoint contour)
    {
        // create a list of ints that represent the index of points in the 'contour' MatOfPoint input (a list)
        MatOfInt hullInt = new MatOfInt();
        // calculate the convex hull points
        Imgproc.convexHull(contour, hullInt);

        // create a list of points from the contours
        List<Point> pointMatList = contour.toList();
        // create a list of integers from the hull indexes
        List<int> hullIntList = hullInt.toList();
        // crate a point list to hold all of the hull points
        List<Point> hullPointList = new List<Point>();

        // loop over each index in the hullInt list
        for (int j = 0; j < hullInt.toList().Count; j++)
        {
            // add the point along the contour that is at index j
            hullPointList.Add(pointMatList[hullIntList[j]]);
        }

        Mat croppedPieceMat = CropBoundingRect(mat, hullPointList, contour);
        if (croppedPieceMat == null)
            return (null, null);

        ChromaKey chromaKey = new ChromaKey();
        Mat chromaKeyedPieceMat = chromaKey.Key(croppedPieceMat, false, false);

        // Find contours in new image
        List<MatOfPoint> matOfPoints = new List<MatOfPoint>();
        Mat hierarchy = new Mat(chromaKeyedPieceMat.height(), chromaKeyedPieceMat.width(), CvType.CV_8UC3);

        // find contours
        Imgproc.findContours(chromaKeyedPieceMat, matOfPoints, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_NONE);

        if (matOfPoints.Count == 0)
        {
            return (null, null);
        }

        double contourArea = Imgproc.contourArea(matOfPoints[0], false);
        if (contourArea < 500d)
        {
            return (null, null);
        }

        return (matOfPoints[0], croppedPieceMat);
    }

    /// <summary>
    /// Crop the image so that it just fits in the mat
    /// </summary>
    /// <param name="baseMat">The input mat</param>
    /// <param name="hullPointList">The input hull points</param>
    /// <param name="contour">The contours of the piece</param>
    /// <returns></returns>
    public Mat CropBoundingRect(Mat baseMat, List<Point> hullPointList, MatOfPoint contour)
    {
        // create a temporary Mat by converting the points on the convex hull into a Mat
        Mat tempMat = Converters.vector_Point2f_to_Mat(hullPointList);

        // create a copy of the input Mat (the Mat that contains every piece)
        Mat pieceMat = new Mat(baseMat.rows(), baseMat.cols(), CvType.CV_8UC4);
        baseMat.copyTo(pieceMat);
        // convert the Mat to the BGRA colour space
        Imgproc.cvtColor(pieceMat, pieceMat, Imgproc.COLOR_BGR2BGRA);

        // create a new piece mat by cropping around the contour of the piece, this removes any unwanted surrounding pieces
        Mat pieceMask = new Mat(pieceMat.rows(), pieceMat.cols(), CvType.CV_8UC1);
        pieceMask.setTo(new Scalar(0));
        Imgproc.drawContours(pieceMask, new List<MatOfPoint>() { contour }, -1, new Scalar(255), -1);

        // create a destination for the cropped out piece, with a solid background for chroma keying
        Mat pieceMatWithMask = new Mat(pieceMat.rows(), pieceMat.cols(), CvType.CV_8UC4);
        pieceMatWithMask.setTo(ChromaKey.low);

        // apply the mask to the piece to single it out
        Core.bitwise_and(pieceMat, pieceMat, pieceMatWithMask, pieceMask);

        // generate a bounding rect around the hull points
        OpenCVForUnity.CoreModule.Rect rect = Imgproc.boundingRect(tempMat);
        // allow 1 pixel around the rect, this is so that when we correct the rotation BORDER_REPLICATE can work best,
        // by replicating only the perfect green background of the image
        rect.width += 2;
        rect.height += 2;
        rect.x -= 1;
        rect.y -= 1;

        if (rect.width < 50 || rect.height < 50)
        {
            // if less than min size, don't bother with it since it's likely artefacting left over from chroma keying,
            // and not a piece
            return null;
        }

        // crop the mat around the bounding rect
        Mat croppedPieceMat = null;
        try
        {
            croppedPieceMat = new Mat(pieceMatWithMask, rect);
        }
        catch (Exception)
        {
            Debug.LogWarning("Pieces couldn't be separated. Try moving them further from each other.");
        }

        return croppedPieceMat;
    }

    /// <summary>
    /// Create a new convex hull around the contour and identify a piece within it
    /// </summary>
    /// <param name="contour">The input contour</param>
    /// <param name="croppedPieceMat">The cropped piece mat</param>
    /// <param name="piece">The piece found</param>
    public void PostprocessContour(MatOfPoint contour, Mat croppedPieceMat, Piece piece)
    {
        // create new convex hull around new contour
        MatOfInt hullInt = new MatOfInt();
        Imgproc.convexHull(contour, hullInt);

        List<Point> pointMatList = contour.toList();
        List<int> hullIntList = hullInt.toList();
        List<Point> hullPointList = new List<Point>();

        for (int j = 0; j < hullInt.toList().Count; j++)
        {
            hullPointList.Add(pointMatList[hullIntList[j]]);
        }

        Mat tempMat = Converters.vector_Point2f_to_Mat(hullPointList);
        MatOfPoint2f pointsAsFloat = new MatOfPoint2f(tempMat);

        if (piece == null)
        { 
            piece = new Piece(contour, hullPointList, pointsAsFloat);
        }
        else
        {
            piece.contours = contour;
            piece.hullPoints = hullPointList;
            piece.pieceBounds = pointsAsFloat;
        }

        piece.pieceMat = croppedPieceMat;
        id++;
        piece.id = id;

        pieceFound = piece;
    }

    /// <summary>
    /// Get the piece identified
    /// </summary>
    /// <returns>The identified piece</returns>
    public Piece GetPiece()
    {
        return pieceFound;
    }

}
