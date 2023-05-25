using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RouteCreator
{

    /// <summary>
    /// Takes in a startingPiece and a list of edge pieces and outputs the most likely solve of the edge pieces.
    /// Uses pruning to limit memory usage, such as calculating a score when a route is found, any subsequent routes
    /// are checked against this score, if they start to exceed the score then the route can be pruned since it
    /// will not beat the existing route.
    /// </summary>
    /// <param name="startingPiece">The piece to start with, should be a corner with WEST and NORTH as edges</param>
    /// <param name="pieces">The remaining edge pieces</param>
    /// <returns>The best route (solve)</returns>
    public static Route GetBestRoute(Piece startingPiece, List<Piece> pieces)
    {
        // Keep track of the best route
        Route bestRoute = new Route();
        bestRoute.score = double.MaxValue;

        // Create a queue to hold the routes we still need to check
        PriorityQueue<double> queuedRoutes = new PriorityQueue<double>();

        // Enqueue all combinations of the starting corner piece
        Side eastSide = startingPiece.GetSide(CardinalDirection.EAST);
        for (int i = 0; i < eastSide.matches.Keys.Count; i++)
        {
            // Create a new route
            Route newRoute = new Route();
            // Add the corner to it
            newRoute.AddPiece(startingPiece);
            // Set the match index of the route
            newRoute.currentPieceMatchIndex = i;
            // Add the remaining piece ids
            newRoute.remainingPieceIds = new List<int>(pieces.Select(e => e.id).ToList());
            // Queue the route
            queuedRoutes.Enqueue(newRoute, newRoute.GetScore());
        }

        // While the queuedRoutes queue contains any value
        while (!queuedRoutes.IsEmpty())
        {
            // Get the current route
            Route currentRoute = queuedRoutes.Dequeue();

            // If there is a current better route and we still have remaining pieces
            if (currentRoute.score > bestRoute.score && currentRoute.remainingPieceIds.Count > 1)
            {
                // Disregard route, there's a better one
                continue;
            }

            // Get the current piece
            Piece currentPiece = currentRoute.GetCurrentPiece();
            // Get the east side of the current piece
            eastSide = currentPiece.GetSide(CardinalDirection.EAST);

            // If we try to check a match that doesn't physically exist
            if (eastSide.matches.Count <= currentRoute.currentPieceMatchIndex)
            {
                // Not possible, discard attempt
                Debug.LogWarning("Tried to check an invalid index, why are we trying in the first place?");
                continue;
            }

            // Get the current match of this route
            KeyValuePair<Side, double> match = eastSide.matches.ElementAt(currentRoute.currentPieceMatchIndex);

            // If the piece is not available
            if (!(currentRoute.remainingPieceIds.Where(e => e == match.Key.piece.id).Any()))
            {
                // Discard this attempt, it won't work
                continue;
            }

            // Increase the current routes score, the lower the score the better
            currentRoute.score += match.Value;

            // Rotate the piece so that the matched edge is facing towards the current edge
            Piece rotatedPiece = match.Key.piece.GetPieceIfSideIsRotatedFromXToY(match.Key, CardinalDirection.WEST);

            // Add the newly rotated piece to the current route
            currentRoute.AddPiece(rotatedPiece);
            currentRoute.remainingPieceIds.Remove(rotatedPiece.id);

            // If a corner, rotate all of the pieces 90 degrees counter-clockwise
            if (rotatedPiece.GetNumberOfEdges() == 2)
            {
                // Rotate the entire route 90 degrees counter-clockwise
                currentRoute.RotateRouteToWest();
                // Reassign the rotatedPiece since we will have cloned it during the rotation process
                rotatedPiece = currentRoute.GetCurrentPiece();

                // If the first dimension is not yet found
                if (currentRoute.dimensions[0] == 0)
                {
                    // Set the first dimension, we have found another corner so we know how wide the jigsaw is
                    currentRoute.dimensions[0] = currentRoute.pieces.Count;
                    // If the first dimension of this branch is longer than the number of remaining pieces, it's definitely not a solution
                    if (currentRoute.dimensions[0] > currentRoute.remainingPieceIds.Count)
                    {
                        // Discard the branch
                        continue;
                    }
                }
                // If the first dimension is assigned and the second dimension isn't
                else if (currentRoute.dimensions[0] != 0 && currentRoute.dimensions[1] == 0)
                {
                    // Assign the second dimension and run a calculation to find the height of the jigsaw based on the first dimension
                    currentRoute.dimensions[1] = currentRoute.pieces.Count + 1 - currentRoute.dimensions[0];

                    int add = currentRoute.dimensions[0] + currentRoute.dimensions[1];

                    // If the combined size of both dimensions does not leave enough pieces for the jigsaw to be completed
                    if (currentRoute.remainingPieceIds.Count != (pieces.Count - add + 2))
                    {
                        // Sides combined do not leave enough pieces to complete the jigsaw in a perfect rectangle, discard the branch
                        continue;
                    }
                }
                else
                {
                    // Set the third dimension, which should equal the first dimension
                    int currentSideLength = currentRoute.pieces.Count - currentRoute.dimensions[0] - currentRoute.dimensions[1];
                    currentRoute.dimensions[2] = currentSideLength + 2;
                }
            }

            // If both dimensions have a value
            if (currentRoute.dimensions[0] != 0 && currentRoute.dimensions[1] != 0)
            {
                if (currentRoute.dimensions[2] == 0)
                {
                    // Calculate the length of the current side
                    int currentSideLength = currentRoute.pieces.Count - currentRoute.dimensions[0] - currentRoute.dimensions[1];

                    if (currentSideLength + 2 > currentRoute.dimensions[0])
                    {
                        // Side is too long, discard the branch
                        continue;
                    }
                }
            }
            
            // If there are no more pieces left to add
            if (currentRoute.remainingPieceIds.Count == 0)
            {
                // If the current route is better than the bestRoute, make it the new bestRoute
                if (currentRoute.score < bestRoute.score)
                {
                    bestRoute = currentRoute;
                }

                // If the average match likeliness is less than 2.5, just select the match, it's probably correct
                if (bestRoute.score <= 2.5f * (pieces.Count + 1))
                {
                    break;
                }

                // Continue, this branch is done
                continue;
            }

            // If total pieces used exceeds the total remaining pieces and we still don't have a second dimension, the solution is invalid
            if (currentRoute.pieces.Count > currentRoute.remainingPieceIds.Count && (currentRoute.dimensions[0] == 0 || currentRoute.dimensions[1] == 0))
            {
                continue;
            }

            // Get the east side of the rotated piece
            eastSide = rotatedPiece.GetSide(CardinalDirection.EAST);

            // If the side has no matches and there are pieces remaining (not including this one)
            if (eastSide.matches.Count == 0 && currentRoute.remainingPieceIds.Count != 1)
            {
                // Prune this branch
                continue;
            }

            // Loop over all the matches on the eastSide
            for (int i = 0; i < eastSide.matches.Keys.Count; i++)
            {
                // Get the match from the eastSide
                KeyValuePair<Side, double> localMatch = eastSide.matches.ElementAt(i);
                // If the match uses a piece that we don't have access to
                if (!currentRoute.remainingPieceIds.Where(e => e == localMatch.Key.piece.id).Any())
                {
                    // Discard, the piece is not available in this branch
                    continue;
                }

                // Create a new route
                Route newRoute = new Route(currentRoute);
                // Set the match index of the route
                newRoute.currentPieceMatchIndex = i;
                // Queue the route
                queuedRoutes.Enqueue(newRoute, newRoute.GetScore());
            }
        }

        // Return the completed routes
        return bestRoute;
    }

}