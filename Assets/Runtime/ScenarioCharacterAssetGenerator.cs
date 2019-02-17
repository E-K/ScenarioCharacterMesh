using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "_Generator", menuName = "ScenarioCharacterAssetGenerator")]
public class ScenarioCharacterAssetGenerator : ScriptableObject
{
    public Texture2D BaseTexture;
    public Texture2D[] DiffTextures;

    public string DiffRectSpriteName = "Default";

    public int BlockSize = 4;

    public bool UseTightMesh = true;

    public RectInt DiffRect;
    public RectInt DiffRectAdjustedByBlockSize;

    public SpriteAlignment SpriteAlignment;
    public Vector2 CustomPivot = new Vector2(0.5f, 0.5f);

    public string DestinationPath;
}
