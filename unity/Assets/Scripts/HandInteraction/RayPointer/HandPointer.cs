using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

[RequireComponent(typeof(HandInteractionManager))]
public class HandPointer : MonoBehaviour
{
    // Start is called before the first frame update

    public OVRHand rightHand;

    [SerializeField] private bool showRaycast = true;
    [SerializeField] private Color highlightColor = Color.green;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private LineRenderer lineRenderer;

    private Color _originalColor = Color.red;
    private Renderer _currentRenderer;

    private Vector3 rayDir;
    private Vector3 rayStartPoint;
    private Vector3 hitPoint;
    private GameObject interactionWrist;

    private float pinchDisThreshold = 0.01f;
    private bool isRayPinch = false;

    public bool isRayMethod = false;

    private HandInteractionManager interactionManager;

    void Start()
    {
        interactionWrist = GameObject.Find("b_r_wrist");
        interactionManager = GetComponent<HandInteractionManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRayMethod)
        {
            CheckHandPointer(rightHand);
            CheckPinch();
        }
    }

    void CheckHandPointer(OVRHand hand)
    {
        //GameObject.Find("testHand").transform.position = hand.PointerPose.position;

        //rayDir = Quaternion.AngleAxis(90, hand.PointerPose.transform.up) * hand.PointerPose.transform.forward;
        //rayDir = Quaternion.AngleAxis(-20, hand.PointerPose.transform.right) * rayDir;
        rayDir = interactionWrist.transform.right;
        rayDir = Quaternion.AngleAxis(-30, interactionWrist.transform.forward) * rayDir;
        rayStartPoint = interactionWrist.transform.position + rayDir * 0.1f - interactionWrist.transform.up * 0.025f;

        rayStartPoint = rightHand.PointerPose.position;
        rayDir = rightHand.PointerPose.forward;

        if (Physics.Raycast(rayStartPoint, rayDir, out RaycastHit hit, 100f, targetLayer))
        {
            GameObject target = hit.transform.gameObject;
            //_currentRenderer = target.GetComponent<Renderer>();
            //_originalColor = _currentRenderer.material.color;
            //_currentRenderer.material.color = highlightColor;
            hitPoint = hit.point;
            UpdateRayVisualization(rayStartPoint, rayStartPoint + rayDir * 0.75f, true);
        }
        else
        {
            //_currentRenderer.material.color = _originalColor;
            hitPoint = new Vector3(10000, 10000, 10000);
            UpdateRayVisualization(rayStartPoint, rayStartPoint + rayDir * 0.75f, false);
        }
    }

    void CheckPinch()
    {
        










        float dis = interactionManager.GetInteractionFingerTipDistance(0, 1);
        float hoverThreshold = interactionManager.GetHoverDistanceThreshold();

        if (dis <= pinchDisThreshold)
        {
            isRayPinch = true;
        }
        else{
            isRayPinch = false;
        }
    }

    private void UpdateRayVisualization(Vector3 startPosition, Vector3 endPosition, bool hitSomething)
    {
        if(showRaycast & lineRenderer != null)
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, startPosition);
            lineRenderer.startWidth = 0.01f;
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.endWidth = 0.005f;
            lineRenderer.material.color = hitSomething ? highlightColor : _originalColor;
        }
        else if(lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    public Vector3 GetRayHitPoint()
    {
        return hitPoint;
    }

    public bool GetRayPinch()
    {
        return isRayPinch;
    }
}
