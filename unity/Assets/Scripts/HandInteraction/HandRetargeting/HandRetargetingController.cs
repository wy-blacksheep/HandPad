using Meta.WitAi;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(HandInteractionManager))]
public class HandRetargetingController : MonoBehaviour
{
    private HandInteractionManager interactionManager;

    private GameObject activeVirtualSurface;
    private GameObject handSurface;
    private GameObject interactionHand;
    private GameObject surfaceHand;
    private GameObject surfaceHandReference;
    private GameObject virtualWindow;
    private GameObject virtualWindowReference;
    private GameObject surfaceReference;
    private GameObject relocateInteractionHand;
    private GameObject relocateSurfaceHand;
    private GameObject interactionHandRelativeToHandSurface;
    private GameObject surfaceHandRelativeToHandSurface;
    private GameObject surfaceMatchVirtualWindow;

    private Vector3 originalLocalScale = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

    private Quaternion initSurfaceRot = Quaternion.identity;

    public bool changeSurfaceRot = false;
    public bool changeSurfacePos = false;

    public bool stopHandUpate = false;

    public float smoothingFactor = 0.1f;  // Adjust this value to control the smoothness

    private Quaternion averageOrientation;

    private GameObject handPinchArrow;
    private GameObject nonDomHandPinchArrow;
    private GameObject retargetHandPinchArrow;
    private GameObject retargetNonDomHandPinchArrow;
    private GameObject pinchArrowRelativeToHandSurface;
    private GameObject nonDomPinchArrowRelativeToHandSurface;

    //for compensate the offset occur when select with gaze and drag with hand
    private Vector3 gazeHandOffset = new Vector3(0,0,0);

    // Start is called before the first frame update
    void Start()
    {
        interactionManager = GetComponent<HandInteractionManager>();

        // Initialize the average orientation to the current orientation at the start
        averageOrientation = GameObject.Find("SurfaceMatchVirtualWindow").transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        activeVirtualSurface = interactionManager.GetActiveVirtualSurfaceGameObject();

        handSurface = interactionManager.GetSurfaceGameObject();
        interactionHand = interactionManager.GetInteractionHandGameObject();
        surfaceHand = interactionManager.GetSurfaceHandGameObject();   
        surfaceReference = interactionManager.GetSurfaceReferenceGameObject();
        surfaceHandReference = interactionManager.GetSurfaceHandReferenceGameObject();
        surfaceMatchVirtualWindow = interactionManager.GetSurfaceMatchVirtualWindowGameObject();

        virtualWindow = interactionManager.GetVirtualWindowGameObject();
        virtualWindowReference = interactionManager.GetVirtualWindowReferenceGameObject();
        relocateInteractionHand = interactionManager.GetRelocateInteractionHandGameObject();
        interactionHandRelativeToHandSurface = interactionManager.GetInteractionHandRelativeToHandSurfaceGameObject();
        relocateSurfaceHand = interactionManager.GetRelocateSurfaceHandGameObject();

        // Update the average orientation using a weighted average
        if (surfaceMatchVirtualWindow != null)
        {
            averageOrientation = Quaternion.Slerp(averageOrientation, surfaceMatchVirtualWindow.transform.rotation, smoothingFactor);
        }

        if(interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.CustomHandPointer ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeMoveHand ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandJump ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            handPinchArrow = interactionManager.GetHandPinchArrowGameObject();
            retargetHandPinchArrow = interactionManager.GetRetargetHandPinchArrowGameObject();
            pinchArrowRelativeToHandSurface = interactionManager.GetPinchArrowRelativeToHandSurfaceGameObject();
            nonDomPinchArrowRelativeToHandSurface = GameObject.Find("NonDomPinchArrowRelativeToHandSurface");
            surfaceHandRelativeToHandSurface = GameObject.Find("surfaceHandRelativeToHandSurface");
            nonDomHandPinchArrow = interactionManager.GetNonDomHandPinchArrowGameObject();
            retargetNonDomHandPinchArrow = interactionManager.GetRetargetNonDomHandPinchArrowGameObject();
        }

        if(interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandPseudoDirectPoke_V2)
        {
            surfaceHandRelativeToHandSurface = GameObject.Find("surfaceHandRelativeToHandSurface");
        }
    }

    //retarget interactionhand from handsurface to virtual window
    public void RelocateInteractionHand()
    {
        if (interactionHand != null)
        {
            interactionHandRelativeToHandSurface.transform.position = interactionHand.transform.position;
            interactionHandRelativeToHandSurface.transform.rotation = interactionHand.transform.rotation;
        }

        if (changeSurfacePos)
        {
            surfaceReference.transform.position = surfaceMatchVirtualWindow.transform.position;
        }
        if (changeSurfaceRot)
        {
            //surfaceReference.transform.rotation = surfaceMatchVirtualWindow.transform.rotation;
            surfaceReference.transform.rotation = averageOrientation;

            
















        }

        if (virtualWindow != null)
        {
            virtualWindowReference.transform.position = virtualWindow.transform.position;
            virtualWindowReference.transform.rotation = virtualWindow.transform.rotation;
        }

        if (interactionHandRelativeToHandSurface != null)
        {
            if (!stopHandUpate)
            {
                relocateInteractionHand.transform.localPosition = interactionHandRelativeToHandSurface.transform.localPosition;
            }
            //relocateInteractionHand.transform.rotation = interactionHand.transform.rotation;
            relocateInteractionHand.transform.localRotation = interactionHandRelativeToHandSurface.transform.localRotation;
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.CustomHandPointer ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeMoveHand ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandJump ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            RelocateHandPinchArrow();
        }
    }

    public void RelocateHandPinchArrow()
    {
        





        if (handPinchArrow != null)
        {
            pinchArrowRelativeToHandSurface.transform.position = handPinchArrow.transform.position;
            pinchArrowRelativeToHandSurface.transform.rotation = handPinchArrow.transform.rotation;
        }

        if (pinchArrowRelativeToHandSurface != null)
        {
            retargetHandPinchArrow.transform.localPosition = pinchArrowRelativeToHandSurface.transform.localPosition;
            if (activeVirtualSurface != null) {
                retargetHandPinchArrow.transform.rotation = Quaternion.LookRotation(activeVirtualSurface.transform.forward);
            }
        }
    }

    public void RetargetInteractionHandToGazeCursor()
    {
        relocateInteractionHand.transform.position -= gazeHandOffset;
        retargetHandPinchArrow.transform.position -= gazeHandOffset;

        relocateSurfaceHand.transform.position -= gazeHandOffset;
        retargetNonDomHandPinchArrow.transform.position -= gazeHandOffset;
    }

    public void SetGazeHandOffset(Vector3 offset)
    {
        gazeHandOffset = offset;
        //RetargetInteractionHandToGazeCursor();
    }

    public void ChangeFrameOfReferenceRotation(bool isChange)
    {
        changeSurfaceRot = isChange;
        RelocateInteractionHand();
    }

    public void ChangeFrameOfReferencePosition(bool isChange)
    {
        changeSurfacePos = isChange;
        RelocateInteractionHand();
    }

    public void UpdateSurfaceReferencePosition(Vector3 offset)
    {
        surfaceReference.transform.position += offset;
        RelocateInteractionHand();
    }

    public void ChangeSurfaceReferencePosition(Vector3 pos)
    {
        surfaceReference.transform.position = pos;
        RelocateInteractionHand();
    }

    public void ChangeSurfaceReferenceRotation(Quaternion rotation)
    {
        surfaceReference.transform.rotation = rotation;
        RelocateInteractionHand();
    }

    //when semi- over- snap gesture starts
    public void InitRelocateSurfaceHand(bool activeVirtualWindow)
    {
        if (activeVirtualWindow)
        {
            relocateSurfaceHand.transform.position = virtualWindow.transform.position;
            relocateSurfaceHand.transform.rotation = virtualWindow.transform.rotation;
        }
        else
        {
            if (activeVirtualSurface != null)
            {
                relocateSurfaceHand.transform.position = activeVirtualSurface.transform.position;
                relocateSurfaceHand.transform.rotation = activeVirtualSurface.transform.rotation;
            }
            else
            {
                relocateSurfaceHand.transform.position = surfaceHand.transform.position;
                relocateSurfaceHand.transform.rotation = surfaceHand.transform.rotation;
            }
        }

        surfaceHandReference.transform.position = relocateSurfaceHand.transform.position;
        surfaceHandReference.transform.rotation = relocateSurfaceHand.transform.rotation;
        surfaceHandReference.transform.SetParent(surfaceHand.transform);
    }

    //surfaceHand always attached to the virtual window
    public void RelocateSurfaceHand(bool activeVirtualWindow)
    {
        if (activeVirtualWindow && virtualWindow!=null)
        {
            virtualWindowReference.transform.position = virtualWindow.transform.position;
            virtualWindowReference.transform.rotation = virtualWindow.transform.rotation;
        }
        else
        {
            if (activeVirtualSurface != null)
            {
                virtualWindowReference.transform.position = activeVirtualSurface.transform.position;
                virtualWindowReference.transform.rotation = activeVirtualSurface.transform.rotation;
            }
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.CustomHandPointer || 
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeMoveHand ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandJump ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandPseudoDirectPoke_V2 ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            RelocateSurfaceHandFOR();
        }
    }

    //redirect same as interactionhand
    public void RelocateSurfaceHandFOR()
    {
        if (surfaceHand != null)
        {
            surfaceHandRelativeToHandSurface.transform.position = surfaceHand.transform.position;
            surfaceHandRelativeToHandSurface.transform.rotation = surfaceHand.transform.rotation;
        }

        if (virtualWindow != null)
        {
            virtualWindowReference.transform.position = virtualWindow.transform.position;
            virtualWindowReference.transform.rotation = virtualWindow.transform.rotation;
        }

        if (surfaceHandRelativeToHandSurface != null)
        {
            if (!stopHandUpate)
            {
                relocateSurfaceHand.transform.localPosition = surfaceHandRelativeToHandSurface.transform.localPosition;
            }

            relocateSurfaceHand.transform.localRotation = surfaceHandRelativeToHandSurface.transform.localRotation;
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.CustomHandPointer ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.HandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeMoveHand ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandMove ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.EyeHandJump ||
            interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.GazePinch)
        {
            RelocateNonDomHandPinchArrow();
        }
    }

    public void RelocateNonDomHandPinchArrow()
    {

        if (nonDomHandPinchArrow != null)
        {
            nonDomPinchArrowRelativeToHandSurface.transform.position = nonDomHandPinchArrow.transform.position;
            nonDomPinchArrowRelativeToHandSurface.transform.rotation = nonDomHandPinchArrow.transform.rotation;
        }

        if (nonDomPinchArrowRelativeToHandSurface != null)
        {
            retargetNonDomHandPinchArrow.transform.localPosition = nonDomPinchArrowRelativeToHandSurface.transform.localPosition;
            if (activeVirtualSurface != null)
            {
                retargetNonDomHandPinchArrow.transform.rotation = Quaternion.LookRotation(activeVirtualSurface.transform.forward);
            }
        }
    }

    public void OutdatedRelocateSurfaceHand()
    {
        //check if the surface hand is behind the surface
        RaycastHit hit;
        float angle = 180f;

        Vector3 realSurfaceHand = surfaceHand.transform.position;
        Vector3 retargetSurfaceHand = surfaceHandReference.transform.position;
        Vector3 rayDir = retargetSurfaceHand - realSurfaceHand;

        Vector3 hitPoint = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);

        if (Physics.Raycast(realSurfaceHand, rayDir, out hit, 10.0f, LayerMask.GetMask("VirtualSurfaceLayer")))
        {
            if (hit.collider.gameObject == activeVirtualSurface)
            {
                hitPoint = hit.point;
            }
        }

        if(!Single.IsInfinity(hitPoint.x))
            angle = Vector3.Angle(activeVirtualSurface.transform.forward, retargetSurfaceHand - hitPoint );
        else 
            relocateSurfaceHand.transform.position = surfaceHandReference.transform.position;

        if (angle < 90f)
            relocateSurfaceHand.transform.position = hitPoint;
        else
            relocateSurfaceHand.transform.position = surfaceHandReference.transform.position;

        relocateSurfaceHand.transform.rotation = surfaceHandReference.transform.rotation;
    }

    public void InitRelocateVirtualSurface()
    {
        surfaceHandReference.transform.position = activeVirtualSurface.transform.position;
        surfaceHandReference.transform.rotation = activeVirtualSurface.transform.rotation;
        surfaceHandReference.transform.SetParent(surfaceHand.transform);
    }

    public void RelocateVirtualSurface()
    {
        //activeVirtualSurface.transform.position = surfaceHandReference.transform.position;

        if (activeVirtualSurface != null)
        {
            float curX = surfaceHandReference.transform.eulerAngles.x;
            float curY = surfaceHandReference.transform.eulerAngles.y;
            float curZ = activeVirtualSurface.transform.eulerAngles.z;
            activeVirtualSurface.transform.rotation = Quaternion.Euler(curX, curY, curZ);
        }
    }

    public void SetRelocateHandSameToRealHand()
    {
        if (relocateInteractionHand != null && interactionManager.GetInteractionHandGameObject() != null)
        {
            relocateInteractionHand.transform.position = interactionManager.GetInteractionHandGameObject().transform.position;
            relocateInteractionHand.transform.rotation = interactionManager.GetInteractionHandGameObject().transform.rotation;
        }
        if (relocateSurfaceHand != null && interactionManager.GetSurfaceHandGameObject() != null)
        {
            relocateSurfaceHand.transform.position = interactionManager.GetSurfaceHandGameObject().transform.position;
            relocateSurfaceHand.transform.rotation = interactionManager.GetSurfaceHandGameObject().transform.rotation;
        }

        if (interactionManager.GetInteractionMethod() == HandInteractionManager.InteractionMethod.CustomHandPointer)
        {
            if (retargetHandPinchArrow != null && handPinchArrow != null)
            {
                retargetHandPinchArrow.transform.position = handPinchArrow.transform.position;
                retargetHandPinchArrow.transform.rotation = handPinchArrow.transform.rotation;
            }
            if (retargetNonDomHandPinchArrow != null && nonDomHandPinchArrow != null)
            {
                retargetNonDomHandPinchArrow.transform.position = nonDomHandPinchArrow.transform.position;
                retargetNonDomHandPinchArrow.transform.rotation = nonDomHandPinchArrow.transform.rotation;
            }
        }

    }
}
