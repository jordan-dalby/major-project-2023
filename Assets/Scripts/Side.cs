using OpenCVForUnity.CoreModule;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Side
{

    public int id;

    public Piece piece;

    public SideType type;
    public CardinalDirection direction;
    public List<Point> points;
    public List<Point> rotatedPoints;

    public Point[] samplePoints;
    public RGBValue[] colourSamples;

    public double length;

    public Dictionary<Side, double> matches = new Dictionary<Side, double>();

    public Side(int _id, Piece _piece, SideType _type, CardinalDirection _dir, List<Point> _points)
    {
        id = _id;
        piece = _piece;
        type = _type;
        direction = _dir;
        points = _points;

        samplePoints = new Point[3];

        colourSamples = new RGBValue[3];
    }

    public Side Clone()
    {
        List<Point> newPoints = new List<Point>();
        foreach (Point pt in points)
            newPoints.Add(new Point(pt.x, pt.y));

        Side newSide = new Side(id, piece, type, direction, newPoints);
        newSide.samplePoints = samplePoints;
        newSide.matches = matches;
        return newSide;
    }

    public Side CloneMinimum()
    {
        Side newSide = new Side(id, piece, type, direction, null);
        newSide.matches = matches;
        return newSide;
    }

    public void OrderMatches()
    {
        matches = matches.OrderBy(e => e.Value).ToDictionary(e => e.Key, e => e.Value);
    }

}
