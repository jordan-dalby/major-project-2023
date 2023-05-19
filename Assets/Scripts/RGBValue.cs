using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RGBValue
{

    // The RGB values of this colour
    public double r;
    public double g;
    public double b;

    public Vector4 lab;

    public RGBValue(double _r, double _g, double _b)
    {
        r = _r;
        g = _g;
        b = _b;

        ToLab();
    }

    private void ToLab()
    {
        lab = RGBToLab(new Vector4((float)r, (float)g, (float)b));
    }

    /// <summary>
    /// Converts an RGB colour to LAB
    /// </summary>
    /// <param name="color">The RGB colour</param>
    /// <returns>RGB converted to LAB</returns>
    /// <reference>https://stackoverflow.com/questions/58952430/rgb-xyz-and-xyz-lab-color-space-conversion-algorithm</reference>
    private Vector4 RGBToLab(Vector4 color)
    {
        float[] xyz = new float[3];
        float[] lab = new float[3];
        float[] rgb = new float[] { color[0], color[1], color[2], color[3] };

        rgb[0] = color[0] / 255.0f;
        rgb[1] = color[1] / 255.0f;
        rgb[2] = color[2] / 255.0f;

        if (rgb[0] > .04045f)
        {
            rgb[0] = (float)Math.Pow((rgb[0] + 0.055) / 1.055, 2.4);
        }
        else
        {
            rgb[0] = rgb[0] / 12.92f;
        }

        if (rgb[1] > .04045f)
        {
            rgb[1] = (float)Math.Pow((rgb[1] + 0.055) / 1.055, 2.4);
        }
        else
        {
            rgb[1] = rgb[1] / 12.92f;
        }

        if (rgb[2] > .04045f)
        {
            rgb[2] = (float)Math.Pow((rgb[2] + 0.055) / 1.055, 2.4);
        }
        else
        {
            rgb[2] = rgb[2] / 12.92f;
        }
        rgb[0] = rgb[0] * 100.0f;
        rgb[1] = rgb[1] * 100.0f;
        rgb[2] = rgb[2] * 100.0f;


        xyz[0] = ((rgb[0] * .412453f) + (rgb[1] * .357580f) + (rgb[2] * .180423f));
        xyz[1] = ((rgb[0] * .212671f) + (rgb[1] * .715160f) + (rgb[2] * .072169f));
        xyz[2] = ((rgb[0] * .019334f) + (rgb[1] * .119193f) + (rgb[2] * .950227f));


        xyz[0] = xyz[0] / 95.047f;
        xyz[1] = xyz[1] / 100.0f;
        xyz[2] = xyz[2] / 108.883f;

        if (xyz[0] > .008856f)
        {
            xyz[0] = (float)Math.Pow(xyz[0], (1.0 / 3.0));
        }
        else
        {
            xyz[0] = (xyz[0] * 7.787f) + (16.0f / 116.0f);
        }

        if (xyz[1] > .008856f)
        {
            xyz[1] = (float)Math.Pow(xyz[1], 1.0 / 3.0);
        }
        else
        {
            xyz[1] = (xyz[1] * 7.787f) + (16.0f / 116.0f);
        }

        if (xyz[2] > .008856f)
        {
            xyz[2] = (float)Math.Pow(xyz[2], 1.0 / 3.0);
        }
        else
        {
            xyz[2] = (xyz[2] * 7.787f) + (16.0f / 116.0f);
        }

        lab[0] = (116.0f * xyz[1]) - 16.0f;
        lab[1] = 500.0f * (xyz[0] - xyz[1]);
        lab[2] = 200.0f * (xyz[1] - xyz[2]);

        return new Vector4(lab[0], lab[1], lab[2], color[3]);
    }

    /// <summary>
    /// Gets the LAB distance between two colours
    /// </summary>
    /// <param name="other">The other colour</param>
    /// <returns>The Delta E colour distance from one colour to another using Cie76</returns>
    /// <reference>http://colormine.org/delta-e-calculator</reference>
    public double GetLabDistance(RGBValue other)
    {
        double deltaE = Math.Sqrt(Math.Pow((lab.x - other.lab.x), 2) + Math.Pow((lab.y - other.lab.y), 2) + Math.Pow((lab.z - other.lab.z), 2));
        return deltaE;
    }

    public Mat GetColourMatchSwatch(RGBValue color1, RGBValue color2, RGBValue color3, RGBValue color4, RGBValue color5, RGBValue color6)
    {
        Mat mat = new Mat(75, 50, CvType.CV_8UC4);
        for (int x = 0; x <= 50; x++)
        {
            for (int y = 0; y <= 75; y++)
            {
                if (x <= 24 && y <= 24)
                {
                    // top left corner
                    mat.put(y, x, new double[] { color1.r, color1.g, color1.b, 255 });
                }
                else if (x <= 24 && y <= 49)
                {
                    // middle left
                    mat.put(y, x, new double[] { color2.r, color2.g, color2.b, 255 });
                }
                else if (x <= 24 && y <= 74)
                {
                    // bottom left corner
                    mat.put(y, x, new double[] { color3.r, color3.g, color3.b, 255 });
                }
                else if (x > 24 && y <= 24)
                {
                    // top right corner
                    mat.put(y, x, new double[] { color4.r, color4.g, color4.b, 255 });
                }
                else if (x > 24 && y <= 49)
                {
                    // middle right
                    mat.put(y, x, new double[] { color5.r, color5.g, color5.b, 255 });
                }
                else if (x > 24 && y <= 74)
                {
                    // bottom right corner
                    mat.put(y, x, new double[] { color6.r, color6.g, color6.b, 255 });
                }
                else
                {
                    mat.put(y, x, new double[] { 0, 0, 0, 255 });
                }
            }
        }
        return mat;
    }

    public void DrawColour()
    {
        Mat mat = new Mat(25, 25, CvType.CV_8UC3, new Scalar(r, g, b));
        ProcessSprites.instance.AddProcessImage(mat, "RGB Swatch", false);
    }

}
