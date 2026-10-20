using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.XR;

public class ProgressVisualizerController : MonoBehaviour
{
    private GameObject progressMask, overlayMask;
    private GameObject progressLayer, stateLayer, overlayLayer;
    private GameObject progressVisualizer;
    
    [SerializeField] private GameObject progressVisualizerPrefab;
    [SerializeField] private bool overlayMode;
    [SerializeField] [Range(0.0f, 1.0f)] private float progressValue;
    [SerializeField][Range(0.0f, 1.0f)] private float alphaValue = 1.0f;

    void Start()
    {
        progressVisualizer = Instantiate(progressVisualizerPrefab, transform);
        progressVisualizer.transform.position = transform.position + ((transform.lossyScale.z + 0.001f) / 2 * -transform.forward);

        overlayMask = progressVisualizer.transform.Find("OverlayMask").gameObject;
        progressMask = progressVisualizer.transform.Find("ProgressMask").gameObject;

        stateLayer = progressMask.transform.Find("StateLayer").gameObject;
        progressLayer = progressMask.transform.Find("ProgressLayer").gameObject;
        overlayLayer = overlayMask.transform.Find("OverlayLayer").gameObject;
    }

    void Update()
    {
        UpdateProgress();
        UpdateMode();
        UpdateAlpha();
    }

    void UpdateMode()
    {
        if (overlayMode)
        {
            overlayMask.SetActive(true);
            overlayLayer.GetComponent<Image>().fillMethod = Image.FillMethod.Radial180;
            overlayLayer.GetComponent<Image>().fillOrigin = (int)Image.Origin180.Bottom;
            progressLayer.GetComponent<Image>().fillMethod = Image.FillMethod.Radial180;
            progressLayer.GetComponent<Image>().fillOrigin = (int)Image.Origin180.Bottom;

        }
        else
        {
            overlayMask.SetActive(false);
            progressLayer.GetComponent<Image>().fillMethod = Image.FillMethod.Radial360;
            progressLayer.GetComponent<Image>().fillOrigin = (int)Image.Origin360.Left;
        }
    }

    void UpdateProgress()
    {
        overlayLayer.GetComponent<Image>().fillAmount = progressValue;
        progressLayer.GetComponent<Image>().fillAmount = progressValue;
    }

    void UpdateAlpha()
    {
        progressVisualizer.GetComponent<CanvasGroup>().alpha = alphaValue;
    }

    public void SetProgress(float value)
    {
        progressValue = value;
    }

    public void SetAlpha(float alpha)
    {
        alphaValue = alpha;
    }

    public void SetOverlayTexture(Sprite texture)
    {
        overlayLayer.GetComponent<Image>().sprite = texture;
    }
    
    public void SetOverlayTexture(string textureDir)
    {
        overlayLayer.GetComponent<Image>().sprite = Resources.Load<Sprite>(textureDir);
    }

    public void SetProgressTexture(Sprite texture)
    {
        progressLayer.GetComponent<Image>().sprite = texture;
    }

    public void SetProgressTexture(string textureDir)
    {
        progressLayer.GetComponent<Image>().sprite = Resources.Load<Sprite>(textureDir);
    }

    public void SetStateTexture(Sprite texture)
    {
        stateLayer.GetComponent<Image>().sprite = texture;
    }

    public void SetStateTexture(string textureDir)
    {
        stateLayer.GetComponent<Image>().sprite = Resources.Load<Sprite>(textureDir);
    }
}