using System;

namespace TrainService.Core.Geometry;

public readonly record struct Vector2D(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);
    public Vector2D Normalized() { var l = Length; return l < 1e-9 ? default : new(X / l, Y / l); }
    public static double Dot(Vector2D a, Vector2D b) => a.X * b.X + a.Y * b.Y;
    public static double Cross(Vector2D a, Vector2D b) => a.X * b.Y - a.Y * b.X; 
    public Vector2D PerpendicularCW() => new(Y, -X);
    
    // Operatörler:
    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);
    public static Vector2D operator *(double scalar, Vector2D a) => new(a.X * scalar, a.Y * scalar);
    public static Vector2D operator /(Vector2D a, double scalar) => new(a.X / scalar, a.Y / scalar);
}

public readonly record struct BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public bool Contains(BoundingBox other)
        => other.MinX >= MinX && other.MaxX <= MaxX
        && other.MinY >= MinY && other.MaxY <= MaxY;

    public bool IntersectsWith(BoundingBox other)
        => !(other.MinX > MaxX || other.MaxX < MinX
          || other.MinY > MaxY || other.MaxY < MinY);

    public static BoundingBox FromPoint(Vector2D p, double tol)
        => new(p.X - tol, p.Y - tol, p.X + tol, p.Y + tol);
}
