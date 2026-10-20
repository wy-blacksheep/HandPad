using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using UnityEngine.Assertions;
using TMPro;

[RequireComponent(typeof(HandInteractionManager))]
public class CustomPseudoDirectPoke : MonoBehaviour
{
    [SerializeField]
    private GameObject _cursor;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private Color _outlineColor = Color.black;

    [SerializeField]
    private float _offsetAlongNormal = 0.001f;

    [SerializeField]
    private PokeInteractor _pokeInteractor;

    [SerializeField]
    private float _overshootMaxDistance = 1f;

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

    [SerializeField] private GameObject targetCanvas;

    private Vector3 _startScale;

    private TextMeshProUGUI text;
    private HandInteractionManager interactionManager;

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
        this.AssertField(_pokeInteractor, nameof(_pokeInteractor));
        this.AssertField(_renderer, nameof(_renderer));
        this.AssertField(_cursor, nameof(_cursor));
        this.AssertField(_selectObject, nameof(_selectObject));

        this.EndStart(ref _started);

        interactionManager = GetComponent<HandInteractionManager>();
    }

    protected virtual void OnEnable()
    {
        if (_started)
        {
            _pokeInteractor.WhenPostprocessed += UpdateVisual;
            _pokeInteractor.WhenStateChanged += UpdateVisualState;
            UpdateVisual();
        }
    }

    protected virtual void OnDisable()
    {
        if (_started)
        {
            _pokeInteractor.WhenPostprocessed -= UpdateVisual;
            _pokeInteractor.WhenStateChanged -= UpdateVisualState;
        }
    }

    private void UpdateVisual()
    {
        //text.SetText(_pokeInteractor.State.ToString());

        if (_pokeInteractor.State == InteractorState.Disabled)
        {
            _cursor.SetActive(false);
            return;
        }

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        //this.transform.position = _pokeInteractor.TouchPoint + _pokeInteractor.TouchNormal * _offsetAlongNormal;
        //GameObject.Find("test").transform.position = GetHoverCursor();

        //GameObject.Find("test").transform.position = GetHoverCursor();
        //_cursor.transform.position = GetHoverCursor() - targetSurface.transform.forward * _offsetAlongNormal;

        _cursor.transform.position = interactionManager.GetWebviewCursorPosition(1) - targetSurface.transform.forward * _offsetAlongNormal;
        _cursor.transform.rotation = Quaternion.LookRotation(- targetSurface.transform.forward, targetSurface.transform.up);

        if (PlayerHead != null)
        {
            float distance = Vector3.Distance(this.transform.position, PlayerHead.position);
            this.transform.localScale = _startScale * distance;
        }

        if (_pokeInteractor.State == InteractorState.Select)
        {
            if (interactionManager.GetInteractionFingerTouchIntention())
            {
                _selectObject.SetActive(true);
            }
            else
            {
                _selectObject.SetActive(false);
            }
            _renderer.material.SetFloat(_shaderRadialGradientScale, 0.15f);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);

            //_cursor.transform.position = GetHoverCursor() - targetSurface.transform.forward * _offsetAlongNormal;
            //_cursor.transform.rotation = Quaternion.LookRotation(- targetSurface.transform.forward, Vector3.up);
        }
        else
        {
            if (interactionManager.GetInteractionFingerTouchIntention())
            {
                _selectObject.SetActive(true);
            }
            else
            {
                _selectObject.SetActive(false);
            }

            Vector3 hitPoint = GetHoverCursor();

            var hoverDistance = hoverDis;

            







            
            var mappedHoverDistance = Mathf.Clamp(hoverDistance / _maxHoverDistance, 0f, 1f);
            var radialScale = mappedHoverDistance;
            radialScale = Mathf.Max(radialScale, .15f);

            //text.SetText(radialScale.ToString());

            _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedHoverDistance, .9f));
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedHoverDistance));
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
    }

    private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

    private Vector3 GetHoverCursor()
    {
        Vector3 originCursor = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
        //GameObject.Find("test").transform.position = originCursor;

        Vector3 cursorDirection = targetSurface.transform.forward;
        Vector3 hitPointOnWebpage;

        if (Physics.Raycast(originCursor, cursorDirection, out RaycastHit hit1, 1f, targetLayer))
        {
            hitPointOnWebpage = hit1.point;
            hoverDis = hit1.distance;
        }
        else if (Physics.Raycast(originCursor, - cursorDirection, out RaycastHit hit2, 1f, targetLayer))
        {
            hitPointOnWebpage = hit2.point;
            hoverDis = hit2.distance;
        }
        else
        {
            hitPointOnWebpage = new Vector3(10000, 10000, 10000);
            hoverDis = 10000f;
        }

        //GameObject.Find("test1").transform.position = hitPointOnWebpage;

        return hitPointOnWebpage;
    }

    public Vector3 GetTouchPoint()
    {
        return GetHoverCursor();
        //return _pokeInteractor.TouchPoint;
    }

    public Vector3 GetTouchNormal()
    {
        return _pokeInteractor.TouchNormal;
    }

    public bool IsIndexFingerTouch()
    {
        if (_pokeInteractor != null)
        {
            return (_pokeInteractor.State == InteractorState.Select);
        }
        else
        {
            return false;
        }
    }


}
