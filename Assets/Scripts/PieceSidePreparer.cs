using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Prepare each side of the piece by sampling colour information
/// </summary>
public class PieceSidePreparer
{

    /// <summary>
    /// The size of the colour sample (x, y)
    /// </summary>
    public static (int, int) sampleSize = (10, 10);

    /// <summary>
    /// The amount to move from the corners or largest defect before taking the colour sample
    /// </summary>
    private double adjustmentFromSide = 10d;

    /// <summary>
    /// Prepare a piece, individually prepares each side
    /// </summary>
    /// <param name="piece">The piece to be prepared</param>
    public void PreparePiece(Piece piece)
    {
        if (piece.sides == null)
            return;
        foreach (Side side in piece.sides)
        {
            PrepareSide(piece, side);
        }
    }

    /// <summary>
    /// Prepares a single side
    /// </summary>
    /// <param name="side">The side to prepare</param>
    public void PrepareSide(Piece piece, Side side)
    {
        // Edges don't need to be prepared, so we can just ignore them
        if (side.type == SideType.EDGE)
            return;
        
        // Rotate the points to be either north or south facing depending if it's a tab or blank
        side.rotatedPoints = PieceFunctions.TransformMidpointToCenter(PieceFunctions.RotatePointsToDirection(side.points, side.direction, side.type == SideType.TAB ? CardinalDirection.NORTH : CardinalDirection.SOUTH));

        // Get corners
        Point highestPoint = null;
        int index = -1;
        for (int i = 0; i < side.rotatedPoints.Count; i++)
        {
            Point pt = side.rotatedPoints[i];
            if (highestPoint == null || pt.y > highestPoint.y)
            {
                highestPoint = pt;
                index = i;
            }
        }

        // No elements in the sides point list
        if (index == -1)
        {
            Debug.LogWarning("No elements side points list, this is definitely a problem.");
            return;
        }

        Point cornerOne = side.points[0];
        Point cornerTwo = side.points[side.points.Count - 1];
        Point defectPoint = side.points[index];

        (int, int, int) minMax = PieceFunctions.ExtremePointsIndexes(side.points);
        if (side.direction == CardinalDirection.NORTH)
        {
            cornerOne = side.points[minMax.Item2];
            cornerTwo = side.points[minMax.Item3];
        }
        else if (side.direction == CardinalDirection.EAST)
        {
            cornerOne = side.points[minMax.Item3];
            cornerTwo = side.points[minMax.Item2];
        }
        else if (side.direction == CardinalDirection.SOUTH)
        {
            cornerOne = side.points[minMax.Item3];
            cornerTwo = side.points[minMax.Item2];
        }
        else if (side.direction == CardinalDirection.WEST)
        {
            cornerOne = side.points[minMax.Item2];
            cornerTwo = side.points[minMax.Item3];
        }

        // Move the points based on what they are and what side they're on
        Point cornerOneAdjustment = new Point();
        Point cornerTwoAdjustment = new Point();
        Point defectAdjustment = new Point();
        switch(side.direction)
        {
            case CardinalDirection.NORTH:
                cornerOneAdjustment = new Point(adjustmentFromSide * 2, -adjustmentFromSide);
                cornerTwoAdjustment = new Point(-adjustmentFromSide * 2, -adjustmentFromSide);
                defectAdjustment.y -= adjustmentFromSide;
                break;
            case CardinalDirection.EAST:
                cornerOneAdjustment = new Point(-adjustmentFromSide, -adjustmentFromSide * 2);
                cornerTwoAdjustment = new Point(-adjustmentFromSide, adjustmentFromSide * 2);
                defectAdjustment.x -= adjustmentFromSide;
                break;
            case CardinalDirection.SOUTH:
                cornerOneAdjustment = new Point(-adjustmentFromSide * 2, adjustmentFromSide);
                cornerTwoAdjustment = new Point(adjustmentFromSide * 2, adjustmentFromSide);
                defectAdjustment.y += adjustmentFromSide;
                break;
            case CardinalDirection.WEST:
                cornerOneAdjustment = new Point(adjustmentFromSide, adjustmentFromSide * 2);
                cornerTwoAdjustment = new Point(adjustmentFromSide, -adjustmentFromSide * 2);
                defectAdjustment.x += adjustmentFromSide;
                break;
        }

        cornerOne += cornerOneAdjustment;
        cornerTwo += cornerTwoAdjustment;
        defectPoint += defectAdjustment;

        Point sampleSizePoint = new Point(sampleSize.Item1 / 2, sampleSize.Item2 / 2);
        Point cornerOneCenter = cornerOne - sampleSizePoint;
        Point cornerTwoCenter = cornerTwo - sampleSizePoint;
        Point defectPointCenter = defectPoint - sampleSizePoint;

        Mat sampleOneMat = CropMat(cornerOneCenter, piece.pieceMat);
        Mat sampleTwoMat = CropMat(cornerTwoCenter, piece.pieceMat);
        Mat defectPointMat = CropMat(defectPointCenter, piece.pieceMat);

        side.colourSamples[0] = GetDominantColours(sampleOneMat);
        side.colourSamples[2] = GetDominantColours(sampleTwoMat);
        side.colourSamples[1] = GetDominantColours(defectPointMat);

        side.samplePoints[0] = cornerOneCenter;
        side.samplePoints[2] = cornerTwoCenter;
        side.samplePoints[1] = defectPointCenter;
    }

    double Lerp(double from, double to, double amount)
    {
        return from * (1 - amount) + to * amount;
    }

    Point Lerp(Point from, Point to, double amount)
    {
        double x = Lerp(from.x, to.x, amount);
        double y = Lerp(from.y, to.y, amount);
        return new Point(x, y);
    }

    public Mat CropMat(Point pt, Mat sampleMat)
    {
        OpenCVForUnity.CoreModule.Rect rect = new OpenCVForUnity.CoreModule.Rect((int)pt.x, (int)pt.y, sampleSize.Item1, sampleSize.Item2);
        Mat mat = new Mat(sampleMat.clone(), rect);
        return mat;
    }

    public RGBValue GetDominantColours(Mat mat)
    {
        // convert to 3-channel color image (RGBA to RGB).
        Mat imgMatRGB = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC3);
        Imgproc.cvtColor(mat, imgMatRGB, Imgproc.COLOR_RGBA2RGB);

        // reshape the image to be a 1 column matrix.
        Mat samples = imgMatRGB.reshape(3, imgMatRGB.cols() * imgMatRGB.rows());
        Mat samples32f = new Mat();
        samples.convertTo(samples32f, CvType.CV_32F, 1.0 / 255.0);

        // run k-means clustering algorithm to segment pixels in RGB color space.
        Mat labels = new Mat();
        TermCriteria criteria = new TermCriteria(TermCriteria.COUNT, 100, 1);
        Mat centers = new Mat();
        Core.kmeans(samples32f, 1, labels, criteria, 1, Core.KMEANS_PP_CENTERS, centers);

        centers.convertTo(centers, CvType.CV_8U, 255.0);
        int label = (int)labels.get(0, 0)[0];

        // return the most dominant RGB value
        return new RGBValue((int)centers.get(label, 0)[0], (int)centers.get(label, 1)[0], (int)centers.get(label, 2)[0]);
    }
}
