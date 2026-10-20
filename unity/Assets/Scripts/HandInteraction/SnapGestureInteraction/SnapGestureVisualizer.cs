using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


[RequireComponent(typeof(HandInteractionManager))]
[RequireComponent(typeof(SnapGestureController))]
[RequireComponent(typeof(SnapGestureHelper))]
public class SnapGestureVisualizer : MonoBehaviour
{
    [SerializeField] private bool visualizeSnapMode = true;
    [SerializeField] private bool visualizeSnapTransition = true;
    
    private HandInteractionManager interactionManager;
    private SnapGestureController gestureController;
    private SnapGestureHelper featureHelper;

    private RadialSpinnerVisualizer radialSpinner;

    private Material defaultHandMaterial;
    private Material snapHandMaterial;
    private Material semiSnapHandMaterial;
    private Material overSnapHandMaterial;

    private TextMeshProUGUI debugText;

    
    void Start()
    {
        interactionManager = GetComponent<HandInteractionManager>();
        gestureController = GetComponent<SnapGestureController>();
        featureHelper = GetComponent<SnapGestureHelper>();

        radialSpinner = Instantiate(Resources.Load("Prefabs/RadialSpinner") as GameObject).GetComponent<RadialSpinnerVisualizer>();
        radialSpinner.SetProgress(0);
        radialSpinner.SetMode(RadialSpinnerVisualizer.SpinnerMode.Ring);
        radialSpinner.SetAnchor(interactionManager.GetSurfaceBottomLeftGameObject());

        defaultHandMaterial = Resources.Load("Materials/DefaultHandMaterial", typeof(Material)) as Material;
        snapHandMaterial = Resources.Load("Materials/SnapHandMaterial", typeof(Material)) as Material;
        semiSnapHandMaterial = Resources.Load("Materials/SemiSnapHandMaterial", typeof(Material)) as Material;
        overSnapHandMaterial = Resources.Load("Materials/OverSnapHandMaterial", typeof(Material)) as Material;

        debugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
    }
    
    void Update()
    {
        VisualizeSnapMode();
        VisualizeSnapTransition();
    }

    void VisualizeSnapTransition()
    {
        if (!visualizeSnapTransition)
        {
            radialSpinner.SetProgress(0);
            return;
        }

        if (gestureController.GetSnapProgress() == 1.0f) radialSpinner.SetProgress(0);
        else if (gestureController.GetSnapProgress() > 0.0f) radialSpinner.SetProgress(gestureController.GetSnapProgress());
        
    }

    private void VisualizeSnapMode()
    {
        if (!visualizeSnapMode) {
            interactionManager.GetSurfaceHandGameObject().GetComponent<Renderer>().material = defaultHandMaterial;
            return;
        }
        
        switch (gestureController.GetSnapMode())
        {
            case SnapGestureController.SnapModes.Snap:
                interactionManager.GetSurfaceHandGameObject().GetComponent<Renderer>().material = snapHandMaterial;
                break;

            case SnapGestureController.SnapModes.SemiSnap:
                interactionManager.GetSurfaceHandGameObject().GetComponent<Renderer>().material = semiSnapHandMaterial;
                break;

            case SnapGestureController.SnapModes.OverSnap:
                interactionManager.GetSurfaceHandGameObject().GetComponent<Renderer>().material = overSnapHandMaterial;
                break;

            default:
                interactionManager.GetSurfaceHandGameObject().GetComponent<Renderer>().material = defaultHandMaterial;
                break;
        }
    }
}
