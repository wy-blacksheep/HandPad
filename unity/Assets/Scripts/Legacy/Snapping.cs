using System;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Collections;

// Legacy component retained by a Resources prefab; study scenes use SnapGestureController.
public class Snapping : MonoBehaviour
{
    [SerializeField]
    private GameObject virtualSurface;

    //will be manipulated
    //contain: 1. surface model; 2. relative relationship between the surface and the dominant (right) hand
    [SerializeField]
    private GameObject physicalSurfaceParent;

    //always go with the hand
    //contain: 1. surface model; 2. manipulated hand model (dominant right hand)
    [SerializeField]
    private GameObject handSurfaceParent;

    [SerializeField]
    private GameObject surfaceToUpdateTexture;

    [SerializeField]
    private GameObject interactionSpace;

    //markers attached to physical surface for tracking/left hand anchor
    [SerializeField]
    private GameObject markerToTrack;

    [SerializeField]
    private GameObject eye;

    //left hand root: wrist
    [SerializeField]
    private GameObject leftHandParent;

    //left hand root: wrist
    [SerializeField]
    private GameObject rightHandParent;

    [SerializeField]
    private GameObject virtualRightHand;

    //[SerializeField]
    private GameObject inputFingertip;

    [SerializeField]
    private GameObject rightHandRelativeToSurface;

    [SerializeField]
    private GameObject relocateDomHand;

    [SerializeField]
    private GameObject cursorMarker;

    //threshold for snapping
    [SerializeField, Range(0, 10.0f)]
    private float minDistanceBeforeSnap;

    //threshold for unsnapping
    [SerializeField, Range(0, 10.0f)]
    private float maxDistanceAfterSnap;

    //2d line
    [SerializeField, Range(0, 30)]
    private int lineWidth = 5;

    [SerializeField]
    private Color lineColor = Color.white;

    private bool isSnap;
    private bool isHandFound;

    private Vector3 eyePos;
    private Vector3 handtipPos;
    private Vector3 handBodyPos;

    private Texture2D surfaceTexture;
    private Color[] lineColors;

    private List<string[]> touch_data = new List<string[]>();
    private List<Vector2> touch_rel_pos = new List<Vector2>();
    private List<float> dis_fingertip_lefthand = new List<float>();
    private List<float> dis_fingertip_surface = new List<float>();
    //not touching:0, is touching:1
    private List<string> istouching = new List<string>();
    private string lastTouchData = "X";

    private GameObject physicalSurface;
    private GameObject handSurface;

    private GameObject rightHand;
    private GameObject leftHand;

    // Start is called before the first frame update
    void Start()
    {
        //init status: upsnap, assign the physical surface to the marker pos
        unSnap();
        getInitTexture();
        lineColors = Enumerable.Repeat(lineColor, lineWidth * lineWidth).ToArray();

        isSnap = false;
        isHandFound = false;

        physicalSurface = physicalSurfaceParent.transform.Find("PhysicalSurface").gameObject;
        handSurface = handSurfaceParent.transform.Find("HandSurface").gameObject;

        rightHand = rightHandParent.transform.Find("RightOVRHand").gameObject;
        leftHand = leftHandParent.transform.Find("LeftOVRHand").gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            getInitTexture();
        }

        if (rightHandParent.transform.Find("RightOVRHand/Bones/Hand_WristRoot/Hand_Index1/Hand_Index2/Hand_Index3/Hand_IndexTip") != null)
        {
            isHandFound = true;
            inputFingertip = rightHandParent.transform.Find("RightOVRHand/Bones/Hand_WristRoot/Hand_Index1/Hand_Index2/Hand_Index3/Hand_IndexTip").gameObject;
            handtipPos = inputFingertip.transform.position;
        }
        else
        {
            isHandFound = false;
        }

        //Debug.Log(isHandFound);

        if (isHandFound)
        {
            eyePos = eye.transform.position;
            handBodyPos = rightHandParent.transform.position;

            getHandSurface();

            if (!isSnap)
            {
                //snap
                float dis = calcDistance(physicalSurfaceParent.transform.position, virtualSurface.transform.position);
                //Debug.Log("Dis: " + dis);

                if (dis < minDistanceBeforeSnap)
                {
                    snapPhysicaltoVirtual();
                }
                else
                {
                    unSnap();
                }
            }
            else
            {
                //unsnap
                float dis = calcDistance(handSurfaceParent.transform.position, virtualSurface.transform.position);
                //Debug.Log("Dis: " + dis);

                if (dis > maxDistanceAfterSnap)
                {
                    unSnap();
                }
                else
                {
                    //keep snapping
                    //perception manipulation：assume the left hand is fixed as the virtual surface,
                    //manipulating the right hand position to make the user feel that two hands maintain the relationship with the virtual surface

                    PerceptionManipulation();

                    //do the interaction
                    Interaction();
                }
            }
        }
    }

    void getInitTexture()
    {
        string filename = "";
        if (interactionSpace.name == "[PHYSICAL HAND SURFACE]")
        {
            filename = "Assets/Resources/Materials/white_texture.png";
        }
        else
        {
            ;
        }

        var rawData = System.IO.File.ReadAllBytes(filename);
        surfaceTexture = new Texture2D(surfaceToUpdateTexture.GetComponent<Renderer>().material.mainTexture.width, (int)surfaceToUpdateTexture.GetComponent<Renderer>().material.mainTexture.height);
        surfaceTexture.LoadImage(rawData);

        surfaceToUpdateTexture.GetComponent<Renderer>().material.mainTexture = surfaceTexture;

        Debug.Log("init texture");
    }


    float calcDistance(Vector3 p1, Vector3 p2)
    {
        return Vector3.Distance(p1, p2);
    }

    void getHandSurface()
    {
        handSurfaceParent.transform.position = markerToTrack.transform.position;
        handSurfaceParent.transform.rotation = markerToTrack.transform.rotation;
    }

    void snapPhysicaltoVirtual()
    {
        //Debug.Log("snapPtoV: " + virtualSurface.transform.position);
        physicalSurfaceParent.transform.position = virtualSurface.transform.position;
        physicalSurfaceParent.transform.rotation = virtualSurface.transform.rotation;

        isSnap = true;
    }

    void snapVirtualtoPhysical()
    {
        //Debug.Log("snapVtoP: " + physicalSurface.transform.position);
        virtualSurface.transform.position = physicalSurfaceParent.transform.position;
        virtualSurface.transform.rotation = physicalSurfaceParent.transform.rotation;

        isSnap = true;
    }

    void unSnap()
    {
        //Debug.Log("upSnap: " + markerToTrack.transform.position);
        relocateDomHand.transform.position = rightHandParent.transform.position;
        relocateDomHand.transform.rotation = rightHandParent.transform.rotation;

        physicalSurfaceParent.transform.position = handSurfaceParent.transform.position;
        physicalSurfaceParent.transform.rotation = handSurfaceParent.transform.rotation;

        isSnap = false;
    }

    private void PerceptionManipulation()
    {
        //manipulate the dominant hand
        //make the relative relationship between the physical surface and manipulated dom hand
        //same as the one between the hand surface and real dom hand
        rightHandRelativeToSurface.transform.position = rightHandParent.transform.position;
        rightHandRelativeToSurface.transform.rotation = rightHandParent.transform.rotation;

        relocateDomHand.transform.localPosition = rightHandRelativeToSurface.transform.localPosition;
        relocateDomHand.transform.localRotation = rightHandRelativeToSurface.transform.localRotation;
    }

    private void Interaction()
    {
        //color the contact point on the physical surface
        RaycastHit touchLeftHandTexture;
        float fingertip_lefthand_dis;

        RaycastHit touchTexture;
        Vector2 touchTexturePos;

        leftHand.transform.GetComponent<MeshCollider>().convex = true;
        Collider leftHandCollider = leftHand.GetComponent<Collider>();
        Vector3 closestOnLeftHand = leftHandCollider.ClosestPoint(handtipPos);
        leftHand.transform.GetComponent<MeshCollider>().convex = false;

        //find the contact point on the real left hand by raycasting
        Ray rayToLeftHand = new Ray(handtipPos, closestOnLeftHand - handtipPos);

        handSurface.transform.GetComponent<MeshCollider>().convex = true;
        Collider surfaceCollider = handSurface.GetComponent<Collider>();
        Vector3 closestOnHandSurface = surfaceCollider.ClosestPoint(handtipPos);
        handSurface.transform.GetComponent<MeshCollider>().convex = false;

        //find the contact point on the physical hand surface by raycasting
        Ray ray = new Ray(handtipPos, closestOnHandSurface - handtipPos);
        if (surfaceCollider.Raycast(ray, out touchTexture, Mathf.Infinity)
                && leftHandCollider.Raycast(rayToLeftHand, out touchLeftHandTexture, Mathf.Infinity))
        {
            if (touchTexture.transform.CompareTag("hand_surface")
                && touchLeftHandTexture.transform.CompareTag("physical_left_hand"))
            {
                cursorMarker.transform.position = closestOnHandSurface;
                UpdateCursorColorTouch();

                istouching.Add(lastTouchData);

                //shortest distance from the fingertip to the real left hand
                fingertip_lefthand_dis = touchLeftHandTexture.distance;
                dis_fingertip_lefthand.Add(touchTexture.distance);

                //shortest distance from the fingertip to the hand surface
                //dis_fingertip_surface.Add(touchTexture.distance);

                touchTexturePos = new Vector2(touchTexture.textureCoord.x, touchTexture.textureCoord.y);

                touch_rel_pos.Add(touchTexturePos);

                //touchpos:x,y; distance
                CheckTouchState(touchTexturePos.x, touchTexturePos.y, fingertip_lefthand_dis);

                Debug.Log(touchTexturePos.x + "," + touchTexturePos.y + "," + fingertip_lefthand_dis);

                if (lastTouchData == "1")
                {
                    //touch
                    var x = (int)(touchTexturePos.x * surfaceTexture.width - (lineWidth / 2));
                    var y = (int)(touchTexturePos.y * surfaceTexture.height - (lineWidth / 2));

                    //Debug.Log(x + ", " + y);

                    if (y < 0 || y > surfaceTexture.height || x < 0 || x > surfaceTexture.width)
                        return;

                    surfaceTexture.SetPixels(x, y, lineWidth, lineWidth, lineColors);

                    surfaceTexture.Apply();
                }
            }
        }
    }

    void OnDestroy()
    {
        SaveDataToFile();
        ClearData();
    }

    void SaveDataToFile()
    {
        var rootPath = Path.Combine(Application.persistentDataPath, "Data") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(rootPath);

        // Creating First row of titles of each column manually..
        string[] rowDataCol = new string[4];
        rowDataCol[0] = "touch_rel_pos_x";
        rowDataCol[1] = "touch_rel_pos_y";
        rowDataCol[2] = "dis_fingertip_lefthand";
        //rowDataCol[2] = "dis_fingertip_surface";
        rowDataCol[3] = "istouching";
        touch_data.Add(rowDataCol);

        for (int i = 0; i < istouching.Count; i++)
        {
            rowDataCol = new string[4];
            rowDataCol[0] = touch_rel_pos[i].x.ToString("F8");
            rowDataCol[1] = touch_rel_pos[i].y.ToString("F8");
            rowDataCol[2] = dis_fingertip_lefthand[i].ToString("F8");
            //rowDataCol[2] = dis_fingertip_surface[i].ToString("F8");
            rowDataCol[3] = istouching[i];
            touch_data.Add(rowDataCol);
        }

        string[][] output = new string[touch_data.Count][];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = touch_data[i];
        }

        int length = output.GetLength(0);
        string delimiter = ",";

        StringBuilder sb = new StringBuilder();

        for (int index = 0; index < length; index++)
            sb.AppendLine(string.Join(delimiter, output[index]));

        string filePath = rootPath + "touchData" + string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}",
            System.DateTime.Now) + ".csv";
        //string filePath ="task1_drawing" + ".csv"; 
        StreamWriter outStream = System.IO.File.CreateText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
    }

    void ClearData()
    {
        touch_rel_pos.Clear();
        dis_fingertip_lefthand.Clear();
        dis_fingertip_surface.Clear();
        istouching.Clear();
    }

    void CheckTouchState(float x, float y, float dis)
    {
        string touch_data = x.ToString("F8") + "," + y.ToString("F8") + "," + dis.ToString("F8");
        interactionSpace.GetComponent<UDPSender>().sendString(touch_data);
    }

    public void UpdateTouchData(string data)
    {
        lastTouchData = data;
    }

    private void UpdateCursorColorTouch()
    {
        if (lastTouchData == "0")
        {
            // curosr material is red
            cursorMarker.GetComponent<Renderer>().material = Resources.Load("Materials/Red", typeof(Material)) as Material;

        }
        else
        {
            // cursor material is green
            cursorMarker.GetComponent<Renderer>().material = Resources.Load("Materials/Green", typeof(Material)) as Material;
        }
    }

}
