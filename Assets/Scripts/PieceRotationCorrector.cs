using OpenCVForUnity.CoreModule;
using System.Collections.Generic;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;

/// <summary>
/// Corrects the rotation of pieces, making them upright
/// </summary>
public class PieceRotationCorrector
{

    public void GetRotationMatrixForPieceAndRotate(Piece piece)
    {
        // create a point list of the contour around the piece
        List<Point> allPiecePoints = new List<Point>();
        Converters.Mat_to_vector_Point(piece.contours, allPiecePoints);

        // create a point mat of the hull points around the piece and set the center of the piece
        MatOfPoint2f points = new MatOfPoint2f(Converters.vector_Point2f_to_Mat(piece.hullPoints));
        RotatedRect rect = Imgproc.minAreaRect(points);
        piece.center = rect.center;

        // create a min area rectangle around the piece contour points
        points = new MatOfPoint2f(Converters.vector_Point2f_to_Mat(allPiecePoints));

        // run approxPolyDP to filter results to mostly corners, this ensures the piece is rotated
        // based on it's square like shape, rather than it's rectangular shape with tabs
        double epsilon = 0.009 * Imgproc.arcLength(points, true);
        Imgproc.approxPolyDP(points, points, epsilon, true);

        // create minAreaRect around contour
        RotatedRect minAreaRect = Imgproc.minAreaRect(points);

        // get the rotation matrix to transform the piece from its current angle (minAreaRect.angle) back to 0
        Mat rotationMatrix = Imgproc.getRotationMatrix2D(piece.center, minAreaRect.angle, 1);

        // rotate the piece
        RotatePiece(piece, rotationMatrix, minAreaRect);
    }

    public Piece RotatePiece(Piece piece, Mat rotationMatrix, RotatedRect rect)
    {
        // src: https://stackoverflow.com/a/75451191/5232304
        // Determine bounding rect
        var boundingRect = new RotatedRect(new Point(), new Size(piece.pieceMat.size().width, piece.pieceMat.size().height), rect.angle).boundingRect();
        // Adjust the rotation matrix
        rotationMatrix.put(0, 2, rotationMatrix.get(0, 2)[0] + (boundingRect.width / 2f) - (piece.pieceMat.size().width / 2f));
        rotationMatrix.put(1, 2, rotationMatrix.get(1, 2)[0] + (boundingRect.height / 2f) - (piece.pieceMat.size().height / 2f));
        // Create a new mat
        Mat mat = new Mat();
        // Warp the piece to fit into the new mat based on the size of the bounding rect and fill the empty space with the border colour
        Imgproc.warpAffine(piece.pieceMat, mat, rotationMatrix, boundingRect.size(), Imgproc.INTER_LINEAR, Core.BORDER_REPLICATE);

        // rotate all hull points
        Mat hullPoints = Converters.vector_Point_to_Mat(piece.hullPoints);
        Core.transform(hullPoints, hullPoints, rotationMatrix);
        Converters.Mat_to_vector_Point(hullPoints, piece.hullPoints);

        // rotate the contours
        Core.transform(piece.contours, piece.contours, rotationMatrix);

        // rotate the center
        Mat centerPoint = Converters.vector_Point_to_Mat(new List<Point>() { piece.center });
        Core.transform(centerPoint, centerPoint, rotationMatrix);
        List<Point> centerPointList = new List<Point>();
        Converters.Mat_to_vector_Point(centerPoint, centerPointList);
        piece.center = centerPointList[0];

        // assign the new mat to the image preview
        piece.pieceMat = mat;

        // chroma key the new image preview so we can cut out the green background
        ChromaKey key = new ChromaKey();
        // assign the new chroma mat to the piece
        piece.pieceChromaMat = key.Key(mat, true, false);

        // likewise with the bounds of the piece, we don't need it anymore
        piece.pieceBounds = null;

        // need to transform rect too
        // create a point list of the contour around the piece
        List<Point> allPiecePoints = new List<Point>();
        Converters.Mat_to_vector_Point(piece.contours, allPiecePoints);

        // return the piece
        return piece;
    }

}
