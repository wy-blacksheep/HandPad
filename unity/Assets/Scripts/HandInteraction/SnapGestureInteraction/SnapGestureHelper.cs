using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HandInteractionManager))]
public class SnapGestureHelper : MonoBehaviour
{
    private HandInteractionManager interactionManager;

    // Snap Gesture Feature Variables
    private float cumulativeSurfaceFingerTipDistance;
    private float surfaceFingerTipToWristDistance;
    private float cumulativeFingerOrientation;
    private float cumulativeFingerSpreadOrientation;

    void Start()
    {
        interactionManager = GetComponent<HandInteractionManager>();
    }

    void Update()
    {
        cumulativeSurfaceFingerTipDistance = ComputeCumulativeSurfaceFingerTipDistance();
        surfaceFingerTipToWristDistance = ComputeSurfaceFingerTipToWristDistance();
        cumulativeFingerOrientation = ComputeCumulativeFingerOrientation();
        cumulativeFingerSpreadOrientation = ComputeCumulativeFingerSpreadOrientation();
    }

    private float ComputeCumulativeSurfaceFingerTipDistance()
    {
        float interFingerTipDistanceSum = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            interFingerTipDistanceSum += interactionManager.GetSurfaceFingerTipDistance(i, i + 1);
        }
        return interFingerTipDistanceSum;
    }

    private float ComputeSurfaceFingerTipToWristDistance()
    {
        float fingerTipToWristDistance = 0.0f;
        for (int i = 0; i < 5; i++)
        {
            fingerTipToWristDistance += Vector3.Distance(interactionManager.GetSurfaceFingerTipPosition(i), interactionManager.GetSurfaceWristPosition());
        }
        return fingerTipToWristDistance;
    }

    private float ComputeCumulativeFingerOrientation()
    {
        float fingerKnuckleOrientationSum = 0.0f;

        for (int i = 1; i < 5; i++)
        {
            






            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerKnuckleOrientationSum += Vector3.Angle(vect2, vect3);
            fingerKnuckleOrientationSum += Vector3.Angle(vect3, vect4);
        }
        return fingerKnuckleOrientationSum;
    }

    //sum of angles between root knuckles of adjacent fingers - to check if all fingers are spread
    private float ComputeCumulativeFingerSpreadOrientation()
    {
        float fingerRootKnuckleOrientationSum = 0.0f;
        for (int i = 1; i < 4; i++)
        {
            Vector3 vect1 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i + 1) - interactionManager.GetFirstKnucklePosition(i + 1);

            fingerRootKnuckleOrientationSum += Vector3.Angle(vect1, vect2);
        }
        return fingerRootKnuckleOrientationSum;
    }


    /*
     * |----------------------------
     * | PUBLIC METHODS
     * |----------------------------
     */

    public List<float> GetFingerOrientation()
    {
        List<float> fingerOrientation = new List<float>();
        for (int i = 0; i < 5; i++)
        {
            Vector3 vect2 = interactionManager.GetSecondKnucklePosition(i) - interactionManager.GetFirstKnucklePosition(i);
            Vector3 vect3 = interactionManager.GetThirdKnucklePosition(i) - interactionManager.GetSecondKnucklePosition(i);
            Vector3 vect4 = interactionManager.GetForthKnucklePosition(i) - interactionManager.GetThirdKnucklePosition(i);

            fingerOrientation.Add(Vector3.Angle(vect3, vect4));
            fingerOrientation.Add(Vector3.Angle(vect2, vect3));
        }
        return fingerOrientation;
    }

    public List<Vector3> GetFingerKuncklePosition()
    {
        List<Vector3> fingerKuncklePosition = new List<Vector3>();
        for (int i = 0; i < 5; i++)
        {
            fingerKuncklePosition.Add(interactionManager.GetFirstKnucklePosition(i));
            fingerKuncklePosition.Add(interactionManager.GetSecondKnucklePosition(i));
            fingerKuncklePosition.Add(interactionManager.GetThirdKnucklePosition(i));
            fingerKuncklePosition.Add(interactionManager.GetForthKnucklePosition(i));
        }

        return fingerKuncklePosition;
    }

    // Snap Gesture Feature Variables
    public float GetCumulativeSurfaceFingerTipDistance()
    {
        return cumulativeSurfaceFingerTipDistance;
    }

    public float GetSurfaceFingerTipToWristDistance()
    {
        return surfaceFingerTipToWristDistance;
    }

    public float GetCmulativeFingerOrientation()
    {
        return cumulativeFingerOrientation;
    }

    public float GetCumulativeFingerSpreadOrientation()
    {
        return cumulativeFingerSpreadOrientation;
    }

}
