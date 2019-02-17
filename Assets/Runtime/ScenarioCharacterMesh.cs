using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class ScenarioCharacterMesh : MonoBehaviour
{
    [SerializeField]
    private Sprite[] _sprites = null;

    [SerializeField]
    private int _spriteIndex = 0;
    public int SpriteIndex
    {
        get => _spriteIndex;
        set
        {
            if(_spriteIndex == value)
            {

                _spriteIndexchanged = true;
            }
        }
    }
    private bool _spriteIndexchanged = true;

    [SerializeField]
    private Color _color = Color.white;
    public Color Color
    {
        get => _color;
        set
        {
            if(_color != value)
            {
                _color = value;
                _colorChanged = true;
            }
        }
    }
    private bool _colorChanged = true;
    private bool _textureChanged = true;

    private MaterialPropertyBlock _block = null;

    private MeshFilter _meshFilter = null;
    public MeshFilter MeshFilter
    {
        get { return _meshFilter ?? (_meshFilter = GetComponent<MeshFilter>()); }
    }

    private MeshRenderer _meshRenderer = null;
    public MeshRenderer MeshRenderer
    {
        get { return _meshRenderer ?? (_meshRenderer = GetComponent<MeshRenderer>()); }
    }

    private Vector2[] _uvs = null;

    private void LateUpdate()
    {
        if (_sprites == null || _sprites.Length <= 0)
            return;

        if(_block == null)
        {
            _block = new MaterialPropertyBlock();
        }

        if(_textureChanged)
        {
            _textureChanged = true;
            _block.SetTexture("_MainTex", _sprites[0].texture);
            this.MeshRenderer.SetPropertyBlock(_block);
        }

        if (_colorChanged)
        {
            _colorChanged = false;
            _block.SetColor("_Color", _color);
            this.MeshRenderer.SetPropertyBlock(_block);
        }

        if (!_spriteIndexchanged)
            return;

        _spriteIndexchanged = false;

        var sprite = _sprites[_spriteIndex];

        var mesh =
#if UNITY_EDITOR
            Application.isPlaying ? this.MeshFilter.mesh : this.MeshFilter.sharedMesh;
#else
            this.MeshFilter.mesh;
#endif
        if (_uvs == null)
            _uvs = mesh.uv;

        float u0, u1, v0, v1;
        u0 = sprite.textureRect.xMin / sprite.texture.width;
        u1 = sprite.textureRect.xMax / sprite.texture.width;
        v0 = sprite.textureRect.yMin / sprite.texture.height;
        v1 = sprite.textureRect.yMax / sprite.texture.height;

        _uvs[0] = new Vector2(u0, v0);
        _uvs[1] = new Vector2(u0, v1);
        _uvs[2] = new Vector2(u1, v1);
        _uvs[3] = new Vector2(u1, v0);
        
        mesh.uv = _uvs;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _spriteIndexchanged = true;
        _colorChanged = true;
        _textureChanged = true;
    }
#endif
}

