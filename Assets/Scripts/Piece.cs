using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Piece
{

    // ID of the piece, unique
    public int id;

    public Mat pieceMat;
    public Mat pieceChromaMat;

    public Mat contours;
    public List<Point> hullPoints;
    public MatOfPoint2f pieceBounds;

    public Point center;
    public List<Side> sides;

    public Piece(Mat _contours, List<Point> _hullPoints, MatOfPoint2f _bounds)
    {
        contours = _contours;
        pieceBounds = _bounds;
        hullPoints = _hullPoints;
    }

    public Piece(Mat _pieceMat, Point _center, List<Side> _sides)
    {
        pieceMat = _pieceMat;
        center = _center;
        sides = _sides;
    }

    public Piece(Mat _pieceMat, Point _center)
    {
        pieceMat = _pieceMat;
        center = _center;
    }

    public int GetNumberOfEdges()
    {
        return sides.Where(e => e.type == SideType.EDGE).Count();
    }

    public List<Side> CloneSides()
    {
        List<Side> newSides = new List<Side>();
        foreach (Side side in sides)
        {
            Side newSide = side.Clone();
            newSides.Add(newSide);
        }
        return newSides;
    }

    public Piece GetPieceIfSideIsRotatedFromXToY(Side targetSide, CardinalDirection target)
    {
        Piece piece = new Piece(pieceMat, center, sides);
        piece.id = id;

        int rotationAmount = PieceFunctions.GetRotationAmount(targetSide.direction, target);
        if (rotationAmount == 0)
        {
            return piece;
        }

        Mat newPieceMat = pieceMat.clone();
        Mat rotMatrix = Imgproc.getRotationMatrix2D(piece.center, rotationAmount, 1);
        Imgproc.warpAffine(newPieceMat, newPieceMat, rotMatrix, newPieceMat.size());

        piece.pieceMat = newPieceMat;

        List<Side> newSides = new List<Side>(CloneSides());
        foreach (Side side in newSides)
        {
            List<Point> newPoints = PieceFunctions.GetRotatedPoints(side.points, rotationAmount);
            CardinalDirection newDirection = PieceFunctions.GetFinalDirectionAfterRotation(side.direction, rotationAmount);
            side.points = newPoints;
            side.direction = newDirection;
            side.piece = piece;
        }

        piece.sides = newSides;

        return piece;
    }

    public Side GetSide(CardinalDirection dir)
    {
        foreach (Side side in sides)
        {
            if (side.direction == dir)
                return side;
        }
        return null;
    }

    public void OrderSides()
    {
        foreach (Side side in sides)
        {
            side.OrderMatches();
        }
    }

    public Piece Clone()
    {
        Piece piece = new Piece(pieceMat.clone(), center);
        piece.id = id;

        List<Side> sides = CloneSides();
        foreach (Side side in sides)
            side.piece = piece;

        piece.sides = sides;

        return piece;
    }

    public Mat GetSideMat(CardinalDirection dir)
    {
        Mat mat = pieceMat.clone();
        Imgproc.circle(mat, center, 3, new Scalar(255, 255, 255, 255));
        Side side = GetSide(dir);
        foreach (Point pt in side.samplePoints)
        {
            Rect rect = new Rect((int)pt.x, (int)pt.y, PieceSidePreparer.sampleSize.Item1, PieceSidePreparer.sampleSize.Item2);
            Imgproc.rectangle(mat, rect, new Scalar(0, 255, 0, 255));
        }
        foreach (Point pt in side.points)
        {
            Imgproc.circle(mat, pt, 2, PieceFunctions.colours[(int)side.type], -2);
        }
        return mat;
    }

    public Mat GetSide(Side side)
    {
        Mat mat = pieceMat.clone();
        Imgproc.circle(mat, center, 3, new Scalar(255, 255, 255, 255));
        foreach (Point pt in side.samplePoints)
        {
            Rect rect = new Rect((int)pt.x, (int)pt.y, PieceSidePreparer.sampleSize.Item1, PieceSidePreparer.sampleSize.Item2);
            Imgproc.rectangle(mat, rect, new Scalar(0, 255, 0, 255));
        }
        foreach (Point pt in side.points)
        {
            Imgproc.circle(mat, pt, 2, PieceFunctions.colours[(int)side.type], -2);
        }
        return mat;
    }

    public void DrawSides()
    {
        Mat mat = pieceMat.clone();
        Imgproc.circle(mat, center, 3, new Scalar(255, 255, 255, 255));
        foreach (Side side in sides)
        {
            foreach (Point pt in side.points)
            {
                Imgproc.circle(mat, pt, 2, PieceFunctions.colours[(int)side.type], -2);
            }
            Imgproc.putText(mat, side.direction.ToString().Substring(0, 1), side.points[side.points.Count / 2], Imgproc.FONT_HERSHEY_COMPLEX_SMALL, 1, new Scalar(0, 0, 0, 255), 1, 4, true);
        }
        ProcessSprites.instance.AddProcessImage(mat, "Piece", true);
    }

}