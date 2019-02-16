using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

[CustomEditor(typeof(ScenarioCharacterAssetGenerator))]
public class ScenarioCharacterAssetGeneratorEditor : Editor
{
    const string TightL = "_Tight-L";
    const string TightR = "_Tight-R";
    const string TightB = "_Tight-B";
    const string TightT = "_Tight-T";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var t = target as ScenarioCharacterAssetGenerator;

        if (GUILayout.Button("表情差分画像生成"))
        {
            AdjustSettings(t);
            CheckDifferentPixels(t);
            EditorUtility.SetDirty(t);
            GenerateDiffTextures(t);
        }

        if(GUILayout.Button("Mesh生成"))
        {
            GenerateMesh(t);
        }
    }

    

    public static void AdjustSettings(ScenarioCharacterAssetGenerator source)
    {
        AdjustSettings(source.BaseTexture);
        foreach(var texture in source.DiffTextures)
        {
            AdjustSettings(texture);
        }
    }

    private static void AdjustSettings(Texture2D texture)
    {
        var path = AssetDatabase.GetAssetPath(texture);
        var t = (TextureImporter)AssetImporter.GetAtPath(path);
        t.isReadable = true;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.SaveAndReimport();
    }

    public static void CheckDifferentPixels(ScenarioCharacterAssetGenerator source)
    {
        MyEditorUtility.ProgressBar("CheckDifferentPixels", progress =>
        {
            var textures = source.DiffTextures;

            if (textures == null || textures.Length <= 1)
                return;

            //全てのテクスチャが同サイズであるかチェック
            var width = source.BaseTexture.width;
            var height = source.BaseTexture.height;

            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i].width != width || textures[i].height != height)
                {
                    Debug.LogError($"サイズ違いのテクスチャ:{textures[i].name}");
                    return;
                }
            }

            //色差分チェック
            
            //色に違いがあった最大のRectを探す
            int left = int.MaxValue;
            int right = -1;
            int top = -1;
            int bottom = int.MaxValue;

            bool isDifferentColor(Color src, Texture2D[] tx, int x, int y)
            {
                for (int i = 0; i < tx.Length; i++)
                {
                    var color = tx[i].GetPixel(x, y);
                    if (src != color)
                        return true;
                }
                return false;
            };

            float progressMax = width * height;

            progress.Report(("ほげ", 0));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = textures[0].GetPixel(x, y);
                    if (isDifferentColor(color, textures, x, y))
                    {
                        if (x < left) left = x;
                        if (x > right) right = x;
                        if (y < bottom) bottom = y;
                        if (y > top) top = y;
                    }
                }
            }

            source.DiffRect = new RectInt(left, bottom, right - left + 1, top - bottom + 1);

            //BlockSizeに応じた調整
            left -= (left % source.BlockSize);
            bottom -= (bottom % source.BlockSize);
            var w = right - left + 1;
            var wmod = w % source.BlockSize;
            w += (wmod == 0 ? 0 : source.BlockSize - wmod);
            var h = top - bottom + 1;
            var hmod = h % source.BlockSize;
            h += (hmod == 0 ? 0 : source.BlockSize - hmod);
            source.DiffRectAdjustedByBlockSize = new RectInt(left, bottom, w, h);
        });
    }

    public static void GenerateDiffTextures(ScenarioCharacterAssetGenerator source)
    {
        MyEditorUtility.ProgressBar("PackSprites", progress =>
        {
            var diffRect = source.DiffRectAdjustedByBlockSize;

            RectInt withRoundBlock(RectInt r, int blockSize) => new RectInt(r.x - blockSize, r.y - blockSize, r.width + blockSize * 2, r.height + blockSize * 2);

            var diffRectWithRoundBlock = withRoundBlock(diffRect, source.BlockSize);
            Debug.Log($"{diffRect}, {diffRectWithRoundBlock}");
            var diffWidth = diffRect.width;
            var diffHeight = diffRect.height;

            var baseWidth = source.BaseTexture.width;
            var baseHeight = source.BaseTexture.height;

            var diffHeightWithRoundBlock = diffHeight + (source.BlockSize * 2);
            var diffWidthWithRoundBlock = diffWidth + (source.BlockSize * 2);

            //
            //表情差分画像を収めるために何列必要か計算
            //

            //縦に並べることのできる数、Base画像のHeightは超えないものとする
            var rows = baseHeight / diffHeightWithRoundBlock;
            //全差分を納めることができる列数
            var columns = (source.DiffTextures.Length / rows) + (source.DiffTextures.Length % rows == 0 ? 0 : 1);

            //パックする画像の縦と横
            var totalHeight = baseHeight;
            var totalWidth = baseWidth + source.BlockSize + (diffWidthWithRoundBlock * columns);

            //パック後のテクスチャ。mipは不要
            var packedTexture = new Texture2D(totalWidth, totalHeight, TextureFormat.ARGB32, false);
            //変な色が入っていたので(0, 0, 0, 0)でクリアする
            for(int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < totalWidth; x++)
                {
                    packedTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
            }

            //まずはベース画像をコピー
            var baseRect = new RectInt(0, 0, baseWidth, baseHeight);
            Texture2DUtility.CopyPixels(source.BaseTexture, packedTexture, baseRect, baseRect);

            //次に差分画像をコピー
            var rects = new RectInt[source.DiffTextures.Length];
            for (int i = 0; i < source.DiffTextures.Length; i++)
            {
                var col = i / rows; //何列目
                var row = i % rows;

                //Pack後の位置
                var rect = new RectInt(baseWidth + (source.BlockSize * 2) + (diffWidthWithRoundBlock * col), (diffHeightWithRoundBlock * row) + source.BlockSize, diffWidth, diffHeight);
                rects[i] = rect;

                var rectWithRoundBlock = withRoundBlock(rect, source.BlockSize);
                Texture2DUtility.CopyPixels(source.DiffTextures[i], packedTexture, diffRectWithRoundBlock, rectWithRoundBlock);
            }

            var bytes = packedTexture.EncodeToPNG();
            var filepath = Application.dataPath + ("/../" + source.DestinationPath).Replace('/', '\\');
            File.WriteAllBytes(filepath, bytes);
            
            AssetDatabase.ImportAsset(source.DestinationPath);

            var t = (TextureImporter)AssetImporter.GetAtPath(source.DestinationPath);
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Multiple;
            var setting = new TextureImporterSettings();
            t.ReadTextureSettings(setting);
            setting.spriteMeshType = source.UseTightMesh ? SpriteMeshType.Tight : SpriteMeshType.FullRect;
            setting.spriteGenerateFallbackPhysicsShape = false;
            t.SetTextureSettings(setting);

            //
            // spritesheetを構築
            //

            SpriteMetaData createSpriteMeshData(string name, RectInt rect)
            {
                return new SpriteMetaData
                {
                    name = name,
                    border = Vector4.zero, //9sliceのborder
                    rect = new Rect(rect.x, rect.y, rect.width, rect.height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            };

            var sprites = new SpriteMetaData[source.DiffTextures.Length + (source.UseTightMesh ? 6 : 2)];
            sprites[0] = createSpriteMeshData(source.BaseTexture.name, baseRect);
            sprites[1] = createSpriteMeshData(source.DiffRectSpriteName, diffRect);
            for(int i = 0; i < source.DiffTextures.Length; i++)
            {
                sprites[i + 2] = createSpriteMeshData(source.DiffTextures[i].name, rects[i]);
            }

            if(source.UseTightMesh)
            {
                var offset = source.DiffTextures.Length + 2;
                //左側
                sprites[offset + 0] = createSpriteMeshData(TightL, new RectInt(baseRect.x, baseRect.y, diffRect.x - baseRect.x, baseRect.height));
                sprites[offset + 1] = createSpriteMeshData(TightR, new RectInt(diffRect.xMax, baseRect.y, baseRect.xMax - diffRect.xMax, baseRect.height));
                sprites[offset + 2] = createSpriteMeshData(TightB, new RectInt(diffRect.x, baseRect.y, diffRect.width, diffRect.y - baseRect.y));
                sprites[offset + 3] = createSpriteMeshData(TightT, new RectInt(diffRect.x, diffRect.yMax, diffRect.width, baseRect.yMax - diffRect.yMax));
            }

            t.spritesheet = sprites;
            EditorUtility.SetDirty(t);
            t.SaveAndReimport();
        });
    }

    private void GenerateMesh(ScenarioCharacterAssetGenerator source)
    {
        if(source.UseTightMesh)
        {
            GenerateTightMesh(source);
        }
        else
        {
            GenerateFullRectMesh(source);
        }
    }

    private void GenerateTightMesh(ScenarioCharacterAssetGenerator source)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(source.DestinationPath).OfType<Sprite>().ToArray();
        var baseSprite = sprites.First(x => x.name == source.BaseTexture.name);
        var texture = baseSprite.texture;
        var diffRectSprite = sprites.First(x => x.name == source.DiffRectSpriteName);
        var tightLSprite = sprites.First(x => x.name == TightL);
        var tightRSprite = sprites.First(x => x.name == TightR);
        var tightBSprite = sprites.First(x => x.name == TightB);
        var tightTSprite = sprites.First(x => x.name == TightT);
        
        //DiffRect部分のメッシュ
        var ppu = baseSprite.pixelsPerUnit;
        var diffRect = new Rect((diffRectSprite.textureRect.position - baseSprite.pivot) / ppu, diffRectSprite.textureRect.size / ppu);
        var diffRectUV = diffRectSprite.UV();
        var diffRectMesh = new Mesh();
        diffRectMesh.vertices = new Vector3[]
        {
            new Vector3(diffRect.xMin, diffRect.yMin),
            new Vector3(diffRect.xMin, diffRect.yMax),
            new Vector3(diffRect.xMax, diffRect.yMax),
            new Vector3(diffRect.xMax, diffRect.yMin),
        };
        diffRectMesh.uv = new Vector2[]
        {
            new Vector2(diffRectUV.xMin, diffRectUV.yMin),
            new Vector2(diffRectUV.xMin, diffRectUV.yMax),
            new Vector2(diffRectUV.xMax, diffRectUV.yMax),
            new Vector2(diffRectUV.xMax, diffRectUV.yMin),
        };
        diffRectMesh.triangles = new int[]
        {
            0,1,2,
            2,3,0,
        };
        var diffRectCombine = new CombineInstance
        {
            mesh = diffRectMesh,
            transform = Matrix4x4.identity,
        };
        var tightLCombine = new CombineInstance
        {
            mesh = tightLSprite.ToMesh(),
            transform = Matrix4x4.Translate((tightLSprite.textureRect.center - baseSprite.textureRect.center) / ppu),
        };
        var tightBCombine = new CombineInstance
        {
            mesh = tightBSprite.ToMesh(),
            transform = Matrix4x4.Translate((tightBSprite.textureRect.center - baseSprite.textureRect.center) / ppu),
        };
        var tightTCombine = new CombineInstance
        {
            mesh = tightTSprite.ToMesh(),
            transform = Matrix4x4.Translate((tightTSprite.textureRect.center - baseSprite.textureRect.center) / ppu),
        };
        var tightRCombine = new CombineInstance
        {
            mesh = tightRSprite.ToMesh(),
            transform = Matrix4x4.Translate((tightRSprite.textureRect.center - baseSprite.textureRect.center) / ppu),
        };

        var finalMesh = new Mesh();
        finalMesh.name = baseSprite.texture.name;
        finalMesh.CombineMeshes(new[]
        {
            diffRectCombine,
            tightLCombine,
            tightBCombine,
            tightTCombine,
            tightRCombine,
        });

        var meshPath = Path.ChangeExtension(source.DestinationPath, ".asset");
        AssetDatabase.CreateAsset(finalMesh, meshPath);
        AssetDatabase.Refresh();
    }

    private void GenerateFullRectMesh(ScenarioCharacterAssetGenerator source)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(source.DestinationPath).OfType<Sprite>().ToArray();
        var baseSprite = sprites.First(x => x.name == source.BaseTexture.name);
        var texture = baseSprite.texture;
        var diffRectSprite = sprites.First(x => x.name == source.DiffRectSpriteName);

        var ppu = baseSprite.pixelsPerUnit;
        var offset = baseSprite.textureRect.position - baseSprite.pivot;
        var diffOffset = diffRectSprite.textureRect.position - baseSprite.pivot;
        Debug.Log($"diffRect  textureRect:{diffRectSprite.textureRect}, textureRectOffset:{diffRectSprite.textureRectOffset} pos:{diffRectSprite.textureRect.position}");

        // 頂点のレイアウト
        //
        // 05-06-12-15  y3
        // |  |   |  |
        // |  11 13  |
        // |  01-02  |  y2
        // |  |   |  |
        // |  00-03  |  y1
        // |  08 09  |
        // |  |   |  |
        // 04-07-10-14  y0
        //
        // x0 x1 x2 x3

        #region 頂点(Position)
        //
        // 頂点の生成
        //
        var vertices = new Vector3[16];
        float x0, x1, x2, x3, y0, y1, y2, y3;
        x0 = offset.x / ppu;
        x1 = diffOffset.x / ppu;
        x2 = (diffOffset.x + diffRectSprite.textureRect.width) / ppu;
        x3 = (offset.x + baseSprite.textureRect.width) / ppu;
        y0 = offset.y / ppu;
        y1 = diffOffset.y / ppu;
        y2 = (diffOffset.y + diffRectSprite.textureRect.height) / ppu;
        y3 = (offset.y + baseSprite.textureRect.height) / ppu;

        Debug.Log($"offset:{offset}, diffOffset:{diffOffset} | {x0}, {x1}, {x2}, {x3} | {y0}, {y1}, {y2}, {y3}");

        //diffRectの頂点
        vertices[0] = new Vector3(x1, y1, 0);
        vertices[1] = new Vector3(x1, y2, 0);
        vertices[2] = new Vector3(x2, y2, 0);
        vertices[3] = new Vector3(x2, y1, 0);

        //その他の頂点
        vertices[4] = new Vector3(x0, y0, 0);
        vertices[5] = new Vector3(x0, y3, 0);
        vertices[6] = new Vector3(x1, y3, 0);
        vertices[7] = new Vector3(x1, y0, 0);

        vertices[8] = vertices[0];
        vertices[9] = vertices[3];
        vertices[10] = new Vector3(x2, y0, 0);
        vertices[11] = vertices[1];
        vertices[12] = new Vector3(x2, y3, 0);
        vertices[13] = vertices[2];
        vertices[14] = new Vector3(x3, y0, 0);
        vertices[15] = new Vector3(x3, y3, 0);
        #endregion

        #region UV
        //
        // UVの生成
        //
        var uvs = new Vector2[16];
        float u0, u1, u2, u3, v0, v1, v2, v3;
        u0 = baseSprite.textureRect.xMin / texture.width;
        u1 = diffRectSprite.textureRect.xMin / texture.width;
        u2 = diffRectSprite.textureRect.xMax / texture.width;
        u3 = baseSprite.textureRect.xMax / texture.width;
        v0 = baseSprite.textureRect.yMin / texture.height;
        v1 = diffRectSprite.textureRect.yMin / texture.height;
        v2 = diffRectSprite.textureRect.yMax / texture.height;
        v3 = baseSprite.textureRect.yMax / texture.height;

        uvs[0] = new Vector2(u1, v1);
        uvs[1] = new Vector2(u1, v2);
        uvs[2] = new Vector2(u2, v2);
        uvs[3] = new Vector2(u2, v1);

        uvs[4] = new Vector2(u0, v0);
        uvs[5] = new Vector2(u0, v3);
        uvs[6] = new Vector2(u1, v3);
        uvs[7] = new Vector2(u1, v0);

        uvs[8] = uvs[0];
        uvs[9] = uvs[3];
        uvs[10] = new Vector2(u2, v0);
        uvs[11] = uvs[1];
        uvs[12] = new Vector2(u2, v3);
        uvs[13] = uvs[2];
        uvs[14] = new Vector2(u3, v0);
        uvs[15] = new Vector2(u3, v3);
        #endregion

        #region Triangles
        int[] triangles = new int[]
        {
            //中央-中央
            0,1,2,
            2,3,0,

            //左
            4,5,6,
            6,7,4,

            //中央-下
            7,8,9,
            9,10,7,

            //中央-上
            11,6,12,
            12,13,11,

            //右
            10,12,15,
            15,14,10
        };
        #endregion

        var mesh = new Mesh();
        mesh.name = texture.name;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        var meshPath = Path.ChangeExtension(source.DestinationPath, ".asset");
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.Refresh();
    }
}
