using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ProcessSprites : MonoBehaviour
{

    public static ProcessSprites instance;

    public Image processImage;

    [SerializeField]
    private List<Sprite> processSprites = new List<Sprite>();
    [SerializeField]
    private int index = 0;

    public bool log = false;

    void Awake()
    {
        instance = this;
    }

    private void AddProcessSprite(Sprite sprite)
    {
        processSprites.Add(sprite);
    }

    public void ChangeIndex(int i)
    {
        index += i;
        if (index < 0)
            index = 0;
        if (index >= processSprites.Count)
            index = processSprites.Count - 1;

        UpdateProcessSprite();
    }

    private void UpdateProcessSprite()
    {
        processImage.sprite = processSprites[index];
    }

    public void SaveToFile(Texture2D t)
    {
        SaveToFile(t, "");
    }

    public void SaveToFile(Texture2D t, string ext)
    {

#if PLATFORM_ANDROID // code can't run on android, so just return
        //return;
#endif

        byte[] bytes = t.EncodeToPNG();
        var dirPath = Application.persistentDataPath + "/SaveImages/";
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + DateTime.Now.ToFileTimeUtc() + ext + ".png", bytes);
    }

    public int AddProcessImage(Mat mat, string source, bool save = true, bool show = true)
    {
        Mat copy = mat.clone();
        if (show)
            Imgproc.putText(copy, (index + 1).ToString(), new Point(1, 1), Imgproc.FONT_HERSHEY_COMPLEX_SMALL, 0.8, new Scalar(200, 200, 255, 255), 1, 4, true);

        Texture2D texture2 = new Texture2D(copy.cols(), copy.rows(), TextureFormat.RGBA32, false);
        try
        {
            Utils.matToTexture2D(copy, texture2, false);
        }
        catch (Exception)
        {
            Mat newMat = GetRegularMat(copy);
            Utils.matToTexture2D(newMat, texture2, false);
        }

        Sprite sprite = Sprite.Create(texture2, new UnityEngine.Rect(0, 0, texture2.width, texture2.height), Vector2.zero, 1);

        if (show)
        {
            AddProcessSprite(sprite);
            ChangeIndex(1);
        }

        if (save)
            SaveToFile(texture2);

        if (log)
            Debug.Log(index + " from " + source);

        return index;
    }

    private Mat GetRegularMat(Mat mat)
    {
        Mat newMat = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC4);
        for (int i = 0; i < mat.rows(); i++)
        {
            for (int j = 0; j < mat.cols(); j++)
            {
                if (mat.get(i, j)[0] != 0)
                {
                    newMat.put(i, j, 255, 255, 255, 255);
                }
            }
        }
        return newMat;
    }

}
