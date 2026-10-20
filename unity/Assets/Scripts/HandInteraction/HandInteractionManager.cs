using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Vuplex.WebView;
public class HandInteractionManager : MonoBehaviour
{
    OneEuroFilter<Vector3>[] retargetPositionFilters = new OneEuroFilter<Vector3>[5];
    OneEuroFilter<Vector3>[] positionFilters = new OneEuroFilter<Vector3>[5];

    OneEuroFilter<Vector3>[] gazePositionFilters = new OneEuroFilter<Vector3>[1];
    OneEuroFilter<Vector3>[] gazeCursorPositionFilters = new OneEuroFilter<Vector3>[1];

    float filterFrequency = 75.0f;
    float filterDcutoff = 1.0f;

    enum Handedness
    {
        Right,
        Left
    };

    [SerializeField]
    private Handedness dominantHand;

    //surface hand
    [SerializeField]
    private GameObject nonDomHand;

    //interaction hand
    [SerializeField]
    private GameObject domHand;

    //surface hand
    [SerializeField]
    private GameObject retargetNonDomHand;

    //interaction hand
    [SerializeField]
    private GameObject retargetDomHand;

    [SerializeField]
    private OVREyeGaze leftEyeGaze;

    [SerializeField]
    private OVREyeGaze rightEyeGaze;

    public enum InteractionMethod
    {
        VRSnap,
        InAir,
        PseudoDirectTouch,
        EyeGaze,
        DirectTouch,
        HandRaycast,
        HandRayPointer,
        //touch based on distance/collide
        HandPoke,
        //touch based on speed change
        HandPoke_V2,
        //touch based on distance/collide
        HandPseudoDirectPoke,
        //touch detection based on hand velocity
        HandPseudoDirectPoke_V2,
        CustomHandPointer,
        HandIndirectTouch,
        //hand to indicate/move/manipulate
        HandMove,
        //eye gaze to indicate/move, hand to manipulate
        EyeMoveHand,
        EyeHandMove,
        EyeHandJump,
        DirectPinch,
        GazePinch
    }

    [SerializeField]
    private InteractionMethod interactionMethod;

    [SerializeField]
    private GameObject interactionSpace;

    [SerializeField]
    [Range(0.0f, 0.5f)] private float hoverDistanceThreshold = 0.1f;

    [SerializeField] private WebViewPrefab activeWebviewPrefab;

    [SerializeField] private Boolean filterOn = true;

    // Beta controls how strongly the cutoff increases with signal speed.
    [SerializeField]
    [Range(0.0f, 100f)] private float filterBeta = 90f;

    [SerializeField]
    [Range(0.1f, 5.0f)] private float filterMinCutoff = 0.9f;

    [SerializeField] private Boolean gazeFilterOn = true;

    // Beta controls how strongly the cutoff increases with signal speed.
    [SerializeField]
    [Range(0.0f, 100f)] private float gazeFilterBeta = 0.01f;

    [SerializeField]
    [Range(0.1f, 5.0f)] private float gazeFilterMinCutoff = 0.5f;


    // Handedness Variables
    private Handedness interactionHandedness;
    private Handedness surfaceHandedness;

    //eye
    private GameObject centerEye;

    // Key Interaction GameObjects
    private GameObject surface;
    //for hand retargeting
    private GameObject surfaceReference;
    private GameObject surfaceMatchVirtualWindow;
    private GameObject surfaceHand;
    private GameObject surfaceHandReference;
    private GameObject surfaceHandPalm;
    private GameObject interactionHand;

    private GameObject virtualSurface;
    private GameObject virtualSurfaceReference;
    private GameObject virtualSurfaceMask;
    private GameObject virtualSurfaceAnchor;
    private GameObject virtualObject;
    private GameObject virtualObjectReference;
    private GameObject activeVirtualSurface;

    //interaction pad
    private GameObject virtualSurfaceFOR;

    private GameObject activeWebview;
    private GameObject LastActiveWebview;
    private GameObject webviewBottomLeft, webviewBottomRight, webviewTopLeft, webviewTopRight;
    private Vector3 webviewXAxis, webviewYAxis, webviewNormal;

    private GameObject virtualWindow;
    private GameObject virtualWindowBottomLeft, virtualWindowBottomRight, virtualWindowTopLeft, virtualWindowTopRight;
    private Vector3 virtualWindowXAxis, virtualWindowYAxis, virtualWindowNormal;

    private GameObject virtualWindowReference;
    private GameObject virtualWindowFrame;
    private GameObject handSurfaceVirtualObject;

    // Hand/Haptic Retargeting GameObjects
    private GameObject relocateInteractionHand;
    private GameObject relocateInteractionHandMesh;
    private GameObject relocateSurfaceHand;
    private GameObject relocateSurfaceHandMesh;
    private GameObject relocateSurfaceHandPalm;
    private GameObject interactionHandRelativeToHandSurface;
    private GameObject pinchArrowRelativeToHandSurface;

    // Surface Position and Orientation Variables
    private GameObject surfaceWrist;
    private GameObject surfaceOffset;
    private GameObject surfaceBottomLeft, surfaceBottomRight, surfaceTopLeft, surfaceTopRight;
    private Vector3 surfaceXAxis, surfaceYAxis, surfaceNormal;

    // FingerTip Variables
    private GameObject[] surfaceFingerTips = new GameObject[5];
    private GameObject[] interactionFingerTips = new GameObject[5];
    private GameObject[] retargetInteractionFingerTips = new GameObject[5];
    private GameObject[] retargetSurfaceFingerTips = new GameObject[5];
    private Vector3[] filteredRetargetFingerTips = new Vector3[5];

    //surface hand/non-dom hand
    //bottom to top (fingertip)
    //only thumb and pinky have 0~3 knuckles+fingertip, others have 1~3+fingertip
    private GameObject[] zeroKnuckles = new GameObject[5];
    private GameObject[] firstKnuckles = new GameObject[5];
    private GameObject[] secondKnuckles = new GameObject[5];
    private GameObject[] thirdKnuckles = new GameObject[5];
    private GameObject[] forthKnuckles = new GameObject[5];

    //interation hand/dom hand
    private GameObject[] interactionHandZeroKnuckles = new GameObject[5];
    private GameObject[] interactionHandFirstKnuckles = new GameObject[5];
    private GameObject[] interactionHandSecondKnuckles = new GameObject[5];
    private GameObject[] interactionHandThirdKnuckles = new GameObject[5];
    private GameObject[] interactionHandForthKnuckles = new GameObject[5];
    private GameObject interactionWrist;

    // FingerTip Interaction Variables
    private float[,] surfaceFingerTipDistances = new float[5, 5];
    private float[,] interactionFingerTipDistances = new float[5, 5];
    private float[] fingerTipToSurfaceDistances = new float[5];
    private float[] fingerTipToSurfaceHandDistances = new float[5];
    private bool[] fingerPenetrateSurface = new bool[5];


    // Cursor Interaction Variables
    private bool[] surfaceCursorStates = new bool[5];
    private bool[] surfaceHandCursorStates = new bool[5];
    private Vector3[] surfaceCursorPositions = new Vector3[5];
    private Vector3[] surfaceHandCursorPositions = new Vector3[5];
    private Vector2[] surfaceCursorRelativePositions = new Vector2[5];

    //for retarget interaction
    private bool[] virtualWindowCursorStates = new bool[5];
    private Vector3[] virtualWindowCursorPositions = new Vector3[5];
    private Vector2[] virtualWindowCursorRelativePositions = new Vector2[5];

    private bool[] fingerPenetrateVirtualSurfaceFOR = new bool[5];
    private bool[] virtualSurfaceFORCursorStates = new bool[5];
    private Vector3[] virtualSurfaceFORCursorPositions = new Vector3[5];
    private float[] interactionFingerTipToVirtualSurfaceFORDistances = new float[5];

    private bool[] webviewCursorStates = new bool[5];
    private Vector3[] webviewCursorPositions = new Vector3[5];
    private Vector2[] webviewCursorRelativePositions = new Vector2[5];
    private bool[] retargetFingerPenetrateSurface = new bool[5];
    private float[] retargetFingerTipToVirtualSurfaceDistances = new float[5];
    private float[] retargetFingerTipToVirtualSurfaceSpeed = new float[5] { 0, 0, 0, 0, 0 };

    private GameObject handPinchArrow;
    private GameObject handPinchArrowOffset;
    private GameObject retargetHandPinchArrow;
    private GameObject cursor;

    private GameObject nonDomHandPinchArrow;
    private GameObject nonDomHandPinchArrowOffset;
    private GameObject retargetNonDomHandPinchArrow;
    private GameObject nonDomCursor;

    //for pinch raycasting
    private bool pinchWebviewCursorState;
    private Vector3 pinchWebviewCursorPosition;
    private Vector2 pinchWebviewCursorRelativePosition;

    private bool nonDomHandPinchWebviewCursorState;
    private Vector3 nonDomHandPinchWebviewCursorPosition;
    private Vector2 nonDomHandPinchWebviewCursorRelativePosition;

    //for pinch raycasting
    private bool indirectCursorState;
    private Vector3 indirectCursorPosition;
    private Vector2 indirectCursorRelativePosition;

    // FingerTip Touch States Variables
    private Boolean[] touchStates = new Boolean[5] { false, false, false, false, false };

    //IndexFinger Touch States List
    private List<int> indexTouchStates = new List<int>();

    //final version touch detection
    private bool[] touchStateAllFinger = new bool[5] { false, false, false, false, false };

    //calculate speed of finger
    private int speedFrameWindow = 10;
    private Queue<Vector3> fingerPositionHistory;
    private Queue<float> timeHistory;
    private float currentFingerSpeed;
    public float dwellingFingerSpeedThreshold = 0.01f;
    private float dwellingTimeThreshold = 1f;
    private float startDwellingTime = - 10000f;

    //calculate speed of gaze
    private int gazeSpeedFrameWindow = 10;
    private Queue<Vector3> gazePositionHistory;
    private Queue<float> gazeTimeHistory;
    private float currentGazeSpeed;

    //calculate move dis of gaze
    private int gazeMoveDisFrameWindow = 70;
    private Queue<Vector3> gazeMovePositionHistory;
    private float currentGazeTotalMoveDis;

    private float initNearSurfaceDis = 0.5f;
    private float initFarSurfaceDis = 2f;

    public float nearSurfaceDis = 0.5f;
    private float middleSurfaceDis = 0.75f;
    private float farSurfaceDis = 2f;

    private float retargetIndexFingerToSurfaceSpeed = 0;

    //interaction finger to virtualSurfaceFOR/Interaction Pad
    private Vector3 prevInteractionIndexFingerPos;
    private float interactionIndexFingerToSurfaceSpeed = 10000;
    private float prevInteractionIndexFingerToSurfaceSpeed = 10000;
    private float interactionIndexFingerToSurfaceSpeedX = 10000;
    private float interactionIndexFingerToSurfaceSpeedY = 10000;

    //normal direction
    private Queue<float> interactionFingerSpeedBuffer;
    private Queue<float> averageInteractionFingerSpeedBuffer;
    private int frameWindow = 20; // Number of frames to average over
    public int prevSpeedFrameWindow = 1;

    //x direction
    private Queue<float> interactionFingerSpeedBufferX;
    //y direction
    private Queue<float> interactionFingerSpeedBufferY;

    //if the finger change from approaching the surface to leaving the surface
    private bool interactionFingerTouchIntention = false;
    public float interactionFingerTouchSpeedThreshold = 0f;
    //normal direction
    private float interactionFingerNormalTouchSpeedThreshold = 0.00f;
    //x,y direction
    private float interactionFingerTouchSpeedThresholdX = 1f;
    private float interactionFingerTouchSpeedThresholdY = 1f;
    public float touchProximityThreshold = 0f;

    private float touchAccelerationThreshold = 0.1f;

    public int webCursorWindow = 30;
    private Queue<Vector3> webCursorBuffer;
    private Queue<Vector3> interactionFingerBuffer;
    private Queue<Vector3> aveInteractionFingerBuffer;

    public int movingSpeedWindow = 20;
    private Queue<float> movingX;
    private Queue<float> movingY;
    private Queue<float> movingNormal;

    //relative position difference between frames
    private Queue<float> movingSpeedX;
    private Queue<float> movingSpeedY;
    private Queue<float> movingSpeedNormal;
    private float alignNormal = 10000;
    public float alignThreshold = 0;

    public bool initSurface = false;
    private bool initVirtualSurfaceFOR = false;
    public bool calibrateFORGesture = false;
    public bool fixFORPad = false;
    public bool updateLookDownAngle = false;
    public bool visualizeTouchPoints = false;
    public bool updateInteractionPadPosition = false;
    public bool updateInteractionPadRotation = false;

    private float handDisplayRatio = 1;

    private Vector3 gazeOrigin = new Vector3(10000, 10000, 10000);
    private Vector3 smoothGazeOrigin = new Vector3(10000, 10000, 10000);
    private Vector3 gazeDirection = new Vector3(10000, 10000, 10000);
    private Vector3 smoothGazeCursor = new Vector3(10000, 10000, 10000);

    public float eyeElbowDis = 0.4f;

    //if it is two hand interaction
    public bool twoHand = false;

    void Start()
    {
        //RenameHand(GameObject.Find("RelocateLeftHand/OVRCustomHandPrefab_L").transform, "Retarget");
        //RenameHand(GameObject.Find("RelocateRightHand/OVRCustomHandPrefab_R").transform, "Retarget");

        RenameHand(retargetDomHand.transform, "Retarget");
        RenameHand(retargetNonDomHand.transform, "Retarget");

        





        InitSurface();
        InitHandedness();

        handPinchArrow = GameObject.Find("RightHand/HandInteractorsRight/HandRayInteractor/Visuals/RaycasterPinchVisual/OculusHandPinchArrowBlended");
        handPinchArrowOffset = GameObject.Find("RightHand/HandInteractorsRight/HandRayInteractor/Visuals/RaycasterPinchVisual/OculusHandPinchArrowBlended/HandPinchArrowOffset");
        retargetHandPinchArrow = GameObject.Find("RelocateOculusHandPinchArrowBlended");
        cursor = GameObject.Find("CursorObject");

        nonDomHandPinchArrow = GameObject.Find("LeftHand/HandInteractorsLeft/HandRayInteractor/Visuals/RaycasterPinchVisual/OculusHandPinchArrowBlended");
        nonDomHandPinchArrowOffset = GameObject.Find("LeftHand/HandInteractorsLeft/HandRayInteractor/Visuals/RaycasterPinchVisual/OculusHandPinchArrowBlended/HandPinchArrowOffset");
        retargetNonDomHandPinchArrow = GameObject.Find("RelocateNonDomOculusHandPinchArrowBlended");
        nonDomCursor = GameObject.Find("NonDomCursorObject");

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                interactionFingerTipDistances[i, j] = 10000;
                surfaceFingerTipDistances[i, j] = 10000;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            retargetPositionFilters[i] = new OneEuroFilter<Vector3>(filterFrequency);
            positionFilters[i] = new OneEuroFilter<Vector3>(filterFrequency);
        }

        gazePositionFilters[0] = new OneEuroFilter<Vector3>(filterFrequency);
        gazeCursorPositionFilters[0] = new OneEuroFilter<Vector3>(filterFrequency);

        fingerPositionHistory = new Queue<Vector3>();
        timeHistory = new Queue<float>();

        gazePositionHistory = new Queue<Vector3>();
        gazeTimeHistory = new Queue<float>();
        gazeMovePositionHistory = new Queue<Vector3>();

        interactionFingerSpeedBuffer = new Queue<float>();
        averageInteractionFingerSpeedBuffer = new Queue<float>();

        interactionFingerSpeedBufferX = new Queue<float>();
        interactionFingerSpeedBufferY = new Queue<float>();

        webCursorBuffer = new Queue<Vector3>();
        interactionFingerBuffer = new Queue<Vector3>();
        aveInteractionFingerBuffer = new Queue<Vector3>();
        movingX = new Queue<float>();
        movingY = new Queue<float>();
        movingNormal = new Queue<float>();
        movingSpeedX = new Queue<float>();
        movingSpeedY = new Queue<float>();
        movingSpeedNormal = new Queue<float>();        
    }

    void Update()
    {
        if (initSurface || Input.GetKeyDown(KeyCode.Space))
        {
            //LocateSurface();
            LocateSurfaceByGaze();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            updateLookDownAngle = true;
        }

        surfaceOffset = GameObject.Find("LeftHandSurfaceOffset");

        if (surfaceOffset != null)
        {
            UpdateSurface();
        }

        if (interactionFingerTips[1] != null && surfaceFingerTips[1] != null)
        {
            UpdateFingerSpeed();
            UpdateSurfaceInteractions();
            if (activeWebview != null)
                UpdateWebview();
        }
        
        
        
        if(interactionMethod == InteractionMethod.EyeGaze || interactionMethod == InteractionMethod.EyeMoveHand || 
            interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump || interactionMethod == InteractionMethod.GazePinch)
        {
            UpdateGazeCusor();
            UpdateGazeSpeed();
            UpdateGazeMoveDistance();
        }
    
    }

    private float CalculateDistanceOnDirection(Vector3 pointA, Vector3 pointB, Vector3 direction)
    {
        // Normalize the direction vector
        Vector3 normalizedDirection = direction.normalized;

        // Get the vector between the two points
        Vector3 pointDifference = pointB - pointA;

        // Project the point difference vector onto the direction vector
        float projectedDistance = Vector3.Dot(pointDifference, normalizedDirection);

        return projectedDistance;
    }

    private void LocateSurface()
    {
        if (GameObject.Find("NearSurface") != null)
        {
            nearSurfaceDis = CalculateDistanceOnDirection(centerEye.transform.position, GetInteractionFingerTipPosition(1), centerEye.transform.forward);
            virtualSurface.transform.position = GetInteractionFingerTipPosition(1);
            virtualSurface.transform.localScale *= (nearSurfaceDis/initNearSurfaceDis);
            virtualSurfaceAnchor.transform.localScale *= (nearSurfaceDis / initNearSurfaceDis);
            initNearSurfaceDis = nearSurfaceDis;
        }

        if (GameObject.Find("FarSurface") != null)
        {
            nearSurfaceDis = CalculateDistanceOnDirection(centerEye.transform.position, GetInteractionFingerTipPosition(1), centerEye.transform.forward);
            farSurfaceDis = 4 * nearSurfaceDis;
            virtualSurface.transform.position = centerEye.transform.position + centerEye.transform.forward * farSurfaceDis - centerEye.transform.up * 0.3f;
            virtualSurface.transform.localScale *= (nearSurfaceDis / initNearSurfaceDis);
            virtualSurfaceAnchor.transform.localScale *= (nearSurfaceDis / initNearSurfaceDis);
            initNearSurfaceDis = nearSurfaceDis;
            initFarSurfaceDis = farSurfaceDis;
        }

        initSurface = false;
        initVirtualSurfaceFOR = true;
    }

    private void LocateSurfaceByGaze()
    {
        if (interactionMethod == InteractionMethod.GazePinch)
        {
            if (GameObject.Find("NearSurface") != null)
            {
                virtualSurface.transform.position = centerEye.transform.position + centerEye.transform.forward * nearSurfaceDis;
            }

            if (GameObject.Find("FarSurface") != null)
            {
                virtualSurface.transform.position = centerEye.transform.position + centerEye.transform.forward * farSurfaceDis;
            }
        }
        else
        {
            if (GameObject.Find("NearSurface") != null)
            {
                virtualSurface.transform.position = centerEye.transform.position + centerEye.transform.forward * nearSurfaceDis;

                virtualSurfaceFOR.transform.parent = null;
                virtualSurfaceAnchor.transform.parent = virtualSurfaceFOR.transform;

                virtualSurfaceFOR.transform.position = centerEye.transform.position + centerEye.transform.forward * nearSurfaceDis * (5f / 10f) - virtualSurface.transform.up * eyeElbowDis;
                virtualSurfaceFOR.transform.rotation = virtualSurface.transform.rotation;
                virtualSurfaceFOR.transform.Rotate(45, 0, 0, Space.Self);

                virtualSurfaceAnchor.transform.parent = null;
                virtualSurfaceFOR.transform.parent = virtualSurfaceAnchor.transform;
            }

            if (GameObject.Find("FarSurface") != null)
            {
                virtualSurface.transform.position = centerEye.transform.position + centerEye.transform.forward * farSurfaceDis;

                virtualSurfaceFOR.transform.parent = null;
                virtualSurfaceAnchor.transform.parent = virtualSurfaceFOR.transform;

                virtualSurfaceFOR.transform.position = centerEye.transform.position + centerEye.transform.forward * nearSurfaceDis * (5f / 10f) - virtualSurface.transform.up * eyeElbowDis;
                virtualSurfaceFOR.transform.rotation = virtualSurface.transform.rotation;
                virtualSurfaceFOR.transform.Rotate(45, 0, 0, Space.Self);

                virtualSurfaceAnchor.transform.parent = null;
                virtualSurfaceFOR.transform.parent = virtualSurfaceAnchor.transform;
            }
        }

        initSurface = false;

        initVirtualSurfaceFOR = true;
    }

    private void RenameHand(Transform parent, string newNamePrefix)
    {
        parent.gameObject.name = newNamePrefix + "_" + parent.gameObject.name;

        // Rename children recursively
        for (int i = 0; i < parent.childCount; i++)
        {
            RenameHand(parent.GetChild(i), newNamePrefix);
        }
    }

    private void InitHandedness()
    {
        // Setting Interaction Handedness
        if (dominantHand == Handedness.Right)
        {
            interactionHandedness = Handedness.Right;
            surfaceHandedness = Handedness.Left;
        }
        else
        {
            interactionHandedness = Handedness.Left;
            surfaceHandedness = Handedness.Right;
        }

        // Assigning Interaction GameObjects
        if (surfaceHandedness == Handedness.Left)
        {
            relocateInteractionHand = GameObject.Find("RelocateRightHand");
            relocateInteractionHand = GameObject.Find("RelocateRightHand");
            relocateInteractionHandMesh = GameObject.Find("Retarget_r_handMeshNode");
            relocateSurfaceHand = GameObject.Find("RelocateLeftHand");
            relocateSurfaceHandMesh = GameObject.Find("Retarget_l_handMeshNode");
            relocateSurfaceHandPalm = GameObject.Find("Retarget_l_palm_center_marker");

            surfaceHand = GameObject.Find("l_handMeshNode");
            surfaceHandPalm = GameObject.Find("l_palm_center_marker");
            interactionHand = GameObject.Find("r_handMeshNode");

            surfaceWrist = GameObject.Find("b_l_wrist");
            surfaceOffset = GameObject.Find("LeftHandSurfaceOffset");

            surfaceFingerTips[0] = GameObject.Find("l_thumb_finger_tip_marker");
            surfaceFingerTips[1] = GameObject.Find("l_index_finger_tip_marker");
            surfaceFingerTips[2] = GameObject.Find("l_middle_finger_tip_marker");
            surfaceFingerTips[3] = GameObject.Find("l_ring_finger_tip_marker");
            surfaceFingerTips[4] = GameObject.Find("l_pinky_finger_tip_marker");

            interactionFingerTips[0] = GameObject.Find("r_thumb_finger_tip_marker");
            interactionFingerTips[1] = GameObject.Find("r_index_finger_tip_marker");
            interactionFingerTips[2] = GameObject.Find("r_middle_finger_tip_marker");
            interactionFingerTips[3] = GameObject.Find("r_ring_finger_tip_marker");
            interactionFingerTips[4] = GameObject.Find("r_pinky_finger_tip_marker");

            interactionWrist = GameObject.Find("b_r_wrist");

            retargetInteractionFingerTips[0] = GameObject.Find("Retarget_r_thumb_finger_tip_marker");
            retargetInteractionFingerTips[1] = GameObject.Find("Retarget_r_index_finger_tip_marker");
            retargetInteractionFingerTips[2] = GameObject.Find("Retarget_r_middle_finger_tip_marker");
            retargetInteractionFingerTips[3] = GameObject.Find("Retarget_r_ring_finger_tip_marker");
            retargetInteractionFingerTips[4] = GameObject.Find("Retarget_r_pinky_finger_tip_marker");

            retargetSurfaceFingerTips[0] = GameObject.Find("Retarget_l_thumb_finger_tip_marker");
            retargetSurfaceFingerTips[1] = GameObject.Find("Retarget_l_index_finger_tip_marker");
            retargetSurfaceFingerTips[2] = GameObject.Find("Retarget_l_middle_finger_tip_marker");
            retargetSurfaceFingerTips[3] = GameObject.Find("Retarget_l_ring_finger_tip_marker");
            retargetSurfaceFingerTips[4] = GameObject.Find("Retarget_l_pinky_finger_tip_marker");

            zeroKnuckles[0] = GameObject.Find("b_l_thumb0");
            firstKnuckles[0] = GameObject.Find("b_l_thumb1");
            secondKnuckles[0] = GameObject.Find("b_l_thumb2");
            thirdKnuckles[0] = GameObject.Find("b_l_thumb3");
            forthKnuckles[0] = GameObject.Find("l_thumb_finger_tip_marker");

            firstKnuckles[1] = GameObject.Find("b_l_index1");
            secondKnuckles[1] = GameObject.Find("b_l_index2");
            thirdKnuckles[1] = GameObject.Find("b_l_index3");
            forthKnuckles[1] = GameObject.Find("l_index_finger_tip_marker");

            firstKnuckles[2] = GameObject.Find("b_l_middle1");
            secondKnuckles[2] = GameObject.Find("b_l_middle2");
            thirdKnuckles[2] = GameObject.Find("b_l_middle3");
            forthKnuckles[2] = GameObject.Find("l_middle_finger_tip_marker");

            firstKnuckles[3] = GameObject.Find("b_l_ring1");
            secondKnuckles[3] = GameObject.Find("b_l_ring2");
            thirdKnuckles[3] = GameObject.Find("b_l_ring3");
            forthKnuckles[3] = GameObject.Find("l_ring_finger_tip_marker");

            zeroKnuckles[4] = GameObject.Find("b_l_pinky0");
            firstKnuckles[4] = GameObject.Find("b_l_pinky1");
            secondKnuckles[4] = GameObject.Find("b_l_pinky2");
            thirdKnuckles[4] = GameObject.Find("b_l_pinky3");
            forthKnuckles[4] = GameObject.Find("l_pinky_finger_tip_marker");

            interactionHandZeroKnuckles[0] = GameObject.Find("b_r_thumb0");
            interactionHandFirstKnuckles[0] = GameObject.Find("b_r_thumb1");
            interactionHandSecondKnuckles[0] = GameObject.Find("b_r_thumb2");
            interactionHandThirdKnuckles[0] = GameObject.Find("b_r_thumb3");
            interactionHandForthKnuckles[0] = GameObject.Find("r_thumb_finger_tip_marker");

            interactionHandFirstKnuckles[1] = GameObject.Find("b_r_index1");
            interactionHandSecondKnuckles[1] = GameObject.Find("b_r_index2");
            interactionHandThirdKnuckles[1] = GameObject.Find("b_r_index3");
            interactionHandForthKnuckles[1] = GameObject.Find("r_index_finger_tip_marker");

            interactionHandFirstKnuckles[2] = GameObject.Find("b_r_middle1");
            interactionHandSecondKnuckles[2] = GameObject.Find("b_r_middle2");
            interactionHandThirdKnuckles[2] = GameObject.Find("b_r_middle3");
            interactionHandForthKnuckles[2] = GameObject.Find("r_middle_finger_tip_marker");

            interactionHandFirstKnuckles[3] = GameObject.Find("b_r_ring1");
            interactionHandSecondKnuckles[3] = GameObject.Find("b_r_ring2");
            interactionHandThirdKnuckles[3] = GameObject.Find("b_r_ring3");
            interactionHandForthKnuckles[3] = GameObject.Find("r_ring_finger_tip_marker");

            interactionHandZeroKnuckles[4] = GameObject.Find("b_r_pinky0");
            interactionHandFirstKnuckles[4] = GameObject.Find("b_r_pinky1");
            interactionHandSecondKnuckles[4] = GameObject.Find("b_r_pinky2");
            interactionHandThirdKnuckles[4] = GameObject.Find("b_r_pinky3");
            interactionHandForthKnuckles[4] = GameObject.Find("r_pinky_finger_tip_marker");

            prevInteractionIndexFingerPos = interactionFingerTips[1].transform.position;
        }
        else
        {
            relocateInteractionHand = GameObject.Find("RelocateLeftHand");
            relocateInteractionHandMesh = GameObject.Find("Retarget_l_handMeshNode");
            relocateSurfaceHand = GameObject.Find("RelocateRightHand");
            relocateSurfaceHandMesh = GameObject.Find("Retarget_r_handMeshNode");
            relocateSurfaceHandPalm = GameObject.Find("Retarget_r_palm_center_marker");

            surfaceHand = GameObject.Find("r_handMeshNode");
            surfaceHandPalm = GameObject.Find("r_palm_center_marker");
            interactionHand = GameObject.Find("l_handMeshNode");

            surfaceWrist = GameObject.Find("b_r_wrist");
            //surfaceOffset = GameObject.Find("RightHandSurfaceOffset");

            surfaceFingerTips[0] = GameObject.Find("r_thumb_finger_tip_marker");
            surfaceFingerTips[1] = GameObject.Find("r_index_finger_tip_marker");
            surfaceFingerTips[2] = GameObject.Find("r_middle_finger_tip_marker");
            surfaceFingerTips[3] = GameObject.Find("r_ring_finger_tip_marker");
            surfaceFingerTips[4] = GameObject.Find("r_pinky_finger_tip_marker");

            interactionFingerTips[0] = GameObject.Find("l_thumb_finger_tip_marker");
            interactionFingerTips[1] = GameObject.Find("l_index_finger_tip_marker");
            interactionFingerTips[2] = GameObject.Find("l_middle_finger_tip_marker");
            interactionFingerTips[3] = GameObject.Find("l_ring_finger_tip_marker");
            interactionFingerTips[4] = GameObject.Find("l_pinky_finger_tip_marker");

            retargetInteractionFingerTips[0] = GameObject.Find("Retarget_l_thumb_finger_tip_marker");
            retargetInteractionFingerTips[1] = GameObject.Find("Retarget_l_index_finger_tip_marker");
            retargetInteractionFingerTips[2] = GameObject.Find("Retarget_l_middle_finger_tip_marker");
            retargetInteractionFingerTips[3] = GameObject.Find("Retarget_l_ring_finger_tip_marker");
            retargetInteractionFingerTips[4] = GameObject.Find("Retarget_l_pinky_finger_tip_marker");

            retargetSurfaceFingerTips[0] = GameObject.Find("Retarget_r_thumb_finger_tip_marker");
            retargetSurfaceFingerTips[1] = GameObject.Find("Retarget_r_index_finger_tip_marker");
            retargetSurfaceFingerTips[2] = GameObject.Find("Retarget_r_middle_finger_tip_marker");
            retargetSurfaceFingerTips[3] = GameObject.Find("Retarget_r_ring_finger_tip_marker");
            retargetSurfaceFingerTips[4] = GameObject.Find("Retarget_r_pinky_finger_tip_marker");

            zeroKnuckles[0] = GameObject.Find("b_r_thumb0");
            firstKnuckles[0] = GameObject.Find("b_r_thumb1");
            secondKnuckles[0] = GameObject.Find("b_r_thumb2");
            thirdKnuckles[0] = GameObject.Find("b_r_thumb3");
            forthKnuckles[0] = GameObject.Find("r_thumb_finger_tip_marker");

            firstKnuckles[1] = GameObject.Find("b_r_index1");
            secondKnuckles[1] = GameObject.Find("b_r_index2");
            thirdKnuckles[1] = GameObject.Find("b_r_index3");
            forthKnuckles[1] = GameObject.Find("r_index_finger_tip_marker");

            firstKnuckles[2] = GameObject.Find("b_r_middle1");
            secondKnuckles[2] = GameObject.Find("b_r_middle2");
            thirdKnuckles[2] = GameObject.Find("b_r_middle3");
            forthKnuckles[2] = GameObject.Find("r_middle_finger_tip_marker");

            firstKnuckles[3] = GameObject.Find("b_r_ring1");
            secondKnuckles[3] = GameObject.Find("b_r_ring2");
            thirdKnuckles[3] = GameObject.Find("b_r_ring3");
            forthKnuckles[3] = GameObject.Find("r_ring_finger_tip_marker");

            zeroKnuckles[4] = GameObject.Find("b_r_pinky0");
            firstKnuckles[4] = GameObject.Find("b_r_pinky1");
            secondKnuckles[4] = GameObject.Find("b_r_pinky2");
            thirdKnuckles[4] = GameObject.Find("b_r_pinky3");
            forthKnuckles[4] = GameObject.Find("r_pinky_finger_tip_marker");

            interactionHandZeroKnuckles[0] = GameObject.Find("b_l_thumb0");
            interactionHandFirstKnuckles[0] = GameObject.Find("b_l_thumb1");
            interactionHandSecondKnuckles[0] = GameObject.Find("b_l_thumb2");
            interactionHandThirdKnuckles[0] = GameObject.Find("b_l_thumb3");
            interactionHandForthKnuckles[0] = GameObject.Find("l_thumb_finger_tip_marker");

            interactionHandFirstKnuckles[1] = GameObject.Find("b_l_index1");
            interactionHandSecondKnuckles[1] = GameObject.Find("b_l_index2");
            interactionHandThirdKnuckles[1] = GameObject.Find("b_l_index3");
            interactionHandForthKnuckles[1] = GameObject.Find("l_index_finger_tip_marker");

            interactionHandFirstKnuckles[2] = GameObject.Find("b_l_middle1");
            interactionHandSecondKnuckles[2] = GameObject.Find("b_l_middle2");
            interactionHandThirdKnuckles[2] = GameObject.Find("b_l_middle3");
            interactionHandForthKnuckles[2] = GameObject.Find("l_middle_finger_tip_marker");

            interactionHandFirstKnuckles[3] = GameObject.Find("b_l_ring1");
            interactionHandSecondKnuckles[3] = GameObject.Find("b_l_ring2");
            interactionHandThirdKnuckles[3] = GameObject.Find("b_l_ring3");
            interactionHandForthKnuckles[3] = GameObject.Find("l_ring_finger_tip_marker");

            interactionHandZeroKnuckles[4] = GameObject.Find("b_l_pinky0");
            interactionHandFirstKnuckles[4] = GameObject.Find("b_l_pinky1");
            interactionHandSecondKnuckles[4] = GameObject.Find("b_l_pinky2");
            interactionHandThirdKnuckles[4] = GameObject.Find("b_l_pinky3");
            interactionHandForthKnuckles[4] = GameObject.Find("l_pinky_finger_tip_marker");

            prevInteractionIndexFingerPos = interactionFingerTips[1].transform.position;
        }
    }

    private void InitSurface()
    {
        centerEye = GameObject.Find("CenterEyeAnchor");

        //hand surface
        surface = GameObject.Find("Surface");
        //for hand retargeting
        surfaceReference = GameObject.Find("SurfaceReference");
        //compensate the difference between the hand surface and the virtual window, i.e., rotation
        surfaceMatchVirtualWindow = GameObject.Find("SurfaceMatchVirtualWindow");
        //object on hand surface - refer to the virtual object on virtual surface
        handSurfaceVirtualObject = GameObject.Find("HandSurfaceVirtualObject");

        surfaceHandReference = GameObject.Find("SurfaceHandReference");

        virtualSurface = GameObject.Find("VirtualSurface");
        activeVirtualSurface = virtualSurface;
        //virtual object on virtual surface
        virtualObject = GameObject.Find("VirtualObject");
        virtualObjectReference = GameObject.Find("VirtualObjectReference");
        //visualization for oversnap - a frame around the virtual surface
        virtualSurfaceMask = GameObject.Find("VirtualSurfaceMask");
        //for changing orientation when oversnap
        virtualSurfaceReference = GameObject.Find("VirtualSurfaceReference");
        virtualSurfaceAnchor = GameObject.Find("VirtualSurfaceAnchor");

        
















        //snap window on virtual surface
        virtualWindow = GameObject.Find("VirtualWindow");
        virtualWindowReference = GameObject.Find("VirtualWindowReference");
        //visualization for snap - a frame around the virtual window
        virtualWindowFrame = GameObject.Find("VirtualWindowFrame");
        virtualSurfaceFOR = GameObject.Find("VirtualSurfaceFOR");

        //same as interaction hand, used for hand retargeting 
        interactionHandRelativeToHandSurface = GameObject.Find("InteractionHandRelativeToHandSurface");
        pinchArrowRelativeToHandSurface = GameObject.Find("PinchArrowRelativeToHandSurface");

        // Initialising Surface Corner GameObjects
        surfaceBottomLeft = new GameObject("SurfaceBottomLeft");
        surfaceBottomRight = new GameObject("SurfaceBottomRight");
        surfaceTopLeft = new GameObject("SurfaceTopLeft");
        surfaceTopRight = new GameObject("SurfaceTopRight");

        // Tethering Surface Corners to Surface
        surfaceBottomLeft.transform.parent = surface.transform;
        surfaceBottomRight.transform.parent = surface.transform;
        surfaceTopLeft.transform.parent = surface.transform;
        surfaceTopRight.transform.parent = surface.transform;

        surfaceBottomLeft.transform.position = new Vector3(surface.transform.position.x - 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z - 0.5f * surface.transform.localScale.z);
        surfaceBottomRight.transform.position = new Vector3(surface.transform.position.x + 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z - 0.5f * surface.transform.localScale.z);
        surfaceTopLeft.transform.position = new Vector3(surface.transform.position.x - 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z + 0.5f * surface.transform.localScale.z);
        surfaceTopRight.transform.position = new Vector3(surface.transform.position.x + 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z + 0.5f * surface.transform.localScale.z);

        // Initialising Virtual Window Corner GameObjects
        virtualWindowBottomLeft = new GameObject("VirtualWindowBottomLeft");
        virtualWindowBottomRight = new GameObject("VirtualWindowBottomRight");
        virtualWindowTopLeft = new GameObject("VirtualWindowTopLeft");
        virtualWindowTopRight = new GameObject("VirtualWindowTopRight");

        // Tethering Virtual Window Corners to Surface
        virtualWindowBottomLeft.transform.parent = virtualWindow.transform;
        virtualWindowBottomRight.transform.parent = virtualWindow.transform;
        virtualWindowTopLeft.transform.parent = virtualWindow.transform;
        virtualWindowTopRight.transform.parent = virtualWindow.transform;

        virtualWindowBottomLeft.transform.position = new Vector3(virtualWindow.transform.position.x - 0.5f * virtualWindow.transform.lossyScale.x, virtualWindow.transform.position.y - 0.5f * virtualWindow.transform.lossyScale.y, virtualWindow.transform.position.z);
        virtualWindowBottomRight.transform.position = new Vector3(virtualWindow.transform.position.x + 0.5f * virtualWindow.transform.lossyScale.x, virtualWindow.transform.position.y - 0.5f * virtualWindow.transform.lossyScale.y, virtualWindow.transform.position.z);
        virtualWindowTopLeft.transform.position = new Vector3(virtualWindow.transform.position.x - 0.5f * virtualWindow.transform.lossyScale.x, virtualWindow.transform.position.y + 0.5f * virtualWindow.transform.lossyScale.y, virtualWindow.transform.position.z);
        virtualWindowTopRight.transform.position = new Vector3(virtualWindow.transform.position.x + 0.5f * virtualWindow.transform.lossyScale.x, virtualWindow.transform.position.y + 0.5f * virtualWindow.transform.lossyScale.y, virtualWindow.transform.position.z);

        // Initialising Webview Corner GameObjects
        webviewBottomLeft = new GameObject("WebviewBottomLeft");
        webviewBottomRight = new GameObject("WebviewBottomRight");
        webviewTopLeft = new GameObject("WebviewTopLeft");
        webviewTopRight = new GameObject("WebviewTopRight");
    }

    private void UpdateSurface()
    {
        if (surface != null && surfaceOffset != null)
        {
            // Tethering Surface to handSurface
            surface.transform.position = surfaceOffset.transform.position;
            surface.transform.rotation = surfaceOffset.transform.rotation;

            // Updating Surface Axes Variables
            surfaceXAxis = surfaceBottomRight.transform.position - surfaceBottomLeft.transform.position;
            surfaceYAxis = surfaceTopLeft.transform.position - surfaceBottomLeft.transform.position;
            surfaceNormal = Vector3.Cross(surfaceYAxis, surfaceXAxis).normalized;

            UpdateVirtualWindow();
        }
    }

    private void UpdateVirtualWindow()
    {
        virtualWindowXAxis = virtualWindowBottomRight.transform.position - virtualWindowBottomLeft.transform.position;
        virtualWindowYAxis = virtualWindowTopLeft.transform.position - virtualWindowBottomLeft.transform.position;
        virtualWindowNormal = Vector3.Cross(virtualWindowYAxis, virtualWindowXAxis).normalized;
    }
    private void UpdateWebview()
    {
        // Tethering Webview Corners to Surface
        if (activeWebview != null && activeWebview != LastActiveWebview)
        {
            webviewBottomLeft.transform.parent = activeWebview.transform;
            webviewBottomRight.transform.parent = activeWebview.transform;
            webviewTopLeft.transform.parent = activeWebview.transform;
            webviewTopRight.transform.parent = activeWebview.transform;

            /*webviewBottomLeft.transform.position = new Vector3(activeWebview.transform.position.x - 0.5f * activeWebview.transform.localScale.x, activeWebview.transform.position.y - 0.5f * activeWebview.transform.localScale.y, activeWebview.transform.position.z);
            webviewBottomRight.transform.position = new Vector3(activeWebview.transform.position.x + 0.5f * activeWebview.transform.localScale.x, activeWebview.transform.position.y - 0.5f * activeWebview.transform.localScale.y, activeWebview.transform.position.z);
            webviewTopLeft.transform.position = new Vector3(activeWebview.transform.position.x - 0.5f * activeWebview.transform.localScale.x, activeWebview.transform.position.y + 0.5f * activeWebview.transform.localScale.y, activeWebview.transform.position.z);
            webviewTopRight.transform.position = new Vector3(activeWebview.transform.position.x + 0.5f * activeWebview.transform.localScale.x, activeWebview.transform.position.y + 0.5f * activeWebview.transform.localScale.y, activeWebview.transform.position.z);*/

            webviewBottomLeft.transform.rotation = activeWebview.transform.rotation;
            webviewBottomRight.transform.rotation = activeWebview.transform.rotation;
            webviewTopLeft.transform.rotation = activeWebview.transform.rotation;
            webviewTopRight.transform.rotation = activeWebview.transform.rotation;

            webviewBottomLeft.transform.localScale = new Vector3(1, 1, 1);
            webviewBottomRight.transform.localScale = new Vector3(1, 1, 1);
            webviewTopLeft.transform.localScale = new Vector3(1, 1, 1);
            webviewTopRight.transform.localScale = new Vector3(1, 1, 1);

            webviewBottomLeft.transform.localPosition = new Vector3(-0.5f, -0.5f, 0);
            webviewBottomRight.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
            webviewTopLeft.transform.localPosition = new Vector3(-0.5f, 0.5f, 0);
            webviewTopRight.transform.localPosition = new Vector3(0.5f, 0.5f, 0);

            /*webviewBottomLeft.transform.localPosition = new Vector3(-0.5f * activeWebview.transform.localScale.x, -0.5f * activeWebview.transform.localScale.y, 0);
            webviewBottomRight.transform.localPosition = new Vector3(0.5f * activeWebview.transform.localScale.x, -0.5f * activeWebview.transform.localScale.y, 0);
            webviewTopLeft.transform.localPosition = new Vector3(-0.5f * activeWebview.transform.localScale.x, 0.5f * activeWebview.transform.localScale.y, 0);
            webviewTopRight.transform.localPosition = new Vector3(0.5f * activeWebview.transform.localScale.x, 0.5f * activeWebview.transform.localScale.y, 0);*/

            LastActiveWebview = activeWebview;
        }

        if (activeWebview != null)
        {
            webviewXAxis = webviewBottomRight.transform.position - webviewBottomLeft.transform.position;
            webviewYAxis = webviewTopLeft.transform.position - webviewBottomLeft.transform.position;
            webviewNormal = Vector3.Cross(webviewYAxis, webviewXAxis).normalized;
        }
    }

    private void UpdateSurfaceInteractions()
    {
        UpdateSurfaceCursors();
        UpdateSurfaceHandCursors();
        UpdateVirtualWindowCursors();
        UpdateVirtuslSurfaceFORCursors();

        //if (activeWebview != null)
        //UpdateWebviewCursors();

        if (activeVirtualSurface != null)
            UpdateWebviewCursors();

        //UpdateWebviewCursorByPinchRaycast();
        //UpdateWebviewCursorByNonDomHandPinchRaycast();

        UpdateInterFingerTipDistances();
        UpdateFingerTipToSurfaceDistances();
        UpdateRetargetFingerTipToVirtualSurfaceDistances();

        

        UpdateInteractionFingerTipToVirtualSurfaceFORDistances();
        //UpdateInteractionFingerTipToVirtualSurfaceFORSpeed();

        if (interactionMethod == InteractionMethod.HandPseudoDirectPoke_V2 || interactionMethod == InteractionMethod.CustomHandPointer ||
            interactionMethod == InteractionMethod.HandMove || interactionMethod == InteractionMethod.EyeMoveHand || 
            interactionMethod == InteractionMethod.EyeHandMove || interactionMethod == InteractionMethod.EyeHandJump || interactionMethod == InteractionMethod.GazePinch)
        {
            //on the interaction pad/frame of reference surface
            UpdateInteractionMovingSpeed();
            CheckInteractionFingerTouchIntention();
        }
        else if(interactionMethod == InteractionMethod.HandPoke_V2)
        {
            UpdateInteractionMovingSpeedToVirtualSurface();
            CheckInteractionFingerTouchIntentionOnVirtualSurface();
        }

        //prevInteractionIndexFingerToSurfaceSpeed = interactionIndexFingerToSurfaceSpeed;
    }

    private void UpdateFingerSpeed()
    {
        // Record the current position and time
        Vector3 currentFingerPosition = GetInteractionFingerTipPosition(1);
        float currentTime = Time.time;

        // Add the current position and time to the history queues
        fingerPositionHistory.Enqueue(currentFingerPosition);
        timeHistory.Enqueue(currentTime);

        // Remove the oldest position and time if the window size is exceeded
        while (fingerPositionHistory.Count > speedFrameWindow)
        {
            fingerPositionHistory.Dequeue();
            timeHistory.Dequeue();
        }

        // Calculate the total distance within the frame window
        Vector3 initialPosition = fingerPositionHistory.Peek();
        float initialTime = timeHistory.Peek();
        float totalDistance = 0f;

        foreach (Vector3 position in fingerPositionHistory)
        {
            totalDistance += Vector3.Distance(initialPosition, position);
            initialPosition = position;
        }

        // Calculate the time elapsed within the frame window
        float totalTime = currentTime - initialTime;

        // Calculate average speed within the frame window
        currentFingerSpeed = totalDistance / totalTime;
    }

    private void UpdateGazeCusor()
    {
        // Calculate midpoint between left and right eye positions
        gazeOrigin = (leftEyeGaze.transform.position + rightEyeGaze.transform.position) / 2;
        // Calculate average gaze direction
        gazeDirection = (leftEyeGaze.transform.forward + rightEyeGaze.transform.forward).normalized;

        smoothGazeOrigin = new Vector3(10000, 10000, 10000);

        if (gazeFilterOn)
        {
            gazePositionFilters[0].UpdateParams(filterFrequency, gazeFilterMinCutoff, gazeFilterBeta, filterDcutoff);
            smoothGazeOrigin = gazePositionFilters[0].Filter(gazeOrigin);
        }
        else
        {
            smoothGazeOrigin = gazeOrigin;
        }

        if (Physics.Raycast(gazeOrigin, gazeDirection, out RaycastHit hit, 100f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (gazeFilterOn)
            {
                gazeCursorPositionFilters[0].UpdateParams(filterFrequency, gazeFilterMinCutoff, gazeFilterBeta, filterDcutoff);
                smoothGazeCursor = gazeCursorPositionFilters[0].Filter(hit.point);
            }
            else
            {
                smoothGazeCursor = hit.point;
            }
        }
        else
        {
            smoothGazeCursor = new Vector3(10000, 10000, 10000);
        }
    }

    private void UpdateGazeSpeed()
    {
        // Record the current position and time
        Vector3 currentGazePosition = smoothGazeCursor;
        float currentGazeTime = Time.time;

        // Add the current position and time to the history queues
        gazePositionHistory.Enqueue(currentGazePosition);
        gazeTimeHistory.Enqueue(currentGazeTime);

        // Remove the oldest position and time if the window size is exceeded
        while (gazePositionHistory.Count > gazeSpeedFrameWindow)
        {
            gazePositionHistory.Dequeue();
            gazeTimeHistory.Dequeue();
        }

        // Calculate the total distance within the frame window
        Vector3 initialPosition = gazePositionHistory.Peek();
        float initialTime = gazeTimeHistory.Peek();
        float totalDistance = 0f;

        foreach (Vector3 position in gazePositionHistory)
        {
            totalDistance += Vector3.Distance(initialPosition, position);
            initialPosition = position;
        }

        // Calculate the time elapsed within the frame window
        float totalTime = currentGazeTime - initialTime;

        // Calculate average speed within the frame window
        currentGazeSpeed = totalDistance / totalTime;
    }

    private void UpdateGazeMoveDistance()
    {
        // Record the current position and time
        Vector3 currentGazePosition = smoothGazeCursor;

        // Add the current position and time to the history queues
        gazeMovePositionHistory.Enqueue(currentGazePosition);

        // Remove the oldest position and time if the window size is exceeded
        while (gazeMovePositionHistory.Count > gazeMoveDisFrameWindow)
        {
            gazeMovePositionHistory.Dequeue();
        }

        // Calculate the total distance within the frame window
        Vector3 initialPosition = gazeMovePositionHistory.Peek();
        float totalDistance = 0f;

        totalDistance = Vector3.Distance(initialPosition, currentGazePosition);

        /*foreach (Vector3 position in gazeMovePositionHistory)
        {
            totalDistance += Vector3.Distance(initialPosition, position);
            initialPosition = position;
        }*/

        // Calculate total dis within the frame window
        currentGazeTotalMoveDis = totalDistance;
    }

    private void UpdateSurfaceCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            Vector3 fingerTipPosition = Vector3.zero;

            if (interactionFingerTips[i] != null)
            {
                if (filterOn)
                {
                    positionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                    fingerTipPosition = positionFilters[i].Filter(interactionFingerTips[i].transform.position);
                }
                else
                {
                    fingerTipPosition = interactionFingerTips[i].transform.position;
                }
            }

            if (Physics.Raycast(fingerTipPosition, -1.0f * surfaceNormal, out hit, 1.0f, LayerMask.GetMask("SurfaceLayer")))
            {
                if (hit.collider.gameObject == surface)
                {
                    fingerPenetrateSurface[i] = false;
                    surfaceCursorStates[i] = true;
                    surfaceCursorPositions[i] = hit.point;
                    surfaceCursorRelativePositions[i] = ComputeSurfaceCursorRelativePosition(i);
                }
            }
            else if (Physics.Raycast(fingerTipPosition, 1.0f * surfaceNormal, out hit, 1.0f, LayerMask.GetMask("SurfaceLayer")))
            {
                if (hit.collider.gameObject == surface)
                {
                    fingerPenetrateSurface[i] = true;
                    surfaceCursorStates[i] = true;
                    surfaceCursorPositions[i] = hit.point;
                    surfaceCursorRelativePositions[i] = ComputeSurfaceCursorRelativePosition(i);
                }
            }
            else
            {
                fingerPenetrateSurface[i] = false;
                surfaceCursorStates[i] = false;
            }
        }
    }

    private void UpdateVirtuslSurfaceFORCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            Vector3 fingerTipPosition = Vector3.zero;

            Vector3 virtualSurfaceFORNormal = virtualSurfaceFOR.transform.forward;

            if (interactionFingerTips[i] != null)
            {
                if (filterOn)
                {
                    positionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                    fingerTipPosition = positionFilters[i].Filter(interactionFingerTips[i].transform.position);
                }
                else
                {
                    fingerTipPosition = interactionFingerTips[i].transform.position;
                }
            }

            if (Physics.Raycast(fingerTipPosition, virtualSurfaceFORNormal, out hit, 1.0f, LayerMask.GetMask("VirtualSurfaceFORLayer")))
            {
                if (hit.collider.gameObject == virtualSurfaceFOR)
                {
                    fingerPenetrateVirtualSurfaceFOR[i] = false;
                    virtualSurfaceFORCursorStates[i] = true;
                    virtualSurfaceFORCursorPositions[i] = hit.point;
                }
            }
            else if (Physics.Raycast(fingerTipPosition, -virtualSurfaceFORNormal, out hit, 1.0f, LayerMask.GetMask("VirtualSurfaceFORLayer")))
            {
                if (hit.collider.gameObject == virtualSurfaceFOR)
                {
                    fingerPenetrateVirtualSurfaceFOR[i] = true;
                    virtualSurfaceFORCursorStates[i] = true;
                    virtualSurfaceFORCursorPositions[i] = hit.point;
                }
            }
            else
            {
                fingerPenetrateVirtualSurfaceFOR[i] = false;
                virtualSurfaceFORCursorStates[i] = false;
            }
        }
    }

    private void UpdateVirtualWindowCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            Vector3 fingerTipPosition = Vector3.zero;

            if (retargetInteractionFingerTips[i] != null)
            {
                if (filterOn)
                {
                    retargetPositionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                    fingerTipPosition = retargetPositionFilters[i].Filter(retargetInteractionFingerTips[i].transform.position);
                }
                else
                {
                    fingerTipPosition = retargetInteractionFingerTips[i].transform.position;
                }
            }

            if (Physics.Raycast(fingerTipPosition, -1.0f * virtualWindowNormal, out hit, 1.0f, LayerMask.GetMask("VirtualWindowLayer")))
            {
                if (hit.collider.gameObject == virtualWindow)
                {
                    virtualWindowCursorStates[i] = true;
                    virtualWindowCursorPositions[i] = hit.point;
                    virtualWindowCursorRelativePositions[i] = ComputeVirtualWindowCursorRelativePosition(i);
                    //webviewCursorRelativePositions[i] = ComputeWebviewCursorRelativePosition(i);
                }
            }
            else if (Physics.Raycast(fingerTipPosition, virtualWindowNormal, out hit, 1.0f, LayerMask.GetMask("VirtualWindowLayer")))
            {
                if (hit.collider.gameObject == virtualWindow)
                {
                    virtualWindowCursorStates[i] = true;
                    virtualWindowCursorPositions[i] = hit.point;
                    virtualWindowCursorRelativePositions[i] = ComputeVirtualWindowCursorRelativePosition(i);
                    //webviewCursorRelativePositions[i] = ComputeWebviewCursorRelativePosition(i);
                }
            }
            else
            {
                virtualWindowCursorStates[i] = false;
            }
        }
    }

    private void UpdateWebviewCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            Vector3 fingerTipPosition = Vector3.zero;

            if (interactionMethod == InteractionMethod.HandPoke || interactionMethod == InteractionMethod.HandPoke_V2)
            {
                if (interactionFingerTips[i] != null)
                {
                    if (filterOn)
                    {
                        positionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                        fingerTipPosition = positionFilters[i].Filter(interactionFingerTips[i].transform.position);
                    }
                    else
                    {
                        fingerTipPosition = interactionFingerTips[i].transform.position;
                    }
                }
            }
            else
            {
                if (retargetInteractionFingerTips[i] != null)
                {
                    if (filterOn)
                    {
                        retargetPositionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                        fingerTipPosition = retargetPositionFilters[i].Filter(retargetInteractionFingerTips[i].transform.position);
                    }
                    else
                    {
                        fingerTipPosition = retargetInteractionFingerTips[i].transform.position;
                    }
                }
            }

            if (Physics.Raycast(fingerTipPosition, -1.0f * activeVirtualSurface.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
            {
                if (hit.collider.gameObject == activeVirtualSurface)
                {
                    retargetFingerPenetrateSurface[i] = true;
                    webviewCursorStates[i] = true;
                    webviewCursorPositions[i] = hit.point;
                    webviewCursorRelativePositions[i] = ComputeWebviewCursorRelativePosition(i);
                }
            }
            else if (Physics.Raycast(fingerTipPosition, activeVirtualSurface.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
            {
                if (hit.collider.gameObject == activeVirtualSurface)
                {
                    retargetFingerPenetrateSurface[i] = false;
                    webviewCursorStates[i] = true;
                    webviewCursorPositions[i] = hit.point;
                    webviewCursorRelativePositions[i] = ComputeWebviewCursorRelativePosition(i);
                }
            }
            else
            {
                retargetFingerPenetrateSurface[i] = false;
                webviewCursorStates[i] = false;
            }
        }
    }

    private void UpdateWebviewCursorByPinchRaycast()
    {
        RaycastHit hit;

        Vector3 pinchRayDirection = interactionFingerTips[1].transform.position - ((interactionHandFirstKnuckles[0].transform.position + interactionHandFirstKnuckles[1].transform.position) / 2);
        //Vector3 pinchRayDirection = interactionFingerTips[1].transform.position - centerEye.transform.position;

        if (Physics.Raycast(interactionFingerTips[1].transform.position, -1.0f * pinchRayDirection, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                pinchWebviewCursorState = true;
                pinchWebviewCursorPosition = hit.point;
                pinchWebviewCursorRelativePosition = ComputeWebviewCursorRelativePositionByPinchRaycasting();
            }
        }
        else if (Physics.Raycast(interactionFingerTips[1].transform.position, pinchRayDirection, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                pinchWebviewCursorState = true;
                pinchWebviewCursorPosition = hit.point;
                pinchWebviewCursorRelativePosition = ComputeWebviewCursorRelativePositionByPinchRaycasting();
            }
        }
        else
        {
            pinchWebviewCursorState = false;
        }
    }

    private void UpdateWebviewCursorByNonDomHandPinchRaycast()
    {
        RaycastHit hit;

        Vector3 pinchRayDirection = surfaceFingerTips[1].transform.position - ((firstKnuckles[0].transform.position + firstKnuckles[1].transform.position) / 2);

        if (Physics.Raycast(surfaceFingerTips[1].transform.position, -1.0f * pinchRayDirection, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                nonDomHandPinchWebviewCursorState = true;
                nonDomHandPinchWebviewCursorPosition = hit.point;
                nonDomHandPinchWebviewCursorRelativePosition = ComputeWebviewCursorRelativePositionByNonDomHandPinchRaycasting();
            }
        }
        else if (Physics.Raycast(surfaceFingerTips[1].transform.position, pinchRayDirection, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                nonDomHandPinchWebviewCursorState = true;
                nonDomHandPinchWebviewCursorPosition = hit.point;
                nonDomHandPinchWebviewCursorRelativePosition = ComputeWebviewCursorRelativePositionByNonDomHandPinchRaycasting();
            }
        }
        else
        {
            nonDomHandPinchWebviewCursorState = false;
        }
    }

    private void UpdateSurfaceHandCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;

            Vector3 fingerTipPosition = Vector3.zero;

            if (interactionFingerTips[i] != null)
            {
                if (filterOn)
                {
                    positionFilters[i].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                    fingerTipPosition = positionFilters[i].Filter(interactionFingerTips[i].transform.position);
                }
                else
                {
                    fingerTipPosition = interactionFingerTips[i].transform.position;
                }
            }

            //previous direction: surfaceCursorPositions[i] - interactionFingerTips[i].transform.position

            if (surfaceCursorStates[i] && Physics.Raycast(fingerTipPosition, surfaceNormal, out hit, 1.0f, LayerMask.GetMask("SurfaceHandLayer")))
            {
                if (hit.collider.gameObject == surfaceHand)
                {
                    surfaceHandCursorStates[i] = true;
                    surfaceHandCursorPositions[i] = hit.point;
                }
            }
            else if (surfaceCursorStates[i] && Physics.Raycast(fingerTipPosition, -1.0f * surfaceNormal, out hit, 1.0f, LayerMask.GetMask("SurfaceHandLayer")))
            {
                if (hit.collider.gameObject == surfaceHand)
                {
                    surfaceHandCursorStates[i] = true;
                    surfaceHandCursorPositions[i] = hit.point;
                }
            }
            else
            {
                surfaceHandCursorStates[i] = false;
            }
        }
    }

    private void UpdateInterFingerTipDistances()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                interactionFingerTipDistances[i, j] = Vector3.Distance(GetInteractionFingerTipPosition(i), GetInteractionFingerTipPosition(j));
                surfaceFingerTipDistances[i, j] = Vector3.Distance(GetSurfaceFingerTipPosition(i), GetSurfaceFingerTipPosition(j));
            }
        }
    }

    private void UpdateFingerTipToSurfaceDistances()
    {
        for (int i = 0; i < 5; i++)
        {
            fingerTipToSurfaceDistances[i] = Vector3.Distance(GetInteractionFingerTipPosition(i), GetSurfaceCursorPosition(i));
            fingerTipToSurfaceHandDistances[i] = Vector3.Distance(GetInteractionFingerTipPosition(i), GetSurfaceHandCursorPosition(i));
        }
    }

    private void UpdateRetargetFingerTipToVirtualSurfaceDistances()
    {
        for (int i = 0; i < 5; i++)
        {
            //retargetFingerTipToVirtualSurfaceDistances[i] = Vector3.Distance(GetRetargetInteractionFingerTipPosition(i), GetWebviewCursorPosition(i));
            Vector3 direction = GetRetargetInteractionFingerTipPosition(i) - GetWebviewCursorPosition(i);
            float distance = direction.magnitude;

            Vector3 referenceDirection = -virtualSurface.transform.forward;
            retargetFingerTipToVirtualSurfaceDistances[i] = Vector3.Dot(direction.normalized, referenceDirection) * distance;
        }
    }

    private void UpdateInteractionFingerTipToVirtualSurfaceFORDistances()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 direction = GetInteractionFingerTipPosition(i) - GetVirtualSurfaceFORCursorPosition(i);
            //GameObject.Find("test").transform.position = GetInteractionFingerTipPosition(1);
            //GameObject.Find("test1").transform.position = GetVirtualSurfaceFORCursorPosition(1);
            float distance = direction.magnitude;

            Vector3 referenceDirection = -virtualSurfaceFOR.transform.forward;
            interactionFingerTipToVirtualSurfaceFORDistances[i] = Vector3.Dot(direction.normalized, referenceDirection) * distance;
        }
    }

    private void UpdateInteractionFingerTipToVirtualSurfaceFORSpeed()
    {
        Vector3 currentFingerPosition = GetInteractionFingerTipPosition(1);
        Vector3 velocity = (currentFingerPosition - prevInteractionIndexFingerPos) / Time.deltaTime;

        // Update the previous position
        prevInteractionIndexFingerPos = currentFingerPosition;

        Vector3 forwardDirection = virtualSurfaceFOR.transform.forward;

        // Calculate the speed in the direction of virtualSurfaceFOR.transform.forward
        float speedInForwardDirection = Vector3.Dot(velocity, forwardDirection);

        

        // Add the new speed to the buffer and to the sum
        interactionFingerSpeedBuffer.Enqueue(speedInForwardDirection);

        // Remove the oldest speed from the buffer if we've exceeded the frame window
        if (interactionFingerSpeedBuffer.Count > frameWindow)
        {
            interactionFingerSpeedBuffer.Dequeue();
        }

        //prevInteractionIndexFingerToSurfaceSpeed = interactionIndexFingerToSurfaceSpeed;
        // Calculate the average speed over the last 'frameWindow' frames
        interactionIndexFingerToSurfaceSpeed = interactionFingerSpeedBuffer.Average();

        averageInteractionFingerSpeedBuffer.Enqueue(interactionIndexFingerToSurfaceSpeed);

        if (averageInteractionFingerSpeedBuffer.Count > frameWindow)
        {
            averageInteractionFingerSpeedBuffer.Dequeue();
        }

        if (averageInteractionFingerSpeedBuffer.Count < prevSpeedFrameWindow)
        {
            prevInteractionIndexFingerToSurfaceSpeed = averageInteractionFingerSpeedBuffer.Peek();
        }
        else
        {
            prevInteractionIndexFingerToSurfaceSpeed = averageInteractionFingerSpeedBuffer.ToList()[averageInteractionFingerSpeedBuffer.Count - prevSpeedFrameWindow + 1];
        }

        // Calculate the speed in the direction of virtualSurfaceFOR.transform.right
        Vector3 Xdirection = virtualSurfaceFOR.transform.right;
        float speedInXDirection = Vector3.Dot(velocity, Xdirection);

        interactionFingerSpeedBufferX.Enqueue(speedInXDirection);

        if (interactionFingerSpeedBufferX.Count > frameWindow)
        {
            interactionFingerSpeedBufferX.Dequeue();
        }
        interactionIndexFingerToSurfaceSpeedX = interactionFingerSpeedBufferX.Average();


        // Calculate the speed in the direction of virtualSurfaceFOR.transform.up
        Vector3 Ydirection = virtualSurfaceFOR.transform.up;
        float speedInYDirection = Vector3.Dot(velocity, Ydirection);

        interactionFingerSpeedBufferY.Enqueue(speedInYDirection);

        if (interactionFingerSpeedBufferY.Count > frameWindow)
        {
            interactionFingerSpeedBufferY.Dequeue();
        }
        interactionIndexFingerToSurfaceSpeedY = interactionFingerSpeedBufferY.Average();

        //Calculate the alignment between the moving speed and the normal of interaction pad
        Vector3 velvec = new Vector3(interactionIndexFingerToSurfaceSpeedX, interactionIndexFingerToSurfaceSpeedY, interactionIndexFingerToSurfaceSpeed);
        Vector3 normal = new Vector3(0, 0, -1);
        alignNormal = Vector3.Dot(velvec, normal) * 1000;
    }

    private void UpdateInteractionMovingSpeed()
    {
        float x = GetWebviewCursorRelativeCoordinate(1).x;
        float y = GetWebviewCursorRelativeCoordinate(1).y;
        float z = GetInteractionFingertipToVirtualSurfaceFORDistance(1);

        Vector3 cursorPosition = new Vector3(x, y, z);
        webCursorBuffer.Enqueue(cursorPosition);
        interactionFingerBuffer.Enqueue(GetInteractionFingerTipPosition(1));

        if (webCursorBuffer.Count > webCursorWindow)
        {
            webCursorBuffer.Dequeue();
            interactionFingerBuffer.Dequeue();
        }

        float curMeanX = webCursorBuffer.Select(v => v.x).Average();
        float curMeanY = webCursorBuffer.Select(v => v.y).Average();
        float curMeanNormal = webCursorBuffer.Select(v => v.z).Average();

        if (movingX.Count > 1) {
            movingSpeedX.Enqueue(curMeanX - movingX.ToList()[movingX.Count - 1]);
            movingSpeedY.Enqueue(curMeanY - movingY.ToList()[movingX.Count - 1]);
            movingSpeedNormal.Enqueue(curMeanNormal - movingNormal.ToList()[movingX.Count - 1]);
        }

        movingX.Enqueue(curMeanX);
        movingY.Enqueue(curMeanY);
        movingNormal.Enqueue(curMeanNormal);

        float meanX = interactionFingerBuffer.Select(v => v.x).Average();
        float meanY = interactionFingerBuffer.Select(v => v.y).Average();
        float meanZ = interactionFingerBuffer.Select(v => v.z).Average();
        Vector3 avePoint = new Vector3(meanX, meanY, meanZ);
        aveInteractionFingerBuffer.Enqueue(avePoint);

        if (movingX.Count > webCursorWindow)
        {
            movingX.Dequeue();
            movingY.Dequeue();
            movingNormal.Dequeue();
            aveInteractionFingerBuffer.Dequeue();
        }

        if (movingSpeedX.Count > movingSpeedWindow)
        {
            movingSpeedX.Dequeue();
            movingSpeedY.Dequeue();
            movingSpeedNormal.Dequeue();
        }

        if (movingSpeedX.Count > 1)
        {
            interactionIndexFingerToSurfaceSpeedX = movingSpeedX.Average();
            interactionIndexFingerToSurfaceSpeedY = movingSpeedY.Average();
            interactionIndexFingerToSurfaceSpeed = movingSpeedNormal.Average();

            if (movingSpeedNormal.Count < prevSpeedFrameWindow)
            {
                prevInteractionIndexFingerToSurfaceSpeed = movingSpeedNormal.ToList()[movingSpeedNormal.Count - 1];
            }
            else
            {
                prevInteractionIndexFingerToSurfaceSpeed = movingSpeedNormal.ToList()[movingSpeedNormal.Count - prevSpeedFrameWindow];
            }

            

            //Calculate the alignment between the moving speed and the normal of interaction pad
            Vector3 velvec = new Vector3(interactionIndexFingerToSurfaceSpeedX, interactionIndexFingerToSurfaceSpeedY, interactionIndexFingerToSurfaceSpeed).normalized;
            Vector3 normal = new Vector3(0, 0, -1);
            alignNormal = Vector3.Dot(velvec, normal);

            
        }
    }

    private void UpdateInteractionMovingSpeedToVirtualSurface()
    {
        float x = GetWebviewCursorRelativeCoordinate(1).x;
        float y = GetWebviewCursorRelativeCoordinate(1).y;
        float z = GetRetargetFingerTipToVirtualSurfaceDistance(1);

        Vector3 cursorPosition = new Vector3(x, y, z);
        webCursorBuffer.Enqueue(cursorPosition);
        interactionFingerBuffer.Enqueue(GetInteractionFingerTipPosition(1));

        if (webCursorBuffer.Count > webCursorWindow)
        {
            webCursorBuffer.Dequeue();
            interactionFingerBuffer.Dequeue();
        }

        float curMeanX = webCursorBuffer.Select(v => v.x).Average();
        float curMeanY = webCursorBuffer.Select(v => v.y).Average();
        float curMeanNormal = webCursorBuffer.Select(v => v.z).Average();

        if (movingX.Count > 1)
        {
            movingSpeedX.Enqueue(curMeanX - movingX.ToList()[movingX.Count - 1]);
            movingSpeedY.Enqueue(curMeanY - movingY.ToList()[movingX.Count - 1]);
            movingSpeedNormal.Enqueue(curMeanNormal - movingNormal.ToList()[movingX.Count - 1]);
        }

        movingX.Enqueue(curMeanX);
        movingY.Enqueue(curMeanY);
        movingNormal.Enqueue(curMeanNormal);

        float meanX = interactionFingerBuffer.Select(v => v.x).Average();
        float meanY = interactionFingerBuffer.Select(v => v.y).Average();
        float meanZ = interactionFingerBuffer.Select(v => v.z).Average();
        Vector3 avePoint = new Vector3(meanX, meanY, meanZ);
        aveInteractionFingerBuffer.Enqueue(avePoint);

        if (movingX.Count > webCursorWindow)
        {
            movingX.Dequeue();
            movingY.Dequeue();
            movingNormal.Dequeue();
            aveInteractionFingerBuffer.Dequeue();
        }

        if (movingSpeedX.Count > movingSpeedWindow)
        {
            movingSpeedX.Dequeue();
            movingSpeedY.Dequeue();
            movingSpeedNormal.Dequeue();
        }

        if (movingSpeedX.Count > 1)
        {
            interactionIndexFingerToSurfaceSpeedX = movingSpeedX.Average();
            interactionIndexFingerToSurfaceSpeedY = movingSpeedY.Average();
            interactionIndexFingerToSurfaceSpeed = movingSpeedNormal.Average();

            if (movingSpeedNormal.Count < prevSpeedFrameWindow)
            {
                prevInteractionIndexFingerToSurfaceSpeed = movingSpeedNormal.ToList()[movingSpeedNormal.Count - 1];
            }
            else
            {
                prevInteractionIndexFingerToSurfaceSpeed = movingSpeedNormal.ToList()[movingSpeedNormal.Count - prevSpeedFrameWindow];
            }

            

            //Calculate the alignment between the moving speed and the normal of interaction pad
            Vector3 velvec = new Vector3(interactionIndexFingerToSurfaceSpeedX, interactionIndexFingerToSurfaceSpeedY, interactionIndexFingerToSurfaceSpeed).normalized;
            Vector3 normal = new Vector3(0, 0, -1);
            alignNormal = Vector3.Dot(velvec, normal);

            
        }
    }

    private float GetSmallestDistanceToSurface()
    {
        float min_dis = 10000;
        float[] movingNormalArray = movingNormal.ToArray();

        if (movingNormal.Count < 1)
            return min_dis;

        for (int i = 0; i < movingNormalArray.Length; i++)
        {
            if (movingNormalArray[i] < min_dis)
            {
                min_dis = movingNormalArray[i];
            }
        }
        return min_dis;
    }

    public Vector3 GetTouchPointfarthestToSurface()
    {
        Vector3 point = new Vector3(10000, 10000, 10000);
        if (movingNormal.Count < 1 || aveInteractionFingerBuffer.Count < 1)
            return point;

        // Convert the queues to arrays for easier index access
        float[] movingNormalArray = movingNormal.ToArray();
        Vector3[] aveInteractionFingerBufferArray = aveInteractionFingerBuffer.ToArray();

        // Find the smallest value in movingNormal and its index
        float min_dis = float.MaxValue;
        int min_index = -1;

        for (int i = 0; i < movingNormalArray.Length; i++)
        {
            if (movingNormalArray[i] < min_dis)
            {
                min_dis = movingNormalArray[i];
                min_index = i;
            }
        }

        // Get the corresponding Vector3 value from aveInteractionFingerBuffer at the same index
        point = aveInteractionFingerBufferArray[min_index];

        return point;
    }

    public Vector2 GetWebCursorClosestToSurface()
    {
        Vector2 cursor = new Vector2(10000, 10000);

        if (movingNormal.Count < 1)
            return cursor;

        // Convert the queues to arrays for easier index access
        float[] movingNormalArray = movingNormal.ToArray();
        float[] movingXArray = movingX.ToArray();
        float[] movingYArray = movingY.ToArray();

        // Find the smallest value in movingNormal and its index
        float min_value = float.MaxValue;
        int min_index = 10000;

        for (int i = 0; i < movingNormalArray.Length; i++)
        {
            if (Mathf.Abs(movingNormalArray[i]) < min_value)
            {
                min_value = Mathf.Abs(movingNormalArray[i]);
                min_index = i;
            }
        }

        cursor = new Vector2 (movingXArray[min_index], movingYArray[min_index]);

        return cursor;
    }

    public Vector2 GetWebCursorFarthestToSurface()
    {
        Vector2 cursor = new Vector2(10000, 10000);

        if (movingNormal.Count < 1)
            return cursor;

        // Convert the queues to arrays for easier index access
        float[] movingNormalArray = movingNormal.ToArray();
        float[] movingXArray = movingX.ToArray();
        float[] movingYArray = movingY.ToArray();

        // Find the smallest value in movingNormal and its index
        float min_value = float.MaxValue;
        int min_index = 10000;

        for (int i = 0; i < movingNormalArray.Length; i++)
        {
            if (movingNormalArray[i] < min_value)
            {
                min_value = movingNormalArray[i];
                min_index = i;
            }
        }

        cursor = new Vector2(movingXArray[min_index], movingYArray[min_index]);

        return cursor;
    }

    private void CheckInteractionFingerTouchIntention()
    {
        
        //check velocity direction to determine if the movement is more align with surface normal or horizontal.

        //check if the finger is moving along x,y axis
        



















        interactionFingerTouchIntention = false;

        if (!virtualSurfaceFORCursorStates[1])
            return;

        //check if the finger is dwelling
        if (Mathf.Abs(currentFingerSpeed) < dwellingFingerSpeedThreshold)
        {
            if (Mathf.Abs(currentFingerSpeed) < 0.01f)
            {
                return;
            }

            if (startDwellingTime == -10000)
            {
                startDwellingTime = Time.time;
            }
            else {
                if (Time.time - startDwellingTime >= dwellingTimeThreshold)
                    return;
            }
        }
        else
        {
            startDwellingTime = - 10000;
        }

        



        //finger is moving along x or y axis
        





        float dis = GetSmallestDistanceToSurface();
        if(dis == 10000)
            dis = GetInteractionFingertipToVirtualSurfaceFORDistance(1);

        if (alignNormal > alignThreshold && dis < touchProximityThreshold)
        {
            if (interactionIndexFingerToSurfaceSpeed < - interactionFingerTouchSpeedThreshold && prevInteractionIndexFingerToSurfaceSpeed > interactionFingerTouchSpeedThreshold) {
                interactionFingerTouchIntention = true;
            }
        }

        
        
    }

    private void CheckInteractionFingerTouchIntentionOnVirtualSurface()
    {

        interactionFingerTouchIntention = false;

        if (!webviewCursorStates[1])
            return;

        //check if the finger is dwelling
        if (Mathf.Abs(currentFingerSpeed) < dwellingFingerSpeedThreshold)
        {
            if (Mathf.Abs(currentFingerSpeed) < 0.01f)
            {
                return;
            }

            if (startDwellingTime == -10000)
            {
                startDwellingTime = Time.time;
            }
            else
            {
                if (Time.time - startDwellingTime >= dwellingTimeThreshold)
                    return;
            }
        }
        else
        {
            startDwellingTime = -10000;
        }

        float dis = GetRetargetFingerTipToVirtualSurfaceDistance(1);

        if (alignNormal > alignThreshold && dis < touchProximityThreshold)
        {
            if (interactionIndexFingerToSurfaceSpeed < - interactionFingerTouchSpeedThreshold && prevInteractionIndexFingerToSurfaceSpeed > interactionFingerTouchSpeedThreshold)
            {
                interactionFingerTouchIntention = true;
            }
        }

        
    }

    private Vector2 ComputeSurfaceCursorRelativePosition(int idx)
    {
        Vector2 relativePosition = new Vector2();

        Vector3 xAxisProjection = surfaceBottomLeft.transform.position + Vector3.Project(surfaceCursorPositions[idx] - surfaceBottomLeft.transform.position, surfaceXAxis);
        Vector3 yAxisProjection = surfaceBottomLeft.transform.position + Vector3.Project(surfaceCursorPositions[idx] - surfaceBottomLeft.transform.position, surfaceYAxis);

        float xDisToBottomLeft = Vector3.Distance(surfaceBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(surfaceBottomRight.transform.position, xAxisProjection);
        float xLength = surface.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(surfaceBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(surfaceBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(surfaceTopLeft.transform.position, yAxisProjection);
        float yLength = surface.transform.lossyScale.z;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(surfaceBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    private Vector2 ComputeVirtualWindowCursorRelativePosition(int idx)
    {
        Vector2 relativePosition = new Vector2();

        Vector3 xAxisProjection = virtualWindowBottomLeft.transform.position + Vector3.Project(virtualWindowCursorPositions[idx] - virtualWindowBottomLeft.transform.position, virtualWindowXAxis);
        Vector3 yAxisProjection = virtualWindowBottomLeft.transform.position + Vector3.Project(virtualWindowCursorPositions[idx] - virtualWindowBottomLeft.transform.position, virtualWindowYAxis);

        float xDisToBottomLeft = Vector3.Distance(virtualWindowBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(virtualWindowBottomRight.transform.position, xAxisProjection);
        float xLength = virtualWindow.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(virtualWindowBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(virtualWindowBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(virtualWindowTopLeft.transform.position, yAxisProjection);
        float yLength = virtualWindow.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(virtualWindowBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    private Vector2 ComputeWebviewCursorRelativePosition(int idx)
    {
        Vector2 relativePosition = new Vector2(-1, -1);

        if (activeWebview == null)
            return relativePosition;

        Vector3 xAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(webviewCursorPositions[idx] - webviewBottomLeft.transform.position, webviewXAxis);
        Vector3 yAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(webviewCursorPositions[idx] - webviewBottomLeft.transform.position, webviewYAxis);

        float xDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(webviewBottomRight.transform.position, xAxisProjection);
        float xLength = activeWebview.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(webviewTopLeft.transform.position, yAxisProjection);
        float yLength = activeWebview.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    public Vector2 ComputeWebviewPointerRelativePosition(Vector3 hitPoint)
    {
        Vector2 relativePosition = new Vector2(-1, -1);

        if (activeWebview == null)
            return relativePosition;

        Vector3 xAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(hitPoint - webviewBottomLeft.transform.position, webviewXAxis);
        Vector3 yAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(hitPoint - webviewBottomLeft.transform.position, webviewYAxis);

        float xDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(webviewBottomRight.transform.position, xAxisProjection);
        float xLength = activeWebview.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(webviewTopLeft.transform.position, yAxisProjection);
        float yLength = activeWebview.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    private Vector2 ComputeWebviewCursorRelativePositionByPinchRaycasting()
    {
        Vector2 relativePosition = new Vector2(-1, -1);

        if (activeWebview == null)
            return relativePosition;

        Vector3 xAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(pinchWebviewCursorPosition - webviewBottomLeft.transform.position, webviewXAxis);
        Vector3 yAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(pinchWebviewCursorPosition - webviewBottomLeft.transform.position, webviewYAxis);

        float xDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(webviewBottomRight.transform.position, xAxisProjection);
        float xLength = activeWebview.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(webviewTopLeft.transform.position, yAxisProjection);
        float yLength = activeWebview.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    private Vector2 ComputeWebviewCursorRelativePositionByNonDomHandPinchRaycasting()
    {
        Vector2 relativePosition = new Vector2(-1, -1);

        if (activeWebview == null)
            return relativePosition;

        Vector3 xAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(nonDomHandPinchWebviewCursorPosition - webviewBottomLeft.transform.position, webviewXAxis);
        Vector3 yAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(nonDomHandPinchWebviewCursorPosition - webviewBottomLeft.transform.position, webviewYAxis);

        float xDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(webviewBottomRight.transform.position, xAxisProjection);
        float xLength = activeWebview.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(webviewTopLeft.transform.position, yAxisProjection);
        float yLength = activeWebview.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    /*
     * |----------------------------
     * | PUBLIC METHODS
     * |----------------------------
     */

    // Primary Interaction GameObjects

    public Vector2 ComputeVirtualWindowRelativePositionOnWebview()
    {
        Vector2 relativePosition = new Vector2(-1, -1);

        if (activeWebview == null)
            return relativePosition;

        Vector3 xAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(virtualWindow.transform.position - webviewBottomLeft.transform.position, webviewXAxis);
        Vector3 yAxisProjection = webviewBottomLeft.transform.position + Vector3.Project(virtualWindow.transform.position - webviewBottomLeft.transform.position, webviewYAxis);

        float xDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection);
        float xDisToBottomRight = Vector3.Distance(webviewBottomRight.transform.position, xAxisProjection);
        float xLength = activeWebview.transform.lossyScale.x;

        if (xDisToBottomLeft > xLength || xDisToBottomRight > xLength) relativePosition.x = -1;
        else relativePosition.x = Vector3.Distance(webviewBottomLeft.transform.position, xAxisProjection) / xLength;

        float yDisToBottomLeft = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection);
        float yDisToTopLeft = Vector3.Distance(webviewTopLeft.transform.position, yAxisProjection);
        float yLength = activeWebview.transform.lossyScale.y;

        if (yDisToBottomLeft > yLength || yDisToTopLeft > yLength) relativePosition.y = -1;
        else relativePosition.y = Vector3.Distance(webviewBottomLeft.transform.position, yAxisProjection) / yLength;

        return relativePosition;
    }

    public void CheckTouchState(float x, float y, float dis)
    {
        string touch_data = x.ToString("F8") + "," + y.ToString("F8") + "," + dis.ToString("F8");
        interactionSpace.GetComponent<UDPSender>().sendString(touch_data);
    }

    public void UpdateTouchState(string data)
    {
        int touch;
        int window = 10;

        int data_int = int.Parse(data);

        indexTouchStates.Add(data_int);

        int length = indexTouchStates.Count;

        if (length >= window)
        {
            int sum = 0;
            for (int i = length - window; i < length; i++)
            {
                sum = sum + indexTouchStates[i];
            }
            touch = (int)Math.Round((double)sum / (double)window, 0);
        }
        else
        {
            int sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum = sum + indexTouchStates[i];
            }
            touch = (int)Math.Round((double)sum / (double)length, 0);
        }

        if (touch == 1) touchStates[1] = true;
        else touchStates[1] = false;
    }

    public Boolean GetTouchState(int idx)
    {
        return touchStates[idx];
    }

    public GameObject GetCenterEyeGameObject()
    {
        return centerEye;
    }

    public GameObject GetSurfaceGameObject()
    {
        return surface;
    }

    public GameObject GetSurfaceReferenceGameObject()
    {
        return surfaceReference;
    }

    public GameObject GetSurfaceMatchVirtualWindowGameObject()
    {
        return surfaceMatchVirtualWindow;
    }

    public GameObject GetRelocateInteractionHandGameObject()
    {
        return relocateInteractionHand;
    }

    public GameObject GetRelocateInteractionHandMeshGameObject()
    {
        return relocateInteractionHandMesh;
    }

    public GameObject GetRelocateSurfaceHandGameObject()
    {
        return relocateSurfaceHand;
    }

    public GameObject GetRelocateSurfaceHandMeshGameObject()
    {
        return relocateSurfaceHandMesh;
    }

    public GameObject GetRelocateSurfaceHandPalmGameObject()
    {
        return relocateSurfaceHandPalm;
    }

    public GameObject GetInteractionHandRelativeToHandSurfaceGameObject()
    {
        return interactionHandRelativeToHandSurface;
    }

    public GameObject GetPinchArrowRelativeToHandSurfaceGameObject()
    {
        return pinchArrowRelativeToHandSurface;
    }

    public GameObject GetSurfaceHandGameObject()
    {
        return surfaceHand;
    }

    public GameObject GetSurfaceHandReferenceGameObject()
    {
        return surfaceHandReference;
    }

    public GameObject GetSurfaceHandPalmGameObject()
    {
        return surfaceHandPalm;
    }

    public GameObject GetInteractionHandGameObject()
    {
        return interactionHand;
    }

    public void SetActiveVirtualSurfaceGameObject(GameObject virtualSurface)
    {
        if (virtualSurface != null)
        {
            if (virtualSurface != activeVirtualSurface)
            {
                virtualSurface.GetComponent<ProgressVisualizerController>().SetProgress(0);
            }

            activeVirtualSurface = virtualSurface;

            if (activeVirtualSurface != null)
            {
                activeWebview = activeVirtualSurface.transform.Find("OculusWebViewDemo/WebViewPrefab(Clone)/WebViewPrefabResizer/WebViewPrefabView").gameObject;
                activeWebview.tag = "webview";
                activeWebview.layer = LayerMask.NameToLayer("WebviewLayer");
                activeWebviewPrefab = activeVirtualSurface.transform.Find("OculusWebViewDemo/WebViewPrefab(Clone)").GetComponent<WebViewPrefab>();
                //activeWebview = activeVirtualSurface.transform.Find("WebViewPrefab/WebViewPrefabResizer/WebViewPrefabView").gameObject;
                //activeWebviewPrefab = activeVirtualSurface.transform.Find("WebViewPrefab").GetComponent<WebViewPrefab>();
                handDisplayRatio = activeVirtualSurface.transform.lossyScale.x / surface.transform.lossyScale.x;
            }
        }
    }

    public GameObject GetActiveVirtualSurfaceGameObject()
    {
        
        return activeVirtualSurface;
    }

    public GameObject GetActiveWebviewGameObject()
    {
        return activeWebview;
    }

    public WebViewPrefab GetActiveWebviewPrefab()
    {
        return activeWebviewPrefab;
    }

    public GameObject GetVirtualSurfaceGameObject()
    {
        
        return virtualSurface;
    }

    public GameObject GetVirtualSurfaceAnchorGameObject()
    {
        
        return virtualSurfaceAnchor;
    }

    public GameObject GetWebviewBottomLeftGameObject()
    {
        
        if (activeWebview != null)
            UpdateWebview();
        return webviewBottomLeft;
    }

    public GameObject GetVirtualSurfaceReferenceGameObject()
    {
        
        return virtualSurfaceReference;
    }

    public GameObject GetVirtualSurfaceMaskGameObject()
    {
        return virtualSurfaceMask;
    }

    public GameObject GetVirtualObjectGameObject()
    {
        return virtualObject;
    }

    public GameObject GetVirtualObjectReferenceGameObject()
    {
        return virtualObjectReference;
    }

    public GameObject GetHandSurfaceVirtualObjectGameObject()
    {
        return handSurfaceVirtualObject;
    }

    public GameObject GetVirtualWindowGameObject()
    {
        return virtualWindow;
    }

    public Vector3 GetVirtualWindowNormal()
    {
        return virtualWindowNormal;
    }

    public GameObject GetVirtualWindowReferenceGameObject()
    {
        return virtualWindowReference;
    }

    public GameObject GetVirtualWindowFrameGameObject()
    {
        return virtualWindowFrame;
    }

    public Vector3 GetZeroKnucklePosition(int idx)
    {
        return zeroKnuckles[idx].transform.position;
    }

    public Vector3 GetFirstKnucklePosition(int idx)
    {
        return firstKnuckles[idx].transform.position;
    }

    public Vector3 GetSecondKnucklePosition(int idx)
    {
        return secondKnuckles[idx].transform.position;
    }

    public Vector3 GetThirdKnucklePosition(int idx)
    {
        return thirdKnuckles[idx].transform.position;
    }

    public Vector3 GetForthKnucklePosition(int idx)
    {
        return forthKnuckles[idx].transform.position;
    }

    public Vector3 GetInteractionHandZeroKnucklePosition(int idx)
    {
        return interactionHandZeroKnuckles[idx].transform.position;
    }

    public Vector3 GetInteractionHandFirstKnucklePosition(int idx)
    {
        return interactionHandFirstKnuckles[idx].transform.position;
    }

    public Vector3 GetInteractionHandSecondKnucklePosition(int idx)
    {
        return interactionHandSecondKnuckles[idx].transform.position;
    }

    public Vector3 GetInteractionHandThirdKnucklePosition(int idx)
    {
        return interactionHandThirdKnuckles[idx].transform.position;
    }

    public Vector3 GetInteractionHandForthKnucklePosition(int idx)
    {
        return interactionHandForthKnuckles[idx].transform.position;
    }


    // Surface Orientation and Position
    public Vector3 GetSurfaceNormal()
    {
        return surfaceNormal;
    }

    public Vector3 GetSurfaceXAxis()
    {
        return surfaceXAxis;
    }

    public Vector3 GetSurfaceYAxis()
    {
        return surfaceYAxis;
    }

    public Vector3 GetSurfacePosition()
    {
        return surface.transform.position;
    }

    public Quaternion GetSurfaceRotation()
    {
        return surface.transform.rotation;
    }

    public GameObject GetSurfaceBottomRightGameObject()
    {
        return surfaceBottomRight;
    }

    public GameObject GetSurfaceBottomLeftGameObject()
    {
        return surfaceBottomLeft;
    }

    public GameObject GetSurfaceTopRightGameObject()
    {
        return surfaceTopRight;
    }

    public GameObject GetSurfaceTopLeftGameObject()
    {
        return surfaceTopLeft;
    }


    // FingerTip Variables
    public Vector3 GetInteractionFingerTipPosition(int idx)
    {
        if (interactionFingerTips[idx] != null)
        {
            if (filterOn)
            {
                positionFilters[idx].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                return positionFilters[idx].Filter(interactionFingerTips[idx].transform.position);
            }
            else
                return interactionFingerTips[idx].transform.position;
        }
        return new Vector3(10000, 10000, 10000);
    }

    public Vector3 GetSurfaceFingerTipPosition(int idx)
    {
        // Return the tracked surface fingertip position without filtering.
        if (surfaceFingerTips[idx] != null)
        {
            







            return surfaceFingerTips[idx].transform.position;
        }
        return new Vector3(10000, 10000, 10000);
    }

    public Vector3 GetRetargetInteractionFingerTipPosition(int idx)
    {
        if (retargetInteractionFingerTips[idx] != null)
        {
            if (filterOn)
            {
                retargetPositionFilters[idx].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                return retargetPositionFilters[idx].Filter(retargetInteractionFingerTips[idx].transform.position);
            }
            else
                return retargetInteractionFingerTips[idx].transform.position;
        }
        return new Vector3(10000, 10000, 10000);
    }

    public Vector3 GetRetargetSurfaceFingerTipPosition(int idx)
    {
        if (retargetSurfaceFingerTips[idx] != null)
        {
            if (filterOn)
            {
                retargetPositionFilters[idx].UpdateParams(filterFrequency, filterMinCutoff, filterBeta, filterDcutoff);
                return retargetPositionFilters[idx].Filter(retargetSurfaceFingerTips[idx].transform.position);
            }
            else
                return retargetSurfaceFingerTips[idx].transform.position;
        }
        return new Vector3(10000, 10000, 10000);
    }

    // FingerTip Interaction Variables
    public float GetHoverDistanceThreshold()
    {
        return hoverDistanceThreshold;
    }

    public float GetInteractionFingerTipDistance(int finger1, int finger2)
    {
        return interactionFingerTipDistances[finger1, finger2];
    }

    public float GetSurfaceFingerTipDistance(int finger1, int finger2)
    {
        return surfaceFingerTipDistances[finger1, finger2];
    }

    public float GetFingerTipToSurfaceDistance(int fingerIndex)
    {
        return fingerTipToSurfaceDistances[fingerIndex];
    }

    public float GetRetargetFingerTipToVirtualSurfaceDistance(int fingerIndex)
    {
        return retargetFingerTipToVirtualSurfaceDistances[fingerIndex];
    }

    public float GetRetargetIndexFingerTipToVirtualSurfaceSpeed()
    {
        return retargetIndexFingerToSurfaceSpeed;
    }

    public float GetInteractionFingertipToVirtualSurfaceFORDistance(int fingerIndex)
    {
        return interactionFingerTipToVirtualSurfaceFORDistances[fingerIndex];
    }

    public float GetInteractionFingertipToVirtualSurfaceFORSpeed()
    {
        return interactionIndexFingerToSurfaceSpeed;
    }

    public float GetPrevInteractionFingertipToVirtualSurfaceFORSpeed()
    {
        return prevInteractionIndexFingerToSurfaceSpeed;
    }

    public float GetInteractionFingertipToVirtualSurfaceFORSpeedX()
    {
        return interactionIndexFingerToSurfaceSpeedX;
    }

    public float GetInteractionFingertipToVirtualSurfaceFORSpeedY()
    {
        return interactionIndexFingerToSurfaceSpeedY;
    }

    public float GetFingerTipToVirtualSurfaceSpeed(int fingerIndex)
    {
        return retargetFingerTipToVirtualSurfaceSpeed[fingerIndex];
    }

    public bool GetFingerPenetrateSurface(int fingerIndex)
    {
        return fingerPenetrateSurface[fingerIndex];
    }

    public bool GetRetargetFingerPenetrateSurface(int fingerIndex)
    {
        return retargetFingerPenetrateSurface[fingerIndex];
    }

    public float GetFingerTipToSurfaceHandDistance(int fingerIndex)
    {
        return fingerTipToSurfaceHandDistances[fingerIndex];
    }


    // Cursor Variables
    public bool GetSurfaceHandCursorState(int idx)
    {
        return surfaceHandCursorStates[idx];
    }

    public bool GetSurfaceCursorState(int idx)
    {
        return surfaceCursorStates[idx];
    }

    public bool GetVirtualWindowCursorState(int idx)
    {
        return virtualWindowCursorStates[idx];
    }

    public bool GetWebviewCursorState(int idx)
    {
        return webviewCursorStates[idx];
    }

    public bool GetPinchWebviewCursorState()
    {
        return pinchWebviewCursorState;
    }

    public bool GetNonDomHandPinchWebviewCursorState()
    {
        return nonDomHandPinchWebviewCursorState;
    }

    public Vector3 GetSurfaceCursorPosition(int idx)
    {
        return surfaceCursorPositions[idx];
    }

    public Vector3 GetSurfaceHandCursorPosition(int idx)
    {
        return surfaceHandCursorPositions[idx];
    }

    public Vector3 GetVirtualSurfaceFORCursorPosition(int idx)
    {
        return virtualSurfaceFORCursorPositions[idx];
    }

    public Vector3 GetVirtualWindowCursorPosition(int idx)
    {
        return virtualWindowCursorPositions[idx];
    }

    public Vector3 GetWebviewCursorPosition(int idx)
    {
        return webviewCursorPositions[idx];
    }

    public Vector3 GetPinchWebviewCursorPosition()
    {
        return pinchWebviewCursorPosition;
    }

    public Vector3 GetNonDomHandPinchWebviewCursorPosition()
    {
        return nonDomHandPinchWebviewCursorPosition;
    }

    public Vector2 GetSurfaceCursorRelativeCoordinate(int idx)
    {
        return surfaceCursorRelativePositions[idx];
    }

    public Vector2 GetVirtualWindowCursorRelativeCoordinate(int idx)
    {
        return virtualWindowCursorRelativePositions[idx];
    }

    public Vector2 GetWebviewCursorRelativeCoordinate(int idx)
    {
        return webviewCursorRelativePositions[idx];
    }

    public Vector2 GetPinchWebviewCursorRelativeCoordinate()
    {
        return pinchWebviewCursorRelativePosition;
    }

    public Vector2 GetNonDomHandPinchWebviewCursorRelativeCoordinate()
    {
        return nonDomHandPinchWebviewCursorRelativePosition;
    }

    // Misc 
    public Vector3 GetSurfaceWristPosition()
    {
        return surfaceWrist.transform.position;
    }

    public GameObject GetInteractionWristGameObject()
    {
        return interactionWrist;
    }

    public GameObject GetSurfaceWristGameObject()
    {
        return surfaceWrist;
    }

    //set touch state
    public void SetTouchStateAllFinger(int idx, bool isTouch)
    {
        touchStateAllFinger[idx] = isTouch;
    }

    //get touch state
    public bool GetTouchStateAllFinger(int idx)
    {
        return touchStateAllFinger[idx];
    }

    //get retarget interaction index finger speed
    public float GetInteractionFingerSpeed()
    {
        return currentFingerSpeed;
    }

    public bool GetInteractionFingerTouchIntention()
    {
        return interactionFingerTouchIntention;
    }

    public bool GetIsCalibrateFORGesture()
    {
        return calibrateFORGesture;
    }

    public bool GetFixFORPad()
    {
        return fixFORPad;
    }

    public void SetCalibrateFORGesture(bool isUpdate)
    {
        calibrateFORGesture = isUpdate;
    }

    public bool GetIsUpdateLookDownAngle()
    {
        return updateLookDownAngle;
    }

    public void SetUpdateLookDownAngle(bool isUpdate)
    {
        updateLookDownAngle = isUpdate;
    }

    public bool GetIsUpdateInteractionPadPosition()
    {
        return updateInteractionPadPosition;
    }

    public bool GetIsUpdateInteractionPadRotation()
    {
        return updateInteractionPadRotation;
    }

    public bool GetIsVisualizeTouchPoints()
    {
        return visualizeTouchPoints;
    }

    public Vector3 GetHeadGazeLocation()
    {
        Vector3 position = new Vector3(10000, 10000, 10000);

        RaycastHit hit;

        if (Physics.Raycast(centerEye.transform.position, centerEye.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                position = hit.point;
            }
        }
        else if (Physics.Raycast(centerEye.transform.position, -centerEye.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                position = hit.point;
            }
        }

        return position;
    }

    public void SetIndirectCursorByHeadGaze()
    {
        indirectCursorState = false;
        indirectCursorPosition = new Vector3(10000, 10000, 10000);
        indirectCursorRelativePosition = new Vector2(10000, 10000);

        RaycastHit hit;

        if (Physics.Raycast(centerEye.transform.position, centerEye.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                indirectCursorState = true;
                indirectCursorPosition = hit.point;
                indirectCursorRelativePosition = ComputeWebviewPointerRelativePosition(indirectCursorPosition);
            }
        }
        else if (Physics.Raycast(centerEye.transform.position, -centerEye.transform.forward, out hit, 100.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                indirectCursorState = true;
                indirectCursorPosition = hit.point;
                indirectCursorRelativePosition = ComputeWebviewPointerRelativePosition(indirectCursorPosition);
            }
        }
    }

    public void UpdateIndirectCursorByHandTouch(float dx, float dy, float dt)
    {
        if (indirectCursorRelativePosition.x != 10000)
        {
            float x = indirectCursorRelativePosition.x;
            float y = indirectCursorRelativePosition.y;
            Vector2 cursorOffset = addAcceleration(dx, dy, dt);

            if (indirectCursorState)
            {
                x = indirectCursorRelativePosition.x + cursorOffset.x;
                y = indirectCursorRelativePosition.y + cursorOffset.y;
                 
                if(x < 0) x = 0;
                if(x > 1) x = 1;
                if (y < 0) y = 0;
                if (y > 1) y = 1;

                indirectCursorRelativePosition = new Vector2 (x, y);
                float xLength = activeWebview.transform.lossyScale.x;
                float yLength = activeWebview.transform.lossyScale.y;
                indirectCursorPosition = webviewBottomLeft.transform.position + activeWebview.transform.right * indirectCursorRelativePosition.x * xLength;
                indirectCursorPosition += activeWebview.transform.up * indirectCursorRelativePosition.y * yLength;
            }
        }
    }

    private Vector2 addAcceleration(float dx, float dy, float dt)
    {
        float speed = Mathf.Sqrt(dx * dx + dy * dy) / dt;

        float accFactor = 1;

        if (speed < 10)
        {
            accFactor = (1.3f / 10) * speed;
        }
        else if (speed < 130)
        {
            accFactor = 1.3f;
        }
        else if (speed < 430)
        {
            accFactor = (3.2f / 300) * (speed - 130) + 1.3f;
        }
        else
        {
            accFactor = 4.5f;
        }

        return new Vector2(dx * accFactor, dy * accFactor);
    }

    public bool GetIndirectCursorState()
    {
        return indirectCursorState;
    }

    public Vector3 GetIndirectCursorPosition()
    {
        return indirectCursorPosition;
    }

    public Vector2 GetIndirectCursorRelativeCoordinate()
    {
        return indirectCursorRelativePosition;
    }

    public float GetMovingX()
    {
        return movingSpeedX.ToArray()[movingSpeedX.Count - 1];
    }

    public float GetMovingY()
    {
        return movingSpeedY.ToArray()[movingSpeedY.Count - 1];
    }

    public InteractionMethod GetInteractionMethod()
    {
        return interactionMethod;
    }

    public GameObject GetHandPinchArrowGameObject()
    {
        return handPinchArrow;
    }

    public GameObject GetRetargetHandPinchArrowGameObject()
    {
        return retargetHandPinchArrow;
    }

    public GameObject GetCursorGameObject()
    {
        return cursor;
    }

    public GameObject GetHandPinchArrowOffsetGameObject()
    {
        return handPinchArrowOffset;
    }

    public GameObject GetNonDomHandPinchArrowGameObject()
    {
        return nonDomHandPinchArrow;
    }

    public GameObject GetRetargetNonDomHandPinchArrowGameObject()
    {
        return retargetNonDomHandPinchArrow;
    }

    public GameObject GetNonDomCursorGameObject()
    {
        return nonDomCursor;
    }

    public GameObject GetNonDomHandPinchArrowOffsetGameObject()
    {
        return handPinchArrowOffset;
    }

    public Vector3 GetSmoothGazeOrigin()
    {
        return smoothGazeOrigin;
    }

    public Vector3 GetGazeDirection()
    {
        return gazeDirection;
    }

    public Vector3 GetSmoothGazeCursor()
    {
        return smoothGazeCursor;
    }

    public float GetGazeSpeed()
    {
        return currentGazeSpeed;
    }

    public float GetGazeMoveDis()
    {
        return currentGazeTotalMoveDis;
    }

    public bool GetInitVirtualSurfaceFOR()
    {
        return initVirtualSurfaceFOR;
    }

    public void SetInitVirtualSurfaceFOR(bool init)
    {
        initVirtualSurfaceFOR = init;
    }

    public bool IsTwoHandInteraction()
    {
        return twoHand;
    }
}
