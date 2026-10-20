using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialSpinnerVisualizer : MonoBehaviour
{
    public enum SpinnerMode
    {
        Filled,
        Ring,
        Both
    }

    private GameObject anchor = null;
    private SpinnerMode mode = SpinnerMode.Both;
    private float progress = 1.0f;

    private GameObject radialSpinner;
    private GameObject filledProgress;
    private GameObject ringProgress;

    
    private void Start()
    {
        radialSpinner = this.gameObject;
        filledProgress = radialSpinner.transform.Find("FilledProgress").gameObject;
        ringProgress = radialSpinner.transform.Find("RingProgress").gameObject;

        filledProgress.GetComponent<Image>().type = Image.Type.Filled;
        ringProgress.GetComponent<Image>().type = Image.Type.Filled;
    }

    void Update()
    {
        UpdateMode();
        UpdatePose();
        UpdateProgress();
    }

    private void UpdateMode()
    {
        if (mode == SpinnerMode.Filled)
        {
            ringProgress.SetActive(false);
            filledProgress.SetActive(true);
        } 
        else if (mode == SpinnerMode.Ring)
        {
            ringProgress.SetActive(true);
            filledProgress.SetActive(false);
        }
        else
        {
            ringProgress.SetActive(true);
            filledProgress.SetActive(true);
        }
    }

    private void UpdatePose()
    {
        if (anchor == null)
        {
            radialSpinner.SetActive(false);
        }
        else
        {
            radialSpinner.SetActive(true);

            radialSpinner.transform.position = anchor.transform.position;
            radialSpinner.transform.rotation = anchor.transform.rotation;
        }
    }

    private void UpdateProgress()
    {
        if (mode == SpinnerMode.Filled) filledProgress.GetComponent<Image>().fillAmount = progress;
        else if (mode == SpinnerMode.Ring) ringProgress.GetComponent<Image>().fillAmount = progress;
        else
        {
            filledProgress.GetComponent<Image>().fillAmount = progress;
            ringProgress.GetComponent<Image>().fillAmount = progress;
        }
    }
    

    /*
     * |----------------------------
     * | PUBLIC METHODS
     * |----------------------------
     */

    public void SetAnchor(GameObject anchor)
    {
        this.anchor = anchor;
    }

    public void SetMode(SpinnerMode mode)
    {
        this.mode = mode;
    }

    public void SetProgress(float progress)
    {
        if (progress < 0.0f) progress = 0.0f;
        if (progress > 1.0f) progress = 1.0f;
        
        this.progress = progress;
    }
    
    public void Destroy()
    {
        Destroy(radialSpinner);
        Destroy(this);
    }
}
