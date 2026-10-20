using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

[RequireComponent(typeof(HandInteractionManager))]
public class EyeGazePointer : MonoBehaviour
{

    [SerializeField]
    private GameObject _cursor;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField, Interface(typeof(IHand))]
    private UnityEngine.Object _hand;
    public IHand Hand { get; private set; }

    [SerializeField]
    private OVREyeGaze leftEyeGaze;  // Attach OVREyeGaze component

    [SerializeField]
    private OVREyeGaze rightEyeGaze;  // Attach OVREyeGaze component

    [SerializeField]
    private float maxDistance = 10f;  // Maximum distance for gaze interaction

    [SerializeField]
    private Color _outlineColor = Color.black;

    [SerializeField]
    private float _offsetAlongNormal = 0.005f;

    [SerializeField] 
    private LayerMask targetLayer;

    [SerializeField] private GameObject targetSurface;

    [SerializeField]
    private GameObject _selectObject;

    [SerializeField]
    private float _maxPinchDistance = 0.02f;

    private float pinchDis;

    private HandInteractionManager interactionManager;

    private int _shaderRadialGradientScale = Shader.PropertyToID("_RadialGradientScale");
    private int _shaderRadialGradientIntensity = Shader.PropertyToID("_RadialGradientIntensity");
    private int _shaderRadialGradientBackgroundOpacity = Shader.PropertyToID("_RadialGradientBackgroundOpacity");
    private int _shaderOutlineColor = Shader.PropertyToID("_OutlineColor");

    private Vector3 hitPointOnWebpage = new Vector3(10000, 10000, 10000);

    protected virtual void Awake()
    {
        Hand = _hand as IHand;
    }

    // Start is called before the first frame update
    void Start()
    {
        interactionManager = GetComponent<HandInteractionManager>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGazeCusor();
    }

    private void UpdateGazeCusor()
    {
        Vector3 smoothGazeCursor = interactionManager.GetSmoothGazeCursor();

        // Raycast from the gaze origin in the gaze direction
        if (smoothGazeCursor.x != 10000)
        {
            if (!_cursor.activeSelf)
            {
                _cursor.SetActive(true);
            }

            hitPointOnWebpage = smoothGazeCursor;

            _cursor.transform.position = smoothGazeCursor - targetSurface.transform.forward * _offsetAlongNormal;
            _cursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);

            if (IsIndexFingerPinch())
            {
                _selectObject.SetActive(true);

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
                

                var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
                var radialScale = mappedPinchDistance;
                radialScale = Mathf.Max(radialScale, .15f);
                _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
                _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
                _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
        }
        else
        {
            hitPointOnWebpage = new Vector3(10000, 10000, 10000);
            _cursor.SetActive(false);
        }
    }

    private void UpdateGazeOrigin()
    {
        Vector3 gazeOrigin = interactionManager.GetSmoothGazeOrigin();

        Vector3 gazeDirection = interactionManager.GetGazeDirection();

        // Raycast from the gaze origin in the gaze direction
        if (Physics.Raycast(gazeOrigin, gazeDirection, out RaycastHit hit, maxDistance, targetLayer))
        {
            if (!_cursor.activeSelf)
            {
                _cursor.SetActive(true);
            }

            hitPointOnWebpage = hit.point;

            _cursor.transform.position = hit.point - targetSurface.transform.forward * _offsetAlongNormal;
            _cursor.transform.rotation = Quaternion.LookRotation(-targetSurface.transform.forward, targetSurface.transform.up);

            if (IsIndexFingerPinch())
            {
                _selectObject.SetActive(true);

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
                

                var mappedPinchDistance = Mathf.Clamp(pinchDis / _maxPinchDistance, 0f, 1f);
                var radialScale = mappedPinchDistance;
                radialScale = Mathf.Max(radialScale, .15f);
                _renderer.material.SetFloat(_shaderRadialGradientScale, radialScale);
                _renderer.material.SetFloat(_shaderRadialGradientIntensity, Mathf.Min(mappedPinchDistance, .9f));
                _renderer.material.SetFloat(_shaderRadialGradientBackgroundOpacity, Mathf.Lerp(0.3f, 0.7f, mappedPinchDistance));
                _renderer.material.SetColor(_shaderOutlineColor, _outlineColor);
            }
        }
        else
        {
            hitPointOnWebpage = new Vector3(10000, 10000, 10000);
            _cursor.SetActive(false);
        }
    }

    public Vector3 GetGazeCursor()
    {
        return hitPointOnWebpage;
    }

    public bool IsIndexFingerPinch()
    {
        return Hand.GetIndexFingerIsPinching();
    }
}
