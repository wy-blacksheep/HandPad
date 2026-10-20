using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction;
using TMPro;

[RequireComponent(typeof(HandInteractionManager))]
public class CustomRayPointer : MonoBehaviour
{
    [SerializeField]
    private RayInteractor _rayInteractor;

    [SerializeField]
    private GameObject rayCursor;

    [SerializeField]
    private GameObject _cursor;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private GameObject _selectObject;


    [SerializeField]
    private RayInteractor _nonDomRayInteractor;

    [SerializeField]
    private GameObject nonDomRayCursor;

    [SerializeField]
    private GameObject _nonDomCursor;

    [SerializeField]
    private Renderer _nonDomRenderer;

    [SerializeField]
    private GameObject _nonDomSelectObject;


    [SerializeField]
    private Color _outlineColor = Color.black;

    [SerializeField]
    private float _offsetAlongNormal = 0.005f;

    [SerializeField]
    private float _overshootMaxDistance = 0.15f;

    [SerializeField]
    private float _maxPinchDistance = 0.02f;

    private float pinchDis;

    [SerializeField, Interface(typeof(IHand))]
    private UnityEngine.Object _hand;
    public IHand Hand { get; private set; }

    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private GameObject targetSurface;

    protected bool _started = false;

    [Tooltip("Players head transform, used to maintain the same cursor size on screen as it is moved in the scene.")]
    [SerializeField, Optional]
    private Transform _playerHead;

    private Vector3 _startScale;

    private TextMeshProUGUI text;

    private HandInteractionManager interactionManager;

    private float hoverDis = 10000;

    protected virtual void Awake()
    {
        Hand = _hand as IHand;
    }

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

    // Start is called before the first frame update
    void Start()
    {
        text = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        interactionManager = GetComponent<HandInteractionManager>();
    }

    // Update is called once per frame
    void Update()
    {
        text.SetText(_rayInteractor.State.ToString());
        UpdateVisual();
        if (interactionManager.IsTwoHandInteraction())
        {
            UpdateNonDomVisual();
        }
    }

    private void UpdateVisual()
    {
        text.SetText(_rayInteractor.State.ToString());

        if (_rayInteractor.State == InteractorState.Disabled)
        {
            _cursor.SetActive(false);
            return;
        }

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        Vector3 originCursor = rayCursor.transform.position;
        Vector3 direction = rayCursor.transform.forward;

        var task = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;
        





        originCursor = _rayInteractor.Ray.origin;
        direction = _rayInteractor.Ray.direction;

        _cursor.transform.position = GetHoverCursor(originCursor, direction) - targetSurface.transform.forward * _offsetAlongNormal;
        _cursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);

        if (PlayerHead != null)
        {
            float distance = Vector3.Distance(this.transform.position, PlayerHead.position);
            this.transform.localScale = _startScale * distance;
        }

        if (_rayInteractor.State == InteractorState.Select)
        {
            _selectObject.SetActive(true);
            //_selectObject.SetActive(false);
            _renderer.material.SetFloat(_shaderRadialGradientScale, 0.15f);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
        else
        {
            _selectObject.SetActive(false);

            if (interactionManager.GetInteractionFingerTipDistance(0, 1) != 10000)
            {
                pinchDis = interactionManager.GetInteractionFingerTipDistance(0, 1);
            }
            else
            {
                pinchDis = _maxPinchDistance;
            }
            Debug.Log(pinchDis);

            var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
            var radialScale = mappedPinchDistance;
            radialScale = Mathf.Max(radialScale, .15f);
            _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
            _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
            _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
            _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
    }

    private void UpdateNonDomVisual()
    {
        if (_nonDomRayInteractor.State == InteractorState.Disabled)
        {
            _nonDomCursor.SetActive(false);
            return;
        }

        if (!_nonDomCursor.activeSelf)
        {
            _nonDomCursor.SetActive(true);
        }

        Vector3 originCursor = nonDomRayCursor.transform.position;
        Vector3 direction = nonDomRayCursor.transform.forward;

        var task = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;
        if (task.ToString() != "WebFitt")
        {
            originCursor = _nonDomRayInteractor.Ray.origin;
            direction = _nonDomRayInteractor.Ray.direction;
        }

        _nonDomCursor.transform.position = GetHoverCursor(originCursor, direction) - targetSurface.transform.forward * _offsetAlongNormal;
        _nonDomCursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);

        if (PlayerHead != null)
        {
            float distance = Vector3.Distance(this.transform.position, PlayerHead.position);
            this.transform.localScale = _startScale * distance;
        }

        if (_nonDomRayInteractor.State == InteractorState.Select)
        {
            _nonDomSelectObject.SetActive(true);
            //_selectObject.SetActive(false);
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientScale, 0.15f);
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
            _nonDomRenderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
        else
        {
            _nonDomSelectObject.SetActive(false);

            if (interactionManager.GetSurfaceFingerTipDistance(0, 1) != 10000)
            {
                pinchDis = interactionManager.GetSurfaceFingerTipDistance(0, 1);
            }
            else
            {
                pinchDis = _maxPinchDistance;
            }
            

            var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
            var radialScale = mappedPinchDistance;
            radialScale = Mathf.Max(radialScale, .15f);
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
            _nonDomRenderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
            _nonDomRenderer.material.SetColor(_shaderOutlineColor, _outlineColor);
        }
    }

    //private void UpdateVisualState(InteractorStateChangeArgs args) => UpdateVisual();

    private Vector3 GetHoverCursor(Vector3 originCursor, Vector3 direction)
    {
        //Vector3 cursorDirection = targetSurface.transform.forward;
        Vector3 cursorDirection = direction;
        Vector3 hitPointOnWebpage;

        if (Physics.Raycast(originCursor, cursorDirection, out RaycastHit hit1, 100f, targetLayer))
        {
            hitPointOnWebpage = hit1.point;
            hoverDis = hit1.distance;
        }
        /*else if (Physics.Raycast(originCursor, -cursorDirection, out RaycastHit hit2, 100f, targetLayer))
        {
            hitPointOnWebpage = hit2.point;
            hoverDis = hit2.distance;
        }*/
        else
        {
            hitPointOnWebpage = new Vector3(10000, 10000, 10000);
            hoverDis = 10000f;
        }

        //GameObject.Find("test1").transform.position = hitPointOnWebpage;

        return hitPointOnWebpage;
    }

    public Vector3 GetRayCursor()
    {
        





















        return _cursor.transform.position;
    }

    public Vector3 GetNonDomRayCursor()
    {
        return _nonDomCursor.transform.position;
    }

    public bool IsIndexFingerPinch()
    {
        return (_rayInteractor.State == InteractorState.Select);
        //Hand.GetIndexFingerIsPinching();
    }

    public bool IsNonDomIndexFingerPinch()
    {
        return (_nonDomRayInteractor.State == InteractorState.Select);
    }
}
