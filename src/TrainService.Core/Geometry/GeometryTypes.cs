using System;

namespace TrainService.Core.Geometry;

public readonly record struct Vector2D(double X, double Y)
{
    public double Length => Math.Sqrt(X * X + Y * Y);
}

public readonly record struct BoundingBox(double MinX, double MinY, double MaxX, double MaxY);
