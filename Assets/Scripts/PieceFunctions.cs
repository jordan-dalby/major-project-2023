using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PieceFunctions
{

    public static List<Scalar> colours = new List<Scalar>() { new Scalar(255, 0, 0, 255), new Scalar(0, 0, 255, 255), new Scalar(255, 0, 255, 255) };

    public static Point GetMidpoint(List<Point> points)
    {
        MatOfPoint pointsMat = new MatOfPoint(Converters.vector_Point_to_Mat(points));
        Moments pieceMoments = Imgproc.moments(pointsMat);
        Point pieceMidpoint = new Point(pieceMoments.get_m10() / pieceMoments.get_m00(), pieceMoments.get_m01() / pieceMoments.get_m00());

        return pieceMidpoint;
    }

    public static List<Point> TransformMidpointToCenter(List<Point> points)
    {
        Point pieceMidpoint = GetMidpoint(points);

        Size transformationFromMidpointToCenter = new Size(150 - pieceMidpoint.x, 150 - pieceMidpoint.y);
        List<Point> normalizedPoints = new List<Point>();
        foreach (Point point in points)
        {
            Point newPoint = new Point(point.x + transformationFromMidpointToCenter.width, point.y + transformationFromMidpointToCenter.height);
            normalizedPoints.Add(newPoint);
        }
        return normalizedPoints;
    }

    /// <summary>
    /// Takes a point list and finds the lowest and highest points, along with the axis of highest deviation
    /// </summary>
    /// <param name="points">The points to take the average from</param>
    /// <returns>Returns 0 for horizontal axis, and 1 for vertical axis, also returns the highest and lowest X or Y depending on the axis</returns>
    public static (int, double, double) ExtremePoints(List<Point> points)
    {
        // Find the largest deviation, is it a vertical or horizontal deviation?
        double lowestX = Mathf.Infinity, highestX = 0, lowestY = Mathf.Infinity, highestY = 0;
        foreach (Point pt in points)
        {
            if (pt.x < lowestX)
                lowestX = pt.x;
            else if (pt.x > highestX)
                highestX = pt.x;
            if (pt.y < lowestY)
                lowestY = pt.y;
            else if (pt.y > highestY)
                highestY = pt.y;
        }

        // If the X axis has a higher deviation than the Y axis
        if (Mathf.Abs((float)(lowestX - highestX)) > Mathf.Abs((float)(lowestY - highestY)))
        {
            // It's a horizontal side, so return Y average
            return (0, highestY, lowestY);
        }
        else
        {
            // It's a vertical side, so return X average
            return (1, highestX, lowestX);
        }
    }

    public static (int, int, int) ExtremePointsIndexes(List<Point> points)
    {
        // Find the largest deviation, is it a vertical or horizontal deviation?
        int lowestXIndex = 0, highestXIndex = 0, lowestYIndex = 0, highestYIndex = 0;
        double lowestX = Mathf.Infinity, highestX = 0, lowestY = Mathf.Infinity, highestY = 0;
        for (int i = 0; i < points.Count; i++)
        {
            Point pt = points[i];
            if (pt.x < lowestX)
            {
                lowestXIndex = i;
                lowestX = pt.x;
            }
            else if (pt.x > highestX)
            {
                highestXIndex = i;
                highestX = pt.x;
            }
            if (pt.y < lowestY)
            {
                lowestYIndex = i;
                lowestY = pt.y;
            }
            else if (pt.y > highestY)
            {
                highestYIndex = i;
                highestY = pt.y;
            }
        }

        // If the X axis has a higher deviation than the Y axis
        if (Mathf.Abs((float)(lowestX - highestX)) > Mathf.Abs((float)(lowestY - highestY)))
        {
            return (0, lowestXIndex, highestXIndex);
        }
        else
        {
            return (1, lowestYIndex, highestYIndex);
        }
    }

    public static List<Point> RotatePointsToDirection(List<Point> points, CardinalDirection currentDirection, CardinalDirection direction)
    {
        return GetRotatedPoints(points, GetRotationAmount(currentDirection, direction));
    }

    public static int GetRotationAmount(CardinalDirection currentDirection, CardinalDirection direction)
    {
        switch (currentDirection)
        {
            case CardinalDirection.NORTH:
                if (direction == CardinalDirection.NORTH)
                    return 0;
                else if (direction == CardinalDirection.EAST)
                    return 90;
                else if (direction == CardinalDirection.SOUTH)
                    return 180;
                else if (direction == CardinalDirection.WEST)
                    return 270;
                break;
            case CardinalDirection.SOUTH:
                if (direction == CardinalDirection.NORTH)
                    return 180;
                else if (direction == CardinalDirection.EAST)
                    return 270;
                else if (direction == CardinalDirection.SOUTH)
                    return 0;
                else if (direction == CardinalDirection.WEST)
                    return 90;
                break;
            case CardinalDirection.EAST:
                if (direction == CardinalDirection.NORTH)
                    return 270;
                else if (direction == CardinalDirection.EAST)
                    return 0;
                else if (direction == CardinalDirection.SOUTH)
                    return 90;
                else if (direction == CardinalDirection.WEST)
                    return 180;
                break;
            case CardinalDirection.WEST:
                if (direction == CardinalDirection.NORTH)
                    return 90;
                else if (direction == CardinalDirection.EAST)
                    return 180;
                else if (direction == CardinalDirection.SOUTH)
                    return 270;
                else if (direction == CardinalDirection.WEST)
                    return 0;
                break;
        }
        return 0;
    }

    public static CardinalDirection GetFinalDirectionAfterRotation(CardinalDirection currentDirection, int rotationAmount)
    {
        switch (rotationAmount)
        {
            case 0:
                return currentDirection;
            case 90:
                if (currentDirection == CardinalDirection.NORTH)
                    return CardinalDirection.EAST;
                else if (currentDirection == CardinalDirection.EAST)
                    return CardinalDirection.SOUTH;
                else if (currentDirection == CardinalDirection.SOUTH)
                    return CardinalDirection.WEST;
                else if (currentDirection == CardinalDirection.WEST)
                    return CardinalDirection.NORTH;
                break;
            case 180:
                if (currentDirection == CardinalDirection.NORTH)
                    return CardinalDirection.SOUTH;
                else if (currentDirection == CardinalDirection.EAST)
                    return CardinalDirection.WEST;
                else if (currentDirection == CardinalDirection.SOUTH)
                    return CardinalDirection.NORTH;
                else if (currentDirection == CardinalDirection.WEST)
                    return CardinalDirection.EAST;
                break;
            case 270:
                if (currentDirection == CardinalDirection.NORTH)
                    return CardinalDirection.WEST;
                else if (currentDirection == CardinalDirection.EAST)
                    return CardinalDirection.NORTH;
                else if (currentDirection == CardinalDirection.SOUTH)
                    return CardinalDirection.EAST;
                else if (currentDirection == CardinalDirection.WEST)
                    return CardinalDirection.SOUTH;
                break;
        }
        return CardinalDirection.NORTH;
    }

    public static List<Point> GetRotatedPoints(List<Point> points, double degrees)
    {
        Point midpoint = GetMidpoint(points);

        Mat rotMatrix = Imgproc.getRotationMatrix2D(midpoint, degrees, 1);

        Mat mat = Converters.vector_Point_to_Mat(points);
        Mat dst = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC3);

        Core.transform(mat, dst, rotMatrix);

        List<Point> rotatedPoints = new List<Point>();
        Converters.Mat_to_vector_Point(dst, rotatedPoints);

        return rotatedPoints;
    }

    public static CardinalDirection GetOpposite(CardinalDirection dir)
    {
        if (dir == CardinalDirection.NORTH)
            return CardinalDirection.SOUTH;
        if (dir == CardinalDirection.SOUTH)
            return CardinalDirection.NORTH;
        if (dir == CardinalDirection.EAST)
            return CardinalDirection.WEST;
        if (dir == CardinalDirection.WEST)
            return CardinalDirection.EAST;

        return dir;
    }

}
