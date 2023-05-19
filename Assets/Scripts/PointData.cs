using OpenCVForUnity.CoreModule;

public class PointData
{
    private Mat magnitude;
    private Mat angle;

    private Point point;
    private double data;

    private Point cartesianPoint;

    public PointData(Point _cartesianPoint, Mat _magnitude, Mat _angle, double _data)
    {
        cartesianPoint = _cartesianPoint;
        magnitude = _magnitude;
        angle = _angle;
        point = new Point(_angle.get(0, 0)[0], _magnitude.get(0, 0)[0] * 5);
        data = _data;
    }

    public Mat GetMagnitude()
    {
        return magnitude;
    }

    public Mat GetAngle()
    {
        return angle;
    }

    public Point GetNormalizedPoint()
    {
        return new Point(data, point.y);
    }

    public Point GetPoint()
    {
        return point;
    }

    public Point GetCartesianPoint()
    {
        return cartesianPoint;
    }
}
