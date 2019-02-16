using System;
using System.Linq;
using UnityEngine;

public static class SpriteExtensions
{
    public static Rect UV(this Sprite sprite)
    {
        var u0 = sprite.textureRect.xMin / sprite.texture.width;
        var u1 = sprite.textureRect.xMax / sprite.texture.width;
        var v0 = sprite.textureRect.yMin / sprite.texture.height;
        var v1 = sprite.textureRect.yMax / sprite.texture.height;
        return new Rect(u0, u1, u1 - u0, v1 - v0);
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
