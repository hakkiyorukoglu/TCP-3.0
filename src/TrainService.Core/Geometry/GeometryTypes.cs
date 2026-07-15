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

public readonly record struct BoundingBox(double MinX, double MinY, double MaxX, double MaxY);
