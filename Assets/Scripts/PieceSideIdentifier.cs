using OpenCVForUnity.CoreModule;
using System.Collections.Generic;
using System.Linq;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using UnityEngine;

public class PieceSideIdentifier
{
    /// <summary>
    /// The smallest cartesian distance one corner can be from another
    /// </summary>
    private const double minimumPeakDistance = 100d;

    /// <summary>
    /// The number of points to consider when determining what a 'peak' is
    /// </summary>
    private const int rangeOfPeaks = 20;
    
    /// <summary>
    /// How physically close a point can be to a corner to be considered for averages
    /// </summary>
    private const double averageDistanceRange = 10d;

    /// <summary>
    /// The number of corners that a piece can have
    /// </summary>
    private const int maxCorners = 4;

    /// <summary>
    /// The value at which an side average would be considered to be an edge
    /// </summary>
    private const double edgeClassificationThreshold = 15d;

    /// <summary>
    /// Convert cartesian contour points to polar coordinates, plot them, then look for peaks (corners)
    /// and use that information to find tabs and blanks
    /// </summary>
    /// <reference>https://stackoverflow.com/questions/36703964/want-to-detect-edge-and-corner-parts-in-a-jigsaw-puzzle-but-cant-find-the-4-co</reference>
    /// <param name="piece">The piece used in this function</param>
    public void IdentifyPieceSidesPolar(Piece piece)
    {
        #region Find Midpoint of Piece
        // Get contours of piece
        List<Point> contourPoints = new List<Point>();
        Converters.Mat_to_vector_Point(piece.contours, contourPoints);

        // Get the center of the piece
        Point center = PieceFunctions.GetMidpoint(contourPoints);
        piece.center = center;
        #endregion

        #region Convert Cartesian points to Polar
        // Convert the contour points cartesian coordinates to polar coordinates
        List<PointData> polarCoordinates = GetPolarCoordinates(contourPoints, center);
        #endregion

        #region Find Corners from Polar Coordinates
        // Find the peaks in the polar coordinates, a peak is a particular part of a graph where a Y value is larger than average
        List<PointData> maximums = FindPeaks(polarCoordinates, center);
        // Gets the corners that most closely make a 45 degree angle with the center of the piece
        List<PointData> selectedCorners = GetMostLikelyCorners(maximums, center, maxCorners);

        // If less than maxCorners were found, there was an issue
        if (selectedCorners.Count != maxCorners)
        {
            Debug.LogWarning($"Only {selectedCorners.Count}/{maxCorners} corners were found for a given piece, it will be omitted from Matching processes.");
        }
        #endregion

        #region Draw Polar Plot
        // Get the maximum Y value for the polar coordinates
        double maxY = polarCoordinates.Max(e => e.GetPoint().y);
        // Create an output Mat
        Mat output = new Mat(Mathf.CeilToInt((float)maxY), polarCoordinates.Count, CvType.CV_8UC3);
        // Set the Mat to be 255, 255, 255 (white)
        output.setTo(new Scalar(255, 255, 255, 255));

        // Draw to the output
        // Create a variable to hold the previous point
        Point prev = null;
        // Loop over all of the polar coordinates
        foreach (PointData pt in polarCoordinates)
        {
            // Get the normalised point from the PointData
            Point normalisedPoint = pt.GetNormalizedPoint();
            // If the prev variable contains any value
            if (prev != null)
            {
                // Get the X distance between the previous and current point, if it's less than 100
                if (Mathf.Abs((float)(prev.x - normalisedPoint.x)) < 100)
                {
                    // Draw a line from prev to normalisedPoint
                    Imgproc.line(output, prev, normalisedPoint, new Scalar(255, 0, 0, 255), 1);
                }
                else
                {
                    // Draw a circle instead
                    Imgproc.circle(output, normalisedPoint, 1, new Scalar(255, 0, 0, 255), -1);
                }
            }

            // Set the previous point to the new normalisedPoint
            prev = normalisedPoint;
        }

        // Draw the peaks
        // Loop over all detected peaks
        foreach (PointData pt in maximums)
        {
            // Draw a circle at the point, if it is selected, draw it in a different colour
            Imgproc.circle(output, pt.GetNormalizedPoint(), 4, selectedCorners.Contains(pt) ? new Scalar(0, 0, 255, 255) : new Scalar(0, 255, 0, 255), -1);
        }

        // Add the image to the output screen
        ProcessSprites.instance.AddProcessImage(output, "Polar Plot", true);
        #endregion

        #region Separate sides
        // Separate the sides of the pieces based on the polar coordinates and the identified corners
        SeparateSides(polarCoordinates, center, selectedCorners, piece);
        #endregion
    }

    /// <summary>
    /// Takes a list of points and a center value and converts all points to polar coordinates around the center
    /// </summary>
    /// <param name="points">The points to convert</param>
    /// <param name="center">The center point to convert from</param>
    /// <returns>The polar coordinates</returns>
    public List<PointData> GetPolarCoordinates(List<Point> points, Point center)
    {
        // Create a list to hold the polar coordinates
        List<PointData> polarCoordinates = new List<PointData>();
        // loop over all of the points
        for (int i = 0; i < points.Count; i++)
        {
            // Select the point from the points list
            Point point = points[i];
            // Create some variables to hold output data
            Mat rho = new Mat();
            Mat phi = new Mat();

            // Convert the cartesian point to a polar point, with references to rho and phi for output
            Core.cartToPolar(new Mat(1, 1, CvType.CV_32F, new Scalar(point.x - center.x)), new Mat(1, 1, CvType.CV_32F, new Scalar(point.y - center.y)), rho, phi);

            // add the coordinate to the list
            polarCoordinates.Add(new PointData(point, rho, phi, i));
        }
        // return the list
        return polarCoordinates;
    }

    /// <summary>
    /// Find the peaks within the polar points
    /// </summary>
    /// <param name="values">The contours of the piece</param>
    /// <param name="center">The center of the piece</param>
    /// <returns>A list of valid peaks</returns>
    /// <reference>https://stackoverflow.com/a/9136236/5232304</reference>
    public List<PointData> FindPeaks(List<PointData> values, Point center)
    {
        // Create a new dictionary to hold the index of each point and it's average gradient
        Dictionary<PointData, double> peaks = new Dictionary<PointData, double>();
        // The Y value of the current point
        double current;
        // The range of values currently being considered
        IEnumerable<double> range;

        // Create a triple list of the values, if we happen to traverse over the length of the current list, we dip into here
        // for more accurate peaks
        List<PointData> tripleValues = new List<PointData>();
        for (int i = 0; i < 3; i++)
            tripleValues.AddRange(values);

        // Halves the rangeOfPeaks value to distribute over both sides
        int checksOnEachSide = rangeOfPeaks / 2;
        for (int i = 0; i < values.Count; i++)
        {
            // Reassigns the current values
            current = values[i].GetPoint().y;
            // Generates a new range
            range = tripleValues.Select(v => v.GetPoint().y);
            range = range.Skip(values.Count - 1);

            // If i is greater than the number of checks on one side then re-adjust the range to only include relevant points
            if (i > checksOnEachSide)
            {
                range = range.Skip(i - checksOnEachSide);
            }

            // Take num:rangeOfPeaks from the range
            range = range.Take(rangeOfPeaks);
            // If the range has items in it, and the current point has a y value equal to the max in the range
            if ((range.Count() > 0) && (current == range.Max()))
            {
                // Get the average magnitude
                double average = GetMedianMagnitude(values, i);

                // Check if corner is very near to another corner, if it is, only include the better of the two
                bool add = true;
                // Get cartesian coordinates for measuring distance
                Point testingPoint = values[i].GetCartesianPoint();
                // Loop all existing peaks
                for (int j = 0; j < peaks.Count; j++)
                {
                    // Get the peak at index j
                    KeyValuePair<PointData, double> data = peaks.ElementAt(j);

                    // Get the cartesian coordiantes of the peakPoint
                    Point peakPoint = data.Key.GetCartesianPoint();

                    // Calculate the distance from the test point to the peak point
                    double distance = Distance(testingPoint, peakPoint);
                    // If the distance is shorter than the allowed minimum
                    if (distance <= minimumPeakDistance)
                    {
                        // The points are too close
                        // Check which point is "better" and pick it
                        if (data.Value > average)
                        {
                            peaks.Remove(data.Key);
                        }
                        else
                        {
                            add = false;
                        }
                        break;
                    }
                }

                // Add as a peak
                if (add)
                    peaks.Add(values[i], average);
            }
        }

        return peaks.Select(e => e.Key).ToList();
    }

    /// <summary>
    /// Gets the median magnitude of a range of points
    /// </summary>
    /// <param name="pointList">The full list of points</param>
    /// <param name="centerIndex">The centerIndex to focus the search around</param>
    /// <returns>The median magnitude of the second derivative across a range of points</returns>
    public double GetMedianMagnitude(List<PointData> pointList, int centerIndex)
    {
        // gets the center point of the piece in cartesian points
        Point cartesianCenter = pointList[centerIndex].GetCartesianPoint();
        // create a new list to store the magnitudes
        List<double> mags = new List<double>();

        // loop over each point in the pointList
        for (int i = 0; i < pointList.Count; i++)
        {
            // get the point data
            PointData point = pointList[i];
            // get the cartesian point
            Point cartesianPoint = point.GetCartesianPoint();
            // if the distance between the point and the center is less than a pre-defined average distance
            if (Distance(cartesianPoint, cartesianCenter) <= averageDistanceRange)
            {
                // add the magnitude to the list
                mags.Add(GetMagnitude(pointList, i));
            }
        }

        // sort the magnitudes
        double[] sortedPNumbers = (double[])mags.ToArray().Clone();
        Array.Sort(sortedPNumbers);

        // calculate the median value in the magitiude list and return it
        int size = sortedPNumbers.Length;
        int mid = size / 2;
        double median = (size % 2 != 0) ? (double)sortedPNumbers[mid] : ((double)sortedPNumbers[mid] + (double)sortedPNumbers[mid - 1]) / 2;
        return median;
    }

    /// <summary>
    /// Gets the distance between one point and another
    /// </summary>
    /// <param name="one">The origin point</param>
    /// <param name="two">The destination point</param>
    /// <returns>The distance between point one and point two</returns>
    private double Distance(Point one, Point two)
    {
        // simple pythagorean theorem to calculate the distance between two points
        return Math.Sqrt(Math.Pow(one.x - two.x, 2) + Math.Pow(one.y - two.y, 2));
    }

    /// <summary>
    /// Gets the second derivative of a given point, using i - 1 and i + 1 as comparison points
    /// </summary>
    /// <param name="pointList">The contours of the piece</param>
    /// <param name="centerIndex">The starting index</param>
    /// <returns>The magnitude of the second derivative from centerIndex - 1 to centerIndex and centerIndex + 1 to centerIndex</returns>
    public double GetMagnitude(List<PointData> pointList, int centerIndex)
    {
        // If the centerIndex isn't within the bounds of pointList, make it
        if (centerIndex < 0)
        {
            centerIndex = centerIndex + pointList.Count;
        }
        else if (centerIndex > pointList.Count - 1)
        {
            centerIndex = centerIndex - pointList.Count;
        }

        // Create a previousIndex, if its value is less than 0 (invalid), wrap it around the list so that it's the last element of the list
        int previousIndex = centerIndex - 1;
        if (previousIndex < 0)
            previousIndex = pointList.Count - 1;

        // Create a nextIndex, if its value is greater than the length of the list (invalid), wrap it around so it's the first element of the list
        int nextIndex = centerIndex + 1;
        if (nextIndex > pointList.Count - 1)
            nextIndex = 0;

        // Get the center point from the center index
        Point crrt = pointList[centerIndex].GetNormalizedPoint();
        // Get the previous point
        Point prev = pointList[previousIndex].GetNormalizedPoint();
        // Normalise the X value incase we had to wrap
        prev.x = crrt.x - 1;
        // Get the next point
        Point next = pointList[nextIndex].GetNormalizedPoint();
        // Normalise the X value incase we had to wrap
        next.x = crrt.x + 1;

        // Calculate the second derivative using the previous, current, and next points
        double secondDerivative = (next.y - (2d * crrt.y) + prev.y) / (2d * (next.x - prev.x));

        // Calculate the gradient of the lhs against the rhs
        return secondDerivative;
    }

    /// <summary>
    /// Gets the most likely corners from a list of corners and a center point
    /// </summary>
    /// <param name="potentialCorners">The list of points to consider</param>
    /// <param name="center">The center of the piece in cartesian coordinates</param>
    /// <param name="select">The number of corners to select</param>
    /// <returns>The points that most accurately create a 45 degree angle from the center point to each corner</returns>
    public List<PointData> GetMostLikelyCorners(List<PointData> potentialCorners, Point center, int select)
    {
        // Dictionary to hold the corners and their angle from the identity line
        Dictionary<PointData, double> corners = new Dictionary<PointData, double>();
        // Loop over all of the corners of the piece
        foreach (PointData pt in potentialCorners)
        {
            // Get the absolute angle between the corner and the center of the piece
            double absoluteAngle = GetAbsoluteAngleDifference(pt.GetCartesianPoint(), center);
            // Add the corner and the absolute difference between its angle and 45 degrees
            corners.Add(pt, Math.Abs(absoluteAngle - 45));
        }
        // Select the corners with an angle that closest resembles 45 degrees
        List<PointData> bestCorners = corners.OrderBy(e => e.Value).Take(select).Select(e => e.Key).ToList();
        // Return the best corners
        return bestCorners;
    }

    /// <summary>
    /// Uses tan-1 to calculate the angle between two points, absolute
    /// </summary>
    /// <param name="point">The point that varies</param>
    /// <param name="center">The center of the piece</param>
    /// <returns>The angle (in degrees) between two points</returns>
    public double GetAbsoluteAngleDifference(Point point, Point center)
    {
        return Mathf.Rad2Deg * Math.Atan2(Math.Abs(point.y - center.y), Math.Abs(point.x - center.x));
    }

    /// <summary>
    /// Takes a list of polar coordinates and separates the elements into 4 lists, one for each side of a piece
    /// </summary>
    /// <param name="polarCoordinates">The input coordinates</param>
    /// <param name="center">The center of the piece</param>
    /// <param name="corners">The corners of the piece</param>
    /// <param name="piece">The piece itself</param>
    public void SeparateSides(List<PointData> polarCoordinates, Point center, List<PointData> corners, Piece piece)
    {
        bool cornerFound = false;

        // Keep track of points missed, before the first corner is found
        List<Point> missed = new List<Point>();
        // Buffer to hold the current point list
        List<Point> buffer = new List<Point>();

        // Declare some useful variables
        (SideType, CardinalDirection) sideInfo = (SideType.EDGE, CardinalDirection.NORTH);
        // Create a list to store the sides of the piece
        List<Side> sides = new List<Side>();

        // Loop over all polar coordinates
        foreach (PointData pt in polarCoordinates)
        {
            // Get cartesian point
            Point point = pt.GetCartesianPoint();

            // If the corners list contains the current point
            if (corners.Contains(pt))
            {
                // If a point has already been found
                if (cornerFound)
                {
                    // Add it to the buffer, then isolate the side into a Side object
                    buffer.Add(point);
                    sideInfo = DetermineSideNature(buffer, center);
                    sides.Add(new Side(piece, sideInfo.Item1, sideInfo.Item2, new List<Point>(buffer)));
                    // Clear the buffer, ready for the next corner, and add the current corner to the buffer
                    buffer.Clear();
                    buffer.Add(point);
                }
                else
                {
                    // Add the point to the missed list
                    cornerFound = true;
                    missed.Add(point);
                }
            }

            // If a corner has been found
            if (cornerFound)
            {
                // Add it to the buffer
                buffer.Add(point);
            }
            else
            {
                // Add it to missed
                missed.Add(point);
            }
        }

        // Add the missed points to the buffer and isolate the side into a Side object
        buffer.AddRange(missed);
        sideInfo = DetermineSideNature(buffer, center);
        sides.Add(new Side(piece, sideInfo.Item1, sideInfo.Item2, new List<Point>(buffer)));

        // Set the pieces sides and draw them
        piece.sides = sides;
        piece.DrawSides();
    }

    /// <summary>
    /// Gets the type (TAB, BLANK, EDGE) of a side, and also its direction (NORTH, EAST, SOUTH, WEST)
    /// </summary>
    /// <param name="cardinalPoints">The points of the side to consider</param>
    /// <param name="center">The center of the piece</param>
    /// <returns>A tuple of SideType (Item1) and CardinalDirection(Item2)</returns>
    public (SideType, CardinalDirection) DetermineSideNature(List<Point> cardinalPoints, Point center)
    {
        // Get the first and last point (corners)
        Point cornerOne = cardinalPoints[0];
        Point cornerTwo = cardinalPoints[cardinalPoints.Count - 1];

        double averageX = (cornerOne.x + cornerTwo.x) / 2d;
        double averageY = (cornerOne.y + cornerTwo.y) / 2d;

        // Get the axis and highest and lowest point
        (int, double, double) pair = PieceFunctions.ExtremePoints(cardinalPoints);

        // Declare some variables for return
        CardinalDirection direction = CardinalDirection.NORTH;
        SideType type = SideType.EDGE;
        // If horizontal
        if (pair.Item1 == 0)
        {
            if (averageY > center.y)
            {
                direction = CardinalDirection.NORTH;

                // Check if side has small enough deviation to be an edge
                if (Mathf.Abs((float)(pair.Item2 - averageY)) <= edgeClassificationThreshold && Mathf.Abs((float)(pair.Item3 - averageY)) <= edgeClassificationThreshold)
                {
                    type = SideType.EDGE;
                }
                else
                {
                    // Get the difference between the low and high and the corners Y value 
                    double highDifference = Math.Abs(pair.Item2 - averageY);
                    double lowDifference = Math.Abs(pair.Item3 - averageY);
                    if (highDifference > lowDifference)
                    {
                        type = SideType.TAB;
                    }
                    else
                    {
                        type = SideType.BLANK;
                    }
                }
            }
            else
            {
                direction = CardinalDirection.SOUTH;

                if (Mathf.Abs((float)(pair.Item2 - averageY)) <= edgeClassificationThreshold && Mathf.Abs((float)(pair.Item3 - averageY)) <= edgeClassificationThreshold)
                {
                    type = SideType.EDGE;
                }
                else
                {
                    double highDifference = Math.Abs(pair.Item2 - cornerOne.y);
                    double lowDifference = Math.Abs(pair.Item3 - cornerOne.y);
                    if (highDifference > lowDifference)
                    {
                        type = SideType.BLANK;
                    }
                    else
                    {
                        type = SideType.TAB;
                    }
                }
            }
        }
        else
        {
            if (averageX > center.x)
            {
                direction = CardinalDirection.EAST;

                if (Mathf.Abs((float)(pair.Item2 - averageX)) <= edgeClassificationThreshold && Mathf.Abs((float)(pair.Item3 - averageX)) <= edgeClassificationThreshold)
                {
                    type = SideType.EDGE;
                }
                else
                {
                    double highDifference = Math.Abs(pair.Item2 - cornerOne.x);
                    double lowDifference = Math.Abs(pair.Item3 - cornerOne.x);
                    if (highDifference > lowDifference)
                    {
                        type = SideType.TAB;
                    }
                    else
                    {
                        type = SideType.BLANK;
                    }
                }
            }
            else
            {
                direction = CardinalDirection.WEST;

                if (Mathf.Abs((float)(pair.Item2 - averageX)) <= edgeClassificationThreshold && Mathf.Abs((float)(pair.Item3 - averageX)) <= edgeClassificationThreshold)
                {
                    type = SideType.EDGE;
                }
                else
                {
                    double highDifference = Math.Abs(pair.Item2 - cornerOne.x);
                    double lowDifference = Math.Abs(pair.Item3 - cornerOne.x);
                    if (highDifference > lowDifference)
                    {
                        type = SideType.BLANK;
                    }
                    else
                    {
                        type = SideType.TAB;
                    }
                }
            }
        }
        return (type, direction);
    }

    /*public Point GetCartesianPointFromPolar(PointData polar, Point center)
    {
        // Declare some variables for outputs
        Mat x = new Mat();
        Mat y = new Mat();

        // Convert the polar coordinates to cartesian coordinates
        Core.polarToCart(polar.GetMagnitude(), polar.GetAngle(), x, y);
        // Create a new point, and add back the center coordinates to go back to how the points originally were
        Point point = new Point(x.get(0, 0)[0] + center.x, y.get(0, 0)[0] + center.y);

        // Return the point list
        return point;
    }*/

    /*public void IdentifyPieceSides(Piece piece)
    {
        Mat output = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
        piece.pieceMat.copyTo(output);

        List<Point> contourPoints = new List<Point>();
        Converters.Mat_to_vector_Point(piece.contours, contourPoints);

        List<Point> tripleContourPoints = new List<Point>(contourPoints);
        tripleContourPoints.AddRange(contourPoints);
        tripleContourPoints.AddRange(contourPoints);

        Point[] convexityDefects = piece.convexityDefects.ToArray();

        // find all tabs
        for (int a = 0; a < convexityDefects.Count() - 1; a++)
        {
            for (int b = a + 1; b < convexityDefects.Count(); b++)
            {
                // if it's been used
                if (convexityDefects[a] == null || convexityDefects[b] == null)
                    continue;

                if (a == b)
                    continue;

                List<Point> points = GetPointsBetweenAandB(contourPoints, convexityDefects[a], convexityDefects[b]);

                Mat matPoints = Converters.vector_Point2f_to_Mat(points);
                double area = Imgproc.contourArea(matPoints);

                MatOfPoint2f matPoints2f = new MatOfPoint2f(matPoints);
                double perimeter = Imgproc.arcLength(matPoints2f, true);

                double circularity = area / Mathf.Pow((float)perimeter, 2);

                if (circularity > 0.0516) // experimented to find this number
                {
                    convexityDefects[a] = null;
                    convexityDefects[b] = null;

                    Mat output2 = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
                    piece.pieceMat.copyTo(output2);
                    MatOfPoint tempPoints2 = new MatOfPoint(Converters.vector_Point_to_Mat(points));
                    Imgproc.drawContours(output2, new List<MatOfPoint>() { tempPoints2 }, -1, new Scalar(0, 255, 255, 255), -1);

                    piece.AddSide(SideType.TAB, points, output2);

                    MatOfPoint tempPoints = new MatOfPoint(Converters.vector_Point_to_Mat(points));
                    Imgproc.drawContours(output, new List<MatOfPoint>() { tempPoints }, -1, new Scalar(0, 255, 255, 255), -1);
                    break;
                }
            }
        }

        // remaining convexity defects are all blanks, but lets confirm that
        for (int a = 0; a < convexityDefects.Count(); a++)
        {
            // march points left and right from the index of this convexity defect in the contour list
            // until a circle is formed
            // TODO: if none is formed within contour.Count attempts, we need to  increment each side of
            // the march by a different amount, if that fails, this is not a blank

            // used defect
            if (convexityDefects[a] == null)
                continue;

            int centreIndex = contourPoints.IndexOf(convexityDefects[a]);

            for (int i = contourPoints.Count / 3; i > 0; i--)
            {
                int indexPointOne = centreIndex + i;
                if (indexPointOne > contourPoints.Count - 1)
                {
                    indexPointOne = contourPoints.Count - 1;
                }

                int indexPointTwo = centreIndex - i;
                if (indexPointTwo < 0)
                {
                    indexPointTwo = 0;
                }

                Point pointOne = contourPoints[indexPointOne];
                Point pointTwo = contourPoints[indexPointTwo];

                List<Point> points = GetPointsBetweenAandB(contourPoints, pointOne, pointTwo);

                Mat matPoints = Converters.vector_Point2f_to_Mat(points);
                double area = Imgproc.contourArea(matPoints);

                MatOfPoint2f matPoints2f = new MatOfPoint2f(matPoints);
                double perimeter = Imgproc.arcLength(matPoints2f, true);

                double circularity = area / Mathf.Pow((float)perimeter, 2);

                if (circularity > 0.0515)
                {
                    bool convexityDefectFound = false;
                    bool done = false;
                    List<Point> edgePoints = new List<Point>();
                    for (int j = contourPoints.Count; j < tripleContourPoints.Count; j++)
                    {
                        if (done)
                            break;

                        Point point = tripleContourPoints[j];
                        if (point == convexityDefects[a])
                        {
                            convexityDefectFound = true;
                            edgePoints.Add(point);
                            continue;
                        }
                        if (convexityDefectFound)
                        {
                            edgePoints.Add(point);

                            Mat o = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
                            piece.pieceMat.copyTo(o);
                            MatOfPoint t = new MatOfPoint(Converters.vector_Point_to_Mat(edgePoints));
                            Imgproc.drawContours(o, new List<MatOfPoint>() { t }, -1, new Scalar(255, 255, 255, 255), -1);
                            ProcessSprites.instance.AddProcessImage(o, "t", false);

                            if (piece.hullPoints.Contains(point))
                            {
                                // first hull point found, now go backwards
                                for (int k = j - edgePoints.Count; k >= 0; k--)
                                {
                                    Point backPoint = tripleContourPoints[k];
                                    edgePoints.Add(backPoint);

                                    Mat o2 = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
                                    piece.pieceMat.copyTo(o2);
                                    MatOfPoint t2 = new MatOfPoint(Converters.vector_Point_to_Mat(edgePoints));
                                    Imgproc.drawContours(o2, new List<MatOfPoint>() { t2 }, -1, new Scalar(255, 255, 255, 255), -1);
                                    ProcessSprites.instance.AddProcessImage(o2, "t2", false);

                                    if (piece.hullPoints.Contains(backPoint))
                                    {
                                        // done
                                        done = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    Debug.Log(edgePoints.Count);

                    Imgproc.circle(output, convexityDefects[a], 3, new Scalar(255, 255, 255, 255), -1);
                    convexityDefects[a] = null;

                    Mat output2 = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
                    piece.pieceMat.copyTo(output2);
                    MatOfPoint tempPoints2 = new MatOfPoint(Converters.vector_Point_to_Mat(edgePoints));
                    Imgproc.drawContours(output2, new List<MatOfPoint>() { tempPoints2 }, -1, new Scalar(255, 255, 255, 255), -1);

                    piece.AddSide(SideType.BLANK, edgePoints, output2);

                    MatOfPoint tempPoints = new MatOfPoint(Converters.vector_Point_to_Mat(edgePoints));
                    Imgproc.drawContours(output, new List<MatOfPoint>() { tempPoints }, -1, new Scalar(255, 255, 255, 255), -1);
                    break;

                    /////////

                    //Imgproc.circle(output, convexityDefects[a], 3, new Scalar(255, 255, 255, 255), -1);
                    //convexityDefects[a] = null;

                    /* TEMPORARY
                    Mat output2 = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC3);
                    piece.pieceMat.copyTo(output2);
                    MatOfPoint tempPoints2 = new MatOfPoint(Converters.vector_Point_to_Mat(points));
                    Imgproc.drawContours(output2, new List<MatOfPoint>() { tempPoints2 }, -1, new Scalar(255, 255, 255, 255), -1);
                              

                    //piece.AddSide(SideType.BLANK, points, output2);

                    //MatOfPoint tempPoints = new MatOfPoint(Converters.vector_Point_to_Mat(points));
                    //Imgproc.drawContours(output, new List<MatOfPoint>() { tempPoints }, -1, new Scalar(255, 255, 255, 255), -1);
                    //break;
                }
            }
        }

        if (!convexityDefects.All(x => x == null))
        {
            Debug.LogWarning("Not all convexity defects were used, this could be a problem.");
        }

        ProcessSprites.instance.AddProcessImage(output, "PieceSideIdentifier", true);
    }

    private List<Point> GetPointsBetweenAandB(List<Point> contour, Point a, Point b)
    {
        int startIndex = contour.IndexOf(a);
        int endIndex = contour.IndexOf(b);

        List<Point> points = new List<Point>();
        if (startIndex > endIndex)
        {
            // swap
            (endIndex, startIndex) = (startIndex, endIndex);
        }

        for (int i = startIndex; i <= endIndex; i++)
        {
            points.Add(contour[i]);
        }

        // add a copy of the list to the end of the current list
        // start iterating from original list length - 1 + startIndex
        // iterate backwards until reach endIndex
        List<Point> newPointList = new List<Point>();
        newPointList.AddRange(contour);
        newPointList.AddRange(contour);

        List<Point> doubleListPoints = new List<Point>();

        for (int i = contour.Count + startIndex; i >= endIndex; i--)
        {
            doubleListPoints.Add(newPointList[i]);
        }

        // return the list with the least items
        return points.Count > doubleListPoints.Count ? doubleListPoints : points;
    }*/
}