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

    public RectInt DiffRect;
    public RectInt DiffRectAdjustedByBlockSize;

    public string DestinationPath;
}
