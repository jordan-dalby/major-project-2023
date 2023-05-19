using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class JigsawSolver : MonoBehaviour
{

    private Texture2D texture;
    public Image previewImage;

    public Text timer;

    [SerializeField]
    private List<Piece> pieceList = new List<Piece>();

    public void TakePicture()
    {
        if (NativeCamera.CheckPermission() != NativeCamera.Permission.Granted)
        {
            NativeCamera.RequestPermission();
        }

        if (!NativeCamera.DeviceHasCamera())
        {
            Debug.LogError("Device does not have camera, cannot continue");
        }

        // src: https://github.com/yasirkula/UnityNativeCamera
        NativeCamera.Permission permission = NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                texture = NativeCamera.LoadImageAtPath(path, -1, false);
                if (texture == null)
                {
                    Debug.Log("Couldn't load image");
                    return;
                }

                texture = Resize(texture, 1360, 1024);

                // src: https://answers.unity.com/questions/938516/convert-texture2d-to-sprite.html
                UnityEngine.Rect sizeRect = new UnityEngine.Rect(0, 0, texture.width, texture.height);
                Sprite sprite = Sprite.Create(texture, sizeRect, Vector2.zero, 1);

                previewImage.sprite = sprite;

                RunJigsawSolver();
            }
        }, -1, true, NativeCamera.PreferredCamera.Rear);

    }

    // src: https://stackoverflow.com/questions/56949217/how-to-resize-a-texture2d-using-height-and-width
    Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
    {
        RenderTexture rt = new RenderTexture(targetX, targetY, 24);
        RenderTexture.active = rt;
        Graphics.Blit(texture2D, rt);
        Texture2D result = new Texture2D(targetX, targetY);
        result.ReadPixels(new UnityEngine.Rect(0, 0, targetX, targetY), 0, 0);
        result.Apply();
        return result;
    }

    public void RunJigsawSolver()
    {
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        pieceList.Clear();

        Mat mat = new Mat(texture.height, texture.width, CvType.CV_8UC3);
        Utils.texture2DToMat(texture, mat);

        MarkerIdentifier markerIdentifier = new MarkerIdentifier(mat);
        markerIdentifier.IdentifyMarkers();

        ChromaKey chromaKey = new ChromaKey();
        Mat chromaKeyed = chromaKey.KeyKMeans(mat);

        ContourIdentifier contourIdentifier = new ContourIdentifier();
        PieceSideIdentifier pieceSideIdentifier = new PieceSideIdentifier();
        PieceRotationCorrector pieceRotationCorrector = new PieceRotationCorrector();
        PieceSidePreparer pieceSidePreparer = new PieceSidePreparer();

        // loop all contours and process them
        List<MatOfPoint> contours = GetContoursOnMat(chromaKeyed);
        List<Piece> pieces = new List<Piece>();
        foreach (MatOfPoint matOfPoint in contours)
        {
            // identify contour, convex hull, and convexity defects
            contourIdentifier.ProcessContour(mat, matOfPoint);

            // get the piece that the ContourIdentifier found
            Piece piece = contourIdentifier.GetPiece();

            // if no piece was found, do not continue because we don't want to include this contour
            if (piece == null)
                continue;

            // add the new piece
            pieces.Add(piece);

            // handles rotation of the image preview, and all neccessary point lists within the piece
            // (hull points, side points)
            pieceRotationCorrector.GetRotationMatrixForPieceAndRotate(piece);

            // identify the corners of the piece, and subsequently, what each side is
            pieceSideIdentifier.IdentifyPieceSidesPolar(piece);

            // remove the green background from the piece so we can overlay pieces without overlap
            Imgproc.cvtColor(piece.pieceMat, piece.pieceMat, Imgproc.COLOR_BGR2BGRA);
            Mat transparentPieceMat = new Mat(piece.pieceMat.rows(), piece.pieceMat.cols(), CvType.CV_8UC4);
            transparentPieceMat.setTo(new Scalar(0, 0, 0, 0));
            Core.bitwise_and(piece.pieceMat, piece.pieceMat, transparentPieceMat, piece.pieceChromaMat);

            // assign the new transparent texture to the piece
            piece.pieceMat = transparentPieceMat;

            pieceSidePreparer.PreparePiece(piece);
        }

        int v = 0;
        // TODO: resize all pieces to 300x300, then we can create a final image canvas of 300*piececount x 300*piececount
        for (int i = 0; i < pieces.Count - 1; i++)
        {
            Piece piece = pieces[i];
            for (int j = i + 1; j < pieces.Count; j++)
            {
                PieceMatcher m = new PieceMatcher(piece, pieces[j]);
                v += m.GetCount();
            }
        }

        Debug.Log("Matches: " + v);

        JigsawAssembler jigsawAssembler = new JigsawAssembler(pieces);
        jigsawAssembler.RunAssembler();

        stopwatch.Stop();

        TimeSpan ts = stopwatch.Elapsed;
        string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

        Debug.Log("RouteCreator took " + elapsedTime);
        timer.text = elapsedTime;
    }

    public List<MatOfPoint> GetContoursOnMat(Mat mat)
    {
        // Find contours in base image, this is how we will split up the pieces
        // Output, contains a list of MatOfPoints, each MatOfPoint is an individual piece
        List<MatOfPoint> matOfPoints = new List<MatOfPoint>();
        // unused
        Mat hierarchy = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC3);

        // find contours using the findContours function, CHAIN_APPROX_SIMPLE uses only the required number of points
        // to represent the contour, for example, a point in one corner can be directly connected to another corner, without
        // the need for additional points
        Imgproc.findContours(mat, matOfPoints, hierarchy, Imgproc.RETR_TREE, Imgproc.CHAIN_APPROX_SIMPLE);

        return matOfPoints;
    }
}
