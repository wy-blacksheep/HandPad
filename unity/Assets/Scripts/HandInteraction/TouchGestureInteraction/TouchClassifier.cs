using Syncfusion.PMML;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;

public enum Gestures
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

public struct ConfidenceList
{
    public float thumb_confidence;
    public float index_confidence;
    public float middle_confidence;
    public float ring_confidence;
    public float pinky_confidence;
}

public class ConfidenceDictionary : Dictionary<Gestures, ConfidenceList>
{
    public void Add(Gestures gesture, float thumb_c, float index_c, float middle_c, float ring_c, float pinky_c)
    {
        ConfidenceList confidenceList;
        confidenceList.thumb_confidence = thumb_c;
        confidenceList.index_confidence = index_c;
        confidenceList.middle_confidence = middle_c;
        confidenceList.ring_confidence = ring_c;
        confidenceList.pinky_confidence = pinky_c;

        this.Add(gesture, confidenceList);
    }
}

public class TouchClassifier
{
    private const string MODEL_RESOURCE = "Models/handpad_touch_model";
    
    private readonly int WINDOW_SIZE = 5;
    // A prediction is positive only when its probability exceeds the finger threshold.
    
    
    private float[] CONFIDENCE = new float[5] { 0.5f, 0.5f, 0.5f, 0.3f, 1.1f };
    private float[] CONFIDENCE1 = new float[5] { 0.3f, 0.3f, 0.3f, 0.3f, 1.1f };

    ConfidenceDictionary confidenceDicionary = new ConfidenceDictionary();

    private List<int>[] runningWindow = new List<int>[5] { new List<int>(), new List<int>(), new List<int>(), new List<int>(), new List<int>() };

    private PMMLEvaluator touchModel;

    public void InitConfidenceDictionary()
    {
        confidenceDicionary.Add(Gestures.None, 0.1f, 0.1f, 0.1f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.Tap, 0.3f, 0.1f, 0.3f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.Drag, 0.1f, 0.1f, 0.3f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.DragToScroll, 0.3f, 0.1f, 0.3f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.DragToNavigate, 0.1f, 0.3f, 0.3f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.Scroll, 0.3f, 0.1f, 0.1f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.Scale, 0.1f, 0.1f, 0.3f, 1.1f, 1.1f);
        confidenceDicionary.Add(Gestures.Rotate, 0.1f, 0.1f, 0.3f, 1.1f, 1.1f);
    }

    public TouchClassifier()
    {
        TextAsset model = Resources.Load<TextAsset>(MODEL_RESOURCE);
        if (model == null) throw new FileNotFoundException("Missing HandPad touch model: " + MODEL_RESOURCE);
        using (MemoryStream fs = new MemoryStream(model.bytes, false))
        {
            touchModel = new PMMLEvaluatorFactory().GetPMMLEvaluatorInstance(fs);
        }
    }
    
    public bool Predict(int idx, float x, float y, float h1, float h2)
    {
        object data = new
        {
            x = x,
            y = y,
            h = h1,
            h2 = h2
        };
        
        PredictedResult result = touchModel.GetResult(data, null);

        // Keep the latest five classifier calls for this finger, including negatives.
        // Require three positive votes even while the window is warming up.
        List<int> window = runningWindow[idx];
        window.Add(result.GetPredictedProbabilities()["1"] > CONFIDENCE1[idx] ? 1 : 0);
        if (window.Count > WINDOW_SIZE) window.RemoveAt(0);

        int positiveVotes = 0;
        foreach (int vote in window) positiveVotes += vote;
        return positiveVotes > WINDOW_SIZE / 2;
    }

    public void ChangeConfidence(string touchGesture)
    {
        if (touchGesture == "Tap")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.Tap].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.Tap].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.Tap].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.Tap].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.Tap].pinky_confidence;
        }
        else if (touchGesture == "Drag")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.Drag].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.Drag].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.Drag].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.Drag].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.Drag].pinky_confidence;
        }
        else if (touchGesture == "DragToScroll")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.DragToScroll].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.DragToScroll].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.DragToScroll].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.DragToScroll].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.DragToScroll].pinky_confidence;
        }
        else if (touchGesture == "DragToNavigate")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.DragToNavigate].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.DragToNavigate].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.DragToNavigate].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.DragToNavigate].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.DragToNavigate].pinky_confidence;
        }
        else if (touchGesture == "Scroll")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.Scroll].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.Scroll].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.Scroll].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.Scroll].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.Scroll].pinky_confidence;
        }
        else if (touchGesture == "Scale")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.Scale].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.Scale].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.Scale].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.Scale].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.Scale].pinky_confidence;
        }
        else if (touchGesture == "Rotate")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.Rotate].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.Rotate].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.Rotate].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.Rotate].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.Rotate].pinky_confidence;
        }
        else if (touchGesture == "None")
        {
            CONFIDENCE[0] = confidenceDicionary[Gestures.None].thumb_confidence;
            CONFIDENCE[1] = confidenceDicionary[Gestures.None].index_confidence;
            CONFIDENCE[2] = confidenceDicionary[Gestures.None].middle_confidence;
            CONFIDENCE[3] = confidenceDicionary[Gestures.None].ring_confidence;
            CONFIDENCE[4] = confidenceDicionary[Gestures.None].pinky_confidence;
        }
    }
}
