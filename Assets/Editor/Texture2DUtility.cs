using System;
using UnityEngine;
using UnityEditor;

public class Texture2DUtility
{
    //矩形コピーユーティリティ
    public static void CopyPixels(Texture2D src, Texture2D dst, RectInt srcRect, RectInt dstRect)
    {
        for (int y = 0; y < srcRect.height; y++)
        {
            var sy = srcRect.y + y;
            var dy = dstRect.y + y;

            for (int x = 0; x < srcRect.width; x++)
            {
                var sx = srcRect.x + x;
                var dx = dstRect.x + x;

                dst.SetPixel(dx, dy, src.GetPixel(sx, sy));
            }
        }
    }

    /// <summary>
    /// Spriteの四辺の色を指定Pixel数分伸ばします
    /// </summary>
    public static void Extrude(Texture2D texture, RectInt spriteRect, int extrude)
    {
        //Debug.Log(spriteRect);
        //Debug.Log($"xMax:{spriteRect.xMax}, yMax:{spriteRect.yMax}");

        //下辺のExtrude
        for (int y = spriteRect.y - 1; y >= spriteRect.y - extrude; y--)
        {
            if (y < 0) break;

            for (int x = spriteRect.x; x < spriteRect.xMax; x++)
            {
                var color = texture.GetPixel(x, spriteRect.y);
                texture.SetPixel(x, y, color);
            }
        }

        //上辺のExtrude
        for (int y = spriteRect.yMax; y < spriteRect.yMax + extrude; y++)
        {
            if (y >= texture.height) break;

            for (int x = spriteRect.x; x < spriteRect.xMax; x++)
            {
                var color = texture.GetPixel(x, spriteRect.yMax - 1);
                texture.SetPixel(x, y, color);
            }
        }

        //左片のExtrude
        for (int x = spriteRect.x - 1; x >= spriteRect.x - extrude; x--)
        {
            if (x < 0) break;

            for (int y = spriteRect.y; y < spriteRect.yMax; y++)
            {
                var color = texture.GetPixel(spriteRect.x, y);
                texture.SetPixel(x, y, color);
            }
        }

        //右片のExtrude
        for (int x = spriteRect.xMax; x < spriteRect.xMax + extrude; x++)
        {
            if (x >= texture.width) break;

            for (int y = spriteRect.y; y < spriteRect.yMax; y++)
            {
                var color = texture.GetPixel(spriteRect.xMax - 1, y);
                texture.SetPixel(x, y, color);
            }
        }
    }
}
