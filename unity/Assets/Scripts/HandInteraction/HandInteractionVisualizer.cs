using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(HandInteractionManager))]
[RequireComponent(typeof(TouchGestureController))]
[RequireComponent(typeof(HandPointer))]
public class HandInteractionVisualizer : MonoBehaviour
{
    [SerializeField]
    private Boolean visualizeSurfaceCursors = true;

    [SerializeField]
    private Boolean visualizeSurfaceHandCursors = true;

    [SerializeField]
    private Boolean visualizeRetargetCursors = true;

    [SerializeField]
    private Boolean visualizeRayCursor = true;

    private HandInteractionManager interactionManager;
    private TouchGestureController touchGestureController;
    private HandPointer handPointer;

    private GameObject[] surfaceCursors = new GameObject[5];
    private GameObject[] surfaceHandCursors = new GameObject[5];
    private GameObject[] retargetCursors = new GameObject[5];
    private GameObject rayCursor;

    private Color defaultColor;
    private Color touchColor = Color.red;
    private Color realTouchColor = Color.green;

    private GameObject pointerRay;
    private LineRenderer hitRay;
    private Boolean drawHitRay = false;

    void Start()
    {
        //pointerRay = new GameObject("pointerRay");
        //hitRay = pointerRay.AddComponent<LineRenderer>();

        interactionManager = GetComponent<HandInteractionManager>();
        touchGestureController = GetComponent<TouchGestureController>();
        handPointer = GetComponent<HandPointer>();

        InitCursors();
    }
    
    void Update()
    {
        if (visualizeSurfaceCursors)
        {
            ShowCursors(surfaceCursors, true);
            ShowCursors(surfaceHandCursors, false);
            ShowCursors(retargetCursors, false);
            ShowRayCursor(rayCursor, false);
            VisualizeSurfaceCursors();
        }
        if (visualizeSurfaceHandCursors)
        {
            ShowCursors(surfaceCursors, false);
            ShowCursors(surfaceHandCursors, true);
            ShowCursors(retargetCursors, false);
            ShowRayCursor(rayCursor, false);
            VisualizeSurfaceHandCursors();
        }
        if (visualizeRetargetCursors)
        {
            ShowCursors(surfaceCursors, false);
            ShowCursors(surfaceHandCursors, false);
            ShowCursors(retargetCursors, true);
            ShowRayCursor(rayCursor, false);
            VisualizeRetargetCursors();
        }
        if(visualizeRayCursor && interactionManager.GetActiveWebviewGameObject() != null)
        {
            ShowCursors(surfaceCursors, false);
            ShowCursors(surfaceHandCursors, false);
            ShowCursors(retargetCursors, false);
            ShowRayCursor(rayCursor, true);
            VisualizeRayCursor();
        }
    }

    void InitCursors()
    {
        Instantiate(new GameObject("SurfaceCursors"));
        GameObject.Find("SurfaceCursors").transform.position = new Vector3(10000, 10000, 10000);
        Instantiate(new GameObject("SurfaceHandCursors"));
        GameObject.Find("SurfaceHandCursors").transform.position = new Vector3(10000, 10000, 10000);
        Instantiate(new GameObject("RetargetCursors"));
        GameObject.Find("RetargetCursors").transform.position = new Vector3(10000, 10000, 10000);

        for (int i = 0; i < 5; i++)
        {
            surfaceCursors[i] = Instantiate(Resources.Load("Prefabs/HandSurfaceCursorPrefab") as GameObject);
            surfaceCursors[i].transform.parent = GameObject.Find("SurfaceCursors").transform;
            SetCursorAlpha(surfaceCursors[i], 0.0f);

            surfaceHandCursors[i] = Instantiate(Resources.Load("Prefabs/PointPrefab") as GameObject);
            surfaceHandCursors[i].transform.parent = GameObject.Find("SurfaceHandCursors").transform;
            SetCursorAlpha(surfaceHandCursors[i], 0.0f);

            retargetCursors[i] = Instantiate(Resources.Load("Prefabs/CursorPrefab") as GameObject);
            retargetCursors[i].transform.parent = GameObject.Find("RetargetCursors").transform;
            SetCursorAlpha(retargetCursors[i], 0.0f);
        }
        defaultColor = retargetCursors[0].GetComponent<Renderer>().material.color;

        rayCursor = GameObject.Find("RayCursor");
    }

    void ShowCursors(GameObject[] objects, bool isShow)
    {
        for (int i = 0; i < 5; i++)
        {
            objects[i].GetComponent<Renderer>().enabled = isShow;
        }
    }

    void ShowRayCursor(GameObject obj, bool isShow)
    {
        obj.GetComponent<Renderer>().enabled = isShow;
    }

    void VisualizeSurfaceCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            if (visualizeSurfaceCursors && interactionManager.GetSurfaceCursorState(i))
            {
                SetCursorAlpha(surfaceCursors[i], 1.0f);
            }
            else
            {
                SetCursorAlpha(surfaceCursors[i], 0.0f);
            }
        }
        
        if (visualizeSurfaceCursors)
        {
            for (int i = 0; i < 5; i++)
            {
                if (interactionManager.GetSurfaceCursorState(i))
                {
                    surfaceCursors[i].transform.position = interactionManager.GetSurfaceCursorPosition(i);
                    surfaceCursors[i].transform.rotation = interactionManager.GetSurfaceRotation();
                    if (interactionManager.GetFingerTipToSurfaceDistance(i) > interactionManager.GetHoverDistanceThreshold())
                    {
                        SetCursorColor(surfaceCursors[i], defaultColor);
                        SetCursorAlpha(surfaceCursors[i], 0.0f);
                    }
                    else
                    {
                        if (interactionManager.GetTouchStateAllFinger(i))
                        {
                            SetCursorColor(surfaceCursors[i], touchColor);
                            SetCursorAlpha(surfaceCursors[i], 1.0f);
                        }
                        else
                        {
                            SetCursorColor(surfaceCursors[i], defaultColor);
                            SetCursorAlpha(surfaceCursors[i], 1.0f - (interactionManager.GetFingerTipToSurfaceDistance(i) / interactionManager.GetHoverDistanceThreshold()));
                        }
                    }
                    ShowSurfaceCursorGameObject(i, true);
                }
                else
                {
                    ShowSurfaceCursorGameObject(i, false);
                }
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                ShowSurfaceCursorGameObject(i, false);
            }
        }
    }

    void VisualizeSurfaceHandCursors()
    {
        for (int i = 0; i < 5; i++)
        {
            if (visualizeSurfaceHandCursors && interactionManager.GetSurfaceHandCursorState(i))
            {
                SetCursorAlpha(surfaceHandCursors[i], 1.0f);
            }
            else
            {
                SetCursorAlpha(surfaceHandCursors[i], 0.0f);
            }
        }

        if (visualizeSurfaceHandCursors)
        {
            for (int i = 0; i < 5; i++)
            {
                if (interactionManager.GetSurfaceHandCursorState(i))
                {
                    surfaceHandCursors[i].transform.position = interactionManager.GetSurfaceHandCursorPosition(i);
                    surfaceHandCursors[i].transform.rotation = interactionManager.GetSurfaceRotation();
                    if (interactionManager.GetFingerTipToSurfaceHandDistance(i) > interactionManager.GetHoverDistanceThreshold())
                    {
                        SetCursorAlpha(surfaceHandCursors[i], 0.0f);
                    }
                    else
                    {
                        SetCursorAlpha(surfaceHandCursors[i], 1.0f - (interactionManager.GetFingerTipToSurfaceHandDistance(i) / interactionManager.GetHoverDistanceThreshold()));
                    }
                    ShowSurfaceHandCursorGameObject(i, true);
                }
                else
                {
                    ShowSurfaceHandCursorGameObject(i, false);
                }
            }
        }
        else
        {
            for (int i = 0; i < 5; i++)
            {
                ShowSurfaceHandCursorGameObject(i, false);
            }
        }
    }

    void VisualizeRetargetCursors()
    {
        /*for (int i = 0; i < 5; i++)
        {
            SetCursorAlpha(retargetCursors[i], 1.0f);
            retargetCursors[i].transform.position = interactionManager.GetVirtualWindowCursorPosition(i);
            retargetCursors[i].transform.rotation = interactionManager.GetVirtualWindowGameObject().transform.rotation;
            retargetCursors[i].transform.RotateAround(retargetCursors[i].transform.position, retargetCursors[i].transform.right, 90f);
        }

        if (!drawHitRay) DrawRay(interactionManager.GetRetargetInteractionFingerTipPosition(1), interactionManager.GetVirtualWindowCursorPosition(1));
        else UpdateRay(interactionManager.GetRetargetInteractionFingerTipPosition(1), interactionManager.GetVirtualWindowCursorPosition(1));*/

        for (int i = 1; i < 2; i++)
        {
            if (visualizeRetargetCursors && interactionManager.GetWebviewCursorState(i))
            {
                SetCursorAlpha(retargetCursors[i], 1.0f);

            }
            else
            {
                SetCursorAlpha(retargetCursors[i], 0.0f);
            }
        }

        if (visualizeRetargetCursors)
        {
            for (int i = 1; i < 2; i++)
            {

                if (interactionManager.GetWebviewCursorState(i))
                {
                    //retarget the cursors
                    //retargetCursors[i].transform.position = interactionManager.GetVirtualWindowCursorPosition(i);
                    //retargetCursors[i].transform.rotation = interactionManager.GetVirtualWindowGameObject().transform.rotation;
                    

                    retargetCursors[i].transform.position = interactionManager.GetWebviewCursorPosition(i);
                    retargetCursors[i].transform.rotation = interactionManager.GetActiveWebviewGameObject().transform.rotation;

                    retargetCursors[i].transform.RotateAround(retargetCursors[i].transform.position, retargetCursors[i].transform.right, 90f);

                    if (interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(i) > interactionManager.GetHoverDistanceThreshold())
                    {
                        SetCursorColor(retargetCursors[i], defaultColor);
                        SetCursorAlpha(retargetCursors[i], 0.0f);
                        
                    }
                    else
                    {
                        if ((interactionManager.GetVirtualWindowCursorState(1) && interactionManager.GetTouchStateAllFinger(i)) || (!interactionManager.GetVirtualWindowCursorState(1) && interactionManager.GetRetargetFingerPenetrateSurface(i)))
                        {
                            SetCursorColor(retargetCursors[i], realTouchColor);
                            SetCursorAlpha(retargetCursors[i], 1.0f);
                            
                        }
                        else
                        {
                            SetCursorColor(retargetCursors[i], touchColor);
                            SetCursorAlpha(retargetCursors[i], 1.0f - (interactionManager.GetRetargetFingerTipToVirtualSurfaceDistance(i) / interactionManager.GetHoverDistanceThreshold()));
                            
                        }
                    }
                    ShowRetargetCursorGameObject(i, true);
                }
                else
                {
                    ShowRetargetCursorGameObject(i, false);
                }
            }

            ShowRetargetCursorGameObject(0, false);
            for (int i = 2; i < 5; i++)
            {
                ShowRetargetCursorGameObject(i, false);
            }
        }
        else
        {
            for (int i = 1; i < 2; i++)
            {
                ShowRetargetCursorGameObject(i, false);
            }
        }
    }

    void VisualizeRayCursor()
    {
        rayCursor.transform.position = handPointer.GetRayHitPoint();
        rayCursor.transform.rotation = interactionManager.GetActiveWebviewGameObject().transform.rotation;
        rayCursor.transform.RotateAround(rayCursor.transform.position, rayCursor.transform.right, 90f);

        bool isRayPinch = handPointer.GetRayPinch();
        if(isRayPinch)
        {
            SetCursorColor(rayCursor, realTouchColor);
        }
        else
        {
            SetCursorColor(rayCursor, touchColor);
        }
    }

    private void SetCursorAlpha(GameObject cursorObject, float alpha)
    {
        
        Color color = cursorObject.GetComponent<Renderer>().material.color;
        color.a = alpha;
        cursorObject.GetComponent<Renderer>().material.color = color;
    }

    
    /*
     * |----------------------------
     * | PUBLIC METHODS
     * |----------------------------
     */

    public void ChangeRetargetCursorSize(float scale)
    {
        float curX = retargetCursors[0].transform.localScale.x;
        float curY = retargetCursors[0].transform.localScale.y;
        float curZ = retargetCursors[0].transform.localScale.z;

        for (int i = 1; i < retargetCursors.Length; i++)
        {
            retargetCursors[0].transform.localScale = new Vector3(curX * scale, curY, curZ * scale);
        }
    }

    public GameObject GetSurfaceCursorGameObject(int idx)
    {
        return surfaceCursors[idx];
    }

    public GameObject GetSurfaceHandCursorGameObject(int idx)
    {
        return surfaceHandCursors[idx];
    }

    public void SetCursorColor(GameObject cursorObject, Color color)
    {
        cursorObject.GetComponent<Renderer>().material.color = color;
    }

    public void ShowSurfaceCursorGameObject(int idx, bool isShow)
    {
        surfaceCursors[idx].GetComponent<Renderer>().enabled = isShow;
    }

    public void ShowSurfaceHandCursorGameObject(int idx, bool isShow)
    {
        surfaceHandCursors[idx].GetComponent<Renderer>().enabled = isShow;
    }

    public void ShowRetargetCursorGameObject(int idx, bool isShow)
    {
        retargetCursors[idx].GetComponent<Renderer>().enabled = isShow;
    }

    private void DrawRay(Vector3 startPos, Vector3 destPos)
    {
        pointerRay.transform.position = startPos;
        hitRay.material = CreateMaterial(UnityEngine.Color.red, "ray_material");

        // set width of the renderer
        hitRay.startWidth = 0.01f;
        hitRay.endWidth = 0.01f;

        // set the position
        hitRay.SetPosition(0, startPos);
        hitRay.SetPosition(1, destPos);

        drawHitRay = true;
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
}
