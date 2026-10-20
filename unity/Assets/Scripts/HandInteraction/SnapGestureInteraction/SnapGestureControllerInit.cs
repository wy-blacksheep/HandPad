using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using System.Drawing;

// Legacy recognizer; retained for compatibility. Study scenes use SnapGestureController.
[RequireComponent(typeof(SnapGestureHelper))]
[RequireComponent(typeof(HandInteractionManager))]
public class SnapGestureControllerInit : MonoBehaviour
{
    public enum SnapModes
    {
        //None,
        UnSnap,
        SemiSnap,
        Snap,
        OverSnap
    }

    [SerializeField] [Range(0.0f, 2.0f)] private float snapDelay = 1.0f;
    
    private SnapGestureHelper featureHelper;
    private HandInteractionManager interactionManager;

    private SnapModes snapMode;
    private SnapModes snapTransitionMode;

    private float snapTimer;

    private RadialSpinnerVisualizer radialSpinner;

    private TextMeshProUGUI text;

    private GameObject virtualObject;
    private GameObject virtualSurface;
    private GameObject virtualSurfaceMask;
    private GameObject virtualWindow;
    private GameObject virtualWindowFrame;
    //points on virtual surface
    private GameObject pointCenterDown;
    private GameObject pointRightDown;

    private Vector3 virtualWindowFrameSize;
    private Vector3 virtualSurfaceMaskSize;

    private GameObject surfaceHand;
    private GameObject handSurface;

    private Vector3 centerEyePos;
    private Vector3 palmPos;
    private Vector3 snapRayDirection;
    private Vector3 handSurfaceNormal;
    private string virtualSurfaceLayer;

    //move the virtual surface
    private Vector3 offsetOversnap;
    //rotate/flip the virtual surface
    private Vector3 handSurfaceOversnapStartRot;
    private Vector3 virtualSurfaceOversnapStartRot;
    private float offsetOversnapRotZ;
    private float offsetOversnapRotX;

    [SerializeField]
    private Boolean visualizeHandSurface = false;

    private GameObject activeVirtualSurface = null;

    private GameObject pointerRay;
    private LineRenderer hitRay;
    private Boolean drawHitRay = false;

    //private float overSnapDisThreshold = 0.2f;
    //private float snapDisThreshold = 0.15f;
    private float overSnapSpreadRotThreshold = 45f;
    private float snapSpreadRotThreshold = 30f;
    private float snapRotThreshold = 45f;
    private float semiSnapRotThresholdMin = 80f;
    private float semiSnapRotThresholdMax = 200f;
    //natural hands
    //private int noneRotThresholdMin = 230;
    //private int noneRotThresholdMax = 500;
    private float unSnapRotThreshold = 250f;

    //for semi-snap, snap, unsnap
    private float currentCmulativeFingerOrientation;

    //for oversnap
    private float currentCumulativeSurfaceFingerTipDistance;
    private float currentCumulativeFingerSpreadOrientation;

    private string currentSnapGesture = "None";

    void Start()
    {
        text = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        featureHelper = GetComponent<SnapGestureHelper>();
        interactionManager = GetComponent<HandInteractionManager>();

        virtualObject = interactionManager.GetVirtualObjectGameObject();
        virtualSurface = interactionManager.GetVirtualSurfaceGameObject();
        virtualSurfaceMask = interactionManager.GetVirtualSurfaceMaskGameObject();
        virtualWindow = interactionManager.GetVirtualWindowGameObject();
        virtualWindowFrame = interactionManager.GetVirtualWindowFrameGameObject();
        surfaceHand = interactionManager.GetSurfaceHandGameObject();
        handSurface = interactionManager.GetSurfaceGameObject();

        virtualWindowFrameSize = new Vector3(virtualWindowFrame.transform.localScale.x, virtualWindowFrame.transform.localScale.y, virtualWindowFrame.transform.localScale.z);
        virtualSurfaceMaskSize = new Vector3(virtualSurfaceMask.transform.localScale.x, virtualSurfaceMask.transform.localScale.y, virtualSurfaceMask.transform.localScale.z);

        virtualSurfaceLayer = "VirtualSurfaceLayer";
        pointCenterDown = GameObject.Find("PointCenterDown");
        pointRightDown = GameObject.Find("PointRightDown");

        showObject(virtualWindow, false);
        //showObject(virtualObject, false);
        showObject(virtualSurfaceMask, false);
        showObject(virtualWindowFrame, false);

        pointerRay = new GameObject("pointerRay");
        hitRay = pointerRay.AddComponent<LineRenderer>();

        changeOpacity(virtualWindowFrame, 0.5f);
        changeOpacity(virtualWindow, 0.5f);
    }

    void Update()
    {
        centerEyePos = interactionManager.GetCenterEyeGameObject().transform.position;
        palmPos = interactionManager.GetSurfaceHandPalmGameObject().transform.position;
        snapRayDirection = palmPos - centerEyePos;
        handSurfaceNormal = interactionManager.GetSurfaceNormal();

        currentCmulativeFingerOrientation = featureHelper.GetCmulativeFingerOrientation();
        currentCumulativeSurfaceFingerTipDistance = featureHelper.GetCumulativeSurfaceFingerTipDistance();
        currentCumulativeFingerSpreadOrientation = featureHelper.GetCumulativeFingerSpreadOrientation();

        UpdateSnapMode();
        UpdateGestureDebugger();
        //VisualizeHandSurface();
    }

    private void VisualizeHandSurface()
    {
        if (visualizeHandSurface)
        {
            //enable the meshrender
            interactionManager.GetSurfaceGameObject().GetComponent<Renderer>().enabled = true;
        }
        else
        {
            //disable the meshrender
            interactionManager.GetSurfaceGameObject().GetComponent<Renderer>().enabled = false;
        }
    }

    private void UpdateGestureDebugger() // Temporary Function
    {
        text.SetText(
            "kuncle rotation angle: " + featureHelper.GetCmulativeFingerOrientation().ToString() + "\n" +
            //"finger distance: " + featureHelper.GetCumulativeSurfaceFingerTipDistance().ToString() + "\n" +
            "finger spread angle: " + featureHelper.GetCumulativeFingerSpreadOrientation().ToString() + "\n" +
            "current gesture: " + currentSnapGesture
        );
        //text.SetText(featureHelper.GetCumulativeSurfaceFingerTipDistance().ToString());
    }

    /*
     * Historical experimental thresholds (not the active study recognizer):
     * SNAP     -> WristDistance > 0.78 | InterFingerTipDistanceSum < 0.14
     * SEMISNAP -> WristDistance < 0.75 | InterFingerTipDistanceSum < 0.12
     * OVERSNAP -> WristDistance > 0.78 | InterFingerTipDistanceSum > 0.22
     */
    private void UpdateSnapMode()
    {
        //orientation sum, distance sum
        //oversnap: 104, 0.21
        //snap: 140, 0.13
        //semi-snap: 200, 0.09
        //unsnap: 800, 0.08

        //orientation sum, distance sum except for thumb
        //oversnap: 70, 0.1
        //snap: 70, 0.05
        //semi-snap: 140, 0.05
        //unsnap: 710, 0.04

        //orientation sum, distance sum, 2 knuckles for each finger
        //oversnap: 70, 0.22
        //snap: 75, 0.14
        //semi-snap: 120, 0.1
        //unsnap: 670, 0.1

        //recognize gestures based on the distance between the wrist and the finger tips
        


















        //if palm is not facing the surface
        







        //recognize gestures based on the orientation of knuckles
        if (currentCmulativeFingerOrientation <= snapRotThreshold)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, false);
            //decide snap/oversnap based on fingertip distance
            
















            //decide snap/oversnap based on finger spread angle
            if (currentCumulativeFingerSpreadOrientation > overSnapSpreadRotThreshold)
            {
                SnapTransition(SnapModes.OverSnap);
                currentSnapGesture = "OverSnap";
            }
            else if (currentCumulativeFingerSpreadOrientation > snapSpreadRotThreshold &&
                currentCumulativeFingerSpreadOrientation <= overSnapSpreadRotThreshold)
            {
                SnapBuffer(SnapModes.Snap, SnapModes.OverSnap);
                currentSnapGesture = "Snap - OverSnap";
            }
            else if (currentCumulativeFingerSpreadOrientation <= snapSpreadRotThreshold)
            {
                SnapTransition(SnapModes.Snap);
                currentSnapGesture = "Snap";
            }
        }
        else if (currentCmulativeFingerOrientation > snapRotThreshold
            && currentCmulativeFingerOrientation <= semiSnapRotThresholdMin)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, false);
            SnapBuffer(SnapModes.Snap, SnapModes.SemiSnap);
            currentSnapGesture = "Snap - SemiSnap";
        }
        else if (currentCmulativeFingerOrientation > semiSnapRotThresholdMin
            && currentCmulativeFingerOrientation <= semiSnapRotThresholdMax)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, true);
            SnapTransition(SnapModes.SemiSnap);
            currentSnapGesture = "SemiSnap";
        }
        /*else if (currentCmulativeFingerOrientation > semiSnapRotThresholdMax
            && currentCmulativeFingerOrientation <= noneRotThresholdMin)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, true);
            SnapBuffer(SnapModes.SemiSnap, SnapModes.None);
            currentSnapGesture = "SemiSnap - None";
        }
        else if (currentCmulativeFingerOrientation > noneRotThresholdMax
            && currentCmulativeFingerOrientation <= unSnapRotThreshold)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, true);
            SnapBuffer(SnapModes.None, SnapModes.UnSnap);
            currentSnapGesture = "None - UnSnap";
        }*/
        else if (currentCmulativeFingerOrientation > semiSnapRotThresholdMax
            && currentCmulativeFingerOrientation <= unSnapRotThreshold)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, true);
            SnapBuffer(SnapModes.SemiSnap, SnapModes.UnSnap);
            currentSnapGesture = "UnSnap";
        }
        else if (currentCmulativeFingerOrientation > unSnapRotThreshold)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, true);
            SnapTransition(SnapModes.UnSnap);
            currentSnapGesture = "UnSnap";
        }
    }

    private void SnapBuffer(SnapModes mode1, SnapModes mode2)
    {
        if (mode1 == SnapModes.Snap && mode2 == SnapModes.SemiSnap && activeVirtualSurface != null)
        {
            if (snapMode == SnapModes.Snap)
                virtualWindowFrame.transform.position = virtualWindow.transform.position;
            else if (snapMode == SnapModes.SemiSnap)
                UpdateVirtualWindowPositionByEyeRaycast();
            showObject(virtualWindow, true);

            //make a frame around the virtual window and change the size
            //showObject(virtualWindowFrame, true);
            //float sizeScale = (currentCmulativeFingerOrientation - snapRotThreshold) / (semiSnapRotThresholdMin - snapRotThreshold);
            //virtualWindowFrame.transform.localScale = new Vector3(virtualWindowFrameSize.x + sizeScale * virtualWindowFrameSize.x, virtualWindowFrameSize.y, virtualWindowFrameSize.z + sizeScale * virtualWindowFrameSize.z);

            //make a frame around the virtual window and loading the texture
            float progress;
            if (currentCmulativeFingerOrientation < snapRotThreshold)
                progress = 1f;
            else if(currentCmulativeFingerOrientation > semiSnapRotThresholdMin)
                progress = 0f;
            else
                progress = 1 - ((currentCmulativeFingerOrientation - snapRotThreshold) / (semiSnapRotThresholdMin - snapRotThreshold));

            changeOpacity(surfaceHand, (1-progress) * 0.5f);
            showObject(surfaceHand, true);
            setTransitionProgressForGameobject(virtualSurface, 0);
            setTransitionProgressForGameobject(virtualWindow, progress);
        }
        else if (mode1 == SnapModes.Snap && mode2 == SnapModes.OverSnap && activeVirtualSurface != null)
        {
            //virtualSurfaceMask.transform.position = virtualWindow.transform.position;
            showObject(virtualWindow, true);
            //showObject(virtualSurfaceMask, true);

            //size up the virtual surface mask to cover the whole virtual surface
            //float sizeScale = (currentCumulativeSurfaceFingerTipDistance - snapDisThreshold) / (overSnapDisThreshold - snapDisThreshold);
            //float sizeScale = (currentCumulativeFingerSpreadOrientation - snapSpreadRotThreshold) / (overSnapSpreadRotThreshold - snapSpreadRotThreshold);
            //virtualSurfaceMask.transform.localScale = new Vector3(virtualWindowFrameSize.x + sizeScale * virtualWindowFrameSize.x, virtualWindowFrameSize.y, virtualWindowFrameSize.z + sizeScale * virtualWindowFrameSize.z);

            //make a frame around the virtual surface and loading the texture
            float progress;
            if (currentCumulativeFingerSpreadOrientation < snapSpreadRotThreshold)
                progress = 0f;
            else if (currentCumulativeFingerSpreadOrientation > overSnapSpreadRotThreshold)
                progress = 1f;
            else
                progress = (currentCumulativeFingerSpreadOrientation - snapSpreadRotThreshold) / (overSnapSpreadRotThreshold - snapSpreadRotThreshold);

            changeOpacity(surfaceHand, progress*0.5f);
            showObject(surfaceHand, true);
            setTransitionProgressForGameobject(virtualWindow, 0);
            setTransitionProgressForGameobject(virtualSurface, progress);
        }
        else if (mode1 == SnapModes.SemiSnap && mode2 == SnapModes.UnSnap && activeVirtualSurface != null)
        {
            UpdateVirtualWindowPositionByEyeRaycast();

            //make a frame to cover virtual window and decrease the size until it dispear
            /*showObject(virtualWindow, false);
            showObject(virtualWindowFrame, true);
            float sizeScale = 1 - ((currentCmulativeFingerOrientation - semiSnapRotThresholdMax) / (unSnapRotThreshold - semiSnapRotThresholdMax));
            virtualWindowFrame.transform.localScale = new Vector3(sizeScale * virtualWindowFrameSize.x, virtualWindowFrameSize.y, sizeScale * virtualWindowFrameSize.z);*/
            
            float opacity;
            if (currentCumulativeFingerSpreadOrientation < semiSnapRotThresholdMax)
                opacity = 0f;
            else if (currentCumulativeFingerSpreadOrientation > unSnapRotThreshold)
                opacity = 1f;
            else
                opacity = (currentCmulativeFingerOrientation - semiSnapRotThresholdMax) / (unSnapRotThreshold - semiSnapRotThresholdMax);

            changeOpacity(virtualWindow, 1 - opacity);
            changeOpacity(surfaceHand, opacity);
            showObject(surfaceHand, true);
            showObject(virtualWindow, true);
        }
    }

    private void setTransitionProgressForGameobject(GameObject obj, float progress)
    {
        obj.GetComponent<ProgressVisualizerController>().SetProgress(progress);
    }

    private void changeOpacity(GameObject obj, float alpha)
    {
        var color = obj.GetComponent<Renderer>().material.color;
        var newColor = new UnityEngine.Color(color.r, color.g, color.b, alpha);
        obj.GetComponent<Renderer>().material.color = newColor;
    }

    private void showObject(GameObject obj, bool isshow)
    {
        obj.GetComponent<Renderer>().enabled = isshow;
    }

    private void showPointerRay(GameObject obj, Vector3 startPoint, Vector3 rayDirection, bool isshow)
    {
        if(!isshow)
            obj.GetComponent<LineRenderer>().enabled = isshow;
        else
        {
            Vector3 endPoint = CalculateEndPoint(palmPos, rayDirection, 3f);
            if (!drawHitRay) DrawRay(startPoint, endPoint);
            else UpdateRay(startPoint, endPoint);
            obj.GetComponent<LineRenderer>().enabled = isshow;
        }
    }

    private GameObject GetRaycastHitObject(Vector3 origin, Vector3 direction, string layermask)
    {
        RaycastHit hit;

        //infinity: no hit
        if (Physics.Raycast(origin, direction, out hit, 10.0f, LayerMask.GetMask(layermask)))
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private Vector3 GetRaycastHitPoint(Vector3 origin, Vector3 direction, string layermask, GameObject targetObj)
    {
        RaycastHit hit;

        //infinity: no hit
        Vector3 hitPoint = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        if (Physics.Raycast(origin, direction, out hit, 10.0f, LayerMask.GetMask(layermask)))
        {
            if (hit.collider.gameObject == targetObj)
            {
                hitPoint = hit.point;
            }
        }
        return hitPoint;
    }

    private void SnapTransition(SnapModes mode)
    {
        if (mode == SnapModes.UnSnap && activeVirtualSurface != null)
        {
            setTransitionProgressForGameobject(virtualWindow, 0);
            setTransitionProgressForGameobject(virtualSurface, 0);

            showObject(virtualWindow, false);
            showObject(surfaceHand, true);
            changeOpacity(surfaceHand, 1.0f);
            showObject(virtualSurfaceMask, false);
            showObject(virtualWindowFrame, false);

            activeVirtualSurface = null;
        }
        else if (mode == SnapModes.SemiSnap)
        {
            setTransitionProgressForGameobject(virtualSurface, 0);
            setTransitionProgressForGameobject(virtualWindow, 0);

            UpdateVirtualWindowPositionByEyeRaycast();

            //virtualWindowFrame.transform.localScale = virtualWindowFrameSize;
            //showObject(virtualWindowFrame, true);
            //showObject(virtualWindow, false);

            showObject(virtualWindow, true);
            showObject(surfaceHand, true);
            changeOpacity(surfaceHand, 0.5f);
            showObject(virtualSurfaceMask, false);
        }
        else if (snapTransitionMode == SnapModes.SemiSnap && mode == SnapModes.Snap)
        {
            setTransitionProgressForGameobject(virtualSurface, 0);
            setTransitionProgressForGameobject(virtualWindow, 1);

            if (snapMode != mode)
            {
                UpdateVirtualWindowPositionByEyeRaycast();
            }
            showObject(virtualWindow, true);
            changeOpacity(virtualWindow, 0.5f);
            showObject(surfaceHand, false);
            showObject(virtualSurfaceMask, false);
            showObject(virtualWindowFrame, false);
        }
        else if (mode == SnapModes.OverSnap)
        {
            setTransitionProgressForGameobject(virtualWindow, 0);

            if (activeVirtualSurface == null)
            {
                GameObject hitSurface1 = GetRaycastHitObject(palmPos, 1.0f * snapRayDirection, virtualSurfaceLayer);
                GameObject hitSurface2 = GetRaycastHitObject(palmPos, -1.0f * snapRayDirection, virtualSurfaceLayer);

                if (hitSurface1 != null || hitSurface2 != null)
                {
                    if (hitSurface1 != null) activeVirtualSurface = hitSurface1;
                    else if (hitSurface2 != null) activeVirtualSurface = hitSurface2;
                }
            }

            if (activeVirtualSurface != null)
            {
                if (snapMode != mode)
                {
                    //need to point at the virtual surface to oversnap
                    










                    //don't need to point at the virtual surface to oversnap
                    Vector3 virtualSurfaceStartPos = activeVirtualSurface.transform.position;
                    Vector3 handSurfaceStartPos = handSurface.transform.position;
                    offsetOversnap = virtualSurfaceStartPos - handSurfaceStartPos;

                    float x = handSurface.transform.eulerAngles.x;
                    float y = handSurface.transform.eulerAngles.y;
                    float z = handSurface.transform.eulerAngles.z;
                    handSurfaceOversnapStartRot = new Vector3(x, y, z);

                    x = activeVirtualSurface.transform.eulerAngles.x;
                    y = activeVirtualSurface.transform.eulerAngles.y;
                    z = activeVirtualSurface.transform.eulerAngles.z;
                    virtualSurfaceOversnapStartRot = new Vector3(x, y, z);

                    offsetOversnapRotZ = virtualSurfaceOversnapStartRot.z - handSurfaceOversnapStartRot.z;
                    offsetOversnapRotX = virtualSurfaceOversnapStartRot.x - handSurfaceOversnapStartRot.x;

                    virtualSurfaceMask.transform.position = virtualSurface.transform.position;
                    virtualSurfaceMask.transform.localScale = virtualSurfaceMaskSize;
                }
                else
                {
                    virtualSurface.transform.position = palmPos + offsetOversnap;

                    float virtualSurfaceCurZ = virtualSurfaceOversnapStartRot.z + (handSurface.transform.eulerAngles.z - handSurfaceOversnapStartRot.z);
                    //float virtualSurfaceCurZ = handSurface.transform.eulerAngles.z + offsetOversnapRotZ;
                    float virtualSurfaceCurX = virtualSurfaceOversnapStartRot.x + (handSurface.transform.eulerAngles.x - handSurfaceOversnapStartRot.x);

                    

                    //virtualSurface.transform.rotation = Quaternion.Euler(virtualSurfaceCurX, virtualSurfaceOversnapStartRot.y, virtualSurfaceOversnapStartRot.z);
                    
                    
                    
                    //float offsetHandSurfaceRot = handSurface.transform.eulerAngles.z - handSurfaceOversnapStartRot.z;

                    //Vector3 rotateAxis = pointRightDown.transform.position - pointCenterDown.transform.position;
                    //virtualSurface.transform.RotateAround(pointCenterDown.transform.position, rotateAxis, 90);

                    









                }
            }
            showObject(virtualWindow, false);
            showObject(surfaceHand, true);
            changeOpacity(surfaceHand, 0.5f);
            //showObject(virtualSurfaceMask, true);
            showObject(virtualWindowFrame, false);
        }
        else
        {
            showObject(virtualWindow, false);
            showObject(surfaceHand, true);
            changeOpacity(surfaceHand, 1.0f);
            showObject(virtualSurfaceMask, false);
            showObject(virtualWindowFrame, false);
        }

        if (snapMode != mode)
        {
            snapTransitionMode = snapMode;
            snapMode = mode;
        }
    }

    private Vector3 CalculateEndPoint(Vector3 startPos, Vector3 direction, float dis)
    {
        Vector3 endPos = startPos + direction * dis;
        return endPos;
    }

    public Material CreateMaterial(UnityEngine.Color color, string name, string shaderName = "Standard")
    {
        Material material = new Material(Shader.Find(shaderName));
        material.name = name;
        material.color = color;
        material.SetColor("rayColor", color);
        return material;
    }

    private void DrawRay(Vector3 startPos, Vector3 destPos)
    {
        pointerRay.transform.position = palmPos;
        hitRay.material = CreateMaterial(UnityEngine.Color.red, "ray_material");

        // set width of the renderer
        hitRay.startWidth = 0.01f;
        hitRay.endWidth = 0.01f;

        // set the position
        hitRay.SetPosition(0, startPos);
        hitRay.SetPosition(1, destPos);

        drawHitRay = true;
    }

    private void UpdateVirtualWindowPositionByEyeRaycast()
    {
        GameObject hitSurface1 = GetRaycastHitObject(palmPos, 1.0f * snapRayDirection, virtualSurfaceLayer);
        GameObject hitSurface2 = GetRaycastHitObject(palmPos, -1.0f * snapRayDirection, virtualSurfaceLayer);

        if (hitSurface1 != null || hitSurface2 != null)
        {
            if (hitSurface1 != null) activeVirtualSurface = hitSurface1;
            else if (hitSurface2 != null) activeVirtualSurface = hitSurface2;

            Vector3 hitpoint1 = GetRaycastHitPoint(palmPos, 1.0f * snapRayDirection, virtualSurfaceLayer, activeVirtualSurface);
            Vector3 hitpoint2 = GetRaycastHitPoint(palmPos, -1.0f * snapRayDirection, virtualSurfaceLayer, activeVirtualSurface);
            if (!Single.IsInfinity(hitpoint1.x))
            {
                virtualWindow.transform.position = hitpoint1;
                virtualWindowFrame.transform.position = hitpoint1;
            }
            else if (!Single.IsInfinity(hitpoint2.x))
            {
                virtualWindow.transform.position = hitpoint2;
                virtualWindowFrame.transform.position = hitpoint2;
            }
        }
    }

    private void UpdateRay(Vector3 startPos, Vector3 destPos)
    {
        hitRay.SetPosition(0, startPos);
        hitRay.SetPosition(1, destPos);
    }

    private void SnapTo(SnapModes mode)
    {
        if (snapMode != mode)
        {
            if (snapTransitionMode != mode)
            {
                snapTimer = 0;
                snapTransitionMode = mode;
            }
            snapTimer += Time.deltaTime;

            if (snapTimer > snapDelay) snapMode = mode;
        }
    }


    /*
     * |----------------------------
     * | PUBLIC METHODS
     * |----------------------------
     */

    public SnapModes GetSnapMode()
    {
        return snapMode;
    }

    public SnapModes GetSnapTransitionMode()
    {
        return snapTransitionMode;
    }

    public float GetSnapProgress()
    {
        if (snapTimer / snapDelay < 0.0f) return 0.0f;
        else if (snapTimer / snapDelay > 1.0f) return 1.0f;
        
        return snapTimer / snapDelay;
    }
}
