using System;
using System.Collections.Generic;
using UnityEngine;

public class Route
{

    public List<Piece> pieces = new List<Piece>();
    public List<int> remainingPieceIds = new List<int>();
    public int currentPieceMatchIndex = 0;

    public int[] dimensions = new int[3];

    public double score = 0;

    public Route() {}

    public Route(Route route)
    {
        pieces.Clear();
        foreach (Piece piece in route.pieces)
        {
            pieces.Add(piece.Clone());
        }

        remainingPieceIds = new List<int> (route.remainingPieceIds);
        score = route.score;
        Array.Copy(route.dimensions, dimensions, 3);
    }

    public Piece GetCurrentPiece()
    {
        if (pieces.Count == 0)
            return null;

        return pieces[pieces.Count - 1];
    }

    public double GetScore()
    {
        return (score / pieces.Count) * pieces.Count;
    }

    public void AddPiece(Piece piece)
    {
        pieces.Add(piece);
    }

    public void RotateRouteToWest()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            Piece newJoinPiece = pieces[i].GetPieceIfSideIsRotatedFromXToY(pieces[i].GetSide(CardinalDirection.NORTH), CardinalDirection.WEST);
            pieces[i] = newJoinPiece;
        }
    }

    public void DrawRoute()
    {
        foreach (Piece piece in pieces)
        {
            ProcessSprites.instance.AddProcessImage(piece.pieceMat, "Route");
        }
        Debug.Log("Route Score: " + score);
    }

    public Piece[,] GetPieceArray()
    {
        Piece[,] pieceArray = new Piece[dimensions[0] + 1, dimensions[1] + 1];

        int firstDimension = 0;
        int secondDimension = 0;
        int dir = 0;
        foreach (Piece piece in pieces)
        {
            // if the piece is trying to get added to an index that doesn't exist, just return what we have
            if (firstDimension < 0 || secondDimension < 0 || firstDimension > dimensions[0] || secondDimension > dimensions[1])
                return pieceArray;

            pieceArray[firstDimension, secondDimension] = piece;

            // if corner, but not the first piece
            if (piece.GetNumberOfEdges() == 2 && !(firstDimension == 0 && secondDimension == 0))
                dir++;

            if (dir == 0)
            {
                firstDimension++;
            }
            else if (dir == 1)
            {
                secondDimension++;
            }
            else if (dir == 2)
            {
                firstDimension--;
            }
            else if (dir == 3)
            {
                secondDimension--;
            }
        }

        return pieceArray;
    }

    public override bool Equals(object obj)
    {
        return obj is Route route &&
               EqualityComparer<List<Piece>>.Default.Equals(pieces, route.pieces) &&
               EqualityComparer<List<int>>.Default.Equals(remainingPieceIds, route.remainingPieceIds) &&
               currentPieceMatchIndex == route.currentPieceMatchIndex;
    }

    public override int GetHashCode()
    {
        int hashCode = -348262954;
        hashCode = hashCode * -1521134295 + EqualityComparer<List<Piece>>.Default.GetHashCode(pieces);
        hashCode = hashCode * -1521134295 + EqualityComparer<List<int>>.Default.GetHashCode(remainingPieceIds);
        hashCode = hashCode * -1521134295 + currentPieceMatchIndex.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(Route routeOne, Route routeTwo)
    {
        if (routeOne.pieces.Count != routeTwo.pieces.Count)
            return false;

        for (int i = 0; i < routeOne.pieces.Count; i++)
        { 
            if (routeOne.pieces[i].id != routeTwo.pieces[i].id)
            {
                return false;
            }
        }

        return true;
    }

    public static bool operator !=(Route routeOne, Route routeTwo)
    {
        return !(routeOne == routeTwo);
    }
}