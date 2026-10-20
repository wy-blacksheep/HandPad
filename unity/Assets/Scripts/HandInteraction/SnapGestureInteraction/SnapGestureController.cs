using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System;
using System.Drawing;
using OVR.OpenVR;
using UnityEngine.UIElements;
public struct SnapThreshold
{
    //sum of angles of finger knuckle rotation
    public float RotThreshold;
    //sum of angles between adjacent fingers in an spread hand
    public float SpreadRotThreshold;
}

public enum SnapStates
{
    None,
    UnSnapState,
    UnSnapToSemiSnap,
    SemiSnapToUnSnap,
    SemiSnapState,
    SemiSnapToSnap,
    SnapToSemiSnap,
    SnapState,
    SnapToOverSnap,
    OverSnapToSnap,
    OverSnapState,
    UnSnapToOverSnap,
    OverSnapToUnSnap,
    SemiSnapToOverSnap,
    OverSnapToSemiSnap,

    //frame of reference
    UnSnapToSetFOR,
    SetFOR,
    SetFORToUnSnap

}

public class SnapDictionary : Dictionary<SnapStates, SnapThreshold>
{
    public void Add(SnapStates snapState, float rotThreshold, float spreadRotThreshold)
    {
        SnapThreshold snapThreshold;
        snapThreshold.RotThreshold = rotThreshold;
        snapThreshold.SpreadRotThreshold = spreadRotThreshold;
        this.Add(snapState, snapThreshold);
    }
}

[RequireComponent(typeof(SnapGestureHelper))]
[RequireComponent(typeof(HandRetargetingController))]
[RequireComponent(typeof(HandInteractionManager))]
[RequireComponent(typeof(HandInteractionVisualizer))]
public class SnapGestureController : MonoBehaviour
{
    public enum SnapModes
    {
        //None,
        UnSnap,
        SemiSnap,
        Snap,
        OverSnap
    }

    public enum InteractionMethod
    {
        VRSnap,
        InAir,
        PseudoDirectTouch,
        EyeGaze,
        DirectTouch,
        HandRaycast,
        HandRayPointer,
        HandPoke,
        HandPoke_V2,
        HandPseudoDirectPoke,
        HandPseudoDirectPoke_V2,
        CustomHandPointer,
        HandIndirectTouch,
        //hand to indicate/move/manipulate
        HandMove,
        //eye gaze to indicate/move, hand to manipulate
        EyeMoveHand,
        EyeHandMove,
        EyeHandJump,
        GazePinch
    }

    public enum SurfaceTpye
    {
        Near,
        Far
    }

    [SerializeField]
    private InteractionMethod interactionMethod;

    [SerializeField]
    private SurfaceTpye surfaceType;

    private SnapGestureHelper featureHelper;
    private HandRetargetingController handRetargetingController;
    private HandInteractionManager interactionManager;
    private HandInteractionVisualizer handInteractionVisualizer;

    private SnapStates curSnapState;
    private SnapStates lastSnapState;
    private SnapStates transitionSnapState;

    [SerializeField] [Range(0.0f, 2.0f)] private float snapDelay = 1.0f;

    private SnapModes snapMode;
    private SnapModes snapTransitionMode;

    private float snapTimer;

    private RadialSpinnerVisualizer radialSpinner;

    private TextMeshProUGUI text;

    private GameObject virtualObject;
    private GameObject virtualSurface;
    private GameObject virtualSurfaceReference;
    private GameObject virtualSurfaceMask;
    private GameObject virtualSurfaceAnchor;
    private GameObject virtualSurfaceFOR;

    private GameObject virtualWindow;
    private float InitVirtualWindowAlpha;
    private GameObject virtualWindowFrame;
    private Vector3 virtualWindowLocalScale;

    private Vector3 virtualWindowFrameSize;
    private Vector3 virtualSurfaceMaskSize;

    private GameObject surfaceHand;
    private GameObject surfaceHandReference;
    private Vector3 initSurfaceHandPosition;
    private GameObject handSurface;
    private GameObject handSurfaceReference;
    private GameObject surfaceMatchVirtualWindow;
    private GameObject interactionHand;

    private Vector3 initHandSurfacePosition;
    private Vector3 initVirtualWindowPosition;

    private GameObject handPinchArrow;
    private GameObject retargetHandPinchArrow;
    private GameObject cursor;
    private GameObject nonDomHandPinchArrow;
    private GameObject retargetNonDomHandPinchArrow;
    private GameObject nonDomCursor;

    private float maxShowPinchHandDis = 0.075f;
    private float minPinchHandOpacity = 0.1f;
    private float maxPinchHandOpacity = 0.5f;

    private GameObject relocateSurfaceHand;
    private Vector3 initRetargetSurfaceHandPosition;
    private GameObject relocateInteractionHandMesh;
    private GameObject relocateSurfaceHandMesh;
    private Vector3 retargetPalmPos;
    private Vector3 retargetSnapRayDirection;

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

    [SerializeField]
    private Boolean visualizeHandSurface = false;
    private GameObject activeVirtualSurface = null;

    private GameObject pointerRay;
    private LineRenderer hitRay;
    private Boolean drawHitRay = false;

    private float overSnapSpreadRotThreshold = 45f;
    private float snapSpreadRotThreshold = 30f;
    private float snapRotThreshold = 50f;
    private float semiSnapRotThresholdMin = 100f;
    private float semiSnapRotThresholdMax = 200f;
    //natural hands/unsnap
    private float unSnapRotThreshold = 250f;

    //for semi-snap, snap, unsnap
    private float currentCmulativeFingerOrientation;
    //for oversnap
    private float currentCumulativeFingerSpreadOrientation;

    private string currentSnapGesture = "";
    private bool initGestureUnSnap = false;

    //is the virtual window active for interaction, aka have snapping happened
    private bool activeVirtualWindow = false;

    SnapDictionary snapDictionary = new SnapDictionary();

    //dom hand
    private bool isPinching = false;
    private Vector3 pinchPosition;
    private Vector3 initPinchPosition;
    private Vector3 initVirtualWindowSize;
    private Vector3 initVirtualSurfaceSize;
    private Vector3 initVirtualSurfaceFORSize;
    private float pinchDisThreshold = 0.02f;

    private string debugPinching = "";
    private string gestureDebug = "";

    private GameObject initActiveVirtualSurface;

    private bool initPinchOversnap = false;
    private Vector3 initPinchPositionOversnap;

    //thumb -> pinky
    private List<float> fingerOrientation;
    private float bendFingerRotThreshold = 110f;
    private float stretchThumbFingerRotThreshold = 50f;
    private float stretchIndexFingerRotThreshold = 30f;

    private List<float> interactionFingerOrientation;
    private float bendInteractionFingerRotThreshold = 100f;
    private float stretchInteractionIndexFingerRotThreshold = 25f;

    private bool isDuringInteraction = false;

    private float lookDownAngleThreshold = 60f;
    private GameObject lookDownCalibrationGameObject;

    public float transparencyThreshold = 20f;
    //public float alpha = 0f;

    private bool isFORGesture = false;

    private bool isGazeDominate = false;

    public bool showNonDomHand = false;

    void Start()
    {
        text = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        featureHelper = GetComponent<SnapGestureHelper>();
        handRetargetingController = GetComponent<HandRetargetingController>();
        interactionManager = GetComponent<HandInteractionManager>();
        handInteractionVisualizer = GetComponent<HandInteractionVisualizer>();

        lookDownCalibrationGameObject = GameObject.Find("LookDownCalibration");

        virtualObject = interactionManager.GetVirtualObjectGameObject();
        virtualSurface = interactionManager.GetVirtualSurfaceGameObject();
        virtualSurfaceReference = interactionManager.GetVirtualSurfaceReferenceGameObject();
        virtualSurfaceMask = interactionManager.GetVirtualSurfaceMaskGameObject();
        virtualSurfaceAnchor = interactionManager.GetVirtualSurfaceAnchorGameObject();
        virtualSurfaceFOR = virtualSurfaceAnchor.transform.Find("VirtualSurfaceFOR").gameObject;

        




















        virtualWindow = interactionManager.GetVirtualWindowGameObject();
        InitVirtualWindowAlpha = virtualWindow.GetComponent<Renderer>().material.color.a;
        virtualWindowFrame = interactionManager.GetVirtualWindowFrameGameObject();
        surfaceHand = interactionManager.GetSurfaceHandGameObject();
        handSurface = interactionManager.GetSurfaceGameObject();
        handSurfaceReference = interactionManager.GetSurfaceReferenceGameObject();
        surfaceMatchVirtualWindow = interactionManager.GetSurfaceMatchVirtualWindowGameObject();

        interactionHand = interactionManager.GetInteractionHandGameObject();
        surfaceHandReference = interactionManager.GetSurfaceHandReferenceGameObject();

        handPinchArrow = interactionManager.GetHandPinchArrowGameObject();
        retargetHandPinchArrow = interactionManager.GetRetargetHandPinchArrowGameObject();
        cursor = interactionManager.GetCursorGameObject();

        nonDomHandPinchArrow = interactionManager.GetNonDomHandPinchArrowGameObject();
        retargetNonDomHandPinchArrow = interactionManager.GetRetargetNonDomHandPinchArrowGameObject();
        nonDomCursor = interactionManager.GetNonDomCursorGameObject();


        relocateSurfaceHand = interactionManager.GetRelocateSurfaceHandGameObject();
        relocateInteractionHandMesh = interactionManager.GetRelocateInteractionHandMeshGameObject();
        relocateSurfaceHandMesh = interactionManager.GetRelocateSurfaceHandMeshGameObject();
        ShowObject(relocateInteractionHandMesh, false);
        ShowObject(relocateSurfaceHandMesh, false);

        virtualWindowFrameSize = new Vector3(virtualWindowFrame.transform.localScale.x, virtualWindowFrame.transform.localScale.y, virtualWindowFrame.transform.localScale.z);
        virtualSurfaceMaskSize = new Vector3(virtualSurfaceMask.transform.localScale.x, virtualSurfaceMask.transform.localScale.y, virtualSurfaceMask.transform.localScale.z);

        initVirtualSurfaceSize = virtualSurface.transform.localScale;
        initVirtualSurfaceFORSize = virtualWindow.transform.localScale;
        initVirtualWindowSize = virtualWindow.transform.localScale;

        virtualSurfaceLayer = "VirtualSurfaceLayer";

        ShowObject(virtualWindow, false);
        //ShowObject(virtualObject, false);

        //pointerRay = new GameObject("pointerRay");
        //hitRay = pointerRay.AddComponent<LineRenderer>();

        snapDictionary.Add(SnapStates.UnSnapState, 350f, 30f);
        snapDictionary.Add(SnapStates.UnSnapToSemiSnap, 250f, 30f);
        snapDictionary.Add(SnapStates.SemiSnapToUnSnap, 300f, 30f);
        snapDictionary.Add(SnapStates.SemiSnapState, 120f, 30f);
        snapDictionary.Add(SnapStates.SemiSnapToSnap, 80f, 30f);
        snapDictionary.Add(SnapStates.SnapToSemiSnap, 100f, 30f);
        snapDictionary.Add(SnapStates.SnapState, 42f, 30f);
        snapDictionary.Add(SnapStates.SnapToOverSnap, 42f, 50f);
        snapDictionary.Add(SnapStates.OverSnapToSnap, 42f, 40f);
        snapDictionary.Add(SnapStates.OverSnapState, 42f, 53f);
        snapDictionary.Add(SnapStates.UnSnapToOverSnap, 42f, 50f);
        snapDictionary.Add(SnapStates.OverSnapToUnSnap, 42f, 40f);
        snapDictionary.Add(SnapStates.SemiSnapToOverSnap, 42f, 50f);
        snapDictionary.Add(SnapStates.OverSnapToSemiSnap, 42f, 40f);

        curSnapState = SnapStates.None;
        lastSnapState = SnapStates.None;
        transitionSnapState = SnapStates.None;

        initSurfaceHandPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        initRetargetSurfaceHandPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        pinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        initPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        initHandSurfacePosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        virtualWindowLocalScale = virtualWindow.transform.localScale;

        fingerOrientation = new List<float>() {0, 0, 0, 0, 0};

        ShowObject(virtualSurfaceFOR, false);
    }

    void Update()
    {
        














        currentCmulativeFingerOrientation = featureHelper.GetCmulativeFingerOrientation();
        currentCumulativeFingerSpreadOrientation = featureHelper.GetCumulativeFingerSpreadOrientation();

        if (interactionMethod == InteractionMethod.VRSnap)
        {
            //UpdateEachFingerRotDebugger();

            centerEyePos = interactionManager.GetCenterEyeGameObject().transform.position;
            palmPos = interactionManager.GetSurfaceHandPalmGameObject().transform.position;
            snapRayDirection = palmPos - centerEyePos;

            retargetPalmPos = interactionManager.GetRelocateSurfaceHandPalmGameObject().transform.position;
            retargetSnapRayDirection = retargetPalmPos - centerEyePos;

            if (!initGestureUnSnap)
            {
                showPointerRay(pointerRay, palmPos, snapRayDirection, false);
                if (currentCmulativeFingerOrientation > snapDictionary[SnapStates.UnSnapState].RotThreshold)
                {
                    curSnapState = SnapStates.UnSnapState;
                    initGestureUnSnap = true;
                }
            }
            else
            {
                CheckPinchGesture();
                if (activeVirtualSurface == null)
                    ActivateVirtualSurface();
                if (activeVirtualSurface != null)
                    interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);
                if (activeVirtualSurface != null)
                {
                    if (virtualWindow.transform.parent != activeVirtualSurface.transform)
                    {
                        virtualWindow.transform.parent = activeVirtualSurface.transform;
                        virtualWindow.transform.localPosition = new Vector3(0, 0, 0);
                        virtualWindow.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        virtualWindow.transform.localScale = virtualWindowLocalScale;
                    }
                    /*else
                    {
                        virtualWindow.transform.parent = surfaceHand.transform;
                    }*/
                    UpdateSnapState();
                }
                //UpdateSnapMode();
            }

            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);

            //UpdateSnapMode();
            //UpdateGestureDebugger();
            //VisualizeHandSurface();

            //GameObject.Find("virtualWindowCenter").transform.position = virtualWindow.transform.position;
            //GameObject.Find("handSurfaceCenter").transform.position = interactionManager.GetSurfaceReferenceGameObject().transform.position;
            //GameObject.Find("realHand").transform.position = interactionManager.GetInteractionHandGameObject().transform.position;
            //GameObject.Find("retargetedHand").transform.position = interactionManager.GetRelocateInteractionHandGameObject().transform.position;

            //only for video demo - mode gestures
            //ChangeOpacity(surfaceHand, 1f);
            //ShowObject(surfaceHand, true);
            //ChangeOpacity(interactionHand, 1f);
            //ShowObject(interactionHand, true);
            //ShowObject(relocateInteractionHandMesh, false);
            //only for video demo - mode gestures
        }
        else if (interactionMethod == InteractionMethod.InAir)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, false);
        }
        else if (interactionMethod == InteractionMethod.PseudoDirectTouch || interactionMethod == InteractionMethod.HandPseudoDirectPoke 
            || interactionMethod == InteractionMethod.HandPseudoDirectPoke_V2 || interactionMethod == InteractionMethod.CustomHandPointer
            || interactionMethod == InteractionMethod.HandMove || interactionMethod == InteractionMethod.EyeMoveHand 
            || interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump || interactionMethod == InteractionMethod.GazePinch)
        {
            //text.SetText("PseudoDirectTouch");
            ShowObject(virtualWindow, false);

            if (interactionManager.GetIsUpdateLookDownAngle())
            {
                UpdateLookDownAngleThreshold();
                interactionManager.SetUpdateLookDownAngle(false);
            }

            if (interactionManager.GetIsCalibrateFORGesture())
            {
                UpdateFORGesture();
                interactionManager.SetCalibrateFORGesture(false);
            }

            showPointerRay(pointerRay, palmPos, snapRayDirection, false);

            CheckPinchGesture();

            if (activeVirtualSurface == null)
                ActivateVirtualSurface();
            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);
            if (activeVirtualSurface != null)
            {
                //text.SetText("activeVirtualSurface: " + activeVirtualSurface);
                if (virtualWindow.transform.parent != activeVirtualSurface.transform)
                {
                    virtualWindow.transform.parent = activeVirtualSurface.transform;
                    virtualWindow.transform.localPosition = new Vector3(0, 0, 0);
                    virtualWindow.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    virtualWindow.transform.localScale = virtualWindowLocalScale;
                }
                UpdateSnapState();
            }

            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);

            ShowObject(handPinchArrow, false);
            ShowObject(retargetHandPinchArrow, false);
        }
        else if (interactionMethod == InteractionMethod.DirectTouch || interactionMethod == InteractionMethod.HandRaycast || 
            interactionMethod == InteractionMethod.HandPoke || interactionMethod == InteractionMethod.HandPoke_V2 || interactionMethod == InteractionMethod.HandRayPointer || interactionMethod == InteractionMethod.EyeGaze ||
            interactionMethod == InteractionMethod.GazePinch)
        {
            ShowObject(virtualWindow, false);
            ShowObject(virtualSurfaceFOR, false);

            showPointerRay(pointerRay, palmPos, snapRayDirection, false);
            if (activeVirtualSurface == null)
                ActivateVirtualSurface();
            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);
            if (activeVirtualSurface != null)
            {
                if (virtualWindow.transform.parent != activeVirtualSurface.transform)
                {
                    virtualWindow.transform.parent = activeVirtualSurface.transform;
                    virtualWindow.transform.localPosition = new Vector3(0, 0, 0);
                    virtualWindow.transform.localRotation = Quaternion.Euler(0, 0, 0);
                    virtualWindow.transform.localScale = virtualWindowLocalScale;
                }
                UpdateSnapState();
            }

            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);
        }
        else if(interactionMethod == InteractionMethod.HandIndirectTouch)
        {
            ShowObject(virtualWindow, false);
            ShowObject(virtualSurfaceFOR, false);

            if (!initGestureUnSnap)
            {
                showPointerRay(pointerRay, palmPos, snapRayDirection, false);
                if (currentCmulativeFingerOrientation > snapDictionary[SnapStates.UnSnapState].RotThreshold)
                {
                    curSnapState = SnapStates.UnSnapState;
                    initGestureUnSnap = true;
                }
            }
            else
            {
                CheckPinchGesture();
                if (activeVirtualSurface == null)
                    ActivateVirtualSurface();
                if (activeVirtualSurface != null)
                    interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);
                if (activeVirtualSurface != null)
                {
                    if (virtualWindow.transform.parent != activeVirtualSurface.transform)
                    {
                        virtualWindow.transform.parent = activeVirtualSurface.transform;
                        virtualWindow.transform.localPosition = new Vector3(0, 0, 0);
                        virtualWindow.transform.localRotation = Quaternion.Euler(0, 0, 0);
                        virtualWindow.transform.localScale = virtualWindowLocalScale;
                    }
                    UpdateSnapState();
                }
            }

            if (activeVirtualSurface != null)
                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);

            //text.SetText(curSnapState.ToString());
        }

        if(!showNonDomHand)
        {
            ShowObject(surfaceHand, false);
            ShowObject(relocateSurfaceHandMesh, false);
        }
    }

    private bool isLookDown()
    {
        bool isLookDown = false;
        Vector3 eyeDir = interactionManager.GetCenterEyeGameObject().transform.forward;
        Vector3 virtualSurfaceDir = virtualSurface.transform.up;
        float angle = Vector3.Angle(eyeDir, virtualSurfaceDir);

        //text.SetText("angle threshold: " + lookDownAngleThreshold + ", look down angle: " + angle.ToString());
        

        if (angle > lookDownAngleThreshold)
        {
            isLookDown = true;
        }

        return isLookDown;
    }

    private float CalculateInteractionPadTransparity()
    {
        float alpha = 0.0f;
        Vector3 eyeDir = interactionManager.GetCenterEyeGameObject().transform.forward;
        Vector3 virtualSurfaceDir = virtualSurface.transform.up;

        float angle = Vector3.Angle(eyeDir, virtualSurfaceDir);

        alpha = Mathf.InverseLerp(lookDownAngleThreshold, lookDownAngleThreshold + transparencyThreshold, angle);
        //text.SetText("alpha: " + alpha);

        return alpha;
    }

    private void UpdateLookDownAngleThreshold()
    {
        Vector3 eyeDir = interactionManager.GetCenterEyeGameObject().transform.forward;
        Vector3 virtualSurfaceDir = virtualSurface.transform.up;
        lookDownAngleThreshold = Vector3.Angle(eyeDir, virtualSurfaceDir);
        ShowObject(lookDownCalibrationGameObject, false);
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
            "finger spread angle: " + featureHelper.GetCumulativeFingerSpreadOrientation().ToString() + "\n" +
            //"current snap state: " + curSnapState + "\n" +
            //"transition snap state: " + transitionSnapState + "\n" +
            "gesture debug: " + gestureDebug + "\n" +
            debugPinching
        ) ;

        



    }

    private void CheckPinchGesture()
    {
        float dis = interactionManager.GetInteractionFingerTipDistance(0, 1);
        float hoverThreshold = interactionManager.GetHoverDistanceThreshold();

        if (dis <= pinchDisThreshold)
        {
            //raycast didn't hit handsurface
            if ((!interactionManager.GetSurfaceCursorState(0) && !interactionManager.GetSurfaceCursorState(1)) || 
                (interactionManager.GetFingerTipToSurfaceDistance(0) >= hoverThreshold && interactionManager.GetFingerTipToSurfaceDistance(1) >= hoverThreshold))
            {
                if (!isPinching)
                {
                    initPinchPosition = interactionManager.GetInteractionFingerTipPosition(1);
                }
                isPinching = true;
            }
            else
            {
                if (isPinching)
                {
                    initPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                }
                isPinching = false;
                initPinchOversnap = false;
            }
        }
        else
        {
            if (isPinching)
            {
                initPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            }
            isPinching = false;
            initPinchOversnap = false;
        }

        debugPinching = "isPinching: " + isPinching + ", pinchDis: " + dis;
    }

    public bool GetPinchState()
    {
        return isPinching;
    }

    private void PinchManipulation(SnapStates snapState)
    {
        if (isPinching && !Single.IsInfinity(initPinchPosition.x))
        {
            pinchPosition = interactionManager.GetInteractionFingerTipPosition(1);
            float pinchDis = Vector3.Distance(initPinchPosition, pinchPosition);

            debugPinching = snapState + " - isPinch: " + isPinching + ", pinchDis: " + pinchDis;

            //snap: pinch to adjust window precisely
            if (snapState == SnapStates.SnapState && IsObjectShow(virtualWindow))
            {
                Vector3 targetPosition = pinchPosition - initPinchPosition + virtualWindow.transform.position;
                Vector3 projTargetPosition1 = GetRaycastHitPoint(targetPosition, activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                Vector3 projTargetPosition2 = GetRaycastHitPoint(targetPosition, -1f * activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                if (!Single.IsInfinity(projTargetPosition1.x))
                    virtualWindow.transform.position = projTargetPosition1;
                else if(!Single.IsInfinity(projTargetPosition2.x))
                    virtualWindow.transform.position = projTargetPosition2;

                initPinchPosition = pinchPosition;
            }
            //semi-snap: resize window
            else if (snapState == SnapStates.SemiSnapState && IsObjectShow(virtualWindow))
            {
                float curX = virtualWindow.transform.localScale.x;
                float curY = virtualWindow.transform.localScale.y;
                float curZ = virtualWindow.transform.localScale.z;

                //float scale = pinchDis * 10;
                float scale = Vector3.Distance(pinchPosition, handSurface.transform.position) / Vector3.Distance(initPinchPosition, handSurface.transform.position);
                
                if ((virtualWindow.transform.localScale.x < initVirtualWindowSize.x && scale < 1) || (virtualWindow.transform.localScale.x > 1 && scale > 1)) scale = 1f;

                virtualWindow.transform.localScale = new Vector3(curX * scale, curY * scale, curZ);
                virtualWindowLocalScale = virtualWindow.transform.localScale;
                interactionManager.GetVirtualWindowReferenceGameObject().transform.localScale *= scale;
                initPinchPosition = pinchPosition;

                //debugPinching += ", scale: " + scale + ", FOR size: " + virtualWindow.transform.localScale;
            }
            //over-snap: resize whole display
            else if (snapState == SnapStates.OverSnapState && activeVirtualSurface != null)
            {
                //change virtual surface size
                














                if (!initPinchOversnap)
                {
                    initPinchPositionOversnap = activeVirtualSurface.transform.position;
                    initPinchOversnap = true;
                }

                if (initPinchOversnap)
                {
                    //change virtual surface position
                    Vector3 targetPosition = (pinchPosition - initPinchPosition)*8 + initPinchPositionOversnap;
                    activeVirtualSurface.transform.position = targetPosition;
                }

                





                //debugPinching += ", move web to: " + targetPosition;
            }
            //frame of reference(FOR): scale the FOR/hand, map to the virtual window
            else if(snapState == SnapStates.SetFOR && IsObjectShow(virtualSurfaceFOR))
            {              
                float curX = virtualSurfaceFOR.transform.localScale.x;
                float curY = virtualSurfaceFOR.transform.localScale.y;
                float curZ = virtualSurfaceFOR.transform.localScale.z;

                float curXpos = virtualSurfaceFOR.transform.localPosition.x;
                float curYpos = virtualSurfaceFOR.transform.localPosition.y;
                float curZpos = virtualSurfaceFOR.transform.localPosition.z;

                //float scale = pinchDis * 10;
                float scale = Vector3.Distance(pinchPosition, handSurface.transform.position) / Vector3.Distance(initPinchPosition, handSurface.transform.position);

                if ((virtualSurfaceFOR.transform.localScale.x < initVirtualSurfaceFORSize.x && scale < 1) || (virtualSurfaceFOR.transform.localScale.x > 1 && scale > 1)) scale = 1f;

                virtualSurfaceFOR.transform.localScale = new Vector3(curX * scale, curY * scale, curZ);
                virtualSurfaceFOR.transform.localPosition = new Vector3(curXpos * scale, curYpos * scale, curZpos);

                interactionManager.GetVirtualWindowReferenceGameObject().transform.localScale *= (1 / scale);
                initPinchPosition = pinchPosition;

                debugPinching += ", scale: " + scale + ", window size: " + virtualWindow.transform.localScale;
            }
        }
    }

    private void UpdateSnapState()
    {
        if (interactionMethod == InteractionMethod.PseudoDirectTouch || interactionMethod == InteractionMethod.HandPseudoDirectPoke 
            || interactionMethod == InteractionMethod.HandPseudoDirectPoke_V2 || interactionMethod == InteractionMethod.CustomHandPointer
            || interactionMethod == InteractionMethod.HandMove || interactionMethod == InteractionMethod.EyeMoveHand 
            || interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump || interactionMethod == InteractionMethod.GazePinch)
        {
            CheckFORGesture();

            if (interactionManager.GetFixFORPad())
            {
                isFORGesture = false;

                if (interactionManager.GetInitVirtualSurfaceFOR())
                {
                    SetHandSurfaceReference();
                    handRetargetingController.RelocateInteractionHand();
                    SnapStateGesture(SnapStates.SetFOR);

                    ShowObject(virtualSurfaceFOR, true);
                    ChangeOpacity(virtualSurfaceFOR, 0.5f);
                    ShowObject(interactionHand, true);
                    ChangeOpacity(interactionHand, 0.8f);
                    ShowObject(surfaceHand, true);
                    ChangeOpacity(surfaceHand, 0.8f);

                    isDuringInteraction = true;

                    interactionManager.SetInitVirtualSurfaceFOR(false);
                }
            }

            if (isFORGesture)
            {
                







                ActivateVirtualSurface();

                SetVirtualSurfaceAnchor();
                handRetargetingController.ChangeFrameOfReferencePosition(true);
                handRetargetingController.ChangeFrameOfReferenceRotation(true);
                SnapStateGesture(SnapStates.SetFOR);

                ShowObject(virtualSurfaceFOR, true);
                ChangeOpacity(virtualSurfaceFOR, 0.5f);
                ShowObject(interactionHand, true);
                ChangeOpacity(interactionHand, 0.8f);
                ShowObject(surfaceHand, true);
                ChangeOpacity(surfaceHand, 0.8f);

                isDuringInteraction = true;
            }
            else
            {
                








                if (curSnapState == SnapStates.SetFOR)
                {
                    





                    //disable for pseudo direct touch selection study
                    //reactive for whole system
                    //end the current interaction session, need to re-activate the interaction pad (FOR)
                    





                    ShowObject(interactionHand, false);
                    ChangeOpacity(interactionHand, 0f);
                    ShowObject(surfaceHand, false);
                    ChangeOpacity(surfaceHand, 0f);
                    ShowObject(virtualSurfaceFOR, false);

                    if (isDuringInteraction)
                    {
                        Vector3 fingerPos = interactionManager.GetInteractionFingerTipPosition(1);
                        float distance = interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(1);
                        bool isTouching = interactionManager.GetRetargetFingerPenetrateSurface(1);

                        



                        /*Debug.Log("finger pos: " + fingerPos.x + "," + fingerPos.y + "," + fingerPos.z + ";" +
                            "distance to surface: " + distance + ";" +
                            "touch: " + isTouching);*/

                        float alpha = CalculateInteractionPadTransparity();
                        if (alpha > 0)
                        {
                            ShowObject(virtualSurfaceFOR, true);
                            ChangeOpacity(virtualSurfaceFOR, alpha);
                            ShowObject(interactionHand, true);
                            ChangeOpacity(interactionHand, alpha);
                            ShowObject(surfaceHand, true);
                            ChangeOpacity(surfaceHand, alpha);

                            if (interactionMethod == InteractionMethod.HandMove || 
                                interactionMethod == InteractionMethod.EyeMoveHand || interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump)
                            {
                                ShowObject(retargetHandPinchArrow, false);
                                ShowObject(handPinchArrow, true);
                                ChangeOpacity(handPinchArrow, alpha);
                            }
                            else if(interactionMethod == InteractionMethod.GazePinch)
                            {
                                ShowObject(virtualSurfaceFOR, false);
                                /*ShowObject(interactionHand, true);
                                ShowObject(surfaceHand, true);
                                ChangeOpacity(interactionHand, 0.8f);
                                ChangeOpacity(surfaceHand, 0.8f);
                                ShowObject(relocateInteractionHandMesh, false);
                                ShowObject(relocateSurfaceHandMesh, false);*/

                            }
                            else if(interactionMethod == InteractionMethod.CustomHandPointer)
                            {
                                ShowObject(handPinchArrow, false);
                                ShowObject(retargetHandPinchArrow, false);
                            }
                        }
                        else
                        {
                            ShowObject(virtualSurfaceFOR, false);
                            ShowObject(interactionHand, false);
                            ChangeOpacity(interactionHand, 0f);
                            ShowObject(surfaceHand, false);
                            ChangeOpacity(surfaceHand, 0f);

                            if (interactionMethod == InteractionMethod.HandMove || 
                                interactionMethod == InteractionMethod.EyeMoveHand || interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump)
                            {
                                ShowObject(retargetHandPinchArrow, true);
                                ChangeOpacity(retargetHandPinchArrow, 1f);
                                ShowObject(handPinchArrow, false);
                                ChangeOpacity(handPinchArrow, 0f);
                            }
                            /*else if (interactionMethod == InteractionMethod.GazePinch)
                            {
                                ShowObject(virtualSurfaceFOR, false);
                                ShowObject(interactionHand, true);
                                ShowObject(surfaceHand, true);
                                ChangeOpacity(interactionHand, 0.8f);
                                ChangeOpacity(surfaceHand, 0.8f);
                                ShowObject(relocateInteractionHandMesh, false);
                                ShowObject(relocateSurfaceHandMesh, false);
                            }*/
                            else if (interactionMethod == InteractionMethod.CustomHandPointer)
                            {
                                ShowObject(handPinchArrow, false);
                                ShowObject(retargetHandPinchArrow, false);
                            }
                        }

                        handRetargetingController.ChangeFrameOfReferencePosition(false);
                        handRetargetingController.ChangeFrameOfReferenceRotation(false);
                        SnapStateGesture(SnapStates.SetFOR);
                    }
                    //SnapStateGesture(SnapStates.UnSnapState);
                }
                else
                {
                    ShowObject(interactionHand, true);
                    ChangeOpacity(interactionHand, 0.8f);
                    ShowObject(surfaceHand, true);
                    ChangeOpacity(surfaceHand, 0.8f);
                    ShowObject(virtualSurfaceFOR, false);
                }
            }
        }
        else if(interactionMethod == InteractionMethod.DirectTouch || interactionMethod == InteractionMethod.HandPoke || interactionMethod == InteractionMethod.HandPoke_V2)
        {
            handRetargetingController.SetRelocateHandSameToRealHand();

            ShowObject(interactionHand, false);
            ShowObject(surfaceHand, false);
            ShowObject(relocateInteractionHandMesh, true);
            ShowObject(relocateSurfaceHandMesh, true);
        }
        else if (interactionMethod == InteractionMethod.HandRaycast || interactionMethod == InteractionMethod.HandRayPointer || interactionMethod == InteractionMethod.EyeGaze)
        {
            handRetargetingController.SetRelocateHandSameToRealHand();

            ShowObject(interactionHand, true);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);
            ShowObject(relocateSurfaceHandMesh, false);

        }
        else if(interactionMethod == InteractionMethod.VRSnap || interactionMethod == InteractionMethod.InAir || interactionMethod == InteractionMethod.HandIndirectTouch)
        {
            ShowObject(virtualSurfaceFOR, false);

            if (curSnapState == SnapStates.UnSnapState)
            {
                SnapStateGesture(SnapStates.UnSnapState);
                if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.UnSnapToOverSnap].SpreadRotThreshold
                    && currentCumulativeFingerSpreadOrientation < snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.UnSnapToOverSnap);
                else if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.OverSnapState);
                else if (currentCmulativeFingerOrientation <= snapDictionary[SnapStates.UnSnapToSemiSnap].RotThreshold
                    && currentCmulativeFingerOrientation > snapDictionary[SnapStates.SemiSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.UnSnapToSemiSnap);
                else if (currentCmulativeFingerOrientation <= snapDictionary[SnapStates.SemiSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.SemiSnapState);
            }
            else if (curSnapState == SnapStates.SemiSnapState)
            {
                SnapStateGesture(SnapStates.SemiSnapState);
                if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.SemiSnapToOverSnap].SpreadRotThreshold
                    && currentCumulativeFingerSpreadOrientation < snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.SemiSnapToOverSnap);
                else if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.OverSnapState);
                else if (currentCmulativeFingerOrientation >= snapDictionary[SnapStates.SemiSnapToUnSnap].RotThreshold
                    && currentCmulativeFingerOrientation < snapDictionary[SnapStates.UnSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.SemiSnapToUnSnap);
                else if (currentCmulativeFingerOrientation >= snapDictionary[SnapStates.UnSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.UnSnapState);
                else if (currentCmulativeFingerOrientation <= snapDictionary[SnapStates.SemiSnapToSnap].RotThreshold
                    && currentCmulativeFingerOrientation > snapDictionary[SnapStates.SnapState].RotThreshold)
                    SnapStateTransition(SnapStates.SemiSnapToSnap);
                else if (currentCmulativeFingerOrientation <= snapDictionary[SnapStates.SnapState].RotThreshold)
                    SnapStateGesture(SnapStates.SnapState);
            }
            else if (curSnapState == SnapStates.SnapState)
            {
                SnapStateGesture(SnapStates.SnapState);
                if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.SnapToOverSnap].SpreadRotThreshold
                    && currentCumulativeFingerSpreadOrientation < snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.SnapToOverSnap);
                else if (currentCumulativeFingerSpreadOrientation >= snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold
                    && currentCmulativeFingerOrientation <= snapDictionary[SnapStates.OverSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.OverSnapState);
                else if (currentCmulativeFingerOrientation >= snapDictionary[SnapStates.SemiSnapToSnap].RotThreshold
                    && currentCmulativeFingerOrientation < snapDictionary[SnapStates.SemiSnapState].RotThreshold)
                    SnapStateTransition(SnapStates.SnapToSemiSnap);
                else if (currentCmulativeFingerOrientation >= snapDictionary[SnapStates.SemiSnapState].RotThreshold)
                    SnapStateGesture(SnapStates.SemiSnapState);
            }
            else if (curSnapState == SnapStates.OverSnapState)
            {
                //SnapStateGesture(SnapStates.OverSnapState);
                if (currentCumulativeFingerSpreadOrientation <= snapDictionary[SnapStates.OverSnapToUnSnap].SpreadRotThreshold
                    && currentCumulativeFingerSpreadOrientation > snapDictionary[SnapStates.UnSnapState].SpreadRotThreshold)
                    SnapStateTransition(SnapStates.OverSnapToUnSnap);
                else if (currentCumulativeFingerSpreadOrientation <= snapDictionary[SnapStates.UnSnapState].SpreadRotThreshold)
                    SnapStateGesture(SnapStates.UnSnapState);
                else
                    SnapStateGesture(SnapStates.OverSnapState);
            }           

            if (interactionMethod == InteractionMethod.HandIndirectTouch)
            {
                handRetargetingController.SetRelocateHandSameToRealHand();

                ShowObject(interactionHand, true);
                ShowObject(surfaceHand, true);
                ShowObject(relocateInteractionHandMesh, false);
                ShowObject(relocateSurfaceHandMesh, false);
            }
        }       
    }

    private float calculateRedirectHandTransparency()
    {
        float alpha = maxPinchHandOpacity;
        float dis = 10000f;

        if (retargetHandPinchArrow != null && cursor != null && activeVirtualSurface != null)
        {
            Vector3 dir = - activeVirtualSurface.transform.forward.normalized;
            Vector3 pinchToCursor = retargetHandPinchArrow.transform.position - cursor.transform.position;
            dis = Vector3.Dot(pinchToCursor, dir);
            float clampDis = Mathf.Clamp(dis, 0f, maxShowPinchHandDis);
            alpha = minPinchHandOpacity + (clampDis / maxShowPinchHandDis) * (maxPinchHandOpacity - minPinchHandOpacity);
        }

        return alpha;
    }

    private float calculateRedirectNonDomHandTransparency()
    {
        float alpha = maxPinchHandOpacity;
        float dis = 10000f;

        if (retargetNonDomHandPinchArrow != null && nonDomCursor != null && activeVirtualSurface != null)
        {
            Vector3 dir = - activeVirtualSurface.transform.forward.normalized;
            Vector3 pinchToCursor = retargetNonDomHandPinchArrow.transform.position - nonDomCursor.transform.position;
            dis = Vector3.Dot(pinchToCursor, dir);
            float clampDis = Mathf.Clamp(dis, 0f, maxShowPinchHandDis);
            alpha = minPinchHandOpacity + (clampDis / maxShowPinchHandDis) * (maxPinchHandOpacity - minPinchHandOpacity);
        }

        return alpha;
    }

    //SnapStateGestureWithSurfaceHandRetargeting
    private void SnapStateGesture(SnapStates snapState)
    {
        if (snapState == SnapStates.UnSnapState)
        {
            ShowObject(virtualSurfaceFOR, false);

            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            ChangeOpacity(virtualWindow, 0f);
            ShowObject(virtualWindow, false);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(surfaceHand, true);
            ChangeOpacity(interactionHand, 1.0f);
            ShowObject(interactionHand, true);
            ShowObject(relocateInteractionHandMesh, false);
            ShowObject(relocateSurfaceHandMesh, false);

            activeVirtualWindow = false;
            activeVirtualSurface = null;
        }
        else if (snapState == SnapStates.SemiSnapState)
        {
            ShowObject(virtualSurfaceFOR, false);

            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            PinchManipulation(SnapStates.SemiSnapState);

            if (activeVirtualWindow)
            {
                if (snapState != curSnapState)
                {
                    //retarget surface hand and move virtual window by raycasting
                    //handRetargetingController.InitRelocateSurfaceHand(activeVirtualWindow);

                    initHandSurfacePosition = new Vector3(handSurface.transform.position.x, handSurface.transform.position.y, handSurface.transform.position.z);
                    initVirtualWindowPosition = new Vector3(virtualWindow.transform.position.x, virtualWindow.transform.position.y, virtualWindow.transform.position.z);
                    initActiveVirtualSurface = activeVirtualSurface;
                }
                else
                {
                    if (!Single.IsInfinity(initHandSurfacePosition.x))
                    {
                        
                        Vector3 targetPosition = ScaleOffset(handSurface.transform.position - initHandSurfacePosition) + initVirtualWindowPosition;

                        //GameObject.Find("testHand").transform.position = targetPosition;

                        Vector3 projTargetPosition1 = GetRaycastHitPoint(targetPosition, activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                        Vector3 projTargetPosition2 = GetRaycastHitPoint(targetPosition, -1f * activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                        if (!Single.IsInfinity(projTargetPosition1.x))
                        {
                            virtualWindow.transform.position = projTargetPosition1;
                            gestureDebug = activeVirtualSurface.name;
                        }
                        else if (!Single.IsInfinity(projTargetPosition2.x))
                        {
                            virtualWindow.transform.position = projTargetPosition2;
                            gestureDebug = activeVirtualSurface.name;
                        }
                        else
                        {
                            //outside the current activeVirtualSurface, need to switch virtualsurface
                            //GameObject hitSurface1 = GetRaycastHitObject(targetPosition, 1.0f * (targetPosition - centerEyePos), virtualSurfaceLayer);
                            //GameObject hitSurface2 = GetRaycastHitObject(targetPosition, -1.0f * (targetPosition - centerEyePos), virtualSurfaceLayer);

                            //GameObject hitSurface1 = GetRaycastHitObject(targetPosition, 1.0f * virtualWindow.transform.forward, virtualSurfaceLayer);
                            //GameObject hitSurface2 = GetRaycastHitObject(targetPosition, -1.0f * virtualWindow.transform.forward, virtualSurfaceLayer);

                            //find new active virtual surface
                            GameObject targetVirtualSurface = HitActiveVirtualSurface(targetPosition);
                            if (targetVirtualSurface != null)
                            {
                                activeVirtualSurface = targetVirtualSurface;
                                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);

                                Vector3 projTargetPosition3 = GetRaycastHitPoint(targetPosition, activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                                Vector3 projTargetPosition4 = GetRaycastHitPoint(targetPosition, -1f * activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);

                                if (!Single.IsInfinity(projTargetPosition3.x))
                                {
                                    virtualWindow.transform.position = projTargetPosition3;
                                    gestureDebug = activeVirtualSurface.name;
                                }
                                else if (!Single.IsInfinity(projTargetPosition4.x))
                                {
                                    virtualWindow.transform.position = projTargetPosition4;
                                    gestureDebug = activeVirtualSurface.name;
                                }
                            }
                            else
                            {
                                virtualWindow.transform.position = targetPosition;
                                gestureDebug = "out of surface";
                            }

                            //different ray direction of finding new active virtual surface
                            /*GameObject hitSurface1 = GetRaycastHitObject(targetPosition, 1.0f * (targetPosition - surfaceHand.transform.position), virtualSurfaceLayer);
                            GameObject hitSurface2 = GetRaycastHitObject(targetPosition, -1.0f * (targetPosition - surfaceHand.transform.position), virtualSurfaceLayer);

                            if (hitSurface1 != null || hitSurface2 != null)
                            {
                                if (hitSurface1 != null) activeVirtualSurface = hitSurface1;
                                else if (hitSurface2 != null) activeVirtualSurface = hitSurface2;

                                interactionManager.SetActiveVirtualSurfaceGameObject(activeVirtualSurface);

                                Vector3 projTargetPosition3 = GetRaycastHitPoint(targetPosition, activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);
                                Vector3 projTargetPosition4 = GetRaycastHitPoint(targetPosition, -1f * activeVirtualSurface.transform.forward, virtualSurfaceLayer, activeVirtualSurface);

                                if (!Single.IsInfinity(projTargetPosition3.x))
                                    virtualWindow.transform.position = projTargetPosition3;
                                else if (!Single.IsInfinity(projTargetPosition4.x))
                                    virtualWindow.transform.position = projTargetPosition4;
                            }
                            else
                            {
                                virtualWindow.transform.position = targetPosition;
                            }*/
                        }

                        //Vector2 virtualWindowRelativePos = interactionManager.ComputeVirtualWindowRelativePositionOnWebview();

                        //initHandSurfacePosition = handSurface.transform.position;
                    }

                    handRetargetingController.RelocateSurfaceHand(activeVirtualWindow);
                    //relocateSurfaceHand.transform.position = virtualWindow.transform.position + surfaceHand.transform.position - handSurface.transform.position;
                    //relocateSurfaceHand.transform.rotation = surfaceHand.transform.rotation;

                    //UpdateVirtualWindowPositionByEyeToRetargetHandRaycast();
                    ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
                    ShowObject(relocateSurfaceHandMesh, true);
                    ShowObject(surfaceHand, false);
                }
            }
            else
            {
                UpdateVirtualWindowPositionByEyeToRealHandRaycast();
                ChangeOpacity(surfaceHand, 0.5f);
                ShowObject(surfaceHand, true);
                ShowObject(relocateSurfaceHandMesh, false);
            }

            handRetargetingController.RelocateInteractionHand();
            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(virtualWindow, true);
            ChangeOpacity(interactionHand, 1f);
            ShowObject(interactionHand, IsObjectShow(surfaceHand));
            ChangeOpacity(relocateInteractionHandMesh, 0.5f);
            ShowObject(relocateInteractionHandMesh, IsObjectShow(relocateSurfaceHandMesh));
        }
        else if (snapState == SnapStates.SnapState)
        {
            ShowObject(virtualSurfaceFOR, false);

            SetTransitionProgressForGameobject(virtualWindow, 1);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            if (snapState != curSnapState)
            {
                if (!activeVirtualWindow)
                    UpdateVirtualWindowPositionByEyeToRealHandRaycast();
            }
            else
                PinchManipulation(SnapStates.SnapState);

            activeVirtualWindow = true;

            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(virtualWindow, true);
            ShowObject(surfaceHand, false);
            ShowObject(relocateSurfaceHandMesh, false);

            //---only for storage task demo
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
            ShowObject(relocateSurfaceHandMesh, true);
            //---only for storage task demo

            handRetargetingController.RelocateInteractionHand();
            handRetargetingController.RelocateSurfaceHand(activeVirtualWindow);
            ShowObject(interactionHand, false);
            ChangeOpacity(relocateInteractionHandMesh, 0.5f);
            ShowObject(relocateInteractionHandMesh, true);
        }
        else if (snapState == SnapStates.OverSnapState)
        {
            ShowObject(virtualSurfaceFOR, false);

            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 1);

            PinchManipulation(SnapStates.OverSnapState);

            if (snapState != curSnapState)
            {
                //handRetargetingController.InitRelocateSurfaceHand(activeVirtualWindow);

                initHandSurfacePosition = handSurface.transform.position;
                handRetargetingController.InitRelocateVirtualSurface();
            }
            else
            {
                if (activeVirtualSurface != null)
                {
                    //handRetargetingController.RelocateVirtualSurface(activeVirtualSurface);

                    if (!Single.IsInfinity(initHandSurfacePosition.x))
                    {
                        //Vector3 targetPosition = handSurface.transform.position - initHandSurfacePosition + activeVirtualSurface.transform.position;
                        //activeVirtualSurface.transform.position = targetPosition;
                        handRetargetingController.RelocateSurfaceHand(activeVirtualWindow);
                        if (activeVirtualWindow)
                            handRetargetingController.RelocateInteractionHand();

                        initHandSurfacePosition = handSurface.transform.position;
                    }

                    handRetargetingController.RelocateVirtualSurface();
                }
            }

            ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
            ShowObject(relocateSurfaceHandMesh, true);
            ChangeOpacity(surfaceHand, 0.5f);
            ShowObject(surfaceHand, false);

            ChangeOpacity(interactionHand, 1f);
            ShowObject(interactionHand, false);
            ChangeOpacity(relocateInteractionHandMesh, 0.5f);
            ShowObject(relocateInteractionHandMesh, true);
        }
        else if (snapState == SnapStates.SetFOR)
        {
            
            if (snapState != curSnapState)
            {
                if (!activeVirtualWindow)
                {
                    virtualWindow.transform.position = interactionManager.GetWebviewBottomLeftGameObject().transform.position;
                    activeVirtualWindow = true;
                }
            }

            //ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            //ShowObject(virtualWindow, true);
            //ShowObject(surfaceHand, true);
            //ShowObject(relocateSurfaceHandMesh, false);

            handRetargetingController.RelocateInteractionHand();
            handRetargetingController.RelocateSurfaceHand(activeVirtualWindow);

            if (interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.GazePinch)
            {
                handRetargetingController.RetargetInteractionHandToGazeCursor();
            }

            //ShowObject(interactionHand, true);
            ChangeOpacity(relocateInteractionHandMesh, 0.5f);
            ShowObject(relocateInteractionHandMesh, true);
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
            ShowObject(relocateSurfaceHandMesh, true);

            if (interactionMethod == InteractionMethod.CustomHandPointer || interactionMethod == InteractionMethod.HandMove ||
                interactionMethod == InteractionMethod.EyeMoveHand || ((interactionMethod == InteractionMethod.EyeHandMove) && !isGazeDominate) ||
                interactionMethod == InteractionMethod.EyeHandJump)
            {
                //ChangeOpacity(retargetHandPinchArrow, 0.5f);
                //ShowObject(retargetHandPinchArrow, true);
                ShowObject(retargetHandPinchArrow, false);
                float alpha = calculateRedirectHandTransparency();
                ChangeOpacity(relocateInteractionHandMesh, alpha);
                

                ShowObject(retargetNonDomHandPinchArrow, false);
                alpha = calculateRedirectNonDomHandTransparency();
                ChangeOpacity(relocateSurfaceHandMesh, alpha);
            }            
            else if ((interactionMethod == InteractionMethod.EyeHandMove) && isGazeDominate)
            {
                ShowObject(retargetHandPinchArrow, false);
                ShowObject(relocateInteractionHandMesh, false);

                ShowObject(retargetNonDomHandPinchArrow, false);
                ShowObject(relocateSurfaceHandMesh, false);
            }
            else if (interactionMethod == InteractionMethod.GazePinch)
            {
                ShowObject(interactionHand, true);
                ShowObject(surfaceHand, true);
                ShowObject(handPinchArrow, true);
                ShowObject(nonDomHandPinchArrow, true);
                ChangeOpacity(interactionHand, 0.8f);
                ChangeOpacity(surfaceHand, 0.8f);
                ShowObject(relocateInteractionHandMesh, false);
                ShowObject(relocateSurfaceHandMesh, false);
                ShowObject(retargetHandPinchArrow, false);
                ShowObject(retargetNonDomHandPinchArrow, false);
            }
        }
        else
        {
            ChangeOpacity(virtualWindow, 0f);
            ShowObject(virtualWindow, false);
            ChangeOpacity(interactionHand, 1.0f);
            ShowObject(interactionHand, true);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);
            ShowObject(relocateSurfaceHandMesh, false);

            if (interactionMethod == InteractionMethod.CustomHandPointer || interactionMethod == InteractionMethod.HandMove || 
                interactionMethod == InteractionMethod.EyeMoveHand || interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump || interactionMethod == InteractionMethod.GazePinch)
            {
                //ShowObject(handPinchArrow, true);
                ShowObject(handPinchArrow, false);
                ShowObject(retargetHandPinchArrow, false);
            }

            activeVirtualWindow = false;
        }

        if (snapState != curSnapState)
        {
            lastSnapState = curSnapState;
            curSnapState = snapState;
        }
    }

    //check virtual window hits which virtual surface when moving hand
    //if it hits multiple, choose the nearest one
    public GameObject HitActiveVirtualSurface(Vector3 startPoint)
    {
        GameObject targetVirtualSurface = null;
        float disToVirtualSurface = 10000.0f;

        GameObject[] virtualSurfaces = GameObject.FindGameObjectsWithTag("virtual_surface");
        foreach (GameObject virtualSurfaceItem in virtualSurfaces)
        {
            RaycastHit hit;

            //infinity: no hit
            if (Physics.Raycast(startPoint, 1.0f * virtualSurfaceItem.transform.forward, out hit, 100.0f, LayerMask.GetMask(virtualSurfaceLayer)))
            {
                if (hit.distance < disToVirtualSurface)
                {
                    disToVirtualSurface = hit.distance;
                    targetVirtualSurface = hit.collider.gameObject;
                }
            }
            if (Physics.Raycast(startPoint, -1.0f * virtualSurfaceItem.transform.forward, out hit, 100.0f, LayerMask.GetMask(virtualSurfaceLayer)))
            {
                if(hit.distance < disToVirtualSurface)
                {
                    disToVirtualSurface = hit.distance;
                    targetVirtualSurface = hit.collider.gameObject;
                }
            }
        }
        return targetVirtualSurface;
    }

    private void SetVirtualSurfaceAnchor()
    {
        virtualSurfaceAnchor.transform.position = handSurfaceReference.transform.position;
        virtualSurfaceAnchor.transform.rotation = handSurfaceReference.transform.rotation;

        ShowObject(virtualSurfaceFOR, true);
    }

    private void SetHandSurfaceReference()
    {
        //handSurfaceReference.transform.position = virtualSurfaceAnchor.transform.position;
        //handSurfaceReference.transform.rotation = virtualSurfaceAnchor.transform.rotation;

        handRetargetingController.ChangeSurfaceReferencePosition(virtualSurfaceAnchor.transform.position);
        handRetargetingController.ChangeSurfaceReferenceRotation(virtualSurfaceAnchor.transform.rotation);

        ShowObject(virtualSurfaceFOR, true);
    }

    private Vector3 ScaleOffset(Vector3 offset)
    {
        Vector3 scaleOffset;

        float eyeToSurfaceDis = Vector3.Distance(interactionManager.GetCenterEyeGameObject().transform.position, activeVirtualSurface.transform.position);
        float eyeToHandDis = Vector3.Distance(interactionManager.GetCenterEyeGameObject().transform.position, handSurface.transform.position);
        float scale = eyeToSurfaceDis/eyeToHandDis;

        scaleOffset = offset * scale;

        return scaleOffset;
    }

    //SnapStateGestureWithSurfaceHandRaycasting
    private void SnapStateGestureWithSurfaceHandRaycasting(SnapStates snapState)
    {
        ActivateVirtualSurface();

        if (snapState == SnapStates.UnSnapState)
        {
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            ChangeOpacity(virtualWindow, 0f);
            ShowObject(virtualWindow, false);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);

            activeVirtualSurface = null;
        }
        else if (snapState == SnapStates.SemiSnapState)
        {
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            UpdateVirtualWindowPositionByEyeToRealHandRaycast();

            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(virtualWindow, true);
            ChangeOpacity(surfaceHand, 0.5f);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);
        }
        else if (snapState == SnapStates.SnapState)
        {
            SetTransitionProgressForGameobject(virtualWindow, 1);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);

            if (snapState != curSnapState)
            {
                UpdateVirtualWindowPositionByEyeToRealHandRaycast();
            }

            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(virtualWindow, true);
            ShowObject(surfaceHand, false);

            handRetargetingController.RelocateInteractionHand();
            ShowObject(interactionHand, false);
            ShowObject(relocateInteractionHandMesh, true);
        }
        else if (snapState == SnapStates.OverSnapState)
        {
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 1);

            if (snapState != curSnapState)
            {
                //don't need to point at the virtual surface to oversnap

                //move virtual surface
                Vector3 virtualSurfaceStartPos = activeVirtualSurface.transform.position;
                Vector3 handSurfaceStartPos = handSurface.transform.position;
                offsetOversnap = virtualSurfaceStartPos - handSurfaceStartPos;

                //rotate virtual surface
                handSurfaceOversnapStartRot = handSurface.transform.eulerAngles;
                virtualSurfaceOversnapStartRot = activeVirtualSurface.transform.eulerAngles;
            }
            else
            {
                virtualSurface.transform.position = palmPos + offsetOversnap;

                float curX = (handSurfaceOversnapStartRot.x - handSurface.transform.eulerAngles.x) / 2 + virtualSurfaceOversnapStartRot.x;
                float curY = (handSurfaceOversnapStartRot.z - handSurface.transform.eulerAngles.z) / 2 + virtualSurfaceOversnapStartRot.y;
                //activeVirtualSurface.transform.eulerAngles = new Vector3(curX, curY, activeVirtualSurface.transform.eulerAngles.z);
            }

            ShowObject(virtualWindow, false);
            ChangeOpacity(surfaceHand, 0.5f);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);
        }
        else
        {
            ChangeOpacity(virtualWindow, 0f);
            ShowObject(virtualWindow, false);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(surfaceHand, true);
            ShowObject(relocateInteractionHandMesh, false);
        }

        if (snapState != curSnapState)
        {
            lastSnapState = curSnapState;
            curSnapState = snapState;
        }
    }

    private void SnapStateTransition(SnapStates snapState)
    {
        if(activeVirtualSurface == null)
            ActivateVirtualSurface();

        if (snapState == SnapStates.UnSnapToOverSnap)
        {
            //make a frame around the virtual surface and loading the texture
            float progress = (currentCumulativeFingerSpreadOrientation - snapDictionary[SnapStates.UnSnapToOverSnap].SpreadRotThreshold) / (snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold - snapDictionary[SnapStates.UnSnapToOverSnap].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 1 - (progress * 0.5f));
            ChangeOpacity(relocateSurfaceHandMesh, 1 - (progress * 0.5f));
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, false);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, progress);
        }
        else if (snapState == SnapStates.SemiSnapToOverSnap)
        {
            //make a frame around the virtual surface and loading the texture
            float progress = (currentCumulativeFingerSpreadOrientation - snapDictionary[SnapStates.SemiSnapToOverSnap].SpreadRotThreshold) / (snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold - snapDictionary[SnapStates.SemiSnapToOverSnap].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 0.5f);
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, false);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, progress);
        }
        else if (snapState == SnapStates.SnapToOverSnap)
        {
            //make a frame around the virtual surface and loading the texture
            float progress = (currentCumulativeFingerSpreadOrientation - snapDictionary[SnapStates.SnapToOverSnap].SpreadRotThreshold)
                / (snapDictionary[SnapStates.OverSnapState].SpreadRotThreshold - snapDictionary[SnapStates.SnapToOverSnap].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, progress * 0.5f);
            ChangeOpacity(relocateSurfaceHandMesh, progress * 0.5f);
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, false);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, progress);
        }
        else if (snapState == SnapStates.OverSnapToUnSnap)
        {
            float progress = (snapDictionary[SnapStates.OverSnapToUnSnap].SpreadRotThreshold - currentCumulativeFingerSpreadOrientation)
                / (snapDictionary[SnapStates.OverSnapToUnSnap].SpreadRotThreshold - snapDictionary[SnapStates.UnSnapState].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 0.5f + (progress * 0.5f));
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f + (progress * 0.5f));
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, false);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, (1 - progress));
        }
        else if (snapState == SnapStates.UnSnapToSemiSnap)
        {
            float progress = (snapDictionary[SnapStates.UnSnapToSemiSnap].RotThreshold - currentCmulativeFingerOrientation)
                / (snapDictionary[SnapStates.UnSnapToSemiSnap].RotThreshold - snapDictionary[SnapStates.SemiSnapState].RotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 1 - (progress * 0.5f));
            ChangeOpacity(relocateSurfaceHandMesh, 1 - (progress * 0.5f));
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ChangeOpacity(virtualWindow, progress * InitVirtualWindowAlpha * 0.5f);
            ShowObject(virtualWindow, true);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);
        }
        else if (snapState == SnapStates.SemiSnapToUnSnap)
        {
            float progress = (currentCumulativeFingerSpreadOrientation - snapDictionary[SnapStates.SemiSnapToUnSnap].SpreadRotThreshold)
                / (snapDictionary[SnapStates.UnSnapState].SpreadRotThreshold - snapDictionary[SnapStates.SemiSnapToUnSnap].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 0.5f + (progress * 0.5f));
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f + (progress * 0.5f));
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f - (progress * InitVirtualWindowAlpha * 0.5f));
            ShowObject(virtualWindow, true);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);
        }
        else if (snapState == SnapStates.SemiSnapToSnap)
        {
            float progress = (snapDictionary[SnapStates.SemiSnapToSnap].RotThreshold - currentCmulativeFingerOrientation)
                / (snapDictionary[SnapStates.SemiSnapToSnap].RotThreshold - snapDictionary[SnapStates.SnapState].RotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, 0.5f - (progress * 0.5f));
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f - (progress * 0.5f));
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, true);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);
            SetTransitionProgressForGameobject(virtualWindow, progress);

            //---only for storage task demo
            ChangeOpacity(relocateSurfaceHandMesh, 0.5f);
            ShowObject(relocateSurfaceHandMesh, true);
            //---only for storage task demo
        }
        else if (snapState == SnapStates.SnapToSemiSnap)
        {
            float progress = (currentCumulativeFingerSpreadOrientation - snapDictionary[SnapStates.SnapToSemiSnap].SpreadRotThreshold)
                / (snapDictionary[SnapStates.SemiSnapState].SpreadRotThreshold - snapDictionary[SnapStates.SnapToSemiSnap].SpreadRotThreshold);
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;

            ChangeOpacity(surfaceHand, progress * 0.5f);
            ChangeOpacity(relocateSurfaceHandMesh, progress * 0.5f);
            ShowObject(surfaceHand, IsObjectShow(surfaceHand));
            ShowObject(relocateSurfaceHandMesh, IsObjectShow(relocateSurfaceHandMesh));
            ShowObject(virtualWindow, true);
            SetTransitionProgressForGameobject(virtualWindow, 1f - progress);
            SetTransitionProgressForGameobject(activeVirtualSurface, 0);
        }

        transitionSnapState = snapState;
    }

    private void UpdateSnapMode()
    {
        //recognize gestures based on the orientation of knuckles
        if (currentCmulativeFingerOrientation <= snapRotThreshold)
        {
            showPointerRay(pointerRay, palmPos, snapRayDirection, false);

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
                UpdateVirtualWindowPositionByEyeToRealHandRaycast();
            ShowObject(virtualWindow, true);

            //make a frame around the virtual window and change the size
            //ShowObject(virtualWindowFrame, true);
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

            ChangeOpacity(surfaceHand, (1-progress) * 0.5f);
            ShowObject(surfaceHand, true);
            SetTransitionProgressForGameobject(virtualSurface, 0);
            SetTransitionProgressForGameobject(virtualWindow, progress);
        }
        else if (mode1 == SnapModes.Snap && mode2 == SnapModes.OverSnap && activeVirtualSurface != null)
        {
            //virtualSurfaceMask.transform.position = virtualWindow.transform.position;
            ShowObject(virtualWindow, true);
            //ShowObject(virtualSurfaceMask, true);

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

            ChangeOpacity(surfaceHand, progress*0.5f);
            ShowObject(surfaceHand, true);
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(virtualSurface, progress);
        }
        else if (mode1 == SnapModes.SemiSnap && mode2 == SnapModes.UnSnap && activeVirtualSurface != null)
        {
            UpdateVirtualWindowPositionByEyeToRealHandRaycast();

            //make a frame to cover virtual window and decrease the size until it dispear
            /*ShowObject(virtualWindow, false);
            ShowObject(virtualWindowFrame, true);
            float sizeScale = 1 - ((currentCmulativeFingerOrientation - semiSnapRotThresholdMax) / (unSnapRotThreshold - semiSnapRotThresholdMax));
            virtualWindowFrame.transform.localScale = new Vector3(sizeScale * virtualWindowFrameSize.x, virtualWindowFrameSize.y, sizeScale * virtualWindowFrameSize.z);*/
            
            float opacity;
            if (currentCumulativeFingerSpreadOrientation < semiSnapRotThresholdMax)
                opacity = 0f;
            else if (currentCumulativeFingerSpreadOrientation > unSnapRotThreshold)
                opacity = 1f;
            else
                opacity = (currentCmulativeFingerOrientation - semiSnapRotThresholdMax) / (unSnapRotThreshold - semiSnapRotThresholdMax);

            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f - (opacity * InitVirtualWindowAlpha * 0.5f));
            ChangeOpacity(surfaceHand, 0.5f + (opacity * 0.5f));
            ShowObject(surfaceHand, true);
            ShowObject(virtualWindow, true);
        }
    }

    private void SetTransitionProgressForGameobject(GameObject obj, float progress)
    {
        if (obj == virtualWindow)
            obj.transform.Find("ProgressVisualizer(Clone)").gameObject.SetActive(true);

        if (obj != null)
            obj.GetComponent<ProgressVisualizerController>().SetProgress(progress);
    }

    private void ChangeOpacity(GameObject obj, float alpha)
    {
        if (obj.GetComponent<Renderer>().material.shader.name == "Interaction/OculusHand")
        {
            obj.GetComponent<Renderer>().material.SetFloat("_Opacity", alpha);
            obj.GetComponent<Renderer>().material.SetFloat("_OutlineOpacity", alpha);
        }
        else
        {
            var color = obj.GetComponent<Renderer>().material.color;
            var newColor = new UnityEngine.Color(color.r, color.g, color.b, alpha);
            obj.GetComponent<Renderer>().material.color = newColor;
        }

        






    }

    public void ShowObject(GameObject obj, bool isShow)
    {
        if (obj != null)
        {
            obj.GetComponent<Renderer>().enabled = isShow;

            if (obj == virtualWindow || obj == virtualSurfaceFOR)
            {
                if (obj.transform.Find("ProgressVisualizer(Clone)") != null)
                {
                    obj.transform.Find("ProgressVisualizer(Clone)").gameObject.SetActive(isShow);
                }
            }
        }      
    }

    private bool IsObjectShow(GameObject obj)
    {
        return obj.GetComponent<Renderer>().enabled;
    }

    private void showPointerRay(GameObject obj, Vector3 startPoint, Vector3 rayDirection, bool isshow)
    {
        








    }

    private GameObject GetRaycastHitObject(Vector3 origin, Vector3 direction, string layermask)
    {
        RaycastHit hit;

        //infinity: no hit
        if (Physics.Raycast(origin, direction, out hit, 100.0f, LayerMask.GetMask(layermask)))
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
        if (Physics.Raycast(origin, direction, out hit, 100.0f, LayerMask.GetMask(layermask)))
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
            SetTransitionProgressForGameobject(virtualWindow, 0);
            SetTransitionProgressForGameobject(virtualSurface, 0);

            ChangeOpacity(virtualWindow, 0f);
            ShowObject(virtualWindow, false);
            ShowObject(surfaceHand, true);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(virtualSurfaceMask, false);
            ShowObject(virtualWindowFrame, false);

            activeVirtualSurface = null;
        }
        else if (mode == SnapModes.SemiSnap)
        {
            SetTransitionProgressForGameobject(virtualSurface, 0);
            SetTransitionProgressForGameobject(virtualWindow, 0);

            UpdateVirtualWindowPositionByEyeToRealHandRaycast();

            //virtualWindowFrame.transform.localScale = virtualWindowFrameSize;
            //ShowObject(virtualWindowFrame, true);
            //ShowObject(virtualWindow, false);

            ShowObject(virtualWindow, true);
            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(surfaceHand, true);
            ChangeOpacity(surfaceHand, 0.5f);
            ShowObject(virtualSurfaceMask, false);
        }
        else if (mode == SnapModes.Snap)
        {
            SetTransitionProgressForGameobject(virtualSurface, 0);
            SetTransitionProgressForGameobject(virtualWindow, 1);

            if (snapMode != mode)
            {
                UpdateVirtualWindowPositionByEyeToRealHandRaycast();
            }
            ShowObject(virtualWindow, true);
            ChangeOpacity(virtualWindow, InitVirtualWindowAlpha * 0.5f);
            ShowObject(surfaceHand, false);
            ShowObject(virtualSurfaceMask, false);
            ShowObject(virtualWindowFrame, false);
        }
        else if (mode == SnapModes.OverSnap)
        {
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
                SetTransitionProgressForGameobject(virtualWindow, 0);
                SetTransitionProgressForGameobject(virtualSurface, 1);

                if (snapMode != mode)
                {
                    //don't need to point at the virtual surface to oversnap

                    //move virtual surface
                    Vector3 virtualSurfaceStartPos = activeVirtualSurface.transform.position;
                    Vector3 handSurfaceStartPos = handSurface.transform.position;
                    offsetOversnap = virtualSurfaceStartPos - handSurfaceStartPos;

                    //rotate virtual surface
                    handSurfaceOversnapStartRot = handSurface.transform.eulerAngles;
                    virtualSurfaceOversnapStartRot = activeVirtualSurface.transform.eulerAngles;

                    //virtualSurfaceMask.transform.position = virtualSurface.transform.position;
                    //virtualSurfaceMask.transform.localScale = virtualSurfaceMaskSize;
                }
                else
                {
                    virtualSurface.transform.position = palmPos + offsetOversnap;

                    float curX = (handSurfaceOversnapStartRot.x - handSurface.transform.eulerAngles.x)/2 + virtualSurfaceOversnapStartRot.x;
                    float curY = (handSurfaceOversnapStartRot.z - handSurface.transform.eulerAngles.z)/2 + virtualSurfaceOversnapStartRot.y;
                    activeVirtualSurface.transform.eulerAngles = new Vector3(curX, curY, activeVirtualSurface.transform.eulerAngles.z);
                }
            }
            ShowObject(virtualWindow, false);
            ShowObject(surfaceHand, true);
            ChangeOpacity(surfaceHand, 0.5f);
            ShowObject(virtualSurfaceMask, false);
            ShowObject(virtualWindowFrame, false);
        }
        else
        {
            ShowObject(virtualWindow, false);
            ShowObject(surfaceHand, true);
            ChangeOpacity(surfaceHand, 1.0f);
            ShowObject(virtualSurfaceMask, false);
            ShowObject(virtualWindowFrame, false);
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

    private void ActivateVirtualSurface()
    {
        GameObject head = interactionManager.GetCenterEyeGameObject();

        if (head != null)
        {
            GameObject hitSurface = GetRaycastHitObject(head.transform.position, head.transform.forward, virtualSurfaceLayer);

            if (hitSurface != null)
            {
                activeVirtualSurface = hitSurface;
            }

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
        }
    }

    private void UpdateVirtualWindowPositionByEyeToRealHandRaycast()
    {       
        if (interactionMethod == InteractionMethod.VRSnap)
        {
            palmPos = interactionManager.GetSurfaceHandPalmGameObject().transform.position;
            snapRayDirection = palmPos - centerEyePos;

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
                    //virtualWindowFrame.transform.position = hitpoint1;
                    //activeVirtualWindow = true;
                }
                else if (!Single.IsInfinity(hitpoint2.x))
                {
                    virtualWindow.transform.position = hitpoint2;
                    //virtualWindowFrame.transform.position = hitpoint2;
                    //activeVirtualWindow = true;
                }
            }
            else
            {
                activeVirtualWindow = false;
            }
        }
        else
        {
            Vector3 windowPos = interactionManager.GetHeadGazeLocation();
            if (windowPos.x != 10000)
            {
                virtualWindow.transform.position = interactionManager.GetHeadGazeLocation();
            }
            else
            {
                activeVirtualWindow = false;
            }
        }
    }

    private void UpdateVirtualWindowPositionByEyeToRetargetHandRaycast()
    {
        retargetPalmPos = interactionManager.GetRelocateSurfaceHandPalmGameObject().transform.position;
        retargetSnapRayDirection = retargetPalmPos - centerEyePos;

        GameObject hitSurface1 = GetRaycastHitObject(retargetPalmPos, 1.0f * retargetSnapRayDirection, virtualSurfaceLayer);
        GameObject hitSurface2 = GetRaycastHitObject(retargetPalmPos, -1.0f * retargetSnapRayDirection, virtualSurfaceLayer);

        if (hitSurface1 != null || hitSurface2 != null)
        {
            if (hitSurface1 != null) activeVirtualSurface = hitSurface1;
            else if (hitSurface2 != null) activeVirtualSurface = hitSurface2;

            Vector3 hitpoint1 = GetRaycastHitPoint(retargetPalmPos, 1.0f * retargetSnapRayDirection, virtualSurfaceLayer, activeVirtualSurface);
            Vector3 hitpoint2 = GetRaycastHitPoint(retargetPalmPos, -1.0f * retargetSnapRayDirection, virtualSurfaceLayer, activeVirtualSurface);
            
            if (!Single.IsInfinity(hitpoint1.x))
                virtualWindow.transform.position = hitpoint1;
            else if (!Single.IsInfinity(hitpoint2.x))
                virtualWindow.transform.position = hitpoint2;
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

    public SnapStates GetSnapState()
    {
        return curSnapState;
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

    private void UpdateEachFingerRotDebugger() // Temporary Function
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerOrientation[i] = Vector3.Angle(vect3, vect4) + Vector3.Angle(vect2, vect3);
        }

        text.SetText(
            "thumb angle: " + fingerOrientation[0].ToString() + "\n" +
            "index angle:  " + fingerOrientation[1].ToString() + "\n" +
            "middle angle:  " + fingerOrientation[2].ToString() + "\n" +
            "ring angle:  " + fingerOrientation[3].ToString() + "\n" +
            "pinky angle:  " + fingerOrientation[4].ToString()
        );
    }

    //set frame of reference
    private void CheckFORGesture()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerOrientation[i] = Vector3.Angle(vect3, vect4) + Vector3.Angle(vect2, vect3);
        }

        isFORGesture = true;

        if(fingerOrientation[0] > stretchThumbFingerRotThreshold) { isFORGesture = false; }
        if (fingerOrientation[1] > stretchIndexFingerRotThreshold) { isFORGesture = false; }

        for (int i = 2; i < 5; i++)
        {
            if (fingerOrientation[i] < bendFingerRotThreshold) { isFORGesture = false; break; }
        }

        if (interactionManager.GetFixFORPad())
        {
            isFORGesture = false;
        }
    }

    private void UpdateFORGesture()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerOrientation[i] = Vector3.Angle(vect3, vect4) + Vector3.Angle(vect2, vect3);
        }

        stretchThumbFingerRotThreshold = fingerOrientation[0] + 20;
        stretchIndexFingerRotThreshold = fingerOrientation[1] + 20;
        bendFingerRotThreshold = fingerOrientation[2] - 20;
    }

    private bool CheckFistGesture()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerOrientation[i] = Vector3.Angle(vect3, vect4) + Vector3.Angle(vect2, vect3);
        }

        bool isFistGesture = true;
        for (int i = 1; i < 5; i++)
        {
            if (fingerOrientation[i] < bendFingerRotThreshold) { isFistGesture = false; break; }
        }

        return isFistGesture;
    }

    private bool CheckIsInteractionHandPointing()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetInteractionHandSecondKnucklePosition(i) - interactionManager.GetInteractionHandFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetInteractionHandThirdKnucklePosition(i) - interactionManager.GetInteractionHandSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetInteractionHandForthKnucklePosition(i) - interactionManager.GetInteractionHandThirdKnucklePosition(i);

            interactionFingerOrientation[i] = Vector3.Angle(vect3, vect4) + Vector3.Angle(vect2, vect3);
        }

        bool isInteractionHandPointing = true;

        if (fingerOrientation[1] > stretchInteractionIndexFingerRotThreshold) { isInteractionHandPointing = false; }

        for (int i = 2; i < 5; i++)
        {
            if (fingerOrientation[i] < bendInteractionFingerRotThreshold) { isInteractionHandPointing = false; break; }
        }

        return isInteractionHandPointing;

    }

    public bool GetIsFORGesture()
    {
        return isFORGesture;
    }

    public void SetIsDuringInteraction(bool InAction)
    {
        isDuringInteraction = InAction;
    }

    public bool GetIsDuringInteraction()
    {
        return isDuringInteraction;
    }

    //switch between gaze/hand for tasks such as docking
    public void IsGazeDominate(bool isGazeDom)
    {
        isGazeDominate = isGazeDom;
    }
}
