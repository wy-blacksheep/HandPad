using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

[RequireComponent(typeof(HandInteractionManager))]
public class EyeGazeHandPinch : MonoBehaviour
{
    [SerializeField]
    private RayInteractor _rayInteractor;

    [SerializeField]
    private RayInteractor _nonDomRayInteractor;

    [SerializeField]
    private GameObject _cursor;

    [SerializeField]
    private GameObject _nonDomCursor;

    [SerializeField]
    private GameObject _gazeCursor;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private Renderer _nonDomRenderer;

    [SerializeField]
    private Renderer _gazeRenderer;

    [SerializeField]
    private GameObject _selectObject;

    [SerializeField]
    private GameObject _nonDomSelectObject;

    [SerializeField]
    private GameObject _gazeSelectObject;

    [SerializeField]
    private Color _outlineColor = Color.black;

    [SerializeField]
    private Color _gazeOutlineColor = Color.red;

    [SerializeField]
    private float _offsetAlongNormal = 0.005f;

    [SerializeField]
    private float _overshootMaxDistance = 0.15f;

    [SerializeField]
    private float _maxPinchDistance = 0.02f;

    private float pinchDis;

    protected bool _started = false;

    [Tooltip("Players head transform, used to maintain the same cursor size on screen as it is moved in the scene.")]
    [SerializeField, Optional]
    private Transform _playerHead;

    [SerializeField] private LayerMask targetLayer;

    [SerializeField] private GameObject targetSurface;

    [SerializeField] private GameObject targetCanvas;

    private Vector3 _startScale;

    private HandInteractionManager interactionManager;

    private float hoverDis = 10000;

    private GameObject relocateDomPinchArrow;
    private GameObject relocateNonDomPinchArrow;

    private Vector3 gazeHitPointOnWebpage = new Vector3(10000, 10000, 10000);

    private bool isGazeDominate = false;

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
        interactionManager = GetComponent<HandInteractionManager>();

        relocateDomPinchArrow = GameObject.Find("RelocateOculusHandPinchArrowBlended");
        relocateNonDomPinchArrow = GameObject.Find("RelocateNonDomOculusHandPinchArrowBlended");
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVisual(_rayInteractor, _cursor, _renderer, _selectObject, relocateDomPinchArrow, "dom");
        UpdateVisual(_nonDomRayInteractor, _nonDomCursor, _nonDomRenderer, _nonDomSelectObject, relocateNonDomPinchArrow, "nonDom");

        if(interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeMoveHand ||
           interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove ||
           interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandJump ||
           interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            UpdateGazeCusor();
        }
    }

    private void UpdateGazeCusor()
    {     
        Vector3 smoothGazeCursor = interactionManager.GetSmoothGazeCursor();

        // Raycast from the gaze origin in the gaze direction
        if (smoothGazeCursor.x != 10000)
        {
            if (!_gazeCursor.activeSelf)
            {
                _gazeCursor.SetActive(true);
            }

            gazeHitPointOnWebpage = smoothGazeCursor;

            _gazeCursor.transform.position = smoothGazeCursor - targetSurface.transform.forward * _offsetAlongNormal;
            _gazeCursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);

            if (IsIndexFingerPinch())
            {
                _gazeSelectObject.SetActive(true);

                _gazeRenderer.material.SetFloat(_shaderRadialGradientScale, 0.15f);
                _gazeRenderer.material.SetFloat(_shaderRadialGradientIntensity, 1f);
                _gazeRenderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, 1f);
                _gazeRenderer.material.SetColor(_shaderOutlineColor, _gazeOutlineColor);
            }
            else
            {
                _gazeSelectObject.SetActive(false);

                if (interactionManager.GetInteractionFingerTipDistance(0, 1) != 10000)
                {
                    pinchDis = interactionManager.GetInteractionFingerTipDistance(0, 1);
                }
                else
                {
                    pinchDis = _maxPinchDistance;
                }
                

                var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
                var radialScale = mappedPinchDistance;
                radialScale = Mathf.Max(radialScale, .15f);
                _gazeRenderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _gazeRenderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
                _gazeRenderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
                _gazeRenderer.material.SetColor(_shaderOutlineColor, _gazeOutlineColor);
            }
        }
        else
        {
            gazeHitPointOnWebpage = new Vector3(10000, 10000, 10000);
            _gazeCursor.SetActive(false);
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove || interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            if (!isGazeDominate)
            {
                _gazeRenderer.enabled = false;
            }
            else
            {
                _gazeRenderer.enabled = true;
            }
        }
    }

    private void UpdateVisual(RayInteractor _rayInteractor, GameObject _cursor, Renderer _renderer, GameObject _selectObject, GameObject pinchArrow, string hand)
    {
        if (_rayInteractor.State == InteractorState.Disabled)
        {
            _cursor.SetActive(false);
            return;
        }       

        if (!_cursor.activeSelf)
        {
            _cursor.SetActive(true);
        }

        Vector3 pinchArrowPos = pinchArrow.transform.position;
        _cursor.transform.position = GetHoverCursor(pinchArrowPos) - targetSurface.transform.forward * _offsetAlongNormal;
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

            pinchDis = _maxPinchDistance;

            var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
            var radialScale = mappedPinchDistance;

            if (hand == "dom")
            {
                pinchDis = interactionManager.GetInteractionFingerTipDistance(0, 1);

                mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
                radialScale = mappedPinchDistance;
                radialScale = Mathf.Max(radialScale, .15f);
                _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
                _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
                _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
            else if (hand == "nonDom")
            {
                pinchDis = interactionManager.GetSurfaceFingerTipDistance(0, 1);

                if (pinchDis == 10000)
                {
                    pinchDis = interactionManager.GetInteractionFingerTipDistance(0, 1);
                }

                mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
                radialScale = mappedPinchDistance;
                radialScale = Mathf.Max(radialScale, .15f);
                _nonDomRenderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _nonDomRenderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
                _nonDomRenderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
                _nonDomRenderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove || interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            if (isGazeDominate)
            {
                _renderer.enabled = false;
            }
            else
            {
                _renderer.enabled = true;
            }
        }
    }

    private Vector3 GetHoverCursor(Vector3 pinchArrowPos)
    {
        Vector3 cursorDirection = targetSurface.transform.forward;
        Vector3 hitPointOnWebpage;

        if (Physics.Raycast(pinchArrowPos, cursorDirection, out RaycastHit hit1, 1f, targetLayer))
        {
            hitPointOnWebpage = hit1.point;
            hoverDis = hit1.distance;
        }
        else if (Physics.Raycast(pinchArrowPos, -cursorDirection, out RaycastHit hit2, 1f, targetLayer))
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

    public bool IsIndexFingerPinch()
    {
        return (_rayInteractor.State == InteractorState.Select);
        //Hand.GetIndexFingerIsPinching();
    }

    public bool IsNonDomIndexFingerPinch()
    {
        return (_nonDomRayInteractor.State == InteractorState.Select);
    }

    public Vector3 GetHandPinchCursor()
    {
        return _cursor.transform.position;
    }

    public Vector3 GetNonDomHandPinchCursor()
    {
        return _nonDomCursor.transform.position;
    }

    public void IsGazeDominate(bool isGazeDom)
    {
        isGazeDominate = isGazeDom;
    }
}
