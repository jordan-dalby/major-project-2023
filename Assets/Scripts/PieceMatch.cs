using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PieceMatch
{
    public Piece piece;
    public int sideIndex;

    public List<Side> matches;
    public int matchIndex;

    public PieceMatch(Piece _piece, int _sideIndex, List<Side> _matches)
    {
        piece = _piece;
        sideIndex = _sideIndex;
        matches = _matches;
        matchIndex = 0;
    }

    public void IncreaseMatchIndex()
    {
        matchIndex++;
    }

    public Side GetCurrentMatch()
    {
        return matches[matchIndex];
    }
}
