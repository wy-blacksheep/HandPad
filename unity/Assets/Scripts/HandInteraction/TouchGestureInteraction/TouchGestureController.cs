//using Syncfusion.PMML;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using UnityEngine.WSA;

[RequireComponent(typeof(HandInteractionManager))]
[RequireComponent(typeof(UDPSender))]
[RequireComponent(typeof(SnapGestureController))]
[RequireComponent(typeof(HandRetargetingController))]
[RequireComponent(typeof(SnapGestureHelper))]
[RequireComponent(typeof(OculusWebViewDemo))]
[RequireComponent(typeof(HandPointer))]
[RequireComponent(typeof(CustomRayPointer))]
[RequireComponent(typeof(CustomPoke))]
[RequireComponent(typeof(CustomPseudoDirectPoke))]
[RequireComponent(typeof(CustomHandPointer))]
[RequireComponent(typeof(IndirectCursorController))]
[RequireComponent(typeof(EyeGazePointer))]
[RequireComponent(typeof(EyeGazeHandPinch))]
public class TouchGestureController : MonoBehaviour
{
    // Velocity-based Touch detector
    List<TouchDetector> touchStates;

    public enum TouchGestures
    {
        None,
        Tap,
        Drag,
        DragToScroll,
        DragToNavigate,
        Scroll,
        Scale,
        Rotate
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
        //touch detection based on hand velocity
        HandPseudoDirectPoke_V2,
        //pinch to select, with haptic retargeting
        CustomHandPointer,
        HandIndirectTouch,
        //hand to indicate/move/manipulate
        HandMove,
        //eye gaze to indicate/move, hand to manipulate
        EyeMoveHand,
        //eye gaze to indicate, hand to move/manipulate, hand jump to object
        EyeHandMove,
        //eye gaze to indicate, hand to move/manipulate, object jump to hand
        EyeHandJump,
        DirectPinch,
        GazePinch
    }

    [SerializeField]
    private InteractionMethod interactionMethod;

    








    private TouchGestures lastTouchGesture = TouchGestures.None;
    private TouchGestures currentTouchGesture = TouchGestures.None;
    private string gestureForDubug = "";

    private HandInteractionManager interactionManager;
    private SnapGestureController snapGestureController;
    private HandRetargetingController handRetargetingController;
    private SnapGestureHelper snapGestureHelper;
    private UDPSender udpSender;
    private OculusWebViewDemo oculusWebViewDemo;
    private HandPointer handPointer;
    private CustomRayPointer customRayPointer;
    private CustomPoke customPoke;
    private CustomPseudoDirectPoke customPseudoDirectPoke;
    private CustomHandPointer customHandPointer;
    private IndirectCursorController indirectCursorController;
    private EyeGazePointer eyeGazePointer;
    private EyeGazeHandPinch eyeGazeHandPinch;

    private OculusWebViewDemo.Tasks task;

    private float[,] interTouchDistance = new float[5, 5];

    //index
    private bool touchState = false;
    private int touchStatus = -1;

    //touch/no touch
    private bool[] lastTouchStateAllFinger = new bool[5];
    private bool[] touchStateAllFinger = new bool[5];

    //touch/dwelling/moving
    private bool[] touchStateAllFingerTDM = new bool[5];
    private bool[] dwellingStateAllFingerTDM = new bool[5];
    private bool[] movingStateAllFingerTDM = new bool[5];

    private Vector3[] initTouchPointAllFinger = new Vector3[5];
    private Vector3[] lastTouchPointAllFinger = new Vector3[5];
    private Vector3[] touchPointAllFinger = new Vector3[5];

    private float[] dwellingTime = new float[5];
    private float[] movingDistance = new float[5];
    private float[] hoverTime = new float[5] { 0f, 0f, 0f, 0f, 0f };
    private float hoverTimeThreshold = 0.5f;

    private int n_touch = 0;
    private int n_dwelling = 0;
    private int n_moving = 0;

    //Touch state: S = {N_touched, N_dwelling, N_moving}
    //touch > 450 ms -> dwelling -> > 5 mm -> moving
    //private List<int> touchStateTDM = new List<int>() { 0, 0, 0 };
    //private List<int> lastTouchStateTDM = new List<int>() { 0, 0, 0 };

    private int[] touchStateTDM = new int[3] { 0, 0, 0 };
    private int[] lastTouchStateTDM = new int[3] { 0, 0, 0 };

    //A gesture is defined by a state sequence: {S_1, S_2, …, S_n}
    private List<int[]> touchStateSequence = new List<int[]>();

    private UnityEngine.Color defaultColor;
    private UnityEngine.Color touchColor;

    private float touchThreshold = 0.01f;
    //for fingers except index
    private float secondaryTouchThreshold = 0.005f;

    //for precheck touch
    private float fingertipHoverThreshold = 0.05f;
    private float[] fingerRotationThreshold = new float[5] { 40f, 80f, 80f, 80f, 80f };

    private float dragToBackForwardDisThreshold = 0.02f;

    private float scaleDisThreshold = 0.03f;
    private float rotateRotThreshold = 30f;

    private float dwellingTimeThreshold = 0.45f;
    private float movingDistanceThreshold = 0.01f;

    
    

    
    

    //distance from interaction finger to surface finger
    private float surfaceFingerTouchThreshold = 0.03f;

    [SerializeField]
    private bool reloadAllWebsites = false;

    [SerializeField]
    private bool interactionOnWebview = true;

    [SerializeField]
    private bool collectFittsData = false;
    private List<string[]> studyTouchData = new List<string[]>();
    private bool isFingerTouch = false;
    private bool isNonDomFingerTouch = false;

    [SerializeField]
    private bool collectTouchData = false;
    private List<string[]> touchData = new List<string[]>();

    [SerializeField]
    private bool collectGestureData = false;
    private List<string[]> gestureData = new List<string[]>();

    [SerializeField]
    private bool collectKnucklePosData = false;
    private List<string[]> knucklePosData = new List<string[]>();

    [SerializeField]
    private bool collectKnuckleOrientationData = false;

    [SerializeField]
    private bool collectFingerOrientationData = false;
    private List<string[]> fingerRotData = new List<string[]>();

    private TextMeshProUGUI debugText;

    private Vector2 cursorPosition;

    private GameObject virtualObject;
    private GameObject virtualObjectReference;
    private GameObject virtualWindow;
    private GameObject virtualSurface;
    private float initVirtualSurfaceScale;
    private GameObject virtualSurfaceFOR;
    private GameObject virtualSurfaceAnchor;

    private GameObject handSurfaceVirtualObject;
    private GameObject handSurface;

    //for interaction on hand surface
    private Vector3 indexFingertip;
    private Vector3 handSurfaceNormal;
    private string handSurfaceVirtualObjectLayer;
    private string handSurfaceLayer;

    //for interaction on virtual window
    private Vector3 retargetThumb;
    private Vector3 retargetIndex;
    private Vector3 retargetMiddle;
    private Vector3 virtualWindowNormal;
    private string virtualObjectLayer;
    private GameObject activeObject = null;

    private Vector3 lastScaleVector = Vector3.one;
    private float lastRotationAngle = 0.0f;

    private TouchClassifier touchClassifier;

    //dom hand
    private bool isPinching = false;
    private Vector3 pinchPosition;
    private Vector3 lastPinchPosition;
    private Vector3 initPinchPosition;
    private Vector3 lastPinchWebviewCursor;
    private float pinchDisThreshold = 0.02f;
    private Vector3 pinchRayDirection;

    private float initPinchTime;
    private float pinchDwellingTime;
    private float pinchMovingDistance;

    private string debugPinching = "";
    private TouchGestures curPinchGesture = TouchGestures.None;
    private TouchGestures lastPinchGesture = TouchGestures.None;

    private GameObject pointerRay;
    private LineRenderer hitRay;
    private Boolean drawHitRay = false;

    private GameObject pinchHitVirtualSurface = null;

    private string debugTouching = "";

    private bool isClick = false;
    private float clickTime = 0.0f;
    public float clickTimeout = 0.5f;

    private bool isPrevIndexFingerTouch = false;
    private bool isPrevFingerPinch = false;

    private bool isSelect = false;
    private bool isDrag = false;
    private bool isCopy = false;
    private bool isScroll = false;
    private float scrollAngle = 90f;

    private bool isInairClick = false;

    private float fingerTouchThreshold = 0.03f;

    private string lastTask = "";
    private string curTask = "";   

    private bool isHitVirtualWindowBoundary = false;

    private string preCheckTouchDebug = "";

    private float currentFingerSpeed = 0.0f; // Initial velocity based on the average speed
    private float minFingerSpeed = 0.005f;
    private float speedDeceleration = 0.1f; // Adjust this value for the desired deceleration rate

    private bool isSurfaceHandPinching = false;
    private float surfaceHandPinchDisThreshold = 0.02f;

    private bool newTouchDetector = false;

    private Vector3 initTextObjectScale = new Vector3(-1, -1, -1);
    private Vector3 initTextObject1Scale = new Vector3(-1, -1, -1);
    private Vector3 initTextObject2Scale = new Vector3(-1, -1, -1);
    private Vector3 initImageObjectScale = new Vector3(-1, -1, -1);
    private Vector3 offsetText1 = Vector3.zero;
    private Vector3 offsetText2 = Vector3.zero;
    private Vector3 offsetImage = Vector3.zero;

    private bool storeTextObject1 = false;
    private bool storeTextObject2 = false;
    private bool storeImageObject = false;

    private string storageText;

    private int frameWindow = 10;
    private GameObject[] touchPoints = new GameObject[10];
    private Queue<Vector3> touchPointPositions = new Queue<Vector3>();

    private float virtualSurfaceFORRotationSpeed = 5f;
    private float virtualSurfaceFORPositionSpeed = 0.05f;

    private float indirectTouchThreshold = 60f;

    private bool isFittsLawStart = false;
    private int successTouch = -1;
    private int fittsTargetNum = 11;
    private int userID = -100;
    private int sessionID = -100;
    private int conditionID = -100;
    private int currentFittsTaskIndex = 0;

    private GameObject rayObj;
    private GameObject nonDomRayObj;

    Vector3 domPinchPos = new Vector3(10000, 10000, 10000);
    Vector3 initDomPinchPos = new Vector3(10000, 10000, 10000);
    Vector3 nonDomPinchPos = new Vector3(10000, 10000, 10000);
    Vector3 initNonDomPinchPos = new Vector3(10000, 10000, 10000);

    Vector3 domPokePos = new Vector3(10000, 10000, 10000);
    Vector3 initDomPokePos = new Vector3(10000, 10000, 10000);
    Vector3 nonDomPokePos = new Vector3(10000, 10000, 10000);
    Vector3 initNonDomPokePos = new Vector3(10000, 10000, 10000);

    private float gazeMoveSpeedThreshold = 0.05f;
    private float gazeMoveDisThreshold = 0.07f;

    //gaze to select, hand to drag
    private Vector2 startSelectCursor;
    private Vector3 gazeHandOffset;

    




    void Start()
    {
        touchStates = new List<TouchDetector>();
        for (int i = 0; i < 5; i++)
        {
            touchStates.Add(new TouchDetector());
        }

        task = GameObject.Find("VirtualSurface").transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;

        interactionManager = GetComponent<HandInteractionManager>();
        snapGestureHelper = GetComponent<SnapGestureHelper>();
        snapGestureController = GetComponent<SnapGestureController>();
        handRetargetingController = GetComponent<HandRetargetingController>();
        udpSender = GetComponent<UDPSender>();
        oculusWebViewDemo = GetComponent<OculusWebViewDemo>();
        handPointer = GetComponent<HandPointer>();
        customRayPointer = GetComponent<CustomRayPointer>();
        customPoke = GetComponent<CustomPoke>();
        customPseudoDirectPoke = GetComponent<CustomPseudoDirectPoke>();
        customHandPointer = GetComponent<CustomHandPointer>();
        indirectCursorController = GetComponent<IndirectCursorController>();
        eyeGazePointer = GetComponent<EyeGazePointer>();
        eyeGazeHandPinch = GetComponent<EyeGazeHandPinch>();

        virtualObject = interactionManager.GetVirtualObjectGameObject();
        virtualObjectReference = interactionManager.GetVirtualObjectReferenceGameObject();
        virtualWindow = interactionManager.GetVirtualWindowGameObject();
        virtualSurface = interactionManager.GetVirtualSurfaceGameObject();
        initVirtualSurfaceScale = virtualSurface.transform.localScale.x;

        handSurfaceVirtualObject = interactionManager.GetHandSurfaceVirtualObjectGameObject();
        handSurface = interactionManager.GetSurfaceGameObject();

        debugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();

        handSurfaceVirtualObjectLayer = "HandSurfaceVirtualObjectLayer";
        handSurfaceLayer = "SurfaceLayer";
        virtualObjectLayer = "VirtualObjectLayer";

        //pointerRay = new GameObject("pointerRay");
        //hitRay = pointerRay.AddComponent<LineRenderer>();

        touchClassifier = new TouchClassifier();

        InitAll();

        if (GameObject.Find("TextObject1") != null)
        {
            offsetText1 = GameObject.Find("TextObject1").transform.localPosition;
            offsetText2 = GameObject.Find("TextObject2").transform.localPosition;
            offsetImage = GameObject.Find("ImageObject").transform.localPosition;
        }

        virtualSurfaceFOR = GameObject.Find("VirtualSurfaceFOR");
        virtualSurfaceAnchor = GameObject.Find("VirtualSurfaceAnchor");
        InitTouchPoints();

        InitStudyTouchData();

        if(interactionMethod == InteractionMethod.HandRayPointer)
        {
            rayObj = GameObject.Find("RaycasterPinchVisual");
            nonDomRayObj = GameObject.Find("RaycasterPinchVisual");
        }
    }

    void Update()
    {
        if (reloadAllWebsites)
        {
            Vuplex.WebView.Web.ClearAllData();
            reloadAllWebsites = false;
        }

        indexFingertip = interactionManager.GetInteractionFingerTipPosition(1);
        handSurfaceNormal = interactionManager.GetSurfaceNormal();

        retargetThumb = interactionManager.GetRetargetInteractionFingerTipPosition(0);
        retargetIndex = interactionManager.GetRetargetInteractionFingerTipPosition(1);
        retargetMiddle = interactionManager.GetRetargetInteractionFingerTipPosition(2);
        virtualWindowNormal = interactionManager.GetVirtualWindowNormal();

        if (interactionManager.GetActiveVirtualSurfaceGameObject() != null)
        {
            task = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;
        }

        UpdateInterTouchDistance();

        //UpdateTouchData();

        isFingerTouch = false;

        if (collectTouchData)
        {
            

















            UpdateTouchData();
        }
        else if (collectGestureData)
        {
            UpdateGestureData();
        }
        else if (collectKnucklePosData)
        {
            UpdateKnucklePosDataData();
        }
        else if (collectKnuckleOrientationData)
        {
            UpateKunckleOrientationData();
        }
        else if(collectFingerOrientationData)
        {
            UpdateFingerOrientationData();
        }
        else
        {
            // touch Test
            //RequestInteractionTouchStateUpdate();

            //RequestTouchStateUpdate();
            //RequestTouchStateUpdateAllFinger();
            //UpdateTouchStateAllFinger();

            /*for (int i = 0; i<5; i++)
            {
                RequestTouchStateUpdateForSpecificFinger(i);
            }*/

            /*RequestTouchStateUpdateForSpecificFinger(1);

            UpdateTouchCursor();
            UpdateTouchPointAllFinger();

            UpdateTouchStateSequence();
            if(touchStateSequence.Count >= 3) virtualobject();*/

            if (interactionMethod == InteractionMethod.VRSnap || interactionMethod == InteractionMethod.PseudoDirectTouch)
            {
                RequestInteractionTouchStateUpdate();

                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                isClick = false;               

                showPointerRay(pointerRay, pinchPosition, pinchRayDirection, false);
                if(interactionManager.GetActiveVirtualSurfaceGameObject() != null)
                {
                    initVirtualSurfaceScale = virtualSurface.transform.lossyScale.x;
                    task = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;
                    
                    if(task.ToString() != lastTask)
                    {
                        lastTask = curTask;
                        curTask = task.ToString();
                    }

                    storageText = task + ", " + interactionManager.GetFingerPenetrateSurface(1) + ", " + RequestFingerTouchState(1);

                    if (storeTextObject1 && initTextObject1Scale.x != -1)
                    {
                        GameObject.Find("TextObject1").transform.position = GameObject.Find("Retarget_l_index_finger_tip_marker").transform.position;
                    }

                    if (storeTextObject2 && initTextObject2Scale.x != -1)
                    {
                        GameObject.Find("TextObject2").transform.position = GameObject.Find("Retarget_l_ring_finger_tip_marker").transform.position;
                    }

                    if(storeImageObject && initImageObjectScale.x != -1)
                    {
                        GameObject.Find("ImageObject").transform.position = GameObject.Find("Retarget_l_pinky_finger_tip_marker").transform.position;
                    }

                    if (task == OculusWebViewDemo.Tasks.Storage1 && (interactionManager.GetRetargetFingerPenetrateSurface(1) || RequestFingerTouchState(1)))
                    {
                        Vector3 rayDirection = interactionManager.GetActiveVirtualSurfaceGameObject().transform.forward;
                        Vector3 virtualSurfacePos = interactionManager.GetActiveVirtualSurfaceGameObject().transform.position;
                        Vector3 manipulateFingerPos = interactionManager.GetRetargetInteractionFingerTipPosition(1);

                        /*offsetText1 = GameObject.Find("TextObject1").transform.localPosition;
                        offsetText2 = GameObject.Find("TextObject2").transform.localPosition;
                        offsetImage = GameObject.Find("ImageObject").transform.localPosition;*/

                        RaycastHit hit;

                        //infinity: no hit
                        if (Physics.Raycast(manipulateFingerPos, rayDirection, out hit, 100.0f, LayerMask.GetMask("TextObjectLayer")))
                        {
                            GameObject hitText = hit.collider.gameObject;

                            if(hitText != null)
                            {
                                storageText = hitText.name;

                                if (hitText.name == "TextObject1")
                                {
                                    if (initTextObject1Scale.x == -1)
                                    {
                                        initTextObject1Scale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initTextObject1Scale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 1))
                                        {
                                            storeTextObject1 = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_index_finger_tip_marker").transform.position;
                                        }
                                    }
                                }

                                if (hitText.name == "TextObject2")
                                {
                                    if (initTextObject2Scale.x == -1)
                                    {
                                        initTextObject2Scale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initTextObject2Scale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 3))
                                        {
                                            storeTextObject2 = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_ring_finger_tip_marker").transform.position;
                                        }
                                    }
                                }

                                if (hitText.name == "ImageObject")
                                {
                                    if (initImageObjectScale.x == -1)
                                    {
                                        initImageObjectScale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initImageObjectScale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 4))
                                        {
                                            storeImageObject = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_pinky_finger_tip_marker").transform.position;
                                        }
                                    }
                                }
                            }
                        }

                        if (Physics.Raycast(manipulateFingerPos, -1f * rayDirection, out hit, 100.0f, LayerMask.GetMask("TextObjectLayer")))
                        {
                            GameObject hitText = hit.collider.gameObject;

                            if (hitText != null)
                            {
                                storageText = hitText.name;

                                if (hitText.name == "TextObject1")
                                {
                                    if (initTextObject1Scale.x == -1)
                                    {
                                        initTextObject1Scale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initTextObject1Scale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 1))
                                        {
                                            storeTextObject1 = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_index_finger_tip_marker").transform.position;
                                        }
                                    }
                                }

                                if (hitText.name == "TextObject2")
                                {
                                    if (initTextObject2Scale.x == -1)
                                    {                                          
                                        initTextObject2Scale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initTextObject2Scale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 3))
                                        {
                                            storeTextObject2 = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_ring_finger_tip_marker").transform.position;
                                        }
                                    }
                                }

                                if (hitText.name == "ImageObject")
                                {
                                    if (initImageObjectScale.x == -1)
                                    {
                                        initImageObjectScale = new Vector3(hitText.transform.localScale.x, hitText.transform.localScale.y, hitText.transform.localScale.z);
                                    }
                                    else
                                    {
                                        hitText.transform.localScale = initImageObjectScale / 10;
                                        //hitText.transform.position = GameObject.Find("Retarget_r_index_finger_tip_marker").transform.position;
                                        hitText.transform.position = interactionManager.GetWebviewCursorPosition(1);
                                        if (CheckSurfaceFingerTouch(1, 4))
                                        {
                                            storeImageObject = true;
                                            hitText.transform.position = GameObject.Find("Retarget_l_pinky_finger_tip_marker").transform.position;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (task == OculusWebViewDemo.Tasks.Storage2)
                    {
                        Vector3 virtualSurfacePos = interactionManager.GetActiveVirtualSurfaceGameObject().transform.position;
                        Quaternion virtualSurfaceQua = interactionManager.GetActiveVirtualSurfaceGameObject().transform.rotation;

                        if (CheckSurfaceFingerTouch(1, 1) && initTextObject1Scale.x != -1)
                        {
                            //GameObject.Find("TextObject1").transform.position = virtualSurfacePos - offsetText1;
                            GameObject.Find("TextObject1").transform.SetParent(interactionManager.GetActiveVirtualSurfaceGameObject().transform);
                            GameObject.Find("TextObject1").transform.localPosition = offsetText1;
                            GameObject.Find("TextObject1").transform.localScale = initTextObject1Scale;
                            GameObject.Find("TextObject1").transform.rotation = virtualSurfaceQua;

                            initTextObject1Scale = new Vector3(-1, -1, -1);
                        }
                        if (CheckSurfaceFingerTouch(1, 3) && initTextObject2Scale.x != -1)
                        {
                            //GameObject.Find("TextObject2").transform.position = virtualSurfacePos - offsetText2;
                            GameObject.Find("TextObject2").transform.SetParent(interactionManager.GetActiveVirtualSurfaceGameObject().transform);
                            GameObject.Find("TextObject2").transform.localPosition = offsetText2;
                            GameObject.Find("TextObject2").transform.localScale = initTextObject2Scale;
                            GameObject.Find("TextObject2").transform.rotation = virtualSurfaceQua;
                            initTextObject2Scale = new Vector3(-1, -1, -1);
                        }
                        if (CheckSurfaceFingerTouch(1, 4) && initImageObjectScale.x != -1)
                        {
                            //GameObject.Find("ImageObject").transform.position = virtualSurfacePos - offsetImage;
                            GameObject.Find("ImageObject").transform.SetParent(interactionManager.GetActiveVirtualSurfaceGameObject().transform);
                            GameObject.Find("ImageObject").transform.localPosition = offsetImage;
                            GameObject.Find("ImageObject").transform.localScale = initImageObjectScale;
                            GameObject.Find("ImageObject").transform.rotation = virtualSurfaceQua;
                            initImageObjectScale = new Vector3(-1, -1, -1);
                        }
                    }

                    











                    
                }

                //non-dom hand pinch to take screenshot
                












                //if (snapGestureController.GetSnapMode() == SnapGestureController.SnapModes.Snap)
                //(snapGestureController.GetSnapState() == SnapStates.SnapState || snapGestureController.GetSnapState() == SnapStates.SemiSnapState || snapGestureController.GetSnapState() == SnapStates.OverSnapState)
                if (!snapGestureController.GetPinchState())
                {
                    /*for (int i = 0; i<5; i++)
                    {
                        RequestTouchStateUpdateForSpecificFinger(i);
                    }*/

                    //RequestTouchStateUpdateForSpecificFinger(0);
                    //RequestTouchStateUpdateForSpecificFinger(1);
                    //RequestTouchStateUpdateForSpecificFinger(2);

                    //RequestInteractionTouchStateUpdate();

                    //UpdateTouchCursor();
                    UpdateTouchPointAllFinger();

                    UpdateTouchStateSequence();
                    if (touchStateSequence.Count >= 3) GestureDetection();

                    //after one touch finishes in each frame, if fingers are still on the surface, update the initial touch point to the current

                    /*for (int i = 0; i < 5; i++)
                        initTouchPointAllFinger[i] = touchPointAllFinger[i];*/

                    for (int i = 0; i < 5; i++)
                        lastTouchPointAllFinger[i] = touchPointAllFinger[i];
                }
                else
                {
                    if (!interactionOnWebview)
                        VirtualObjectMoveWithVirtualWindow();
                }



                //for click on ring finger to copy
                























                
                //if didn't click on hand, check if click in the air but on virtual surface
                if (!isClick)
                {
                    float timeout = Time.time - clickTime;
                    //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                    if (timeout > clickTimeout && (snapGestureController.GetSnapState() == SnapStates.SnapState || snapGestureController.GetSnapState() == SnapStates.SetFOR || snapGestureController.GetSnapState() == SnapStates.SemiSnapState))
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null && interactionManager.GetRetargetFingerPenetrateSurface(1) && interactionManager.GetWebviewCursorState(1) && lastTouchGesture == TouchGestures.None)
                        {
                            isInairClick = true;
                        }
                        else if(activeWebviewPrefab != null && !interactionManager.GetRetargetFingerPenetrateSurface(1) && interactionManager.GetWebviewCursorState(1) && isInairClick)
                        {    
                            Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                            //if cursor is inside webview
                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "In-air Click: " + cursorPosition;

                                isInairClick = false;
                                currentTouchGesture = TouchGestures.Tap;

                                clickTime = Time.time;
                            }
                        }
                    }
                }

                //UpdateTouchGestureDebugger();
            }           
            else if(interactionMethod == InteractionMethod.InAir)
            {
                RequestInteractionTouchStateUpdate();

                showObject(virtualObjectReference, false);
                showObject(virtualObjectReference.transform.Find("mark").gameObject, false);
                showObject(virtualObject, false);
                showObject(virtualObject.transform.Find("mark").gameObject, false);
                showObject(virtualWindow, false);
                showObject(interactionManager.GetRelocateInteractionHandMeshGameObject(), false);
                showObject(interactionManager.GetRelocateSurfaceHandMeshGameObject(), false);

                CheckPinchGesture();
                interactionManager.SetActiveVirtualSurfaceGameObject(pinchHitVirtualSurface);

                //UpdatePinchGestureDebugger();
            }
            else if (interactionMethod == InteractionMethod.DirectTouch)
            {
                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                if (timeout > clickTimeout)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null && interactionManager.GetRetargetFingerPenetrateSurface(1) && interactionManager.GetWebviewCursorState(1) && lastTouchGesture == TouchGestures.None)
                    {
                        isInairClick = true;
                    }
                    else if (activeWebviewPrefab != null && !interactionManager.GetRetargetFingerPenetrateSurface(1) && interactionManager.GetWebviewCursorState(1) && isInairClick)
                    {
                        Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                        //if cursor is inside webview
                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "In-air Click: " + cursorPosition;

                            isInairClick = false;
                            currentTouchGesture = TouchGestures.Tap;

                            clickTime = Time.time;
                        }
                    }
                }
            }
            else if (interactionMethod == InteractionMethod.HandRaycast)
            {
                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                if (timeout > clickTimeout)
                {
                    Vector3 hitPoint = handPointer.GetRayHitPoint();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        bool isRayPinch = handPointer.GetRayPinch();

                        if (isRayPinch && activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "ray pointer: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;
                        }
                    }
                }
            }
            else if (interactionMethod == InteractionMethod.HandRayPointer)
            {
                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = customRayPointer.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = false;
                if (interactionManager.IsTwoHandInteraction()) isNonDomRayPinch = customRayPointer.IsNonDomIndexFingerPinch();
                isNonDomFingerTouch = isNonDomRayPinch;

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandRayPinch();
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = customRayPointer.GetRayCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();
                       
                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();
                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = customRayPointer.GetRayCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.EyeGaze)
            {
                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazePointer.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = eyeGazePointer.GetGazeCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();
                            }
                        }
                    }
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.DirectPinch)
            {
                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = customHandPointer.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = false;
                if(interactionManager.IsTwoHandInteraction()) isNonDomRayPinch = customHandPointer.IsNonDomIndexFingerPinch();
                isNonDomFingerTouch = isNonDomRayPinch;

                float hoverDis = interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(1);

                if (isRayPinch && isNonDomRayPinch && hoverDis <= interactionManager.GetHoverDistanceThreshold())
                {
                    ScaleRotateInteractionByTwoHandDirectPinch();
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch && hoverDis <= interactionManager.GetHoverDistanceThreshold())
                {
                    Vector3 hitPoint = customHandPointer.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                




                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch && hoverDis <= interactionManager.GetHoverDistanceThreshold())
                {
                    Vector3 hitPoint = customHandPointer.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.CustomHandPointer)
            {
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = customHandPointer.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                //bool isNonDomRayPinch = customHandPointer.IsNonDomIndexFingerPinch();

                bool isNonDomRayPinch = false;

                if (isRayPinch && isNonDomRayPinch)
                {

                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = customHandPointer.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);                          

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                if (interactionManager.GetIsUpdateInteractionPadPosition())
                                {
                                    UpdateTouchPoints();
                                }
                                UpdateVirtualSurfaceFOR();
                            }
                        }
                    }
                }
                else if((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = customHandPointer.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }                    
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.HandMove)
            {
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazeHandPinch.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = eyeGazeHandPinch.IsNonDomIndexFingerPinch();
                isNonDomFingerTouch = isNonDomRayPinch;

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandPinch();
                }

                //for video demo
                else if (isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                //if (interactionManager.GetIsUpdateInteractionPadPosition())
                                //{
                                //UpdateTouchPoints();
                                //}
                                //UpdateVirtualSurfaceFOR();
                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                //activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                activeWebviewPrefab.WebView.Scroll(new Vector2(0, 0.25f));

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                
                /*else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                //if (interactionManager.GetIsUpdateInteractionPadPosition())
                                //{
                                    //UpdateTouchPoints();
                                //}
                                //UpdateVirtualSurfaceFOR();
                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }*/
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.EyeMoveHand)
            {           
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazeHandPinch.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = eyeGazeHandPinch.IsNonDomIndexFingerPinch();

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandPinch();
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = interactionManager.GetSmoothGazeCursor();

                    if (hitPoint.x != 10000)
                    {
                        
                        if (interactionManager.GetGazeMoveDis() < gazeMoveDisThreshold)
                        {
                            hitPoint = eyeGazeHandPinch.GetHandPinchCursor();
                        }

                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                




                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = interactionManager.GetSmoothGazeCursor();

                    if (hitPoint.x != 10000)
                    {

                        

                        //if gaze is not moving drasmatically, use hand pinch to adjust
                        




                        
                        if (interactionManager.GetGazeMoveDis() < gazeMoveDisThreshold)
                        {
                            hitPoint = eyeGazeHandPinch.GetHandPinchCursor();
                        }

                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.EyeHandJump)
            {
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazeHandPinch.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = eyeGazeHandPinch.IsNonDomIndexFingerPinch();

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandPinch();
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    Vector3 hitPoint = interactionManager.GetSmoothGazeCursor();                    

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);                           

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                startSelectCursor = cursorPosition;

                                UpdateSuccessTouch();

                                




                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (currentTouchGesture == TouchGestures.Tap)
                            {
                                cursor = startSelectCursor;
                            }

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                    handRetargetingController.SetGazeHandOffset(new Vector3(0,0,0));
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.EyeHandMove)
            {
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazeHandPinch.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = eyeGazeHandPinch.IsNonDomIndexFingerPinch();

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandPinch();
                    //handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                    snapGestureController.IsGazeDominate(false);
                    eyeGazeHandPinch.IsGazeDominate(false);
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    snapGestureController.IsGazeDominate(true);
                    eyeGazeHandPinch.IsGazeDominate(true);
                    handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                    Vector3 hitPoint = interactionManager.GetSmoothGazeCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {

                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                





                                startSelectCursor = cursorPosition;
                                gazeHandOffset = eyeGazeHandPinch.GetHandPinchCursor() - hitPoint;
                                handRetargetingController.SetGazeHandOffset(gazeHandOffset);
                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    snapGestureController.IsGazeDominate(false);
                    eyeGazeHandPinch.IsGazeDominate(false);

                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    snapGestureController.IsGazeDominate(true);
                    eyeGazeHandPinch.IsGazeDominate(true);

                    currentTouchGesture = TouchGestures.None;
                    //handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                }

                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.GazePinch)
            {
                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                bool isRayPinch = eyeGazeHandPinch.IsIndexFingerPinch();
                isFingerTouch = isRayPinch;

                bool isNonDomRayPinch = eyeGazeHandPinch.IsNonDomIndexFingerPinch();
                isNonDomFingerTouch = isNonDomRayPinch;

                if (isRayPinch && isNonDomRayPinch)
                {
                    ScaleRotateInteractionByTwoHandPinch();
                    //handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                    snapGestureController.IsGazeDominate(false);
                    eyeGazeHandPinch.IsGazeDominate(false);
                }
                else if (timeout > clickTimeout && isRayPinch && !isPrevFingerPinch)
                {
                    snapGestureController.IsGazeDominate(true);
                    eyeGazeHandPinch.IsGazeDominate(true);
                    handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                    Vector3 hitPoint = interactionManager.GetSmoothGazeCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {

                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                                gestureForDubug = "ray pointer: " + cursorPosition;

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Tap;

                                UpdateSuccessTouch();

                                





                                startSelectCursor = cursorPosition;
                                gazeHandOffset = eyeGazeHandPinch.GetHandPinchCursor() - hitPoint;
                                handRetargetingController.SetGazeHandOffset(gazeHandOffset);
                            }
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isRayPinch)
                {
                    snapGestureController.IsGazeDominate(false);
                    eyeGazeHandPinch.IsGazeDominate(false);

                    Vector3 hitPoint = eyeGazeHandPinch.GetHandPinchCursor();

                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);

                            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                            {
                                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                    cursorPosition = cursor;
                                else
                                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                                string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                                activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                                clickTime = Time.time;
                                currentTouchGesture = TouchGestures.Drag;
                            }
                        }
                    }
                }
                else
                {
                    snapGestureController.IsGazeDominate(true);
                    eyeGazeHandPinch.IsGazeDominate(true);

                    currentTouchGesture = TouchGestures.None;
                    handRetargetingController.SetGazeHandOffset(new Vector3(0, 0, 0));
                }
                isPrevFingerPinch = isRayPinch;
            }
            else if (interactionMethod == InteractionMethod.HandPoke)
            {
                float timeout = Time.time - clickTime;

                bool isPoke = customPoke.IsIndexFingerTouch();
                isFingerTouch = isPoke;

                //debugText.SetText("timeout: " + (timeout > clickTimeout) + "isPoke: " + isPoke + "isPrevTouch" + isPrevIndexFingerTouch);

                bool isNonDomPoke = customPoke.IsNonDomIndexFingerTouch();

                if (isPoke && isNonDomPoke)
                {
                    ScaleRotateInteractionByTwoHandPoke();
                }
                else if (timeout > clickTimeout && isPoke && !isPrevIndexFingerTouch)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);
                        //debugText.SetText("cursor: " + cursor.x + "," + cursor.y);

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "hand poke: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;

                            UpdateSuccessTouch();
                        }
                    }
                }
                else if ((currentTouchGesture == TouchGestures.Drag || currentTouchGesture == TouchGestures.Tap) && isPoke)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            string draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                            activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Drag;
                        }
                    }
                }
                else
                {
                    currentTouchGesture = TouchGestures.None;
                }

                isPrevIndexFingerTouch = isPoke;
            }
            else if (interactionMethod == InteractionMethod.HandPoke_V2)
            {
                float timeout = Time.time - clickTime;

                //customPseudoDirectPoke.IsIndexFingerTouch()
                bool isPoke = interactionManager.GetInteractionFingerTouchIntention();
                isFingerTouch = isPoke;

                if (timeout > clickTimeout && isPoke && !isPrevIndexFingerTouch)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        Vector2 cursor = interactionManager.GetWebCursorFarthestToSurface();

                        if (cursor.x == 10000)
                        {
                            cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);
                        }

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "hand poke: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;

                            UpdateSuccessTouch();
                        }
                    }
                }
                else
                {
                    //debugText.SetText("unselect");
                }

                isPrevIndexFingerTouch = isPoke;
            }
            else if (interactionMethod == InteractionMethod.HandPseudoDirectPoke)
            {
                float timeout = Time.time - clickTime;

                bool isPoke = customPseudoDirectPoke.IsIndexFingerTouch();
                //gestureForDubug = "Check In-air Click: " + interactionManager.GetRetargetFingerPenetrateSurface(1);
                //debugText.SetText(isPrevIndexFingerTouch + "," + isPoke);

                if (timeout > clickTimeout && isPoke && !isPrevIndexFingerTouch)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);
                        //debugText.SetText("cursor: " + cursor.x + "," + cursor.y);

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "hand poke: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;
                        }
                    }
                }
                else
                {
                    //debugText.SetText("unselect");
                }

                isPrevIndexFingerTouch = isPoke;
            }
            else if (interactionMethod == InteractionMethod.HandPseudoDirectPoke_V2)
            {
                





                





                if (!snapGestureController.GetIsDuringInteraction() || snapGestureController.GetIsFORGesture())
                {
                    InitTouchPointPositions();
                    return;
                }

                float timeout = Time.time - clickTime;

                //customPseudoDirectPoke.IsIndexFingerTouch()
                bool isPoke = interactionManager.GetInteractionFingerTouchIntention();
                isFingerTouch = isPoke;

                if (timeout > clickTimeout && isPoke && !isPrevIndexFingerTouch)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {                      
                        Vector2 cursor = interactionManager.GetWebCursorFarthestToSurface();

                        if (cursor.x == 10000)
                        {
                            cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);
                        }

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "hand poke: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;

                            UpdateSuccessTouch();

                            if (interactionManager.GetIsUpdateInteractionPadPosition())
                            {
                                UpdateTouchPoints();
                            }
                            UpdateVirtualSurfaceFOR();
                        }
                    }
                }
                else
                {
                    //debugText.SetText("unselect");
                }

                isPrevIndexFingerTouch = isPoke;
            }
            else if (interactionMethod == InteractionMethod.HandIndirectTouch)
            {
                debugText.SetText(snapGestureController.GetSnapState().ToString() + "," + indirectCursorController.GetIndirectCursor().activeSelf.ToString());
                if (snapGestureController.GetSnapState() == SnapStates.SnapState && !indirectCursorController.GetIndirectCursor().activeSelf)
                {
                    indirectCursorController.EnableIndirectCursor();
                    indirectCursorController.InitIndirectCursor();
                }
                if (snapGestureController.GetSnapState() == SnapStates.None && indirectCursorController.GetIndirectCursor().activeSelf)
                {
                    indirectCursorController.DisableIndirectCursor();
                }

                float timeout = Time.time - clickTime;

                //customPseudoDirectPoke.IsIndexFingerTouch()
                bool isPoke = touchStateAllFinger[1];
                isFingerTouch = isPoke;

                if (timeout > clickTimeout && isPoke && !isPrevIndexFingerTouch && indirectCursorController.GetIndirectCursor().activeSelf)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        Vector2 cursor = interactionManager.GetIndirectCursorRelativeCoordinate();

                        if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                        {
                            if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                                cursorPosition = cursor;
                            else
                                cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                            activeWebviewPrefab.WebView.Click(cursorPosition, false);
                            gestureForDubug = "hand poke: " + cursorPosition;

                            clickTime = Time.time;
                            currentTouchGesture = TouchGestures.Tap;

                            UpdateSuccessTouch();

                        }
                    }
                }
                else if (timeout > clickTimeout && isPoke && isPrevIndexFingerTouch && indirectCursorController.GetIndirectCursor().activeSelf)
                {
                    float dx = interactionManager.GetMovingX();
                    float dy = interactionManager.GetMovingY();
                    indirectCursorController.UpdateIndirectCursor(dx, dy, Time.deltaTime);
                }
                else
                {
                    //debugText.SetText("unselect");
                }

                isPrevIndexFingerTouch = isPoke;

                /*isClick = false;

                showPointerRay(pointerRay, pinchPosition, pinchRayDirection, false);
                if (interactionManager.GetActiveVirtualSurfaceGameObject() != null)
                {
                    initVirtualSurfaceScale = virtualSurface.transform.lossyScale.x;
                    task = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>().task;

                    if (task.ToString() != lastTask)
                    {
                        lastTask = curTask;
                        curTask = task.ToString();
                    }
                }

                if (!snapGestureController.GetPinchState())
                {
                    UpdateTouchPointAllFinger();

                    UpdateTouchStateSequence();
                    if (touchStateSequence.Count >= 3) GestureDetection();

                    for (int i = 0; i < 5; i++)
                        lastTouchPointAllFinger[i] = touchPointAllFinger[i];
                }
                else
                {
                    if (!interactionOnWebview)
                        VirtualObjectMoveWithVirtualWindow();
                }*/

                //UpdateTouchGestureDebugger();
            }
        }

        















        if(!isFingerTouch && !isNonDomFingerTouch)
        {
            NoneInteractionOnWebview();
        }

        if (userID != -100)
        {
            UpdateStudyTouchData();
        }
        else
        {
            if (interactionManager.GetActiveVirtualSurfaceGameObject() != null)
            {
                OculusWebViewDemo webview = interactionManager.GetActiveVirtualSurfaceGameObject().transform.Find("OculusWebViewDemo").GetComponent<OculusWebViewDemo>();
                userID = webview.P_ID;
                sessionID = webview.S_ID;
                conditionID = webview.C_ID;
            }
        }
    }

    private void UpdateTouchPoints()
    {
        Vector3 touchPoint = new Vector3(10000, 10000, 10000);

        if(interactionMethod == InteractionMethod.CustomHandPointer)
        {
            touchPoint = interactionManager.GetHandPinchArrowOffsetGameObject().transform.position;
        }
        else
        {
            touchPoint = interactionManager.GetTouchPointfarthestToSurface();
        }

        if (touchPoint.x != 10000)
        {
            //check previous frames, get the point that are farthest to the interaction pad when penetrating
            //if no penetration, then it's the one with smallest hover distance
            touchPointPositions.Enqueue(touchPoint);
        }
        else
        {
            //if there's not enough points yet, just choose the current position of the interaction finger
            touchPointPositions.Enqueue(interactionManager.GetInteractionFingerTipPosition(1));
        }

        if (touchPointPositions.Count > frameWindow)
        {
            touchPointPositions.Dequeue();
        }

        if (interactionManager.GetIsVisualizeTouchPoints())
        {
            int idx = 0;

            foreach (Vector3 pos in touchPointPositions)
            {
                touchPoints[idx].transform.position = pos;
                idx++;
            }
        }
    }

    private void UpdateVirtualSurfaceFOR()
    {
        //Vector3 offset = interactionManager.GetInteractionFingerTipPosition(1) - interactionManager.GetVirtualSurfaceFORCursorPosition(1);

        if (touchPointPositions.Count >= 5)
        {
            if (interactionManager.GetIsUpdateInteractionPadPosition())
            {
                //Vector3 middlePoint = CalculateMiddlePoint(touchPointPositions);
                //Vector3 offset = CalculateOffset(middlePoint);

                Vector3 nearestPoint = CalculateNearestPoint(touchPointPositions);
                Vector3 offset = CalculateOffset(nearestPoint);

                if (offset.x != 10000 && Mathf.Abs(offset.magnitude) < 0.1f)
                {
                    //virtualSurfaceFOR.transform.position += offset;
                    virtualSurfaceAnchor.transform.position += offset;

                    //virtualSurfaceAnchor.transform.position = Vector3.MoveTowards(virtualSurfaceAnchor.transform.position, virtualSurfaceAnchor.transform.position + offset, virtualSurfaceFORPositionSpeed * Time.deltaTime);
                    handRetargetingController.UpdateSurfaceReferencePosition(offset);
                }
                
            }

            if (interactionManager.GetIsUpdateInteractionPadRotation())
            {
                Vector3 normal = CalculateSurfaceBasedOnTouchPoints();
                //RotateGameObjectToMatchNormal(virtualSurfaceFOR, normal);
                RotateGameObjectToMatchNormal(virtualSurfaceAnchor, normal);
                handRetargetingController.ChangeSurfaceReferenceRotation(virtualSurfaceAnchor.transform.rotation);
            }
        }
    }

    private Vector3 CalculateOffset(Vector3 point)
    {
        RaycastHit hit;

        Vector3 offset = new Vector3(10000f,10000f,10000f);

        Vector3 virtualSurfaceFORNormal = virtualSurfaceFOR.transform.forward;

        if (Physics.Raycast(point, virtualSurfaceFORNormal, out hit, 1.0f, LayerMask.GetMask("VirtualSurfaceFORLayer")))
        {
            if (hit.collider.gameObject == virtualSurfaceFOR)
            {
                offset = (point - hit.point) * 0.5f;
            }
        }
        else if (Physics.Raycast(point, - virtualSurfaceFORNormal, out hit, 1.0f, LayerMask.GetMask("VirtualSurfaceFORLayer")))
        {
            if (hit.collider.gameObject == virtualSurfaceFOR)
            {
                offset = point - hit.point;
            }
        }

        return offset;
    }

    private Vector3 CalculateMiddlePoint(Queue<Vector3> pointsQueue)
    {
        Vector3 sum = Vector3.zero;

        // If the queue is empty, return zero
        if (pointsQueue.Count == 0)
        {
            Vector3 point = new Vector3(10000, 10000, 10000);
            return point;
        }

        // Sum all the points in the queue
        foreach (Vector3 point in pointsQueue)
        {
            sum += point;
        }

        // Divide by the number of points to get the average (middle point)
        return sum / pointsQueue.Count;
    }

    private Vector3 CalculateNearestPoint(Queue<Vector3> pointsQueue)
    {
        Vector3 point = new Vector3(10000, 10000, 10000);

        if (pointsQueue.Count < 1)
            return point;

        // Convert the queues to arrays for easier index access
        Vector3[] touchPointArray = pointsQueue.ToArray();

        // Find the smallest value in movingNormal and its index
        float min_value = float.MaxValue;
        int min_index = 10000;

        for (int i = 0; i < touchPointArray.Length; i++)
        {
            float dis = CalculateOffset(touchPointArray[i]).magnitude;
            if (dis < min_value)
            {
                min_value = dis;
                min_index = i;
            }
        }

        return touchPointArray[min_index];
    }

    private Vector3 CalculateSurfaceBasedOnTouchPoints()
    {
        List<Vector3> points = new List<Vector3>(touchPointPositions);

        // Step 1: Compute the centroid
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        // Step 2: Compute the covariance matrix
        float xx = 0f, xy = 0f, xz = 0f;
        float yy = 0f, yz = 0f, zz = 0f;

        foreach (var point in points)
        {
            Vector3 relativePoint = point - centroid;
            xx += relativePoint.x * relativePoint.x;
            xy += relativePoint.x * relativePoint.y;
            xz += relativePoint.x * relativePoint.z;
            yy += relativePoint.y * relativePoint.y;
            yz += relativePoint.y * relativePoint.z;
            zz += relativePoint.z * relativePoint.z;
        }

        float[,] covarianceMatrix = new float[3, 3] {
            { xx, xy, xz },
            { xy, yy, yz },
            { xz, yz, zz }
        };

        // Step 3: Estimate a normal from the covariance rows.
        Vector3 normal = GetNormalFromCovarianceMatrix(covarianceMatrix);
        return normal;
    }

    Vector3 GetNormalFromCovarianceMatrix(float[,] matrix)
    {
        // Estimate a normal from the first two covariance rows.
        // This cross-product heuristic falls back to the third row when the cross product is zero.

        Vector3 row1 = new Vector3(matrix[0, 0], matrix[0, 1], matrix[0, 2]);
        Vector3 row2 = new Vector3(matrix[1, 0], matrix[1, 1], matrix[1, 2]);

        Vector3 normal = Vector3.Cross(row1, row2).normalized;

        if (normal == Vector3.zero)
        {
            normal = new Vector3(matrix[2, 0], matrix[2, 1], matrix[2, 2]).normalized;
        }

        return normal;
    }

    private void RotateGameObjectToMatchNormal(GameObject obj, Vector3 normal)
    {
        // Step 1: Get the current normal of the GameObject (assuming it's transform.up or any other direction)
        Vector3 currentNormal = obj.transform.forward;
        Vector3 targetNormal = normal;

        // Step 2: Calculate the rotation needed to align currentNormal with targetNormal around transform.right
        // Project the target normal onto the plane defined by the GameObject's right axis
        Vector3 projectedTargetNormal = Vector3.ProjectOnPlane(targetNormal, obj.transform.right).normalized;
        Vector3 projectedCurrentNormal = Vector3.ProjectOnPlane(currentNormal, obj.transform.right).normalized;

        // Step 3: Calculate the angle between the projected normals
        float angle = Vector3.SignedAngle(projectedCurrentNormal, projectedTargetNormal, obj.transform.right);

        if (Mathf.Abs(angle) < 30)
        {
            // Step 4: Rotate the GameObject around its transform.right axis
            obj.transform.Rotate(obj.transform.right, angle, Space.World);
            //obj.transform.rotation = Quaternion.RotateTowards(obj.transform.rotation, Quaternion.AngleAxis(angle, obj.transform.right), virtualSurfaceFORRotationSpeed * Time.deltaTime);
        }

        
    }

    private void InitTouchPoints()
    {
        Instantiate(new GameObject("TouchPoints"));
        GameObject.Find("TouchPoints").transform.position = new Vector3(10000, 10000, 10000);

        for (int i = 0; i < frameWindow; i++)
        {
            touchPoints[i] = Instantiate(Resources.Load("Prefabs/TouchPointPrefab") as GameObject);
            touchPoints[i].transform.parent = GameObject.Find("TouchPoints").transform;
        }
    }

    private void InitTouchPointPositions()
    {
        for (int i = 0; i < frameWindow; i++)
        {
            touchPoints[i].transform.localPosition = new Vector3(0,0,0);
        }

        touchPointPositions.Clear();
    }

    public void selectOnWebview()
    {
        Vector3 hitPoint = customPoke.GetTouchPoint();

        if (hitPoint.x != 10000)
        {
            var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

            if (activeWebviewPrefab != null)
            {
                Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                    cursorPosition = cursor;
                else
                    cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                gestureForDubug = "hand poke: " + cursorPosition;

                currentTouchGesture = TouchGestures.Tap;
            }
        }
    }

    private bool isSurfaceHandPinch()
    {
        float dis = interactionManager.GetSurfaceFingerTipDistance(0, 1);
        return (dis <= pinchDisThreshold);     
    }

    private void InitAndMoveDragCusor()
    {
        GameObject dragCursor = GameObject.Find("DragCusor");
        if (dragCursor == null)
        {
            dragCursor = Instantiate(Resources.Load("Prefabs/DragCursorPrefab") as GameObject);
        }
        else
        {
            dragCursor.transform.position = interactionManager.GetRetargetInteractionFingerTipPosition(1);
        }
    }

    private bool CheckSurfaceFingerTouch(int interactionFinger, int surfaceFinger)
    {
        bool isTouching = false;
        Vector3 interactionFingerPos = interactionManager.GetInteractionFingerTipPosition(interactionFinger);
        Vector3 surfaceFingerPos = interactionManager.GetSurfaceFingerTipPosition(surfaceFinger);
        float dis = Vector3.Distance(interactionFingerPos, surfaceFingerPos);

        if(dis <= surfaceFingerTouchThreshold)
            isTouching = true;

        return isTouching;
    }

    private void CheckPinchGesture()
    {
        float dis = interactionManager.GetInteractionFingerTipDistance(0, 1);

        if (dis <= pinchDisThreshold)
        {
            //is pinching
            pinchPosition = interactionManager.GetInteractionFingerTipPosition(1);
            pinchRayDirection = pinchPosition - ((interactionManager.GetInteractionHandFirstKnucklePosition(0) + interactionManager.GetInteractionHandFirstKnucklePosition(1)) / 2);
            //pinchRayDirection = pinchPosition - interactionManager.GetCenterEyeGameObject().transform.position;

            showPointerRay(pointerRay, pinchPosition, pinchRayDirection, true);
            UpdateRayColor(UnityEngine.Color.green);

            if (!isPinching)
            {

                PinchInteraction(TouchGestures.Tap);

                initPinchPosition = interactionManager.GetInteractionFingerTipPosition(1);
                initPinchTime = Time.time;
                pinchDwellingTime = 0.0f;
                pinchMovingDistance = 0.0f;
            }
            isPinching = true;

            pinchDwellingTime = Time.time - initPinchTime;

            if (!Single.IsInfinity(lastPinchPosition.x))
                pinchMovingDistance += Vector3.Distance(pinchPosition, lastPinchPosition);

            if (pinchDwellingTime > dwellingTimeThreshold)
            {
                if (pinchMovingDistance <= movingDistanceThreshold)
                {
                    //dwelling
                }
                else
                {
                    //movings
                    PinchInteraction(TouchGestures.Drag);
                }
            }

            lastPinchPosition = pinchPosition;
            lastPinchWebviewCursor = interactionManager.GetPinchWebviewCursorPosition();
        }
        else
        {
            pinchPosition = interactionManager.GetInteractionFingerTipPosition(1);
            pinchRayDirection = pinchPosition - ((interactionManager.GetInteractionHandFirstKnucklePosition(0) + interactionManager.GetInteractionHandFirstKnucklePosition(1)) / 2);

            showPointerRay(pointerRay, pinchPosition, pinchRayDirection, true);
            UpdateRayColor(UnityEngine.Color.red);

            NoneInteractionOnWebview();
            TouchInteraction(TouchGestures.None);
            PinchInteraction(TouchGestures.None);

            if (isPinching)
            {
                initPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            }
            isPinching = false;
            pinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            lastPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        }

        //debugPinching = "isPinching: " + isPinching + ", pinchDis: " + dis + ", dewelling time: " + pinchDwellingTime + ", moving dis: " + pinchMovingDistance;
    }

    private void PinchInteraction(TouchGestures pinchGesture)
    {
        lastPinchGesture = curPinchGesture;
        curPinchGesture = pinchGesture;

        debugPinching = "PinchInteraction: " + pinchGesture;

        //check if the ray from eye to pinching finger hit the virtual surface/web
        ActivateVirtualSurfaceByPinching();

        if (pinchHitVirtualSurface != null)
        {
            if (curPinchGesture == TouchGestures.Tap)
            {
                if (interactionOnWebview)
                    ClickInteractionOnWebviewByPinching();
            }
            else if (curPinchGesture == TouchGestures.Drag)
            {
                if (interactionOnWebview)
                {
                    if (task == OculusWebViewDemo.Tasks.Scroll)
                    {
                        DragToScrollInteractionOnWebviewByPinching();
                    }
                    else if (task == OculusWebViewDemo.Tasks.Docking)
                    {
                        ScrollScaleRotateInteractionOnWebviewByPinching();
                    }
                    else if(task == OculusWebViewDemo.Tasks.Sketching)
                    {
                        DraggingInteractionOnWebviewByPinching();
                    }
                    else
                    {
                        DragToScrollInteractionOnWebviewByPinching();
                    }
                }
            }
            else
            {
                NoneInteractionOnWebview();
            }
        }
        else
        {
            NoneInteractionOnWebview();
        }
    }

    private void ClickInteractionOnWebviewByPinching()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();
        if (activeWebviewPrefab != null && interactionManager.GetPinchWebviewCursorState())
        {
            Vector2 cursor = interactionManager.GetPinchWebviewCursorRelativeCoordinate();

            //if cursor is inside webview
            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
            {
                cursorPosition = cursor;
                activeWebviewPrefab.WebView.Click(cursorPosition, false);
                gestureForDubug = "Click by pinching: " + cursorPosition;
            }
        }
    }

    private void DragToScrollInteractionOnWebviewByPinching()
    {
        //debugPinching = "DragToScrollInteractionOnWebviewByPinching";
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        //debugPinching = "activeWebviewPrefab: " + activeWebviewPrefab + ", GetPinchWebviewCursorState: " + interactionManager.GetPinchWebviewCursorState()
            //+ ", lastPinchWebviewCursor: " + lastPinchWebviewCursor;

        if (activeWebviewPrefab != null && interactionManager.GetPinchWebviewCursorState())
        {
            if (!Single.IsInfinity(lastPinchWebviewCursor.x))
            {
                Vector3 curPinchWebviewCursor = interactionManager.GetPinchWebviewCursorPosition();
                Vector3 moveDir = curPinchWebviewCursor - lastPinchWebviewCursor;
                Vector3 surfaceZDir = interactionManager.GetActiveWebviewGameObject().transform.up;
                float angle = Vector3.Angle(moveDir, surfaceZDir);
                float dis = Vector3.Distance(curPinchWebviewCursor, lastPinchWebviewCursor);
                float portionOfVirtualSurface = dis / interactionManager.GetActiveWebviewGameObject().transform.lossyScale.y;

                //gestureForDubug = "finger movedis: " + dis + ", portionOfHandSurface: " + portionOfHandSurface + ", finger movedis on window: " + disOnVirtualWindow + ", portionOfVirtualSurface: " + portionOfVirtualSurface;
                debugPinching = "finger movedis: " + dis + ", angle: " + angle + ", portionOfVirtualSurface: " + portionOfVirtualSurface;

                //angle<90, scroll up, otherwise scroll down
                if (angle < 90f)
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, -normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((-0.5).ToString());
                    activeWebviewPrefab.WebView.PostMessage(portionOfVirtualSurface.ToString());
                    gestureForDubug = "Drag to scroll up by pinching: " + portionOfVirtualSurface;
                }
                else
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((0.5).ToString());
                    activeWebviewPrefab.WebView.PostMessage((-portionOfVirtualSurface).ToString());
                    gestureForDubug = "Drag to scroll down by pinching: " + portionOfVirtualSurface;
                }
            }
            currentTouchGesture = TouchGestures.Scroll;
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
        }
    }

    private void DraggingInteractionOnWebviewByPinching()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null && interactionManager.GetPinchWebviewCursorState())
        {
            Vector2 cursor = interactionManager.GetPinchWebviewCursorRelativeCoordinate();

            //if cursor is inside webview
            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
            {
                string draggingMessage = SendPositionRotationScaleData(0, cursor.x, cursor.y, 0, 0);
                activeWebviewPrefab.WebView.PostMessage(draggingMessage);
                gestureForDubug = "Dragging: " + cursor;
                currentTouchGesture = TouchGestures.Drag;
            }
            else
            {
                NoneInteractionOnWebview();
                currentTouchGesture = TouchGestures.None;
            }
        }
        else
        {
            NoneInteractionOnWebview();
            currentTouchGesture = TouchGestures.None;
        }
    }

    private void ScrollScaleRotateInteractionOnWebviewByPinching()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null && interactionManager.GetPinchWebviewCursorState())
        {
        }
    }

    private void ActivateVirtualSurfaceByPinching()
    {
        GameObject hitSurface1 = GetRaycastHitObject(pinchPosition, 1.0f * pinchRayDirection, "VirtualSurfaceLayer");
        GameObject hitSurface2 = GetRaycastHitObject(pinchPosition, -1.0f * pinchRayDirection, "VirtualSurfaceLayer");

        if (hitSurface1 != null || hitSurface2 != null)
        {
            if (hitSurface1 != null) pinchHitVirtualSurface = hitSurface1;
            else if (hitSurface2 != null) pinchHitVirtualSurface = hitSurface2;

            interactionManager.SetActiveVirtualSurfaceGameObject(pinchHitVirtualSurface);
        }
        else
        {
            pinchHitVirtualSurface = null;
        }
    }

    private void showPointerRay(GameObject obj, Vector3 startPoint, Vector3 rayDirection, bool isshow)
    {
        








    }

    private Vector3 CalculateEndPoint(Vector3 startPos, Vector3 direction, float dis)
    {
        Vector3 endPos = startPos + direction * dis;
        return endPos;
    }

    private void DrawRay(Vector3 startPos, Vector3 destPos)
    {
        pointerRay.transform.position = startPos;
        hitRay.material = CreateMaterial(UnityEngine.Color.red, "ray_material");

        // set width of the renderer
        hitRay.startWidth = 0.01f;
        hitRay.endWidth = 0.005f;

        // set the position
        hitRay.SetPosition(0, startPos);
        hitRay.SetPosition(1, destPos);

        drawHitRay = true;
    }

    private void UpdateRayColor(UnityEngine.Color rayColor)
    {
        hitRay.material.color = rayColor;
    }

    private void UpdateRay(Vector3 startPos, Vector3 destPos)
    {
        hitRay.SetPosition(0, startPos);
        hitRay.SetPosition(1, destPos);
    }

    public Material CreateMaterial(UnityEngine.Color color, string name, string shaderName = "Standard")
    {
        Material material = new Material(Shader.Find(shaderName));
        material.name = name;
        material.color = color;
        material.SetColor("rayColor", color);
        return material;
    }

    private void UpdateTouchGestureDebugger()
    {
        







        







        if (touchStateSequence.Count >= 3)
        {
            debugText.SetText(
                //"thumb: " + touchStateAllFinger[0].ToString() + "\n" +
                //"index: " + touchStateAllFinger[1].ToString() + "\n" +
                //"middle: " + touchStateAllFinger[2].ToString() + "\n" +
                //"ring: " + touchStateAllFinger[3].ToString() + "\n" +
                //"pinky: " + touchStateAllFinger[4].ToString().ToString() + "\n\n" +
                "touch state: " + "(" + touchStateSequence[touchStateSequence.Count - 3][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 3][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 3][2].ToString() + ")" +
                ", " +
                "(" + touchStateSequence[touchStateSequence.Count - 2][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 2][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 2][2].ToString() + ")" +
                ", " +
                "(" + touchStateSequence[touchStateSequence.Count - 1][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 1][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 1][2].ToString() + ")" + "\n" +
                //"last gesture: " + lastTouchGesture + "\n" +
                //"current gesture: " + currentTouchGesture + "\n" +
                //"debug touch: " + debugTouching + "\n" +
                //"debug touch: " + interactionManager.GetFingerTipToSurfaceDistance(1) + "\n" +
                "debug gesture: " + gestureForDubug + "\n" +
                "webview cursor position: " + interactionManager.GetWebviewCursorRelativeCoordinate(1) + "\n" +
                //"preCheck touch: " + preCheckTouchDebug + "\n" +
                "test: " + storageText +"\n" +
                ""
            );

            /*Debug.Log("(" + touchStateSequence[touchStateSequence.Count - 3][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 3][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 3][2].ToString() + ")" +
                ", " +
                "(" + touchStateSequence[touchStateSequence.Count - 2][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 2][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 2][2].ToString() + ")" +
                ", " +
                "(" + touchStateSequence[touchStateSequence.Count - 1][0].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 1][1].ToString() + "," +
                touchStateSequence[touchStateSequence.Count - 1][2].ToString() + ")");
            Debug.Log(currentTouchGesture);
            Debug.Log("--------------------");*/
        }
        else
        {
            debugText.SetText(
                "penetrate surface: " + interactionManager.GetRetargetFingerPenetrateSurface(1) + "\n" +
                "webview cursor position: " + interactionManager.GetWebviewCursorRelativeCoordinate(1) + "\n" +
                ""
            );
        }
    }

    private void UpdatePinchGestureDebugger()
    {
        debugText.SetText(
            "debug gesture: " + gestureForDubug + "\n" +
            "webview cursor position: " + interactionManager.GetPinchWebviewCursorRelativeCoordinate() + "\n" +
            debugPinching
        );
    }

    private bool PrecheckTouchState(int idx)
    {
        bool isTouch = true;

        //check fingertip hover distance
        if (interactionManager.GetFingerTipToSurfaceDistance(idx) > fingertipHoverThreshold) isTouch = false;

        //check finger rotation
        //if(GetFingerRotation(idx) > fingerRotationThreshold[idx]) isTouch = false;

        //check distance between thumb tip and middle finger second knuckle for pointing gesture
        






        //check if fingertip can be projected inside the hand surface
        //if (!interactionManager.GetSurfaceCursorState(idx) || !interactionManager.GetVirtualWindowCursorState(idx) || !interactionManager.GetSurfaceHandCursorState(idx)) isTouch = false;

        //return true;
        return isTouch;
    }

    private float GetFingerRotation(int idx)
    {
        float fingerRotation = 0.0f;

        Vector3 vect2 = interactionManager.GetInteractionHandSecondKnucklePosition(idx) - interactionManager.GetInteractionHandFirstKnucklePosition(idx);
        Vector3 vect3 = interactionManager.GetInteractionHandThirdKnucklePosition(idx) - interactionManager.GetInteractionHandSecondKnucklePosition(idx);
        Vector3 vect4 = interactionManager.GetInteractionHandForthKnucklePosition(idx) - interactionManager.GetInteractionHandThirdKnucklePosition(idx);

        fingerRotation += Vector3.Angle(vect3, vect4);
        fingerRotation += Vector3.Angle(vect2, vect3);

        return fingerRotation;
    }

    private void InitAll()
    {
        //defaultColor = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
        //touchColor = new UnityEngine.Color(0.86f, 0.25f, 0.25f);

        touchClassifier.InitConfidenceDictionary();


        cursorPosition = Vector2.zero;

        pinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        lastPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        initPinchPosition = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        lastPinchWebviewCursor = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        UpdateTouchStateTDM();
        touchStateSequence.Add(touchStateTDM);

        InitBooleanList(lastTouchStateAllFinger);
        InitBooleanList(touchStateAllFinger);
        InitVector3List(initTouchPointAllFinger);
        InitVector3List(lastTouchPointAllFinger);
        InitVector3List(touchPointAllFinger);

        InitBooleanList(touchStateAllFingerTDM);
        InitBooleanList(dwellingStateAllFingerTDM);
        InitBooleanList(movingStateAllFingerTDM);
    }

    private void InitVector3List(Vector3[] list)
    {
        Vector3 temp = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        for (int i = 0; i < list.Length; i++)
        {
            list[i] = temp;
        }
    }

    private void InitBooleanList(bool[] list)
    {
        for (int i = 0; i < list.Length; i++)
        {
            list[i] = false;
        }
    }

    private void InitTouchStateTDM()
    {
        n_touch = 0;
        n_dwelling = 0;
        n_moving = 0;
    }

    private void UpdateTouchStateTDM()
    {
        
        touchStateTDM[0] = n_touch;
        touchStateTDM[1] = n_dwelling;
        touchStateTDM[2] = n_moving;
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

    private void TouchInteraction(TouchGestures gesture)
    {
        lastTouchGesture = currentTouchGesture;
        currentTouchGesture = gesture;

        //single finger
        if (currentTouchGesture == TouchGestures.Tap && (snapGestureController.GetSnapState() == SnapStates.SnapState || snapGestureController.GetSnapState() == SnapStates.SemiSnapState))
        {
            currentFingerSpeed = 0.0f;
            isScroll = false;

            if (interactionOnWebview)
                ClickInteractionOnWebview();
            else
            {
                TapInteraction();
                VirtualObjectManipulation();
            }
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("Tap");
        }
        //single finger
        else if (currentTouchGesture == TouchGestures.Drag && (snapGestureController.GetSnapState() == SnapStates.SnapState || snapGestureController.GetSnapState() == SnapStates.SemiSnapState))
        {
            currentFingerSpeed = 0.0f;
            isScroll = false;

            if (interactionOnWebview)
            {
                if (task == OculusWebViewDemo.Tasks.Scroll)
                {
                    DragToScrollInteractionOnWebview();
                }
                else if (task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching || task == OculusWebViewDemo.Tasks.Map)
                {
                    DraggingInteractionOnWebview();

                    if (interactionMethod == InteractionMethod.HandIndirectTouch)
                    {
                        
                        /*snapGestureController.ShowObject(interactionHand, true);
                        snapGestureController.ShowObject(surfaceHand, true);
                        snapGestureController.ShowObject(relocateInteractionHandMesh, false);
                        snapGestureController.ShowObject(relocateSurfaceHandMesh, false);*/
                    }
                }
                else if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    SelectInteractionOnWebview();
                    DraggingInteractionOnWebview();
                }
                else
                {
                    if (touchStateAllFinger[0])
                        ThumbDragToNavigateOnWebview();
                    else if (touchStateAllFinger[1])
                        SelectInteractionOnWebview();
                }
               
            }
            else
            {
                DragInteraction();
                VirtualObjectManipulation();
            }
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("Drag");
        }
        //double fingers
        else if (currentTouchGesture == TouchGestures.Scroll)
        {
            currentFingerSpeed = 0.0f;
            isScroll = false;

            if (interactionOnWebview)
            {
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    SelectInteractionOnWebview();
                    ScrollScaleRotateInteractionOnWebview();
                    //DraggingInteractionOnWebview();

                }
                else
                {
                    ScrollScaleRotateInteractionOnWebview();
                }
            }
            else
            {
                ScrollScaleRotateInteraction();
                VirtualObjectManipulation();
            }
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("Scroll");
        }
        else if(currentTouchGesture == TouchGestures.None)
        {
            //gestureForDubug = "none gesture";
            NoneInteractionOnWebview();
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("None");
        }
    }

    //replace TouchInteraction with TouchInteractionOnVirtualSurface if you want to test it on virtual surface/window/objects
    private void TouchInteractionOnVirtualSurface(TouchGestures gesture)
    {
        lastTouchGesture = currentTouchGesture;
        currentTouchGesture = gesture;

        if (currentTouchGesture == TouchGestures.Tap)
        {
            TapInteraction();
            VirtualObjectManipulation();
        }
        else if (currentTouchGesture == TouchGestures.Drag)
        {
            DragInteraction();
            VirtualObjectManipulation();
        }
        else if (currentTouchGesture == TouchGestures.Scroll)
        {
            ScrollScaleRotateInteraction();
            VirtualObjectManipulation();
        }
        else
        {
            //activeObject = null;
        }
    }

    private void changeOpacity(GameObject obj, float alpha)
    {
        var color = obj.GetComponent<Renderer>().material.color;
        var newColor = new UnityEngine.Color(color.r, color.g, color.b, alpha);
        obj.GetComponent<Renderer>().material.color = newColor;
    }

    private void changeColor(GameObject obj, float r, float g, float b, float a)
    {
        var newColor = new UnityEngine.Color(r, g, b, a);
        obj.GetComponent<Renderer>().material.color = newColor;
    }

    private void flipColor(GameObject obj)
    {
        var color = obj.GetComponent<Renderer>().material.color;
        UnityEngine.Color newColor;

        if (color.r == 0)
        {
            newColor = new UnityEngine.Color(255, 0, 0);
        }
        else
        {
            newColor = new UnityEngine.Color(0, 255, 0);
        }
        //isFlip = !isFlip;

        obj.GetComponent<Renderer>().material.color = newColor;
    }

    private void ActivateObject(GameObject obj, bool isActive)
    {
        if(isActive)
            //green
            changeColor(obj, 0, 255, 0, 1);
        else
            //red
            changeColor(obj, 255, 0, 0, 1);
    }

    private void showObject(GameObject obj, bool isShow)
    {
        obj.GetComponent<Renderer>().enabled = isShow;
    }

    private void VirtualObjectManipulation()
    {
        virtualObject.transform.position = virtualObjectReference.transform.position;
        //virtualObject.transform.rotation = virtualObjectReference.transform.rotation;

        virtualObject.transform.localScale = virtualObjectReference.transform.lossyScale;

        virtualObject.GetComponent<Renderer>().material.color = virtualObjectReference.GetComponent<Renderer>().material.color;


        showObject(virtualObjectReference, false);
        showObject(virtualObjectReference.transform.Find("mark").gameObject, false);
        showObject(virtualObject, true);
        showObject(virtualObject.transform.Find("mark").gameObject, true);
    }

    private void VirtualObjectMoveWithVirtualWindow()
    {
        virtualObject.transform.position = virtualObjectReference.transform.position;
        showObject(virtualObjectReference, false);
        showObject(virtualObjectReference.transform.Find("mark").gameObject, false);
        showObject(virtualObject, true);
        showObject(virtualObject.transform.Find("mark").gameObject, true);
    }

    private void NoneInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();
        if (activeWebviewPrefab != null && (task == OculusWebViewDemo.Tasks.Sketching || task == OculusWebViewDemo.Tasks.Docking))
        {
            string message = SendPositionRotationScaleData(-1, -1, -1, 0, 0);

            if (activeWebviewPrefab.WebView != null)
            {
                activeWebviewPrefab.WebView.PostMessage(message);
            }

            gestureForDubug = "none interaction: " + message;
        }
        if (activeWebviewPrefab != null && task == OculusWebViewDemo.Tasks.Quiz && interactionManager.GetVirtualWindowCursorState(1))
        {
            Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

            //if cursor is inside webview
            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
            {
                if (isDrag)
                {
                    //position.x, position.y, isCopy, isPaste
                    string draggingMSG = cursor.x.ToString() + "," + cursor.y.ToString() + ",0,1";
                    activeWebviewPrefab.WebView.PostMessage(draggingMSG);
                    gestureForDubug = "paste";
                    isDrag = false;
                }

                isClick = false;
                isSelect = false;
                isCopy = false;
                isHitVirtualWindowBoundary = false;

                /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                    Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
                };*/
            }
        }
        if (activeWebviewPrefab != null && task == OculusWebViewDemo.Tasks.Canvas && interactionManager.GetVirtualWindowCursorState(1))
        {

            string message = "";
            if (isDrag)
            {
                Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                //if cursor is inside webview
                if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                {
                    //position.x, position.y, isCopy, isPaste
                    message = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, 0, 0, 0, 1);
                    gestureForDubug = "paste";
                    isDrag = false;
                }
            }
            else
            {
                message = SendPositionRotationScaleDragPasteData(-1, -1, 0, 0, 0, 0);
                gestureForDubug = "none interaction";
            }
            activeWebviewPrefab.WebView.PostMessage(message);

            







            isClick = false;
            isSelect = false;
            isCopy = false;
            isHitVirtualWindowBoundary = false;

            /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
            };*/
        }

        //scroll inertia
        if (activeWebviewPrefab != null && isScroll)
        {
            
            if(currentFingerSpeed <= minFingerSpeed)
            {
                currentFingerSpeed = 0.0f;
                isScroll = false;
            }
            else
            {
                // Apply deceleration
                currentFingerSpeed -= speedDeceleration * Time.deltaTime;
                float scrollDis = currentFingerSpeed * Time.deltaTime;

                float portionOfHandSurface = scrollDis / handSurface.transform.lossyScale.z;
                float disOnVirtualWindow = portionOfHandSurface * virtualWindow.transform.lossyScale.y;
                float portionOfVirtualSurface = disOnVirtualWindow / interactionManager.GetActiveWebviewGameObject().transform.lossyScale.y;

                //angle<90, scroll up, otherwise scroll down
                if (scrollAngle < 90f)
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, -normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((-0.5).ToString());
                    if (task == OculusWebViewDemo.Tasks.Scroll)
                        activeWebviewPrefab.WebView.PostMessage(portionOfVirtualSurface.ToString());
                    else
                        activeWebviewPrefab.WebView.Scroll(new Vector2(0, portionOfVirtualSurface));
                    gestureForDubug = "Two-finger Scroll up: " + portionOfVirtualSurface;
                    //gestureForDubug = "Two-finger Scroll up: " + speed;
                }
                else if (scrollAngle > 90f)
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((0.5).ToString());
                    if (task == OculusWebViewDemo.Tasks.Scroll)
                        activeWebviewPrefab.WebView.PostMessage((-portionOfVirtualSurface).ToString());
                    else
                        activeWebviewPrefab.WebView.Scroll(new Vector2(0, -portionOfVirtualSurface));
                    gestureForDubug = "Two-finger Scroll down: " + portionOfVirtualSurface;
                    //gestureForDubug = "Two-finger Scroll down: " + speed;
                }
            }
        }
    }

    private void ClickInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();
        //gestureForDubug = "Click but lastTouchGesture: " + lastTouchGesture;
        //activeWebviewPrefab != null && (lastTouchStateAllFinger[1] || touchStateAllFinger[1]) && interactionManager.GetVirtualWindowCursorState(1)
        //activeWebviewPrefab != null && hoverTime[1] <= hoverTimeThreshold && interactionManager.GetVirtualWindowCursorState(1)

        float timeout = Time.time - clickTime;

        if (interactionMethod == InteractionMethod.HandIndirectTouch)
        {
            //customPseudoDirectPoke.IsIndexFingerTouch()
            bool isIndirectTouch = interactionManager.GetInteractionFingerTouchIntention();

            if (timeout > clickTimeout && isIndirectTouch && !isPrevIndexFingerTouch)
            {
                if (activeWebviewPrefab != null)
                {
                    Vector2 cursor = interactionManager.GetIndirectCursorRelativeCoordinate();
                    //debugText.SetText("cursor: " + cursor.x + "," + cursor.y);
                   

                    if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                    {
                        if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                            cursorPosition = cursor;
                        else
                            cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                        activeWebviewPrefab.WebView.Click(cursorPosition, false);
                        gestureForDubug = "hand indirect cursor control: " + cursorPosition;

                        clickTime = Time.time;
                        currentTouchGesture = TouchGestures.Tap;
                    }
                }
            }
            else
            {
                //debugText.SetText("unselect");
            }

            isPrevIndexFingerTouch = isIndirectTouch;
        }
        else
        {
            if (timeout > clickTimeout && activeWebviewPrefab != null && interactionManager.GetVirtualWindowCursorState(1) && lastTouchGesture == TouchGestures.None)
            {
                Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                //if cursor is inside webview
                if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                {
                    if (task == OculusWebViewDemo.Tasks.Scroll || task == OculusWebViewDemo.Tasks.Docking || task == OculusWebViewDemo.Tasks.Sketching)
                        cursorPosition = cursor;
                    else
                        cursorPosition = new Vector2(cursor.x, 1 - cursor.y);

                    activeWebviewPrefab.WebView.Click(cursorPosition, false);
                    gestureForDubug = "Click: " + cursorPosition;

                    isClick = true;
                    clickTime = Time.time;
                }
            }
        }
    }

    private void SelectInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();
        
        if (activeWebviewPrefab != null && interactionManager.GetVirtualWindowCursorState(1))
        {
            Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

            //if cursor is inside webview
            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
            {
                //pos.x, pos.y, isDrag(select), isPaste
                string selectionMSG = "";
                if (!isDrag && (task == OculusWebViewDemo.Tasks.NationalParkOverview || task == OculusWebViewDemo.Tasks.ListOfPark || task == OculusWebViewDemo.Tasks.YellowStonePark)) {
                    selectionMSG = cursor.x.ToString() + "," + cursor.y.ToString() + ",1,0";
                }
                else if (!isDrag && task == OculusWebViewDemo.Tasks.Canvas)
                {
                    selectionMSG = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, 0, 0, 1, 0);
                }
                else if (isDrag && task != OculusWebViewDemo.Tasks.Canvas)
                {
                    selectionMSG = cursor.x.ToString() + "," + cursor.y.ToString() + ",0,0"; 
                }
                activeWebviewPrefab.WebView.PostMessage(selectionMSG);

                isClick = true;
                isSelect = true;
                isDrag = true;
                isCopy = false;
                gestureForDubug = "select";
                /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                    Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
                };*/
            }
            currentTouchGesture = TouchGestures.Drag;
        }
    }

    private void MenuInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            string copyMSG = "-1,-1,1,0";
            activeWebviewPrefab.WebView.PostMessage(copyMSG);
            /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
            };*/
        }
    }

    private void CopyInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            string copyMSG = "-1,-1,0,1";
            activeWebviewPrefab.WebView.PostMessage(copyMSG);
            /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
            };*/
            isCopy = true;
        }
    }

    private void PasteInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            activeWebviewPrefab.WebView.Paste();
        }
    }

    private void ThumbDragToNavigateOnWebview()
    {
        Vector3 touchFinger = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null && interactionManager.GetVirtualWindowCursorState(0) && touchStateAllFinger[0])
        {
            










            touchFinger = touchPointAllFinger[0];
            initTouchFinger = initTouchPointAllFinger[0];

            Vector3 moveDir = touchFinger - initTouchFinger;
            Vector3 surfaceZDir = handSurface.transform.forward;
            float angle = Vector3.Angle(moveDir, surfaceZDir);
            float dis = Vector3.Distance(touchFinger, initTouchFinger);
            






































            surfaceZDir = handSurface.transform.right;
            angle = Vector3.Angle(moveDir, surfaceZDir);

            gestureForDubug = "thumb drag: " + dis;

            if (dis > dragToBackForwardDisThreshold)
            {
                if (angle < 45f)
                {
                    activeWebviewPrefab.WebView.GoBack();
                    gestureForDubug = "Drag to go back.";
                }
                else if (angle > 135f)
                {
                    activeWebviewPrefab.WebView.GoForward();
                    gestureForDubug = "Drag to go forward.";
                }
            }
            currentTouchGesture = TouchGestures.DragToNavigate;
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("DragToNavigate");
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
        }
    }

    private void DragToScrollInteractionOnWebview()
    {
        Vector3 touchFinger = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null && touchStateAllFinger[1])
        {
            if (currentTouchGesture == lastTouchGesture)
            {
                touchFinger = touchPointAllFinger[1];
                initTouchFinger = lastTouchPointAllFinger[1];
            }
            else
            {
                touchFinger = touchPointAllFinger[1];
                initTouchFinger = initTouchPointAllFinger[1];
            }

            //touchFinger = touchPointAllFinger[1];
            //initTouchFinger = initTouchPointAllFinger[1];

            Vector3 moveDir = touchFinger - initTouchFinger;
            Vector3 surfaceZDir = handSurface.transform.forward;
            float angle = Vector3.Angle(moveDir, surfaceZDir);
            float dis = Vector3.Distance(touchFinger, initTouchFinger);
            float portionOfHandSurface = dis / handSurface.transform.lossyScale.z;
            float disOnVirtualWindow = portionOfHandSurface * virtualWindow.transform.lossyScale.y;
            float portionOfVirtualSurface = disOnVirtualWindow / interactionManager.GetActiveWebviewGameObject().transform.lossyScale.y;

            gestureForDubug = "finger movedis: " + dis + ", portionOfHandSurface: " + portionOfHandSurface + ", finger movedis on window: " + disOnVirtualWindow + ", portionOfVirtualSurface: " + portionOfVirtualSurface;

            Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

            if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
            {
                //angle<90, scroll up, otherwise scroll down
                if (angle < 90f)
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, -normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((-0.5).ToString());
                    if (task == OculusWebViewDemo.Tasks.Scroll)
                        activeWebviewPrefab.WebView.PostMessage(portionOfVirtualSurface.ToString());
                    else
                        activeWebviewPrefab.WebView.Scroll(new Vector2(0, portionOfVirtualSurface), new Vector2(cursor.x, 1 - cursor.y));
                    gestureForDubug = "Drag to scroll up: " + portionOfVirtualSurface;
                }
                else
                {
                    //activeWebviewPrefab.WebView.Scroll(new Vector2(0, normalizedDis));
                    //activeWebviewPrefab.WebView.PostMessage((0.5).ToString());
                    if (task == OculusWebViewDemo.Tasks.Scroll)
                        activeWebviewPrefab.WebView.PostMessage((-portionOfVirtualSurface).ToString());
                    else
                        activeWebviewPrefab.WebView.Scroll(new Vector2(0, -portionOfVirtualSurface), new Vector2(cursor.x, 1 - cursor.y));
                    gestureForDubug = "Drag to scroll down: " + portionOfVirtualSurface;
                }
            }
            currentTouchGesture = TouchGestures.DragToScroll;
            if (lastTouchGesture != currentTouchGesture)
                touchClassifier.ChangeConfidence("DragToScroll");
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
        }
    }

    private void DraggingInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (interactionMethod == InteractionMethod.HandIndirectTouch)
        {
            if (activeWebviewPrefab != null && indirectCursorController.GetIndirectCursor().activeSelf)
            {
                float dx = interactionManager.GetMovingX();
                float dy = interactionManager.GetMovingY();
                indirectCursorController.UpdateIndirectCursor(dx, dy, Time.deltaTime);

                Vector2 cursor = interactionManager.GetIndirectCursorRelativeCoordinate();

                if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                {
                    cursorPosition = cursor;

                    string draggingMessage = "";

                    if (task == OculusWebViewDemo.Tasks.Canvas)
                    {
                        draggingMessage = SendPositionRotationScaleDragPasteData(cursorPosition.x, cursorPosition.y, 0, 0, 0, 0);
                    }
                    else
                    {
                        draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                    }

                    activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                    activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) =>
                    {
                        Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
                        gestureForDubug = $"Console message logged: [{eventArgs.Level}] {eventArgs.Message}";
                    };                   

                    gestureForDubug = "Dragging: " + cursorPosition;
                }
                else
                {
                    NoneInteractionOnWebview();
                }

                currentTouchGesture = TouchGestures.Drag;
            }
            else
            {
                NoneInteractionOnWebview();
                currentTouchGesture = TouchGestures.None;
            }
        }
        else
        {
            if (activeWebviewPrefab != null && interactionManager.GetVirtualWindowCursorState(1))
            {
                Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

                //if cursor is inside webview
                if (cursor.x >= 0 && cursor.x <= 1 && cursor.y >= 0 && cursor.y <= 1)
                {
                    cursorPosition = cursor;

                    //two meanings: position.x, position.y, isDrag, isPaste - for drag and drop task such as quiz
                    //or: position.x, position.y, rotation, paste - for multi-touch task such as rotate/scale on canvas
                    string draggingMessage = "";

                    if (task == OculusWebViewDemo.Tasks.Canvas)
                    {
                        draggingMessage = SendPositionRotationScaleDragPasteData(cursorPosition.x, cursorPosition.y, 0, 0, 0, 0);
                    }
                    else
                    {
                        draggingMessage = SendPositionRotationScaleData(0, cursorPosition.x, cursorPosition.y, 0, 0);
                    }

                    activeWebviewPrefab.WebView.PostMessage(draggingMessage);

                    activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) =>
                    {
                        Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
                        gestureForDubug = $"Console message logged: [{eventArgs.Level}] {eventArgs.Message}";
                    };

                    //activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) =>
                    //{
                    
                    //};

                    gestureForDubug = "Dragging: " + cursorPosition;

                    //automatically move virtual window if sketching hit edges
                    Vector2 virtualWindowRelativePos = interactionManager.ComputeVirtualWindowRelativePositionOnWebview();
                    //gestureForDubug = "Dragging: " + (virtualWindowRelativePos.x + 0.4 * virtualWindow.transform.lossyScale.x/interactionManager.GetActiveWebviewGameObject().transform.lossyScale.x);
                    if (!isHitVirtualWindowBoundary && cursor.x >= (virtualWindowRelativePos.x + 0.4 * virtualWindow.transform.lossyScale.x / interactionManager.GetActiveWebviewGameObject().transform.lossyScale.x))
                    {
                        isHitVirtualWindowBoundary = true;
                    }
                }
                else
                {
                    NoneInteractionOnWebview();
                }

                currentTouchGesture = TouchGestures.Drag;
            }
            else
            {
                NoneInteractionOnWebview();
                currentTouchGesture = TouchGestures.None;
            }
        }
    }

    private void ScaleRotateInteractionByTwoHandPoke()
    {      
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            if (currentTouchGesture != TouchGestures.Scale)
            {
                initDomPokePos = customPoke.GetTouchPoint();
                initNonDomPokePos = customPoke.GetNonDomTouchPoint();

                currentTouchGesture = TouchGestures.Scale;
            }
            else
            {               
                domPokePos = customPoke.GetTouchPoint();
                nonDomPokePos = customPoke.GetNonDomTouchPoint();

                float initInterFingerDis = Vector3.Distance(initDomPokePos, initNonDomPokePos);
                float curInterFingerDis = Vector3.Distance(domPokePos, nonDomPokePos);
                float scale = (curInterFingerDis - initInterFingerDis) / virtualSurface.transform.lossyScale.y * 0.5f;
                if (scale <= -0.05f) scale = -0.05f;
                if (scale >= 0.05f) scale = 0.05f;

                Vector3 initInterFingerVector = initNonDomPokePos - initDomPokePos;
                Vector3 curInterFingerVector = nonDomPokePos - domPokePos;
                float rotateAngle = -Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, -activeWebviewPrefab.transform.forward);
                if (rotateAngle <= -5f) rotateAngle = -5f;
                if (rotateAngle >= 5f) rotateAngle = 5f;

                

                Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(domPokePos);

                string scaleRotateMessage = "";
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    scaleRotateMessage = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, rotateAngle, scale, 0, 0);
                }
                else
                {
                    scaleRotateMessage = SendPositionRotationScaleData(1, cursor.x, cursor.y, rotateAngle, scale);
                }
                activeWebviewPrefab.WebView.PostMessage(scaleRotateMessage);

                gestureForDubug = "Scale up: " + scale + ", rotate: " + rotateAngle;

                initDomPokePos = customPoke.GetTouchPoint();
                initNonDomPokePos = customPoke.GetNonDomTouchPoint();
            }
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
            NoneInteractionOnWebview();
        }
    }

    private void ScaleRotateInteractionByTwoHandRayPinch()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            if (currentTouchGesture != TouchGestures.Scale)
            {
                initDomPinchPos = customRayPointer.GetRayCursor();
                initNonDomPinchPos = customRayPointer.GetNonDomRayCursor();

                currentTouchGesture = TouchGestures.Scale;
            }
            else
            {
                domPinchPos = customRayPointer.GetRayCursor();
                nonDomPinchPos = customRayPointer.GetNonDomRayCursor();

                float initInterFingerDis = Vector3.Distance(initDomPinchPos, initNonDomPinchPos);
                float curInterFingerDis = Vector3.Distance(domPinchPos, nonDomPinchPos);
                float scale = (curInterFingerDis - initInterFingerDis) / virtualSurface.transform.lossyScale.y * 0.5f;
                if (scale <= -0.05f) scale = -0.05f;
                if (scale >= 0.05f) scale = 0.05f;

                Vector3 initInterFingerVector = initNonDomPinchPos - initDomPinchPos;
                Vector3 curInterFingerVector = nonDomPinchPos - domPinchPos;
                float rotateAngle = -Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, -activeWebviewPrefab.transform.forward);
                if (rotateAngle <= -5f) rotateAngle = -5f;
                if (rotateAngle >= 5f) rotateAngle = 5f;


                

                Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(domPinchPos);

                string scaleRotateMessage = "";
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    scaleRotateMessage = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, rotateAngle, scale, 0, 0);
                }
                else
                {
                    scaleRotateMessage = SendPositionRotationScaleData(1, cursor.x, cursor.y, rotateAngle, scale);
                }
                activeWebviewPrefab.WebView.PostMessage(scaleRotateMessage);

                gestureForDubug = "Scale up: " + scale + ", rotate: " + rotateAngle;

                initDomPinchPos = customRayPointer.GetRayCursor();
                initNonDomPinchPos = customRayPointer.GetNonDomRayCursor();
            }
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
            NoneInteractionOnWebview();
        }
    }

    private void ScaleRotateInteractionByTwoHandDirectPinch()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            if (currentTouchGesture != TouchGestures.Scale)
            {
                initDomPinchPos = customHandPointer.GetHandPinchCursor();
                initNonDomPinchPos = customHandPointer.GetNonDomHandPinchCursor();

                currentTouchGesture = TouchGestures.Scale;
            }
            else
            {
                domPinchPos = customHandPointer.GetHandPinchCursor();
                nonDomPinchPos = customHandPointer.GetNonDomHandPinchCursor();

                float initInterFingerDis = Vector3.Distance(initDomPinchPos, initNonDomPinchPos);
                float curInterFingerDis = Vector3.Distance(domPinchPos, nonDomPinchPos);
                float scale = (curInterFingerDis - initInterFingerDis) / virtualSurface.transform.lossyScale.y * 0.5f;
                if (scale <= -0.05f) scale = -0.05f;
                if (scale >= 0.05f) scale = 0.05f;

                Vector3 initInterFingerVector = initNonDomPinchPos - initDomPinchPos;
                Vector3 curInterFingerVector = nonDomPinchPos - domPinchPos;
                float rotateAngle = -Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, -activeWebviewPrefab.transform.forward);
                if (rotateAngle <= -5f) rotateAngle = -5f;
                if (rotateAngle >= 5f) rotateAngle = 5f;


                

                Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(domPinchPos);
                if (interactionMethod == InteractionMethod.EyeMoveHand)
                {
                    cursor = interactionManager.ComputeWebviewPointerRelativePosition(interactionManager.GetSmoothGazeCursor());
                }

                string scaleRotateMessage = "";
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    scaleRotateMessage = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, rotateAngle, scale, 0, 0);
                }
                else
                {
                    scaleRotateMessage = SendPositionRotationScaleData(1, cursor.x, cursor.y, rotateAngle, scale);
                }
                activeWebviewPrefab.WebView.PostMessage(scaleRotateMessage);

                gestureForDubug = "Scale up: " + scale + ", rotate: " + rotateAngle;

                initDomPinchPos = customHandPointer.GetHandPinchCursor();
                initNonDomPinchPos = customHandPointer.GetNonDomHandPinchCursor();
            }
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
            NoneInteractionOnWebview();
        }
    }

    private void ScaleRotateInteractionByTwoHandPinch()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        if (activeWebviewPrefab != null)
        {
            if (currentTouchGesture != TouchGestures.Scale)
            {
                initDomPinchPos = eyeGazeHandPinch.GetHandPinchCursor();
                initNonDomPinchPos = eyeGazeHandPinch.GetNonDomHandPinchCursor();

                currentTouchGesture = TouchGestures.Scale;
            }
            else
            {
                domPinchPos = eyeGazeHandPinch.GetHandPinchCursor();
                nonDomPinchPos = eyeGazeHandPinch.GetNonDomHandPinchCursor();

                float initInterFingerDis = Vector3.Distance(initDomPinchPos, initNonDomPinchPos);
                float curInterFingerDis = Vector3.Distance(domPinchPos, nonDomPinchPos);
                float scale = (curInterFingerDis - initInterFingerDis) / virtualSurface.transform.lossyScale.y * 0.5f;
                if (scale <= - 0.05f) scale = - 0.05f;
                if (scale >= 0.05f) scale = 0.05f;

                Vector3 initInterFingerVector = initNonDomPinchPos - initDomPinchPos;
                Vector3 curInterFingerVector = nonDomPinchPos - domPinchPos;
                float rotateAngle = - Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, - activeWebviewPrefab.transform.forward);
                if (rotateAngle <= - 5f) rotateAngle = -5f;
                if (rotateAngle >= 5f) rotateAngle = 5f;


                

                Vector2 cursor = interactionManager.ComputeWebviewPointerRelativePosition(domPinchPos);
                if(interactionMethod == InteractionMethod.EyeMoveHand)
                {
                    cursor = interactionManager.ComputeWebviewPointerRelativePosition(interactionManager.GetSmoothGazeCursor());
                }

                string scaleRotateMessage = "";
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    scaleRotateMessage = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, rotateAngle, scale, 0, 0);
                }
                else
                {
                    scaleRotateMessage = SendPositionRotationScaleData(1, cursor.x, cursor.y, rotateAngle, scale);
                }
                activeWebviewPrefab.WebView.PostMessage(scaleRotateMessage);

                gestureForDubug = "Scale up: " + scale + ", rotate: " + rotateAngle;

                initDomPinchPos = eyeGazeHandPinch.GetHandPinchCursor();
                initNonDomPinchPos = eyeGazeHandPinch.GetNonDomHandPinchCursor();
            }
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
            NoneInteractionOnWebview();
        }
    }

    private void ScrollScaleRotateInteractionOnWebview()
    {
        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

        Vector3 touchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 touchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        if (activeWebviewPrefab != null && touchStateAllFinger[1] && touchStateAllFinger[2] && snapGestureController.GetSnapState() == SnapStates.SnapState)
        {
            














            if (!Single.IsInfinity(lastTouchPointAllFinger[1].x) && !Single.IsInfinity(lastTouchPointAllFinger[1].y))
            {
                touchFinger1 = touchPointAllFinger[1];
                initTouchFinger1 = lastTouchPointAllFinger[1];
                touchFinger2 = touchPointAllFinger[2];
                initTouchFinger2 = lastTouchPointAllFinger[2];
            }

            Vector3 moveDir = touchFinger1 - initTouchFinger1;
            Vector3 surfaceZDir = handSurface.transform.forward;
            float angle = Vector3.Angle(moveDir, surfaceZDir);
            float dis = Vector3.Distance(touchFinger1, initTouchFinger1);

            float portionOfHandSurface = dis / handSurface.transform.lossyScale.z;
            float disOnVirtualWindow = portionOfHandSurface * virtualWindow.transform.lossyScale.y;
            float portionOfVirtualSurface = disOnVirtualWindow / interactionManager.GetActiveWebviewGameObject().transform.lossyScale.y;

            //angle<90, scroll up, otherwise scroll down
            if (angle < 90f)
            {
                //activeWebviewPrefab.WebView.Scroll(new Vector2(0, -normalizedDis));
                //activeWebviewPrefab.WebView.PostMessage((-0.5).ToString());
                if (task == OculusWebViewDemo.Tasks.Scroll)
                    activeWebviewPrefab.WebView.PostMessage(portionOfVirtualSurface.ToString());
                else
                    activeWebviewPrefab.WebView.Scroll(new Vector2(0, portionOfVirtualSurface));
                gestureForDubug = "Two-finger Scroll up: " + portionOfVirtualSurface;
                //gestureForDubug = "Two-finger Scroll up: " + speed;
            }
            else if (angle > 90f)
            {
                //activeWebviewPrefab.WebView.Scroll(new Vector2(0, normalizedDis));
                //activeWebviewPrefab.WebView.PostMessage((0.5).ToString());
                if (task == OculusWebViewDemo.Tasks.Scroll)
                    activeWebviewPrefab.WebView.PostMessage((-portionOfVirtualSurface).ToString());
                else
                    activeWebviewPrefab.WebView.Scroll(new Vector2(0, -portionOfVirtualSurface));
                gestureForDubug = "Two-finger Scroll down: " + portionOfVirtualSurface;
                //gestureForDubug = "Two-finger Scroll down: " + speed;
            }
            isScroll = true;
            scrollAngle = angle;
            currentFingerSpeed = interactionManager.GetInteractionFingerSpeed();
            //currentTouchGesture = TouchGestures.Scroll;
        }
        //scale or rotate
        else if (activeWebviewPrefab != null && touchStateAllFinger[0] && touchStateAllFinger[1])
        {
            













            if (!Single.IsInfinity(lastTouchPointAllFinger[0].x) && !Single.IsInfinity(lastTouchPointAllFinger[1].y))
            {
                touchFinger1 = touchPointAllFinger[0];
                initTouchFinger1 = lastTouchPointAllFinger[0];
                touchFinger2 = touchPointAllFinger[1];
                initTouchFinger2 = lastTouchPointAllFinger[1];
            }

            float initInterFingerDis = Vector3.Distance(initTouchFinger1, initTouchFinger2);
            float curInterFingerDis = Vector3.Distance(touchFinger1, touchFinger2);
            float scaleDis = Mathf.Abs(curInterFingerDis - initInterFingerDis);

            Vector3 initInterFingerVector = initTouchFinger2 - initTouchFinger1;
            Vector3 curInterFingerVector = touchFinger2 - touchFinger1;
            float rotateAngle = Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, handSurface.transform.up);

            //gestureForDubug = "rotateAngle: " + rotateAngle + ", scaleDis" + scaleDis;

            Vector2 cursor = interactionManager.GetWebviewCursorRelativeCoordinate(1);

            //send rotation and scale together
            if (snapGestureController.GetSnapState() == SnapStates.SnapState)
            {
                //scale it to match window size
                float scale = (curInterFingerDis - initInterFingerDis) / handSurface.transform.lossyScale.z * virtualWindow.transform.lossyScale.y;

                string scaleRotateMessage = "";
                if (task == OculusWebViewDemo.Tasks.Canvas)
                {
                    scaleRotateMessage = SendPositionRotationScaleDragPasteData(cursor.x, cursor.y, rotateAngle, scale, 0, 0);
                }
                else
                {
                    scaleRotateMessage = SendPositionRotationScaleData(1, cursor.x, cursor.y, rotateAngle, scale);
                }
                activeWebviewPrefab.WebView.PostMessage(scaleRotateMessage);

                /*activeWebviewPrefab.WebView.ConsoleMessageLogged += (sender, eventArgs) => {
                    gestureForDubug = $"Console message logged: [{eventArgs.Level}] {eventArgs.Message}";

                    Debug.Log($"Console message logged: [{eventArgs.Level}] {eventArgs.Message}");
                };*/

                gestureForDubug = "Scale up: " + scale + ", rotate: " + rotateAngle;

            }
            else if (snapGestureController.GetSnapState() == SnapStates.OverSnapState)
            {
                float scale = curInterFingerDis / initInterFingerDis;
                GameObject virtualSurface = interactionManager.GetActiveVirtualSurfaceGameObject();

                float curX = virtualSurface.transform.localScale.x;
                float curY = virtualSurface.transform.localScale.y;
                float curZ = virtualSurface.transform.localScale.z;

                if ((virtualSurface.transform.localScale.x < 0.3 * initVirtualSurfaceScale && scale < 1) || (virtualSurface.transform.localScale.x > 3 * initVirtualSurfaceScale && scale > 1)) scale = 1f;
                virtualSurface.transform.localScale = new Vector3(curX * scale, curY * scale, curZ);
            }


            //send rotation, scale separately
            //update when one gesture ends
            //scale
            










































































            //gestureForDubug = "rotateAngle: " + rotateAngle + ", scaleDis" + scaleDis;
        }
        else
        {
            currentTouchGesture = TouchGestures.None;
            NoneInteractionOnWebview();
        }
    }

    private string SendPositionRotationScaleData(float task, float x, float y, float rotationAngle, float scale)
    {
        string data = task.ToString() + "," + x.ToString() + "," + y.ToString() + "," + rotationAngle.ToString() + "," + scale.ToString();
        return data;
    }

    private string SendPositionRotationScaleDragPasteData(float x, float y, float rotationAngle, float scale, int drag, int paste)
    {
        string data = x.ToString() + "," + y.ToString() + "," + rotationAngle.ToString() + "," + scale.ToString() + "," + drag.ToString() + "," + paste.ToString();
        return data;
    }

    //interaction with virtual gameobject
    private void TapInteraction()
    {
        //interaction on virtual window with retargeted hand

        GameObject tapObj1 = GetRaycastHitObject(retargetIndex, 1.0f * virtualWindowNormal, virtualObjectLayer);
        GameObject tapObj2 = GetRaycastHitObject(retargetIndex, -1.0f * virtualWindowNormal, virtualObjectLayer);

        if (tapObj1 != null || tapObj2 != null)
        {
            if (tapObj1 != null) activeObject = tapObj1;
            else if (tapObj2 != null) activeObject = tapObj2;

            if(touchStateAllFinger[1] && interactionManager.GetVirtualWindowCursorState(1) && lastTouchGesture == TouchGestures.None)
                ActivateObject(activeObject, true);

            gestureForDubug = "Tap";
        }
        else
        {
            //if (activeObject != null)
            //{
                //ActivateObject(activeObject, false);
                //ActivateObject(virtualObject, false);
                //activeObject = null;
            //}
        }
    }

    private void TapInteractionOnHandSurface()
    {
        //interaction on hand surface with real hand
        GameObject tapObj1 = GetRaycastHitObject(indexFingertip, 1.0f * handSurfaceNormal, handSurfaceVirtualObjectLayer);
        GameObject tapObj2 = GetRaycastHitObject(indexFingertip, -1.0f * handSurfaceNormal, handSurfaceVirtualObjectLayer);

        

        if (tapObj1 != null || tapObj2 != null)
        {
            if (tapObj1 != null) activeObject = tapObj1;
            else if (tapObj2 != null) activeObject = tapObj2;
            
            Vector3 hitpoint1 = GetRaycastHitPoint(indexFingertip, 1.0f * handSurfaceNormal, handSurfaceLayer, handSurface);
            Vector3 hitpoint2 = GetRaycastHitPoint(indexFingertip, -1.0f * handSurfaceNormal, handSurfaceLayer, handSurface);

            if (!Single.IsInfinity(hitpoint1.x))
            {
                //change color of handSurfaceVirtualObject
                if (lastTouchGesture == TouchGestures.None)
                {
                    ActivateObject(activeObject, true);
                    ActivateObject(virtualObject, true);
                }
            }
            else if (!Single.IsInfinity(hitpoint2.x))
            {
                if (lastTouchGesture == TouchGestures.None)
                {
                    ActivateObject(activeObject, true);
                    ActivateObject(virtualObject, true);
                }
            }
            gestureForDubug = "Tap";
        }
        else
        {
            





        }
    }

    //interaction with virtual gameobject
    private void DragInteraction()
    {
        if (activeObject != null)
        {
            //if (touchStateAllFinger[1] && interactionManager.GetVirtualWindowCursorState(1) && (lastTouchGesture == TouchGestures.None || lastTouchGesture == TouchGestures.Tap))
            if (touchStateAllFinger[1] && interactionManager.GetVirtualWindowCursorState(1))
            {
                Vector3 hitpoint = interactionManager.GetVirtualWindowCursorPosition(1);
                float jumpDistance = Vector3.Distance(hitpoint, activeObject.transform.position);
                if (jumpDistance < activeObject.transform.lossyScale.x * 2)
                {
                    activeObject.transform.position = hitpoint;
                }
                gestureForDubug = "Drag";
            }
        }
    }

    private void DragInteractionOnHandSurface()
    {
        //interaction on hand surface with real hand
        if (activeObject!=null)
        {
            Vector3 hitpoint1 = GetRaycastHitPoint(indexFingertip, 1.0f * handSurfaceNormal, handSurfaceLayer, handSurface);
            Vector3 hitpoint2 = GetRaycastHitPoint(indexFingertip, -1.0f * handSurfaceNormal, handSurfaceLayer, handSurface);
            if (!Single.IsInfinity(hitpoint1.x))
            {
                float jumpDistance = Vector3.Distance(hitpoint1, activeObject.transform.position);
                if (jumpDistance < activeObject.transform.lossyScale.x * 2)
                {
                    activeObject.transform.position = hitpoint1;
                    virtualObject.transform.localPosition = new Vector3(activeObject.transform.localPosition.x, activeObject.transform.localPosition.z, activeObject.transform.localPosition.y);
                }
                gestureForDubug = "Drag";
            }
            else if (!Single.IsInfinity(hitpoint2.x))
            {
                float jumpDistance = Vector3.Distance(hitpoint2, activeObject.transform.position);
                if (jumpDistance < activeObject.transform.lossyScale.x * 2)
                {
                    activeObject.transform.position = hitpoint2;
                    virtualObject.transform.localPosition = new Vector3(activeObject.transform.localPosition.x, activeObject.transform.localPosition.z, activeObject.transform.localPosition.y);
                }
                gestureForDubug = "Drag";
            }
        }
    }

    private void ScrollScaleRotateInteraction()
    {
        Vector3 touchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 touchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        if (activeObject != null && touchStateAllFinger[1] && touchStateAllFinger[2])
        {
            touchFinger1 = touchPointAllFinger[1];
            initTouchFinger1 = initTouchPointAllFinger[1];
            touchFinger2 = touchPointAllFinger[2];
            initTouchFinger2 = initTouchPointAllFinger[2];

            float x = activeObject.transform.localPosition.x;
            float y = activeObject.transform.localPosition.y;
            float z = activeObject.transform.localPosition.z;

            Vector3 moveDir = touchFinger1 - initTouchFinger1;
            Vector3 surfaceZDir = handSurface.transform.forward;
            float angle = Vector3.Angle(moveDir, surfaceZDir);
            float dis = Vector3.Distance(touchFinger1, initTouchFinger1);

            //angle<90, scroll up, otherwise scroll down
            if (angle < 90f)
                activeObject.transform.localPosition = new Vector3(x, y + dis * 2, z);
            else
                activeObject.transform.localPosition = new Vector3(x, y - dis * 2, z);

            //after one touch gesture finishes, if fingers are still on the surface, update the initial touch point to the current
            //initTouchPointAllFinger[0] = touchPointAllFinger[0];
            //initTouchPointAllFinger[1] = touchPointAllFinger[1];

            gestureForDubug = "Scroll";
        }
        //scale or rotate
        else if (activeObject != null && touchStateAllFinger[0] && touchStateAllFinger[1])
        {
            touchFinger1 = touchPointAllFinger[0];
            initTouchFinger1 = initTouchPointAllFinger[0];
            touchFinger2 = touchPointAllFinger[1];
            initTouchFinger2 = initTouchPointAllFinger[1];

            float initInterFingerDis = Vector3.Distance(initTouchFinger1, initTouchFinger2);
            float curInterFingerDis = Vector3.Distance(touchFinger1, touchFinger2);
            float scaleDis = Mathf.Abs(curInterFingerDis - initInterFingerDis);

            Vector3 initInterFingerVector = initTouchFinger2 - initTouchFinger1;
            Vector3 curInterFingerVector = touchFinger2 - touchFinger1;
            float rotateAngle = Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, handSurface.transform.up);

            gestureForDubug = "rotateAngle: " + rotateAngle + ", scaleDis" + scaleDis;

            //update when one gesture ends
            //scale
            if (Mathf.Abs(rotateAngle) < 2)
            {
                currentTouchGesture = TouchGestures.Scale;

                float curX = activeObject.transform.localScale.x;
                float curY = activeObject.transform.localScale.y;
                float curZ = activeObject.transform.localScale.z;

                float scale = curInterFingerDis / initInterFingerDis;
                if ((activeObject.transform.localScale.x < 0.1 && scale < 1) || (activeObject.transform.localScale.x > 2 && scale > 1)) scale = 1f;

                activeObject.transform.localScale = new Vector3(curX * scale, curY * scale, curZ);

                //initTouchPointAllFinger[0] = touchPointAllFinger[0];
                //initTouchPointAllFinger[1] = touchPointAllFinger[1];

                gestureForDubug = "Scale: " + scale;
            }
            //rotate
            else if (Mathf.Abs(rotateAngle) >= 2)
            {
                currentTouchGesture = TouchGestures.Rotate;

                virtualObject.transform.RotateAround(virtualObject.transform.position, virtualObject.transform.forward, rotateAngle);

                //initTouchPointAllFinger[0] = touchPointAllFinger[0];
                //initTouchPointAllFinger[1] = touchPointAllFinger[1];

                gestureForDubug = "Rotate: " + rotateAngle;
            }

            //based on the first touch
            //scale
            




































        }
        else
        {
            currentTouchGesture = TouchGestures.None;
        }
    }

    private void ScrollScaleRotateInteractionOnHandSurface()
    {
        //interaction on hand surface with real hand
        Vector3 touchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger1 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 touchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
        Vector3 initTouchFinger2 = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        if(activeObject != null && touchStateAllFinger[1] && touchStateAllFinger[2])
        {
            touchFinger1 = touchPointAllFinger[1];
            initTouchFinger1 = initTouchPointAllFinger[1];
            touchFinger2 = touchPointAllFinger[2];
            initTouchFinger2 = initTouchPointAllFinger[2];

            float x = handSurfaceVirtualObject.transform.localPosition.x;
            float y = handSurfaceVirtualObject.transform.localPosition.y;
            float z = handSurfaceVirtualObject.transform.localPosition.z;
            //virtualObject.transform.localPosition = activeObject.transform.localPosition;

            Vector3 moveDir = touchFinger1 - initTouchFinger1;
            Vector3 surfaceZDir = handSurface.transform.forward;
            float angle = Vector3.Angle(moveDir, surfaceZDir);
            float dis = Vector3.Distance(touchFinger1, initTouchFinger1);

            //angle<90, scroll up, otherwise scroll down
            if (angle < 90f)
            {
                handSurfaceVirtualObject.transform.localPosition = new Vector3(x, y, z + dis*2);
                virtualObject.transform.localPosition = new Vector3(activeObject.transform.localPosition.x, activeObject.transform.localPosition.z, activeObject.transform.localPosition.y);
            }
            else
            {
                handSurfaceVirtualObject.transform.localPosition = new Vector3(x, y, z - dis*2);
                virtualObject.transform.localPosition = new Vector3(activeObject.transform.localPosition.x, activeObject.transform.localPosition.z, activeObject.transform.localPosition.y);
            }

            //after one touch gesture finishes, if fingers are still on the surface, update the initial touch point to the current
            //initTouchPointAllFinger[0] = touchPointAllFinger[0];
            //initTouchPointAllFinger[1] = touchPointAllFinger[1];

            gestureForDubug = "Scroll";
        }
        //scale or rotate
        else if(activeObject != null && touchStateAllFinger[0] && touchStateAllFinger[1])
        {
            touchFinger1 = touchPointAllFinger[0];
            initTouchFinger1 = initTouchPointAllFinger[0];
            touchFinger2 = touchPointAllFinger[1];
            initTouchFinger2 = initTouchPointAllFinger[1];

            float initInterFingerDis = Vector3.Distance(initTouchFinger1, initTouchFinger2);
            float curInterFingerDis = Vector3.Distance(touchFinger1, touchFinger2);
            float scaleDis = Mathf.Abs(curInterFingerDis - initInterFingerDis);

            Vector3 initInterFingerVector = initTouchFinger2 - initTouchFinger1;
            Vector3 curInterFingerVector = touchFinger2 - touchFinger1;
            float rotateAngle = Vector3.SignedAngle(curInterFingerVector, initInterFingerVector, handSurface.transform.up);

            //scale
            if (Mathf.Abs(rotateAngle) < 2)
            {
                currentTouchGesture = TouchGestures.Scale;

                float curX = activeObject.transform.localScale.x;
                float curY = activeObject.transform.localScale.y;
                float curZ = activeObject.transform.localScale.z;

                float scale = curInterFingerDis / initInterFingerDis;
                if ((activeObject.transform.localScale.x < 0.1 && scale < 1) || (activeObject.transform.localScale.x > 1 && scale > 1)) scale = 1f;

                activeObject.transform.localScale = new Vector3(curX * scale, curY, curZ * scale);
                virtualObject.transform.localScale = new Vector3(activeObject.transform.localScale.x, activeObject.transform.localScale.z, activeObject.transform.localScale.y);

                //initTouchPointAllFinger[0] = touchPointAllFinger[0];
                //initTouchPointAllFinger[1] = touchPointAllFinger[1];

                gestureForDubug = "Scale";
            }
            //rotate
            else if (Mathf.Abs(rotateAngle) >= 2)
            {
                currentTouchGesture = TouchGestures.Rotate;

                activeObject.transform.SetParent(null);
                virtualObject.transform.SetParent(null);

                activeObject.transform.RotateAround(activeObject.transform.position, activeObject.transform.up, rotateAngle);
                virtualObject.transform.RotateAround(virtualObject.transform.position, virtualObject.transform.forward, rotateAngle);

                activeObject.transform.SetParent(handSurface.transform);
                virtualObject.transform.SetParent(virtualWindow.transform);

                //initTouchPointAllFinger[0] = touchPointAllFinger[0];
                //initTouchPointAllFinger[1] = touchPointAllFinger[1];

                gestureForDubug = "Rotate";
            }
        }

        //either two fingers touch
        /*for (int i = 0; i < 5; i++)
        {
            if (touchStateAllFinger[i])
            {
                if(Single.IsInfinity(touchFinger1.x))
                {
                    touchFinger1 = touchPointAllFinger[i];
                    initTouchFinger1 = initTouchPointAllFinger[i];
                    continue;
                }
                else
                {
                    touchFinger2 = touchPointAllFinger[i];
                    initTouchFinger2 = initTouchPointAllFinger[i];
                }

                if (!Single.IsInfinity(touchFinger2.x))
                {
                    float initInterFingerDis = Vector3.Distance(initTouchFinger1, initTouchFinger2);
                    float curInterFingerDis = Vector3.Distance(touchFinger1, touchFinger2);
                    float scaleDis = curInterFingerDis - initInterFingerDis;

                    Vector3 initInterFingerVector = initTouchFinger2 - initTouchFinger1;
                    Vector3 curInterFingerVector = touchFinger2 - touchFinger1;
                    float rotateAngle = Vector3.Angle(initInterFingerVector, curInterFingerVector);

                    gestureForDubug = "scaleDis: " + scaleDis + ", rotateAngle: " + rotateAngle;

                    //scroll
                    if (Mathf.Abs(scaleDis) < 0.01 && rotateAngle < 15)
                    {
                        float x = handSurfaceVirtualObject.transform.localPosition.x;
                        float y = handSurfaceVirtualObject.transform.localPosition.y;
                        float z = handSurfaceVirtualObject.transform.localPosition.z;
                        //virtualObject.transform.localPosition = activeObject.transform.localPosition;

                        Vector3 moveDir = touchFinger1 - initTouchFinger1;
                        Vector3 surfaceZDir = handSurface.transform.forward;
                        float angle = Vector3.Angle(moveDir, surfaceZDir);
                        float dis = Vector3.Distance(touchFinger1, initTouchFinger1);

                        //angle<90, scroll up, otherwise scroll down
                        if (angle < 90f)
                        {
                            handSurfaceVirtualObject.transform.localPosition = new Vector3(x, y, z + dis);
                            //virtualObject.transform.localPosition = activeObject.transform.localPosition;
                            //gestureForDubug = "Scroll up: " + dis;
                            //Debug.Log("Scroll up: " + dis);
                        }
                        else
                        {
                            handSurfaceVirtualObject.transform.localPosition = new Vector3(x, y, z - dis);
                            //virtualObject.transform.localPosition = activeObject.transform.localPosition;
                            //gestureForDubug = "Scroll down: " + dis;
                            //Debug.Log("Scroll down: " + dis);
                        }
                    }
                    //scale
                    else if(activeObject != null && Mathf.Abs(scaleDis) > 0.02 && rotateAngle < 15)
                    {
                        currentTouchGesture = TouchGestures.Scale;

                        float curX = activeObject.transform.localScale.x;
                        float curY = activeObject.transform.localScale.y;
                        float curZ = activeObject.transform.localScale.z;

                        float scale = curInterFingerDis / initInterFingerDis;

                        activeObject.transform.localScale = new Vector3(curX * scale, curY * scale, curZ);
                    }
                    //rotate
                    else if(activeObject != null && rotateAngle > 20)
                    {
                        currentTouchGesture = TouchGestures.Rotate;

                        Quaternion relativeRot = Quaternion.Inverse(Quaternion.Euler(initInterFingerVector)) * Quaternion.Euler(curInterFingerVector);
                        activeObject.transform.rotation = activeObject.transform.rotation * relativeRot;
                    }

                    break;
                }                 
            }
        }*/
    }

    private int[] CreateTouchStateTDM(int n_t, int n_d, int n_m)
    {
        int[] ts = new int[3] { n_t, n_d, n_m };
        return ts;
    }

    private Boolean CheckTouchStateTDM(int[] TDM, int n_t, int n_d, int n_m)
    {
        if (TDM[0] == n_t && TDM[1] == n_d && TDM[2] == n_m)
        {
            return true;
        }
        else return false;
    }

    private void GestureDetection()
    {
        int[] touchState0 = touchStateSequence[touchStateSequence.Count - 3];
        int[] touchState1 = touchStateSequence[touchStateSequence.Count - 2];
        int[] touchState2 = touchStateSequence[touchStateSequence.Count - 1];

        //touchState0 == CreateTouchStateTDM(0,0,0) && touchState1 == CreateTouchStateTDM(1, 0, 0) && touchState2 == CreateTouchStateTDM(0, 0, 0)
        



        //touchState0 == CreateTouchStateTDM(0, 0, 0) && touchState1 == CreateTouchStateTDM(1, 0, 0) && touchState2 == CreateTouchStateTDM(0, 0, 1)
        /*else if ((CheckTouchStateTDM(touchState1, 1, 0, 0) || CheckTouchStateTDM(touchState1, 0, 1, 0)) && CheckTouchStateTDM(touchState2, 0, 0, 1))
        {
            TouchInteraction(TouchGestures.Drag);
        }
        else if ((CheckTouchStateTDM(touchState1, 2, 0, 0) || CheckTouchStateTDM(touchState1, 1, 1, 0) || 
            CheckTouchStateTDM(touchState1, 0, 2, 0) || CheckTouchStateTDM(touchState1, 0, 1, 1) || CheckTouchStateTDM(touchState1, 1, 0, 1)) && CheckTouchStateTDM(touchState2, 0, 0, 2))
        {
            TouchInteraction(TouchGestures.Scroll);
        }*/

        //if all fingers are released for 100ms, go none touch interaction
        if (hoverTime[0] > hoverTimeThreshold && hoverTime[1] > hoverTimeThreshold && hoverTime[2] > hoverTimeThreshold)
        {
            NoneInteractionOnWebview();
            TouchInteraction(TouchGestures.None);
        }
        else if ((CheckTouchStateTDM(touchState1, 1, 0, 0) || CheckTouchStateTDM(touchState1, 0, 1, 0)) && CheckTouchStateTDM(touchState2, 0, 0, 1))
        {
            TouchInteraction(TouchGestures.Drag);
        }
        /*else if (currentTouchGesture == TouchGestures.Drag && CheckTouchStateTDM(touchState2, 0, 0, 1))
        {
            TouchInteraction(TouchGestures.Drag);
        }*/
        else if (CheckTouchStateTDM(touchState2, 0, 0, 2))
        {
            TouchInteraction(TouchGestures.Scroll);
        }
        //touchState0 == CreateTouchStateTDM(0,0,0) && touchState1 == CreateTouchStateTDM(1, 0, 0) && touchState2 == CreateTouchStateTDM(0, 0, 0)
        //else if ((CheckTouchStateTDM(touchState0, 0, 0, 0) && (CheckTouchStateTDM(touchState1, 1, 0, 0) || CheckTouchStateTDM(touchState1, 0, 1, 0)) && CheckTouchStateTDM(touchState2, 0, 0, 0)) ||
        //(CheckTouchStateTDM(touchState0, 1, 0, 0) && CheckTouchStateTDM(touchState1, 0, 1, 0) && CheckTouchStateTDM(touchState2, 0, 0, 0)))
        else if(CheckTouchStateTDM(touchState1, 0, 0, 0) && CheckTouchStateTDM(touchState2, 1, 0, 0))
        {
            TouchInteraction(TouchGestures.Tap);
        }
        else
        {
            NoneInteractionOnWebview();
            TouchInteraction(TouchGestures.None);
        }
    }

    private void UpdateTouchStateSequence()
    {
        for (int i = 0; i < 5; i++)
        {
            





            if (touchStateAllFinger[i] && !lastTouchStateAllFinger[i])
            {
                dwellingTime[i] = 0.0f;
                movingDistance[i] = 0.0f;
                hoverTime[i] = 0.0f;

                lastTouchPointAllFinger[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                initTouchPointAllFinger[i] = touchPointAllFinger[i];

                touchStateAllFingerTDM[i] = true;
                dwellingStateAllFingerTDM[i] = false;
                movingStateAllFingerTDM[i] = false;
            }
            else if (touchStateAllFinger[i] && lastTouchStateAllFinger[i])
            {
                hoverTime[i] = 0.0f;

                dwellingTime[i] = dwellingTime[i] + Time.deltaTime;
                if (!Single.IsInfinity(lastTouchPointAllFinger[i][0]))
                    movingDistance[i] = movingDistance[i] + Vector3.Distance(touchPointAllFinger[i], lastTouchPointAllFinger[i]);

                if (dwellingTime[i] > dwellingTimeThreshold)
                {
                    if (movingDistance[i] <= movingDistanceThreshold) 
                    {
                        touchStateAllFingerTDM[i] = false;
                        dwellingStateAllFingerTDM[i] = true;
                        movingStateAllFingerTDM[i] = false;
                    }
                    else
                    {
                        touchStateAllFingerTDM[i] = false;
                        dwellingStateAllFingerTDM[i] = false;
                        movingStateAllFingerTDM[i] = true;
                    }
                }

                if(i == 1)
                {
                    debugTouching = "dwelling: " + dwellingTime[i].ToString("F2") + ", moving: " + movingDistance[i].ToString("F2");
                }

                








            }
            //if hovertime is small, maybe a mistake
            /*else if (!touchStateAllFinger[i] && hoverTime[i] < hoverTimeThreshold)
            {
                hoverTime[i] = hoverTime[i] + Time.deltaTime;

                dwellingTime[i] = dwellingTime[i] + Time.deltaTime;
                if (!Single.IsInfinity(lastTouchPointAllFinger[i][0]))
                    movingDistance[i] = movingDistance[i] + Vector3.Distance(touchPointAllFinger[i], lastTouchPointAllFinger[i]);

                if (dwellingTime[i] > dwellingTimeThreshold)
                {
                    if (movingDistance[i] <= movingDistanceThreshold)
                    {
                        touchStateAllFingerTDM[i] = false;
                        dwellingStateAllFingerTDM[i] = true;
                        movingStateAllFingerTDM[i] = false;
                    }
                    else
                    {
                        touchStateAllFingerTDM[i] = false;
                        dwellingStateAllFingerTDM[i] = false;
                        movingStateAllFingerTDM[i] = true;
                    }
                }
            }*/
            else
            {
                hoverTime[i] = hoverTime[i] + Time.deltaTime;
                
                touchStateAllFingerTDM[i] = false;
                dwellingStateAllFingerTDM[i] = false;
                movingStateAllFingerTDM[i] = false;
                initTouchPointAllFinger[i] = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
            }
        }

        InitTouchStateTDM();

        for (int i = 0; i < 5; i++)
        {
            if (touchStateAllFingerTDM[i]) n_touch++;
            if (dwellingStateAllFingerTDM[i]) n_dwelling++;
            if (movingStateAllFingerTDM[i]) n_moving++;    
        }

        UpdateTouchStateTDM();

        
        
        

        
        

        if (touchStateTDM[0] != lastTouchStateTDM[0] || touchStateTDM[1] != lastTouchStateTDM[1] || touchStateTDM[2] != lastTouchStateTDM[2]) {
            
            

            int[] touchStateTemp = new int[3] { touchStateTDM[0], touchStateTDM[1], touchStateTDM[2] };
            touchStateSequence.Add(touchStateTemp);

            













            
            lastTouchStateTDM[0] = touchStateTDM[0];
            lastTouchStateTDM[1] = touchStateTDM[1];
            lastTouchStateTDM[2] = touchStateTDM[2];
        }
    }

    private void CalculateTouchStateAllFinger()
    {
        //if index finger is touching, check other if fingers touch based on distance to surface
        if (touchState)
        {
            for (int i = 0; i < 5; i++)
            {
                lastTouchStateAllFinger[i] = touchStateAllFinger[i];
                if (interactionManager.GetFingerTipToSurfaceDistance(i) < secondaryTouchThreshold)
                    touchStateAllFinger[i] = true;
                else
                    touchStateAllFinger[i] = false;
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                lastTouchStateAllFinger[i] = touchStateAllFinger[i];
                touchStateAllFinger[i] = false;
            }
            InitTouchStateTDM();
        }
        touchStateAllFinger[1] = touchState;
    }

    










    private void UpdateTouchPointAllFinger()
    {
        for (int i = 0; i < 5; i++)
        {
            lastTouchPointAllFinger[i] = touchPointAllFinger[i];
            touchPointAllFinger[i] = interactionManager.GetSurfaceCursorPosition(i);
        }
    }

    private void UpdateInterTouchDistance()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (interactionManager.GetSurfaceCursorState(i) && interactionManager.GetSurfaceCursorState(j))
                {
                    interTouchDistance[i, j] = Vector2.Distance(interactionManager.GetSurfaceCursorRelativeCoordinate(i), interactionManager.GetSurfaceCursorRelativeCoordinate(j));
                }
                else
                {
                    interTouchDistance[i, j] = Mathf.Infinity;
                }
            }
        }
    }

    








    








    private void UpdateTouchDataInit()
    {
        if (interactionManager.GetSurfaceHandCursorState(1)) 
        {
            touchData.Add(
                new string[5] {
                        interactionManager.GetSurfaceCursorRelativeCoordinate(1).x.ToString("F8"),
                        interactionManager.GetSurfaceCursorRelativeCoordinate(1).y.ToString("F8"),
                        interactionManager.GetFingerTipToSurfaceDistance(1).ToString("F8"),
                        interactionManager.GetFingerTipToSurfaceHandDistance(1).ToString("F8"),
                        touchStatus.ToString()
                        //touchState.ToString()
                }
            );
            
        }
    }

    private void UpdateTouchData()
    {
        //debugText.SetText("CollectingStatus: " + touchStatus + "\nTouchInference: " + touchState);
        if (interactionManager.GetSurfaceHandCursorState(1))
        {
            if (Input.GetKey(KeyCode.A)) touchStatus = 0;
            else if (Input.GetKey(KeyCode.B)) touchStatus = 1;
            else if (Input.GetKey(KeyCode.C)) touchStatus = 2;
            else touchStatus = -1;

            if (touchStatus != -1)
            {
                touchData.Add(
                    new string[5] {
                            interactionManager.GetSurfaceCursorRelativeCoordinate(1).x.ToString("F8"),
                            interactionManager.GetSurfaceCursorRelativeCoordinate(1).y.ToString("F8"),
                            interactionManager.GetFingerTipToSurfaceDistance(1).ToString("F8"),
                            interactionManager.GetFingerTipToSurfaceHandDistance(1).ToString("F8"),
                            touchStatus.ToString()
                    }
                );
            }
        }
        else touchStatus = -1;
    }

    private void InitStudyTouchData()
    {
        Debug.Log("task: " + task);
        if (task == OculusWebViewDemo.Tasks.WebFitt)
        {
            studyTouchData.Add(
                new string[50] {
                "Participant Code",
                "Session Code",
                "Condition Code",
                "Task Index",
                "Touch",
                "Cursor X",
                "Cursor Y",
                "Distance to Surface",
                "Speed to Surface",
                "Finger Position X",
                "Finger Position Y",
                "Finger Position Z",
                "Retarget Finger Position X",
                "Retarget Finger Position Y",
                "Retarget Finger Position Z",
                "Hand Position X",
                "Hand Position Y",
                "Hand Position Z",
                "Hand Rotation X",
                "Hand Rotation Y",
                "Hand Rotation Z",
                "Hand Rotation W",
                "Ray Position X",
                "Ray Position Y",
                "Ray Position Z",
                "Ray Rotation X",
                "Ray Rotation Y",
                "Ray Rotation Z",
                "Ray Rotation W",
                "Pad Position X",
                "Pad Position Y",
                "Pad Position Z",
                "Pad Orientation X",
                "Pad Orientation Y",
                "Pad Orientation Z",
                "Pad Orientation W",
                "Webpage Position X",
                "Webpage Position Y",
                "Webpage Position Z",
                "Webpage Orientation X",
                "Webpage Orientation Y",
                "Webpage Orientation Z",
                "Webpage Orientation W",
                "Headset Position X",
                "Headset Position Y",
                "Headset Position Z",
                "Headset Orientation X",
                "Headset Orientation Y",
                "Headset Orientation Z",
                "Headset Orientation W"
                }
            );
        }
        else if (task == OculusWebViewDemo.Tasks.Docking)
        {
            studyTouchData.Add(
                new string[88] {
                "Participant Code",
                "Session Code",
                "Condition Code",
                "Task Index",
                "Touch",
                "Cursor X",
                "Cursor Y",
                "NonDomCursor X",
                "NonDomCursor Y",
                "GazeCursor X",
                "GazeCursor Y",

                "Distance to Surface",
                "Speed to Surface",

                "Finger Position X",
                "Finger Position Y",
                "Finger Position Z",
                "Retarget Finger Position X",
                "Retarget Finger Position Y",
                "Retarget Finger Position Z",

                "NonDom Finger Position X",
                "NonDom Finger Position Y",
                "NonDom Finger Position Z",
                "NonDom Retarget Finger Position X",
                "NonDom Retarget Finger Position Y",
                "NonDom Retarget Finger Position Z",

                "Hand Position X",
                "Hand Position Y",
                "Hand Position Z",
                "Hand Rotation X",
                "Hand Rotation Y",
                "Hand Rotation Z",
                "Hand Rotation W",

                "NonDomHand Position X",
                "NonDomHand Position Y",
                "NonDomHand Position Z",
                "NonDomHand Rotation X",
                "NonDomHand Rotation Y",
                "NonDomHand Rotation Z",
                "NonDomHand Rotation W",

                "PinchRay Position X",
                "PinchRay Position Y",
                "PinchRay Position Z",
                "PinchRay Rotation X",
                "PinchRay Rotation Y",
                "PinchRay Rotation Z",
                "PinchRay Rotation W",

                "Retarget PinchRay Position X",
                "Retarget PinchRay Position Y",
                "Retarget PinchRay Position Z",
                "Retarget PinchRay Rotation X",
                "Retarget PinchRay Rotation Y",
                "Retarget PinchRay Rotation Z",
                "Retarget PinchRay Rotation W",

                "NonDomPinchRay Position X",
                "NonDomPinchRay Position Y",
                "NonDomPinchRay Position Z",
                "NonDomPinchRay Rotation X",
                "NonDomPinchRay Rotation Y",
                "NonDomPinchRay Rotation Z",
                "NonDomPinchRay Rotation W",

                "Retarget NonDomPinchRay Position X",
                "Retarget NonDomPinchRay Position Y",
                "Retarget NonDomPinchRay Position Z",
                "Retarget NonDomPinchRay Rotation X",
                "Retarget NonDomPinchRay Rotation Y",
                "Retarget NonDomPinchRay Rotation Z",
                "Retarget NonDomPinchRay Rotation W",

                "Pad Position X",
                "Pad Position Y",
                "Pad Position Z",
                "Pad Orientation X",
                "Pad Orientation Y",
                "Pad Orientation Z",
                "Pad Orientation W",
                "Webpage Position X",
                "Webpage Position Y",
                "Webpage Position Z",
                "Webpage Orientation X",
                "Webpage Orientation Y",
                "Webpage Orientation Z",
                "Webpage Orientation W",
                "Headset Position X",
                "Headset Position Y",
                "Headset Position Z",
                "Headset Orientation X",
                "Headset Orientation Y",
                "Headset Orientation Z",
                "Headset Orientation W"
                }
            );
        }
    }

    private void UpdateStudyTouchData()
    {
        if (task == OculusWebViewDemo.Tasks.WebFitt && successTouch > 0)
        {
            if (interactionMethod == InteractionMethod.HandRayPointer)
            {
                Vector3 hitPoint = customRayPointer.GetRayCursor();
                Vector2 cursor = new Vector2(10000, 10000);

                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }

                studyTouchData.Add(
                    new string[50] {
                        userID.ToString(),
                        sessionID.ToString(),
                        conditionID.ToString(),
                        currentFittsTaskIndex.ToString(),
                        (Convert.ToInt32(isFingerTouch) + Convert.ToInt32(isNonDomFingerTouch)).ToString(),
                        cursor.x.ToString("F8"),
                        cursor.y.ToString("F8"),
                        "10000",
                        "10000",
                        interactionManager.GetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.w.ToString("F8"),
                        rayObj.transform.position.x.ToString("F8"),
                        rayObj.transform.position.y.ToString("F8"),
                        rayObj.transform.position.z.ToString("F8"),
                        rayObj.transform.rotation.x.ToString("F8"),
                        rayObj.transform.rotation.y.ToString("F8"),
                        rayObj.transform.rotation.z.ToString("F8"),
                        rayObj.transform.rotation.w.ToString("F8"),
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        virtualSurface.transform.position.x.ToString("F8"),
                        virtualSurface.transform.position.y.ToString("F8"),
                        virtualSurface.transform.position.z.ToString("F8"),
                        virtualSurface.transform.rotation.x.ToString("F8"),
                        virtualSurface.transform.rotation.y.ToString("F8"),
                        virtualSurface.transform.rotation.z.ToString("F8"),
                        virtualSurface.transform.rotation.w.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.w.ToString("F8")
                    }
                );
            }
            else if (interactionMethod == InteractionMethod.HandPoke || interactionMethod == InteractionMethod.HandPoke_V2)
            {
                studyTouchData.Add(
                    new string[50] {
                        userID.ToString(),
                        sessionID.ToString(),
                        conditionID.ToString(),
                        currentFittsTaskIndex.ToString(),
                        Convert.ToInt32(isFingerTouch).ToString(),
                        interactionManager.GetWebviewCursorRelativeCoordinate(1).x.ToString("F8"),
                        interactionManager.GetWebviewCursorRelativeCoordinate(1).y.ToString("F8"),
                        interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(1).ToString("F8"),
                        interactionManager.GetInteractionFingertipToVirtualSurfaceFORSpeed().ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.w.ToString("F8"),
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        virtualSurfaceFOR.transform.position.x.ToString("F8"),
                        virtualSurfaceFOR.transform.position.y.ToString("F8"),
                        virtualSurfaceFOR.transform.position.z.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.x.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.y.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.z.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.w.ToString("F8"),
                        virtualSurface.transform.position.x.ToString("F8"),
                        virtualSurface.transform.position.y.ToString("F8"),
                        virtualSurface.transform.position.z.ToString("F8"),
                        virtualSurface.transform.rotation.x.ToString("F8"),
                        virtualSurface.transform.rotation.y.ToString("F8"),
                        virtualSurface.transform.rotation.z.ToString("F8"),
                        virtualSurface.transform.rotation.w.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.w.ToString("F8")
                    }
                );
            }
            else
            {
                studyTouchData.Add(
                    new string[50] {
                        userID.ToString(),
                        sessionID.ToString(),
                        conditionID.ToString(),
                        currentFittsTaskIndex.ToString(),
                        (Convert.ToInt32(isFingerTouch) + Convert.ToInt32(isNonDomFingerTouch)).ToString(),
                        interactionManager.GetWebviewCursorRelativeCoordinate(1).x.ToString("F8"),
                        interactionManager.GetWebviewCursorRelativeCoordinate(1).y.ToString("F8"),
                        interactionManager.GetInteractionFingertipToVirtualSurfaceFORDistance(1).ToString("F8"),
                        interactionManager.GetInteractionFingertipToVirtualSurfaceFORSpeed().ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).x.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).y.ToString("F8"),
                        interactionManager.GetRetargetInteractionFingerTipPosition(1).z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetInteractionWristGameObject().transform.rotation.w.ToString("F8"),
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        "10000",
                        virtualSurfaceFOR.transform.position.x.ToString("F8"),
                        virtualSurfaceFOR.transform.position.y.ToString("F8"),
                        virtualSurfaceFOR.transform.position.z.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.x.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.y.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.z.ToString("F8"),
                        virtualSurfaceFOR.transform.rotation.w.ToString("F8"),
                        virtualSurface.transform.position.x.ToString("F8"),
                        virtualSurface.transform.position.y.ToString("F8"),
                        virtualSurface.transform.position.z.ToString("F8"),
                        virtualSurface.transform.rotation.x.ToString("F8"),
                        virtualSurface.transform.rotation.y.ToString("F8"),
                        virtualSurface.transform.rotation.z.ToString("F8"),
                        virtualSurface.transform.rotation.w.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.position.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.x.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.y.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.z.ToString("F8"),
                        interactionManager.GetCenterEyeGameObject().transform.rotation.w.ToString("F8")
                    }
                );
            }
        }
        else if (task == OculusWebViewDemo.Tasks.Docking)
        {
            Vector3 hitPoint = new Vector3(10000, 10000, 10000);
            Vector2 cursor = new Vector2(10000, 10000);
            Vector2 nonDomCursor = new Vector2(10000, 10000);
            Vector2 gazeCursor = new Vector2(10000, 10000);

            if (interactionMethod == InteractionMethod.HandRayPointer)
            {
                hitPoint = customRayPointer.GetRayCursor();
                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }

                hitPoint = customRayPointer.GetNonDomRayCursor();

                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        nonDomCursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }
            }
            else if (interactionMethod == InteractionMethod.DirectPinch)
            {
                hitPoint = customHandPointer.GetHandPinchCursor();
                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }

                hitPoint = customHandPointer.GetNonDomHandPinchCursor();

                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        nonDomCursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }
            }
            else if (interactionMethod == InteractionMethod.GazePinch || interactionMethod == InteractionMethod.HandMove)
            {
                hitPoint = eyeGazeHandPinch.GetHandPinchCursor();
                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        cursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }

                hitPoint = eyeGazeHandPinch.GetNonDomHandPinchCursor();

                if (hitPoint.x != 10000)
                {
                    var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                    if (activeWebviewPrefab != null)
                    {
                        nonDomCursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                    }
                }

                if (interactionMethod == InteractionMethod.GazePinch)
                {
                    hitPoint = interactionManager.GetSmoothGazeCursor();
                    if (hitPoint.x != 10000)
                    {
                        var activeWebviewPrefab = interactionManager.GetActiveWebviewPrefab();

                        if (activeWebviewPrefab != null)
                        {
                            gazeCursor = interactionManager.ComputeWebviewPointerRelativePosition(hitPoint);
                        }
                    }
                }
            }


            studyTouchData.Add(
                new string[88] {
                    userID.ToString(),
                    sessionID.ToString(),
                    conditionID.ToString(),
                    currentFittsTaskIndex.ToString(),
                    (Convert.ToInt32(isFingerTouch) + Convert.ToInt32(isNonDomFingerTouch)).ToString(),
                    cursor.x.ToString("F8"),
                    cursor.y.ToString("F8"),
                    nonDomCursor.x.ToString("F8"),
                    nonDomCursor.y.ToString("F8"),
                    gazeCursor.x.ToString("F8"),
                    gazeCursor.x.ToString("F8"),
                    interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(1).ToString("F8"),
                    interactionManager.GetInteractionFingertipToVirtualSurfaceFORSpeed().ToString("F8"),
                    interactionManager.GetInteractionFingerTipPosition(1).x.ToString("F8"),
                    interactionManager.GetInteractionFingerTipPosition(1).y.ToString("F8"),
                    interactionManager.GetInteractionFingerTipPosition(1).z.ToString("F8"),
                    interactionManager.GetRetargetInteractionFingerTipPosition(1).x.ToString("F8"),
                    interactionManager.GetRetargetInteractionFingerTipPosition(1).y.ToString("F8"),
                    interactionManager.GetRetargetInteractionFingerTipPosition(1).z.ToString("F8"),

                    interactionManager.GetSurfaceFingerTipPosition(1).x.ToString("F8"),
                    interactionManager.GetSurfaceFingerTipPosition(1).y.ToString("F8"),
                    interactionManager.GetSurfaceFingerTipPosition(1).z.ToString("F8"),
                    interactionManager.GetRetargetSurfaceFingerTipPosition(1).x.ToString("F8"),
                    interactionManager.GetRetargetSurfaceFingerTipPosition(1).y.ToString("F8"),
                    interactionManager.GetRetargetSurfaceFingerTipPosition(1).z.ToString("F8"),

                    interactionManager.GetInteractionWristGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetInteractionWristGameObject().transform.rotation.w.ToString("F8"),


                    interactionManager.GetSurfaceWristGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetSurfaceWristGameObject().transform.rotation.w.ToString("F8"),

                    interactionManager.GetHandPinchArrowGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetHandPinchArrowGameObject().transform.rotation.w.ToString("F8"),

                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetRetargetHandPinchArrowGameObject().transform.rotation.w.ToString("F8"),

                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetNonDomHandPinchArrowGameObject().transform.rotation.w.ToString("F8"),

                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetRetargetNonDomHandPinchArrowGameObject().transform.rotation.w.ToString("F8"),

                    virtualSurfaceFOR.transform.position.x.ToString("F8"),
                    virtualSurfaceFOR.transform.position.y.ToString("F8"),
                    virtualSurfaceFOR.transform.position.z.ToString("F8"),
                    virtualSurfaceFOR.transform.rotation.x.ToString("F8"),
                    virtualSurfaceFOR.transform.rotation.y.ToString("F8"),
                    virtualSurfaceFOR.transform.rotation.z.ToString("F8"),
                    virtualSurfaceFOR.transform.rotation.w.ToString("F8"),
                    virtualSurface.transform.position.x.ToString("F8"),
                    virtualSurface.transform.position.y.ToString("F8"),
                    virtualSurface.transform.position.z.ToString("F8"),
                    virtualSurface.transform.rotation.x.ToString("F8"),
                    virtualSurface.transform.rotation.y.ToString("F8"),
                    virtualSurface.transform.rotation.z.ToString("F8"),
                    virtualSurface.transform.rotation.w.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.position.x.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.position.y.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.position.z.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.rotation.x.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.rotation.y.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.rotation.z.ToString("F8"),
                    interactionManager.GetCenterEyeGameObject().transform.rotation.w.ToString("F8")
                }
            );
        }
    }

    private void UpdateSuccessTouch()
    {
        if (isFittsLawStart) {
            successTouch ++;
        }

        if(successTouch != 0 && (successTouch % fittsTargetNum == 0))
        {
            currentFittsTaskIndex ++;
            successTouch = 0;
        }

        //touch the selection button
        if (successTouch == -1)
        {
            isFittsLawStart = true;
            currentFittsTaskIndex = 0;
        }
    }

    private void UpdateGestureData()
    {
        List<float> kunckleOrientation = snapGestureHelper.GetFingerOrientation();
        float fingertipDistance = snapGestureHelper.GetCumulativeSurfaceFingerTipDistance();
        string[] gestureDataString = new string[11];

        gestureDataString[0] = Time.time.ToString("F8");
        //gestreuDataString[1] = fingertipDistance.ToString("F8");

        for (int i = 1; i < 11; i++)
        {
            gestureDataString[i] = kunckleOrientation[i-1].ToString("F8");
        }

        gestureData.Add(gestureDataString);
    }

    private void UpdateKnucklePosDataData()
    {
        List<Vector3> kuncklePosition = snapGestureHelper.GetFingerKuncklePosition();
        string[] kuncklePosDataString = new string[21];

        kuncklePosDataString[0] = Time.time.ToString("F8");
        for (int i = 1; i < 21; i++)
        {
            kuncklePosDataString[i] = kuncklePosition[i - 1].x.ToString("F8") + "_"
                + kuncklePosition[i - 1].y.ToString("F8") + "_" + kuncklePosition[i - 1].z.ToString("F8");
        }
        knucklePosData.Add(kuncklePosDataString);
    }

    private void UpdateFingerOrientationData()
    {
        List<float> kunckleOrientation = snapGestureHelper.GetFingerOrientation();
        string[] fingerRotDataString = new string[6];

        fingerRotDataString[0] = Time.time.ToString("F8");
        for (int i = 1; i < 6; i++)
        {
            fingerRotDataString[i] = (kunckleOrientation[(i - 1)*2] + kunckleOrientation[(i - 1)*2+1]).ToString("F8");
        }
        fingerRotData.Add(fingerRotDataString);
    }

    private void UpateKunckleOrientationData()
    {
        List<float> kunckleOrientation = snapGestureHelper.GetFingerOrientation();
        

        string kunckleOrientationData = "";

        for (int i = 0;i < kunckleOrientation.Count; i++)
        {
            kunckleOrientationData += kunckleOrientation[i].ToString("F8") + ",";
        }
        kunckleOrientationData = kunckleOrientationData.Remove(kunckleOrientationData.Length-1);
        

        udpSender.sendString(
            kunckleOrientationData
        );
    }

    //request touch state of thumb/index/middle finger within Unity
    private void RequestInteractionTouchStateUpdate()
    {
        //only detect touch for thumb/index/middle
        for (int i = 0; i < 3; i++) 
        {
            lastTouchStateAllFinger[i] = touchStateAllFinger[i];

            if (PrecheckTouchState(i))
            {
                




                if (interactionManager.GetFingerPenetrateSurface(i) || RequestFingerTouchState(i))
                {
                    touchStateAllFinger[i] = true;
                }
                else
                    touchStateAllFinger[i] = false;
                //touchStateAllFinger[i] = RequestFingerTouchState(i);
            }
            else touchStateAllFinger[i] = false;
            interactionManager.SetTouchStateAllFinger(i, touchStateAllFinger[i]);
        }
    }

    private bool RequestFingerTouchState(int idx)
    {
        if (newTouchDetector)
        {
            TouchDetector.TouchPhase currentPhase = touchStates[idx].DetectTouch(interactionManager.GetFingerTipToSurfaceDistance(idx));

            // For debug
            







            if (currentPhase == TouchDetector.TouchPhase.away || currentPhase == TouchDetector.TouchPhase.touching)
                return false;
            else
                return true;
        }
        else
        {
            return touchClassifier.Predict(
                idx,
                interactionManager.GetSurfaceCursorRelativeCoordinate(idx).x,
                interactionManager.GetSurfaceCursorRelativeCoordinate(idx).y,
                interactionManager.GetFingerTipToSurfaceDistance(idx),
                interactionManager.GetFingerTipToSurfaceHandDistance(idx)
            );
        }
    }

    private bool RequestFingerTouchState1(int idx)
    {
        TouchDetector.TouchPhase currentPhase = touchStates[idx].DetectTouch(interactionManager.GetFingerTipToSurfaceDistance(idx));

        // For debug
        







        if (currentPhase == TouchDetector.TouchPhase.away || currentPhase == TouchDetector.TouchPhase.touching)
            return false;
        else
            return true;
    }

    //request touch state of index finger by sending udp message
    private void RequestTouchStateUpdate()
    {
        udpSender.sendString(
            interactionManager.GetSurfaceCursorRelativeCoordinate(1).x.ToString("F8") + "," +
            interactionManager.GetSurfaceCursorRelativeCoordinate(1).y.ToString("F8") + "," +
            interactionManager.GetFingerTipToSurfaceDistance(1).ToString("F8") + "," +
            interactionManager.GetFingerTipToSurfaceHandDistance(1).ToString("F8")
        );
    }

    private void RequestTouchStateUpdateForSpecificFinger(int idx)
    {
        udpSender.sendString(
            idx + "," +
            interactionManager.GetSurfaceCursorRelativeCoordinate(idx).x.ToString("F8") + "," +
            interactionManager.GetSurfaceCursorRelativeCoordinate(idx).y.ToString("F8") + "," +
            interactionManager.GetFingerTipToSurfaceDistance(idx).ToString("F8") + "," +
            interactionManager.GetFingerTipToSurfaceHandDistance(idx).ToString("F8")
        );

    }

    //request touch state of all fingers
    private void RequestTouchStateUpdateAllFinger()
    {
        string touchStateDataAllFinger = "";

        for (int i = 0; i < 5; i++)
        {
            touchStateDataAllFinger += interactionManager.GetSurfaceCursorRelativeCoordinate(i).x.ToString("F8") + ",";
            touchStateDataAllFinger += interactionManager.GetSurfaceCursorRelativeCoordinate(i).y.ToString("F8") + ",";
            touchStateDataAllFinger += interactionManager.GetFingerTipToSurfaceDistance(i).ToString("F8") + ",";
            touchStateDataAllFinger += interactionManager.GetFingerTipToSurfaceHandDistance(i).ToString("F8") + ",";
        }

        touchStateDataAllFinger = touchStateDataAllFinger.Remove(touchStateDataAllFinger.Length - 1);

        udpSender.sendString(
            touchStateDataAllFinger
        );
    }

    public void UpdateTouchState(string state)
    {
        if (state == "1") touchState = true;
        else touchState = false;

        //if (touchState) debugText.SetText("Touch");
        //else debugText.SetText("No Touch");
    }

    public void UpdateTouchStateAllFinger(string state)
    {
        for (int i = 0; i < 5; i++)
        {
            if (state[i] == '1') touchStateAllFinger[i] = true;
            else touchStateAllFinger[i] = false;
        }   
    }

    public void UpdateTouchStateBySpecificFinger(int idx, int state)
    {
        if (state == 1)
        {
            lastTouchStateAllFinger[idx] = touchStateAllFinger[idx];
            touchStateAllFinger[idx] = true;
        }
        else
        {
            lastTouchStateAllFinger[idx] = touchStateAllFinger[idx];
            touchStateAllFinger[idx] = false;
        }
    }

    public void UpdateIndirectTouchStates(int idx, int touchValue)
    {
        if (touchValue > indirectTouchThreshold)
        {
            lastTouchStateAllFinger[idx] = touchStateAllFinger[idx];
            touchStateAllFinger[idx] = true;
        }
        else
        {
            lastTouchStateAllFinger[idx] = touchStateAllFinger[idx];
            touchStateAllFinger[idx] = false;
        }
    }

    public bool isOnHandSurface()
    {
        return isClick;
    }

    private void OnDestroy()
    {
        
        if (collectFittsData && userID != -100)
        {
            string data = DataUtils.ListToCSVString(studyTouchData);
            
            DataUtils.SaveDataToFile(task.ToString() + "StudyTouchData_" + userID.ToString() + "_" + sessionID.ToString() + "_" + conditionID.ToString(), data);
        }

        








































    }

    public class TouchDetector
    {
        static int averagingLength = 10;
        int transitionDuration = 3;
        double[] heightDeltaHist = new double[averagingLength];
        double prevHeight = 0;
        public double heightDeltaAvg = 0;
        double touchingVelocityThresh = -0.002;
        double leavingVelocityThresh = 0.002;
        double heightThreshold = 0.03;
        int touchingCount = 0;
        int leavingCount = 0;

        public enum TouchPhase { touching, leaving, touched, away }
        TouchPhase currentPhase = TouchPhase.away;

        public TouchPhase DetectTouch(double height)
        {
            AddHeightDelta(height - prevHeight);
            prevHeight = height;

            if (currentPhase == TouchPhase.away)
            {
                if (heightDeltaAvg < touchingVelocityThresh)
                {
                    touchingCount = 0;
                    currentPhase = TouchPhase.touching;
                }
            }
            else if (currentPhase == TouchPhase.touching)
            {
                if (heightDeltaAvg < touchingVelocityThresh)
                {
                    if (touchingCount < transitionDuration)
                        touchingCount++;
                    else
                    {
                        touchingCount = 0;
                        currentPhase = TouchPhase.touched;
                    }
                }
                else
                {
                    currentPhase = TouchPhase.away;
                }
            }
            else if (currentPhase == TouchPhase.touched)
            {
                if (heightDeltaAvg > leavingVelocityThresh)
                {
                    leavingCount = 0;
                    currentPhase = TouchPhase.leaving;
                }
            }
            else if (currentPhase == TouchPhase.leaving)
            {
                if (heightDeltaAvg > leavingVelocityThresh)
                {
                    if (leavingCount < transitionDuration)
                        leavingCount++;
                    else
                    {
                        leavingCount = 0;
                        currentPhase = TouchPhase.away;
                    }
                }
                else
                {
                    currentPhase = TouchPhase.touched;
                }
            }

            if (height > heightThreshold)
                currentPhase = TouchPhase.away;
            return currentPhase;
        }

        int heightIdx = 0;
        private void AddHeightDelta(double heightDelta)
        {
            if (heightIdx < averagingLength - 1)
                heightIdx++;
            else
                heightIdx = 0;
            heightDeltaHist[heightIdx] = heightDelta;
            heightDeltaAvg = heightDeltaHist.Average();
        }
    }
}
