using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Takes all pieces and attempts to match them together
/// -describe process-
/// </summary>
public class JigsawAssembler
{

    /// <summary>
    /// All of the pieces in the jigsaw
    /// </summary>
    public List<Piece> pieces;

    /// <summary>
    /// Constructor to initialise the pieces
    /// </summary>
    /// <param name="_pieces">All pieces in the jigsaw</param>
    public JigsawAssembler(List<Piece> _pieces)
    {
        pieces = _pieces;
    }

    public void RunAssembler()
    {
        List<Piece> edges = new List<Piece>();
        List<Piece> remaining = new List<Piece>();
        Piece corner = null;
        foreach (Piece piece in pieces)
        {
            piece.OrderSides();
            if (piece.GetNumberOfEdges() == 2)
            {
                if (corner == null)
                {
                    corner = piece;
                }
                else
                {
                    edges.Add(piece);
                }
            }
            else if (piece.GetNumberOfEdges() == 1)
            {
                edges.Add(piece);
            }
            else
            {
                remaining.Add(piece);
            }
        }

        if (corner == null)
        {
            Debug.LogError("Could not solve the jigsaw, no corners were found");
            return;
        }

        SideType relativeNorth = corner.GetSide(CardinalDirection.NORTH).type;
        SideType relativeEast = corner.GetSide(CardinalDirection.EAST).type;
        SideType relativeSouth = corner.GetSide(CardinalDirection.SOUTH).type;
        SideType relativeWest = corner.GetSide(CardinalDirection.WEST).type;
        bool northEdge = relativeNorth == SideType.EDGE;
        bool eastEdge = relativeEast == SideType.EDGE;
        bool southEdge = relativeSouth == SideType.EDGE;
        bool westEdge = relativeWest == SideType.EDGE;

        if (northEdge && eastEdge)
        {
            corner = corner.GetPieceIfSideIsRotatedFromXToY(corner.GetSide(CardinalDirection.NORTH), CardinalDirection.WEST);
        }
        else if (northEdge && westEdge)
        {
            // Do nothing, already correct
        }
        else if (eastEdge && southEdge)
        {
            corner = corner.GetPieceIfSideIsRotatedFromXToY(corner.GetSide(CardinalDirection.EAST), CardinalDirection.WEST);
        }
        else if (southEdge && westEdge)
        {
            corner = corner.GetPieceIfSideIsRotatedFromXToY(corner.GetSide(CardinalDirection.SOUTH), CardinalDirection.WEST);
        }
        
        Route route = RouteCreator.GetBestRoute(corner, edges);

        if (route.score == double.MaxValue)
        {
            Debug.LogError("Couldn't solve the jigsaw");
            return;
        }

        route.DrawRoute();

        PrintJigsaw(route.GetPieceArray());
        FillRemainingPieces(route.GetPieceArray(), remaining);
    }

    public void FillRemainingPieces(Piece[,] pieceArray, List<Piece> remainingPieces)
    {
        List<KeyValuePair<int, int>> toCheck = new List<KeyValuePair<int, int>>(); 
        // look for empty spaces
        for (int i = 0; i < pieceArray.GetLength(0); i++)
        {
            for (int j = 0; j < pieceArray.GetLength(1); j++)
            {
                if (pieceArray[i, j] == null)
                {
                    toCheck.Add(new KeyValuePair<int, int>(i, j));
                }
            }
        }

        for (int i = 0; i < toCheck.Count; i++)
        {
            // get the pair
            KeyValuePair<int, int> pair = toCheck[i];

            // check surrounding pieces
            List<Piece> neighbours = new List<Piece>();
            neighbours.Add(pieceArray[pair.Key, pair.Value + 1]);
            neighbours.Add(pieceArray[pair.Key, pair.Value - 1]);
            neighbours.Add(pieceArray[pair.Key + 1, pair.Value]);
            neighbours.Add(pieceArray[pair.Key - 1, pair.Value]);

            // if the piece has no neighbours then come back to it at the end when
            // we have some more pieces placed
            if (neighbours.Count == 0)
            {
                toCheck.Add(pair);
                continue;
            }

            // check all pieces, find best one
            for (int j = 0; j < remainingPieces.Count; j++)
            {
                Piece pieceToCheck = remainingPieces[j];

                // check if piece fits in any orientation
                foreach (Piece piece in neighbours)
                {
                    if (piece == null)
                        continue;

                    // try to adjust routecreator so that it snakes around and checks every side, maybe
                }
            }
        }
    }

    public void PrintJigsaw(Piece[,] pieceArray)
    {
        string s = "";
        for (int i = 0; i < pieceArray.GetLength(0); i++)
        {
            for (int j = 0; j < pieceArray.GetLength(1); j++)
            {
                if (pieceArray[i, j] == null)
                    s += "  ";
                else
                    s += pieceArray[i, j].id + " ";
            }
            s += "\n";
        }
        Debug.Log(s);
    }

}