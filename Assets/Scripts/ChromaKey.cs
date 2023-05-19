using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;

/// <summary>
/// Class for performing different chroma keying operations, such as K-means and inRange
/// </summary>
public class ChromaKey
{

    public static Scalar lowGreen = new Scalar(0, 130, 0, 0);
    public static Scalar highGreen = new Scalar(130, 255, 130, 255);
    public static int greenKMeansSamples = 11;

    public static Scalar lowRed = new Scalar(190, 0, 0, 0);
    public static Scalar highRed = new Scalar(255, 80, 80, 255);
    public static int redKMeansSamples = 100;

    public static Scalar low = lowGreen;
    public static Scalar high = highGreen;
    public static int kMeansSamples = greenKMeansSamples;

    /// <summary>
    /// Key a mat with regular inRange operation, blurring the output image
    /// </summary>
    /// <param name="mat">The mat to key</param>
    /// <param name="transparency">Should the mat support transparency/</param>
    /// <param name="show">Should the output mat be shown in the process list?</param>
    /// <returns>The chroma keyed mat</returns>
    public Mat Key(Mat mat, bool transparency = false, bool show = true)
    {
        Mat newMat = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC4);
        mat.copyTo(newMat);

        if (transparency)
        {
            Imgproc.cvtColor(newMat, newMat, Imgproc.COLOR_BGR2BGRA);
        }

        // chroma key the image to more accurately determine contours
        Mat chromaKeyed = new Mat();
        Core.inRange(newMat, low, high, chromaKeyed);

        Mat output = new Mat();
        chromaKeyed.copyTo(output);

        Core.bitwise_not(output, output);

        Imgproc.medianBlur(output, output, 5);
        Imgproc.GaussianBlur(output, output, new Size(3, 3), 3);

        if (show)
            ProcessSprites.instance.AddProcessImage(output, "ChromaKey");

        return output;
    }

    /// <summary>
    /// Returns a chroma keyed mat based on the input mat, uses K-means to reduce colour information before keying
    /// </summary>
    /// <param name="mat">The mat to key</param>
    /// <returns>The chroma keyed mat</returns>
    /// <reference>OpenCV for Unity examples, used for K-means calculation</reference>
    public Mat KeyKMeans(Mat mat)
    {
        mat = mat.clone();

        // convert to 3-channel color image (RGBA to RGB).
        Mat imgMatRGB = new Mat(mat.rows(), mat.cols(), CvType.CV_8UC3);
        Imgproc.cvtColor(mat, imgMatRGB, Imgproc.COLOR_RGBA2RGB);

        // reshape the image to be a 1 column matrix.
        Mat samples = imgMatRGB.reshape(3, imgMatRGB.cols() * imgMatRGB.rows());
        Mat samples32f = new Mat();
        samples.convertTo(samples32f, CvType.CV_32F, 1.0 / 255.0);

        // run k-means clustering algorithm to segment pixels in RGB color space.
        Mat labels = new Mat();
        TermCriteria criteria = new TermCriteria(TermCriteria.COUNT, 100, 1);
        Mat centers = new Mat();
        Core.kmeans(samples32f, kMeansSamples, labels, criteria, 1, Core.KMEANS_PP_CENTERS, centers);

        // make a each centroid represent all pixels in the cluster.
        centers.convertTo(centers, CvType.CV_8U, 255.0);
        int rows = 0;
        for (int y = 0; y < imgMatRGB.rows(); y++)
        {
            for (int x = 0; x < imgMatRGB.cols(); x++)
            {
                int label = (int)labels.get(rows, 0)[0];
                int r = (int)centers.get(label, 0)[0];
                int g = (int)centers.get(label, 1)[0];
                int b = (int)centers.get(label, 2)[0];
                imgMatRGB.put(y, x, r, g, b);
                rows++;
            }
        }

        // convert to 4-channel color image (RGB to RGBA).
        Imgproc.cvtColor(imgMatRGB, mat, Imgproc.COLOR_RGB2RGBA);

        // create an output image
        Mat chromaKeyed = new Mat();
        // run the inRange with a low and high value, where high is the upper boundary of the thresholding operation
        // and low is the lower boundary
        Core.inRange(mat, low, high, chromaKeyed);

        Mat output = new Mat();
        chromaKeyed.copyTo(output);

        Core.bitwise_not(output, output);

        ProcessSprites.instance.AddProcessImage(output, "test");

        return output;
    }

}
