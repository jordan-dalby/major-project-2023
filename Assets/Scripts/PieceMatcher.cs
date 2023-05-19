using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UtilsModule;
using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Attempts to match piece contours
 * Takes in two pieces, returns match data
 */
public class PieceMatcher
{

    /// <summary>
    /// Should match attempts be drawn?
    /// </summary>
    private bool drawMatchAttempts = false;

    /// <summary>
    /// The first piece
    /// </summary>
    private Piece pieceOne;
    /// <summary>
    /// The second piece
    /// </summary>
    private Piece pieceTwo;

    /// <summary>
    /// The number at which a contour match below is a "good match"
    /// </summary>
    private const double pieceMatchMax = 10f;
    
    /// <summary>
    /// The number at which a colour match below is a "good match"
    /// </summary>
    private const double pieceColourMatchMax = 40f;

    /// <summary>
    /// The number at which a difference in side length below is a "good match"
    /// </summary>
    private const double pieceSideLengthDifferenceMax = 10f;

    /// <summary>
    /// The number at which a difference in defect distance is a "good match"
    /// </summary>
    private const double pieceSideDefectDifferenceMax = 2f;


    private int count = 0;

    /// <summary>
    /// Constructor that takes in the first and second piece for comparison
    /// </summary>
    /// <param name="_pieceOne">The first piece</param>
    /// <param name="_pieceTwo">The second piece</param>
    public PieceMatcher(Piece _pieceOne, Piece _pieceTwo)
    {
        pieceOne = _pieceOne;
        pieceTwo = _pieceTwo;

        Run();
    }

    /// <summary>
    /// Compares two pieces entirely, inclusive only of sides where a tab meets a blank, populating each piece with
    /// a list of side matches with their probable fit
    /// </summary>
    public void Run()
    {
        #region Meta Match
        // A corner piece can only be connected to an edge piece, so we can rule any pieces out that don't
        // conform to that rule
        if ((pieceOne.GetNumberOfEdges() == 2 && pieceTwo.GetNumberOfEdges() != 1) || (pieceTwo.GetNumberOfEdges() == 2 && pieceOne.GetNumberOfEdges() != 1))
        {
            // Can't possibly match
            return;
        }
        #endregion

        foreach (Side pieceOneSide in pieceOne.sides)
        {
            // Edges won't have any connections, skip them
            if (pieceOneSide.type == SideType.EDGE)
                continue;

            List<Point> pieceOnePoints = pieceOneSide.rotatedPoints;

            Side north = pieceOne.GetSide(CardinalDirection.NORTH);
            Side east = pieceOne.GetSide(CardinalDirection.EAST);
            Side south = pieceOne.GetSide(CardinalDirection.SOUTH);
            Side west = pieceOne.GetSide(CardinalDirection.WEST);

            foreach (Side pieceTwoSide in pieceTwo.sides)
            {
                // Edges can't have connections, skip
                if (pieceTwoSide.type == SideType.EDGE)
                    continue;

                // Both have the same type, they can't be connected so skip
                if (pieceOneSide.type == pieceTwoSide.type)
                    continue;

                #region Check adjacent edges
                // All matches must have one edge since this is a corner, PieceMatcher ensures this
                CardinalDirection opposite = PieceFunctions.GetOpposite(pieceOneSide.direction);
                Piece attachedPiece = pieceTwo.GetPieceIfSideIsRotatedFromXToY(pieceTwoSide, opposite);

                Side attachedNorth = attachedPiece.GetSide(CardinalDirection.NORTH);
                Side attachedEast = attachedPiece.GetSide(CardinalDirection.EAST);
                Side attachedSouth = attachedPiece.GetSide(CardinalDirection.SOUTH);
                Side attachedWest = attachedPiece.GetSide(CardinalDirection.WEST);

                // Check to make sure that any adjacent edges are edges on both pieces
                if (pieceOneSide.direction == CardinalDirection.NORTH || pieceOneSide.direction == CardinalDirection.SOUTH)
                {
                    // Check east and west
                    // If either of the easts are edges, are they both an edge?
                    if ((east.type == SideType.EDGE || attachedEast.type == SideType.EDGE) && (east.type != attachedEast.type))
                    {
                        continue;
                    }
                    if ((west.type == SideType.EDGE || attachedWest.type == SideType.EDGE) && (west.type != attachedWest.type))
                    {
                        continue;
                    }
                }
                else if (pieceOneSide.direction == CardinalDirection.EAST || pieceOneSide.direction == CardinalDirection.WEST)
                {
                    if ((north.type == SideType.EDGE || attachedNorth.type == SideType.EDGE) && (north.type != attachedNorth.type))
                    {
                        continue;
                    }
                    if ((south.type == SideType.EDGE || attachedSouth.type == SideType.EDGE) && (south.type != attachedSouth.type))
                    {
                        continue;
                    }
                }
                #endregion

                List<Point> pieceTwoPoints = pieceTwoSide.rotatedPoints;

                #region Length Match
                double pieceOneSideLength = Math.Abs(pieceOnePoints[pieceOnePoints.Count - 1].x - pieceOnePoints[0].x);
                double pieceTwoSideLength = Math.Abs(pieceTwoPoints[pieceTwoPoints.Count - 1].x - pieceTwoPoints[0].x);

                double normalisedLengthDifference = Math.Abs(pieceOneSideLength - pieceTwoSideLength) / pieceSideLengthDifferenceMax;
                #endregion

                #region Contour Match
                Mat pieceOneMat = Converters.vector_Point_to_Mat(pieceOnePoints);
                Mat pieceTwoMat = Converters.vector_Point_to_Mat(pieceTwoPoints);

                // Lower is better
                double matchAmount = Imgproc.matchShapes(pieceOneMat, pieceTwoMat, Imgproc.CONTOURS_MATCH_I1, 0);

                // Any number between 0 and 10 is good, anything else is probably not a match
                double normalisedMatchAmount = matchAmount / pieceMatchMax;
                #endregion

                #region Colour Match
                // First Point : Last Point | Middle Point : Middle Point | Last Point : First Point
                double firstMatch = pieceOneSide.colourSamples[0].GetLabDistance(pieceTwoSide.colourSamples[2]);
                double secondMatch = pieceOneSide.colourSamples[1].GetLabDistance(pieceTwoSide.colourSamples[1]);
                double thirdMatch = pieceOneSide.colourSamples[2].GetLabDistance(pieceTwoSide.colourSamples[0]);

                double normalisedFirstMatch = firstMatch / pieceColourMatchMax;
                double normalisedSecondMatch = secondMatch / pieceColourMatchMax;
                double normalisedThirdMatch = thirdMatch / pieceColourMatchMax;

                double normalisedColourMatch = normalisedFirstMatch + normalisedSecondMatch + normalisedThirdMatch;
                #endregion

                double matchLikeliness = normalisedMatchAmount + normalisedColourMatch + normalisedLengthDifference;

                if (matchLikeliness >= 15f)
                    continue;

                pieceOneSide.matches.Add(pieceTwoSide, matchLikeliness);
                pieceTwoSide.matches.Add(pieceOneSide, matchLikeliness);

                count++;

                #region Draw the match attempt
                if (drawMatchAttempts)
                {
                    Mat colourMatch = pieceOneSide.colourSamples[0].GetColourMatchSwatch(pieceOneSide.colourSamples[0], pieceOneSide.colourSamples[1], pieceOneSide.colourSamples[2], pieceTwoSide.colourSamples[2], pieceTwoSide.colourSamples[1], pieceTwoSide.colourSamples[0]);
                    Mat one = pieceOne.GetSide(pieceOneSide);
                    Mat two = pieceTwo.GetSide(pieceTwoSide);

                    Size max = new Size(Math.Max(one.size().width, Math.Max(two.size().width, colourMatch.size().width)), Math.Max(one.size().height, Math.Max(two.size().height, colourMatch.size().height)));
                    Imgproc.resize(one, one, max);
                    Imgproc.resize(two, two, max);
                    Imgproc.resize(colourMatch, colourMatch, max);

                    Mat combined = new Mat();
                    Core.hconcat(new List<Mat>() { one, two, colourMatch }, combined);

                    Imgproc.putText(combined, matchLikeliness.ToString(), new Point(1, 50), Imgproc.FONT_HERSHEY_COMPLEX_SMALL, 0.8, new Scalar(255, 255, 255, 255), 1, 4, true);
                    ProcessSprites.instance.AddProcessImage(combined, "Comparison");
                }
                #endregion
            }
        }
    }

    public int GetCount()
    {
        return count;
    }

}
