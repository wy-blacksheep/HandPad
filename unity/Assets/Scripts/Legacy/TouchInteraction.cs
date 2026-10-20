using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TouchInteraction : MonoBehaviour
{
    enum Handedness
    {
        Right,
        Left
    };

    [SerializeField]
    private Handedness dominantHand;

    [SerializeField]
    [Range(0.0f, 0.5f)] private float hoverDistanceThreshold = 0.05f;

    [SerializeField]
    private Boolean visualizeSurfaceCursors = true;

    [SerializeField]
    private Boolean visualizeSurfaceHandCursors = true;

    [SerializeField]
    private Boolean visualizeSurfaceHand = false;

    private Handedness interactionHandedness;
    private Handedness surfaceHandedness;

    private GameObject surfaceOffset;
    
    private GameObject surface;
    private GameObject surfaceBottomLeft, surfaceBottomRight, surfaceTopLeft, surfaceTopRight;
    private Vector3 surfaceXAxis, surfaceYAxis, surfaceNormal;

    private GameObject surfaceHand;

    private GameObject[] interactionFingerTips = new GameObject[5];

    private GameObject surfaceWrist;
    private GameObject[] surfaceFingerTips = new GameObject[5];

    private GameObject[] surfaceCursors = new GameObject[5];
    private Vector3[] surfaceCursorPositions = new Vector3[5];
    private Boolean[] surfaceCursorStates = new Boolean[5];
    
    private GameObject[] surfaceHandCursors = new GameObject[5];
    private Vector3[] surfaceHandCursorPositions = new Vector3[5];
    private Boolean[] surfaceHandCursorStates = new Boolean[5];

    private float[,] surfaceFingerTipDistances = new float[5, 5];
    
    private float[,] interactionFingerTipDistances = new float[5, 5];
    private float[] fingerTipDistanceToSurface = new float[5];
    private float[] fingerTipDistanceToSurfaceHand = new float[5];
    private Vector2[] surfaceCursorRelativeCoordinates = new Vector2[5];

    private Boolean touchState = false;
    private List<string[]> touchData = new List<string[]>();
    private UDPSender udpSender;

    private TextMeshProUGUI text;


    void Start()
    {
        text = GameObject.Find("Text").GetComponent<TextMeshProUGUI>();
        udpSender = GetComponent<UDPSender>();
        InitHandedness();
        InitSurface();
        InitCursors();
    }

    void Update()
    {
        UpdateSurface();
        UpdateFingerInteractionValues();

        VisualizeSurfaceHand();
        VisualizeSurfaceCursors();
        VisualizeSurfaceHandCursors();

        UpdateFingerData();

        UpdateGestureDebugger();
    }

    void InitCursors()
    {
        Instantiate(new GameObject("SurfaceCursors"));
        Instantiate(new GameObject("SurfaceHandCursors"));
        for (int i = 0; i < 5; i++)
        {
            surfaceCursors[i] = Instantiate(Resources.Load("Prefabs/CursorPrefab") as GameObject);
            surfaceCursors[i].transform.parent = GameObject.Find("SurfaceCursors").transform;
            surfaceHandCursors[i] = Instantiate(Resources.Load("Prefabs/PointPrefab") as GameObject);
            surfaceHandCursors[i].transform.parent = GameObject.Find("SurfaceHandCursors").transform;
        }
    }

    void InitHandedness()
    {
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
        
        if (interactionHandedness == Handedness.Left) surfaceHandedness = Handedness.Right;
        else surfaceHandedness = Handedness.Left;

        if (surfaceHandedness == Handedness.Left) surfaceOffset = GameObject.Find("LeftHandSurfaceOffset");
        else surfaceOffset = GameObject.Find("RightHandSurfaceOffset");

        if (surfaceHandedness == Handedness.Left) surfaceHand = GameObject.Find("l_handMeshNode");
        else surfaceHand = GameObject.Find("r_handMeshNode");

        if (interactionHandedness == Handedness.Left)
        {
            interactionFingerTips[0] = GameObject.Find("l_thumb_finger_tip_marker");
            interactionFingerTips[1] = GameObject.Find("l_index_finger_tip_marker");
            interactionFingerTips[2] = GameObject.Find("l_middle_finger_tip_marker");
            interactionFingerTips[3] = GameObject.Find("l_ring_finger_tip_marker");
            interactionFingerTips[4] = GameObject.Find("l_pinky_finger_tip_marker");

            surfaceWrist = GameObject.Find("b_r_wrist");
            surfaceFingerTips[0] = GameObject.Find("r_thumb_finger_tip_marker");
            surfaceFingerTips[1] = GameObject.Find("r_index_finger_tip_marker");
            surfaceFingerTips[2] = GameObject.Find("r_middle_finger_tip_marker");
            surfaceFingerTips[3] = GameObject.Find("r_ring_finger_tip_marker");
            surfaceFingerTips[4] = GameObject.Find("r_pinky_finger_tip_marker");
        }
        else
        {
            interactionFingerTips[0] = GameObject.Find("r_thumb_finger_tip_marker");
            interactionFingerTips[1] = GameObject.Find("r_index_finger_tip_marker");
            interactionFingerTips[2] = GameObject.Find("r_middle_finger_tip_marker");
            interactionFingerTips[3] = GameObject.Find("r_ring_finger_tip_marker");
            interactionFingerTips[4] = GameObject.Find("r_pinky_finger_tip_marker");

            surfaceWrist = GameObject.Find("b_l_wrist");
            surfaceFingerTips[0] = GameObject.Find("l_thumb_finger_tip_marker");
            surfaceFingerTips[1] = GameObject.Find("l_index_finger_tip_marker");
            surfaceFingerTips[2] = GameObject.Find("l_middle_finger_tip_marker");
            surfaceFingerTips[3] = GameObject.Find("l_ring_finger_tip_marker");
            surfaceFingerTips[4] = GameObject.Find("l_pinky_finger_tip_marker");
        }
    }

    void VisualizeSurfaceHand()
    {
        if (visualizeSurfaceHand)
        {
            surfaceHand.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0.7f);
        }
        else
        {
            surfaceHand.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
        }
    }

    void InitSurface()
    {
        surface = GameObject.Find("Surface");

        surfaceBottomLeft = new GameObject("SurfaceBottomLeft");
        surfaceBottomRight = new GameObject("SurfaceBottomRight");
        surfaceTopLeft = new GameObject("SurfaceTopLeft");
        surfaceTopRight = new GameObject("SurfaceTopRight");

        surfaceBottomLeft.transform.parent = surface.transform;
        surfaceBottomRight.transform.parent = surface.transform;
        surfaceTopLeft.transform.parent = surface.transform;
        surfaceTopRight.transform.parent = surface.transform;

        surfaceBottomLeft.transform.position = new Vector3(surface.transform.position.x - 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z - 0.5f * surface.transform.localScale.z);
        surfaceBottomRight.transform.position = new Vector3(surface.transform.position.x + 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z - 0.5f * surface.transform.localScale.z);
        surfaceTopLeft.transform.position = new Vector3(surface.transform.position.x - 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z + 0.5f * surface.transform.localScale.z);
        surfaceTopRight.transform.position = new Vector3(surface.transform.position.x + 0.5f * surface.transform.localScale.x, surface.transform.position.y, surface.transform.position.z + 0.5f * surface.transform.localScale.z);
    }

    void UpdateSurface()
    {
        surface.transform.position = surfaceOffset.transform.position;
        surface.transform.rotation = surfaceOffset.transform.rotation;

        surfaceXAxis = surfaceBottomRight.transform.position - surfaceBottomLeft.transform.position;
        surfaceYAxis = surfaceTopLeft.transform.position - surfaceBottomLeft.transform.position;
        surfaceNormal = Vector3.Cross(surfaceYAxis, surfaceXAxis).normalized;
        
        Debug.DrawLine(surfaceBottomLeft.transform.position, surfaceBottomLeft.transform.position + (100.0f * surfaceNormal), Color.magenta);
    }
    
    void ComputeSurfaceTouchCoordinates()
    {
        // compute normal to physicalsurface
    }

    void ComputeRelativeSurfaceTouchCoordinates()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 xAxisProjection = surfaceBottomLeft.transform.position + Vector3.Project(surfaceCursorPositions[i] - surfaceBottomLeft.transform.position, surfaceXAxis);
            Vector3 yAxisProjection = surfaceBottomLeft.transform.position + Vector3.Project(surfaceCursorPositions[i] - surfaceBottomLeft.transform.position, surfaceYAxis);
            
            float xAxisDistance = Vector3.Distance(surfaceBottomLeft.transform.position, xAxisProjection);
            float yAxisDistance = Vector3.Distance(surfaceBottomLeft.transform.position, yAxisProjection);
            
            xAxisDistance = xAxisDistance / Vector3.Distance(surfaceBottomLeft.transform.position, surfaceBottomRight.transform.position);
            yAxisDistance = yAxisDistance / Vector3.Distance(surfaceBottomLeft.transform.position, surfaceTopLeft.transform.position);

            surfaceCursorRelativeCoordinates[i] = new Vector2(xAxisDistance, yAxisDistance);
        }
    }

    void UpdateFingerInteractionValues()
    {
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            
            if (Physics.Raycast(interactionFingerTips[i].transform.position, -1.0f * surfaceNormal, out hit, 1.0f, LayerMask.GetMask("SurfaceLayer")))
            {
                if (hit.collider.gameObject == surface)
                {
                    Debug.Log("Hit" + i.ToString());
                    surfaceCursorStates[i] = true;
                    surfaceCursorPositions[i] = hit.point;
                    fingerTipDistanceToSurface[i] = Vector3.Distance(interactionFingerTips[i].transform.position, surfaceCursorPositions[i]);
                }
            }
            else
            {
                surfaceCursorStates[i] = false;
            }


            if (surfaceCursorStates[i] && Physics.Raycast(interactionFingerTips[i].transform.position, surfaceCursorPositions[i] - interactionFingerTips[i].transform.position, out hit, 1.0f, LayerMask.GetMask("SurfaceHandLayer")))
            {
                if (hit.collider.gameObject == surfaceHand)
                {
                    surfaceHandCursorStates[i] = true;
                    surfaceHandCursorPositions[i] = hit.point;
                    fingerTipDistanceToSurfaceHand[i] = Vector3.Distance(interactionFingerTips[i].transform.position, surfaceHandCursorPositions[i]);

                    SendFingerDataForTouchDetection();
                }
            }
            else
            {
                surfaceHandCursorStates[i] = false;
            }
        }


        ComputeRelativeSurfaceTouchCoordinates();
        ComputeInterFingerTipDistances();
    }

    void ComputeInterFingerTipDistances()
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                interactionFingerTipDistances[i, j] = Vector3.Distance(interactionFingerTips[i].transform.position, interactionFingerTips[j].transform.position);
                surfaceFingerTipDistances[i, j] = Vector3.Distance(surfaceFingerTips[i].transform.position, surfaceFingerTips[j].transform.position);
            }
        }
    }

    void VisualizeSurfaceCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            if (visualizeSurfaceCursors && surfaceCursorStates[i])
            {
                surfaceCursors[i].SetActive(true);
            }
            else
            {
                surfaceCursors[i].SetActive(false);
            }
        }

        Color color;
        if (visualizeSurfaceCursors)
        {
            for (int i = 0; i < 5; i++)
            {
                surfaceCursors[i].transform.position = surfaceCursorPositions[i];
                surfaceCursors[i].transform.rotation = surface.transform.rotation;
                if (fingerTipDistanceToSurface[i] > hoverDistanceThreshold) surfaceCursors[i].GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
                else
                {
                    color = surfaceCursors[i].GetComponent<Renderer>().material.color;
                    color.a = 1.0f - (fingerTipDistanceToSurface[i] / hoverDistanceThreshold);
                    surfaceCursors[i].GetComponent<Renderer>().material.color = color;
                }
            }
        }
    }

    void VisualizeSurfaceHandCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            if (visualizeSurfaceHandCursors && surfaceHandCursorStates[i])
            {
                surfaceHandCursors[i].SetActive(true);
            }
            else
            {
                surfaceHandCursors[i].SetActive(false);
            }
        }

        Color color;
        if (visualizeSurfaceHandCursors)
        {
            for (int i = 0; i < 5; i++)
            {
                surfaceHandCursors[i].transform.position = surfaceHandCursorPositions[i];
                surfaceHandCursors[i].transform.rotation = surface.transform.rotation;
                if (fingerTipDistanceToSurfaceHand[i] > hoverDistanceThreshold) surfaceHandCursors[i].GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0);
                else
                {
                    color = surfaceHandCursors[i].GetComponent<Renderer>().material.color;
                    color.a = 1.0f - (fingerTipDistanceToSurfaceHand[i] / hoverDistanceThreshold);
                    surfaceHandCursors[i].GetComponent<Renderer>().material.color = color;
                }
            }
        }
    }

    public void UpdateTouchState(string data)
    {
        if (data == "1") touchState = true;
        else touchState = false;
    }


    void UpdateFingerData()
    {
        if (fingerTipDistanceToSurface[1] < hoverDistanceThreshold)
        {
            if (surfaceHandCursorStates[1] && surfaceCursorStates[1])
            {
                touchData.Add(
                    new string[5] {
                        surfaceCursorRelativeCoordinates[1].x.ToString("F8"),
                        surfaceCursorRelativeCoordinates[1].y.ToString("F8"),
                        fingerTipDistanceToSurface[1].ToString("F8"),
                        fingerTipDistanceToSurfaceHand[1].ToString("F8"),
                        touchState.ToString()
                    }
                );
            }
        }
    }

    void SendFingerDataForTouchDetection()
    {
        string fingerData = (
            surfaceCursorRelativeCoordinates[1].x.ToString("F8") + "," +
            surfaceCursorRelativeCoordinates[1].y.ToString("F8") + "," +
            fingerTipDistanceToSurface[1].ToString("F8") + "," +
            fingerTipDistanceToSurfaceHand[1].ToString("F8")
        );
        udpSender.sendString(fingerData);
    }

    void UpdateGestureDebugger() // Temporary Function
    {
        float sumOfAllFingerTipDistance = 0.0f;
        float sumOfWristToFingerTipsDistance = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            sumOfAllFingerTipDistance += surfaceFingerTipDistances[i, i + 1];
            sumOfWristToFingerTipsDistance += Vector3.Distance(surfaceFingerTips[i].transform.position, surfaceWrist.transform.position);
        }
        text.SetText(sumOfAllFingerTipDistance.ToString() + "\n" + sumOfWristToFingerTipsDistance);
    }

    private void OnDestroy()
    {
        string header = "x,y,distanceToSurface,distanceToSurfaceHand,touchState\n";
        string data = DataUtils.ListToCSVString(touchData);
        data = header + data;
        DataUtils.SaveDataToFile("touchData", data);
    }



}
