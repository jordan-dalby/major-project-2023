using OpenCVForUnity.CoreModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using OpenCVForUnity.ImgprocModule;

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

        Piece[,] pieceArray = route.GetPieceArray();

        PrintJigsaw(pieceArray);
        FillRemainingPieces(pieceArray, remaining);

        ShowJigsaw(pieceArray);
    }

    public void FillRemainingPieces(Piece[,] pieceArray, List<Piece> remainingPieces)
    {
        List<KeyValuePair<int, int>> toCheck = new List<KeyValuePair<int, int>>(); 
        // look for empty spaces
        for (int i = 0; i < pieceArray.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < pieceArray.GetLength(1) - 1; j++)
            {
                if (pieceArray[i, j] == null)
                {
                    toCheck.Add(new KeyValuePair<int, int>(i, j));
                }
                else
                {
                    pieceArray[i, j] = pieceArray[i, j].GetPieceIfSideIsRotatedFromXToY(pieceArray[i, j].GetSide(CardinalDirection.WEST), CardinalDirection.EAST);
                }
            }
        }

        CardinalDirection[] orientations = new CardinalDirection[4] { CardinalDirection.NORTH, CardinalDirection.EAST, CardinalDirection.SOUTH, CardinalDirection.WEST };

        for (int i = 0; i < toCheck.Count; i++)
        {
            // get the pair
            KeyValuePair<int, int> pair = toCheck[i];

            Debug.Log(pair.Key + " " + pair.Value);

            // check surrounding pieces
            List<(Piece, int, int)> neighbours = new List<(Piece, int, int)>();
            neighbours.Add((pieceArray[pair.Key, pair.Value + 1], 0, 1));
            neighbours.Add((pieceArray[pair.Key, pair.Value - 1], 0, -1));
            neighbours.Add((pieceArray[pair.Key + 1, pair.Value], 1, 0));
            neighbours.Add((pieceArray[pair.Key - 1, pair.Value], -1, 0));

            // if the piece has no neighbours then come back to it at the end when
            // we have some more pieces placed
            if (neighbours.Count == 0)
            {
                toCheck.Add(pair);
                continue;
            }

            // check all pieces, find best one
            // average score
            double score = double.MaxValue;
            Piece bestPiece = null;
            for (int j = 0; j < remainingPieces.Count; j++)
            {
                Piece pieceToCheck = remainingPieces[j];

                // check if piece fits in any orientation
                foreach (CardinalDirection dir in orientations)
                {
                    Piece rotatedPiece = pieceToCheck.GetPieceIfSideIsRotatedFromXToY(pieceToCheck.GetSide(CardinalDirection.NORTH), dir);

                    // check all the neighbours
                    int matches = 0;
                    double currScore = double.MaxValue;
                    foreach ((Piece, int, int) piece in neighbours)
                    {
                        if (piece.Item1 == null)
                            continue;

                        Side checkingSide = null;
                        Side checkingAgainst = null;
                        if (piece.Item2 == 1)
                        {
                            checkingSide = piece.Item1.GetSide(CardinalDirection.SOUTH);
                            checkingAgainst = rotatedPiece.GetSide(CardinalDirection.NORTH);
                        }
                        else if (piece.Item2 == -1)
                        {
                            checkingSide = piece.Item1.GetSide(CardinalDirection.NORTH);
                            checkingAgainst = rotatedPiece.GetSide(CardinalDirection.SOUTH);
                        }
                        else if (piece.Item3 == 1)
                        {
                            checkingSide = piece.Item1.GetSide(CardinalDirection.WEST);
                            checkingAgainst = rotatedPiece.GetSide(CardinalDirection.EAST);
                        }
                        else if (piece.Item3 == -1)
                        {
                            checkingSide = piece.Item1.GetSide(CardinalDirection.EAST);
                            checkingAgainst = rotatedPiece.GetSide(CardinalDirection.WEST);
                        }

                        Debug.Log(checkingSide.type);
                        Debug.Log(checkingAgainst.type);

                        // if we try to match two sides that have the same type, they're definitely not a match
                        if (checkingSide.type == checkingAgainst.type)
                            break;

                        // if there is no match, give it a value of 15, we don't want to entirely rule it out
                        double val = 15;
                        if (checkingSide.matches.Where(e => e.Key.id == checkingAgainst.id).Count() != 0)
                        {
                            val = checkingSide.matches.Where(e => e.Key.id == checkingAgainst.id).First().Value;
                        }

                        matches++;
                        currScore += val;
                    }

                    currScore = currScore / (double)matches;
                    if (currScore < score)
                    {
                        score = currScore;
                        bestPiece = rotatedPiece;
                    }
                }
            }

            if (bestPiece == null)
            {
                Debug.LogError("Couldn't find a match");
            }
            else
            {
                remainingPieces.RemoveAll(e => e.id == bestPiece.id);
                bestPiece = bestPiece.GetPieceIfSideIsRotatedFromXToY(bestPiece.GetSide(CardinalDirection.WEST), CardinalDirection.EAST);
                pieceArray[pair.Key, pair.Value] = bestPiece;
            }
        }
    }

    public void PrintJigsaw(Piece[,] pieceArray)
    {
        string s = "";
        for (int i = 0; i < pieceArray.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < pieceArray.GetLength(1) - 1; j++)
            {
                if (pieceArray[i, j] == null)
                    s += "X ";
                else
                    s += pieceArray[i, j].id + " ";
            }
            s += "\n";
        }
        Debug.Log(s);
    }

    public void ShowJigsaw(Piece[,] pieceArray)
    {
        Size max = new Size(100, 100);
        List<Mat> toCombine = new List<Mat>();
        for (int i = 0; i < pieceArray.GetLength(0) - 1; i++)
        {
            List<Mat> toHConcat = new List<Mat>();
            for (int j = 0; j < pieceArray.GetLength(1) - 1; j++)
            {
                if (pieceArray[i, j] == null)
                {
                    Mat temp = new Mat(100, 100, CvType.CV_8UC4);
                    toHConcat.Add(temp);
                    continue;
                }
                Mat mat = pieceArray[i, j].pieceMat;
                Imgproc.resize(mat, mat, max);

                toHConcat.Add(mat);
            }
            Mat combined = new Mat();
            Core.hconcat(toHConcat, combined);
            toCombine.Add(combined);
        }

        Mat finalCombined = new Mat();
        Core.vconcat(toCombine, finalCombined);

        ProcessSprites.instance.AddProcessImage(finalCombined, "Fully Solved Jigsaw");
    }

}