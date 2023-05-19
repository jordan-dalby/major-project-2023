using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Joins are used in the JigsawAssembler, they represent a central piece, and the sides of the connected piece, but NOT the piece itself
/// </summary>
public class Join
{

    public Piece centralPiece;

    public Join(Piece _piece)
    {
        centralPiece = _piece;
    }

}
