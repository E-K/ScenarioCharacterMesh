using System;
using System.Linq;
using UnityEngine;

public static class SpriteExtensions
{
    public static Rect UV(this Sprite sprite)
    {
        var u0 = sprite.rect.xMin / sprite.rect.width;
        var u1 = sprite.rect.xMax / sprite.rect.width;
        var v0 = sprite.rect.yMin / sprite.rect.height;
        var v1 = sprite.rect.yMax / sprite.rect.height;
        return new Rect(u0, u1, u1 - u0, v1 - v0);
    }

    public static Rect VerticesRect(this Sprite sprite)
    {
        var xMin = float.PositiveInfinity;
        var xMax = float.NegativeInfinity;
        var yMin = float.PositiveInfinity;
        var yMax = float.NegativeInfinity;

        foreach(var p in sprite.vertices)
        {
            if (p.x < xMin) xMin = p.x;
            if (p.x > xMax) xMax = p.x;
            if (p.y < yMin) yMin = p.y;
            if (p.y > yMax) yMax = p.y;
        }

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    public static Rect ToPivotRect(this Sprite sprite)
    {
        return new Rect(-sprite.pivot, sprite.rect.size);
    }

    public static Mesh ToMesh(this Sprite sprite)
    {
        var mesh = new Mesh();
        mesh.name = sprite.name;
        mesh.vertices = Array.ConvertAll(sprite.vertices, x => (Vector3)x);
        mesh.uv = sprite.uv;
        mesh.triangles = Array.ConvertAll(sprite.triangles, x => (int)x);
        return mesh;
    }
}
