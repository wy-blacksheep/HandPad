using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using UnityEngine.Assertions;
using TMPro;

[RequireComponent(typeof(HandInteractionManager))]
public class IndirectCursorController : MonoBehaviour
{
    [SerializeField]
    private GameObject _cursor;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private Color _outlineColor = Color.black;

    [SerializeField]
    private float _offsetAlongNormal = 0.005f;

    [SerializeField]
    private float _maxHoverDistance = 0.15f;

    [SerializeField]
    private GameObject _selectObject;

    protected bool _started = false;

    [Tooltip("Players head transform, used to maintain the same cursor size on screen as it is moved in the scene.")]
    [SerializeField, Optional]
    private Transform _playerHead;

    [SerializeField] private LayerMask targetLayer;

    [SerializeField] private GameObject targetSurface;

    private Vector3 _startScale;

    private TextMeshProUGUI text;
    private HandInteractionManager interactionManager;

    private bool isSelect = false;
    private bool isCursorActive = false;

    #region Properties
    public Transform PlayerHead
    {
        get
        {
            return _playerHead;
        }
        set
        {
            _playerHead = value;
            if (_started && value is null)
            {
                this.transform.localScale = _startScale;
            }
        }
    }

    public Color OutlineColor
    {
        get
        {
            return _outlineColor;
        }
        set
        {
            _outlineColor = value;
        }
    }

    public float OffsetAlongNormal
    {
        get
        {
            return _offsetAlongNormal;
        }
        set
        {
            _offsetAlongNormal = value;
        }
    }

    #endregion

    private int _shaderRadialGradientScale = Shader.PropertyToID("_RadialGradientScale");
    private int _shaderRadialGradientIntensity = Shader.PropertyToID("_RadialGradientIntensity");
    private int _shaderRadialGradientBackgroundOpacity = Shader.PropertyToID("_RadialGradientBackgroundOpacity");
    private int _shaderOutlineColor = Shader.PropertyToID("_OutlineColor");

    private float hoverDis = 10000;

    protected virtual void Start()
    {
        text = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();

        this.BeginStart(ref _started);
        this.AssertField(_renderer, nameof(_renderer));
        this.AssertField(_cursor, nameof(_cursor));
        this.AssertField(_selectObject, nameof(_selectObject));

        this.EndStart(ref _started);

        interactionManager = GetComponent<HandInteractionManager>();
    }

    void Update()
    {
        isCursorActive = interactionManager.GetIndirectCursorState();

        if (!isCursorActive)
        {
            _cursor.SetActive(false);
            return;
        }

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        if (PlayerHead != null)
        {
            float distance = Vector3.Distance(this.transform.position, PlayerHead.position);
            this.transform.localScale = _startScale * distance;
        }

        if (isSelect)
        {
            //_selectObject.SetActive(true);
            _selectObject.SetActive(false);
            _renderer.material.SetFloat(_shaderRadialGradientScale, 0.15f);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
        else
        {
            _selectObject.SetActive(false);

            float hoverDistance = 0.1f;

            var mappedHoverDistance = Mathf.Clamp(hoverDistance / _maxHoverDistance, 0f, 1f);
            var radialScale = mappedHoverDistance;
            radialScale = Mathf.Max(radialScale, .15f);

            _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedHoverDistance, .9f));
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedHoverDistance));
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }

        targetSurface = interactionManager.GetActiveVirtualSurfaceGameObject();
    }

    public void DisableIndirectCursor()
    {
        _cursor.SetActive(false);
    }

    public void EnableIndirectCursor()
    {
        Debug.Log("Enable cursor.");
        _cursor.SetActive(true);
    }

    public GameObject GetIndirectCursor()
    {
        return _cursor;
    }

    public void InitIndirectCursor()
    {
        if (targetSurface != null)
        {
            Debug.Log("Init cursor.");
            interactionManager.SetIndirectCursorByHeadGaze();
            _cursor.transform.position = interactionManager.GetIndirectCursorPosition() - targetSurface.transform.forward * _offsetAlongNormal;
            _cursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);
        }
    }

    public void UpdateIndirectCursor(float dx, float dy, float dt)
    {
        if (targetSurface != null)
        {
            interactionManager.UpdateIndirectCursorByHandTouch(dx, dy, dt);
            _cursor.transform.position = interactionManager.GetIndirectCursorPosition() - targetSurface.transform.forward * _offsetAlongNormal;
            _cursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);
        }
    }
}
