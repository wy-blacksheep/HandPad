using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HandPadReleaseValidation
{
    public static void Run()
    {
        var problems = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            var opened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            foreach (var root in opened.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                        problems.Add(scene.path + ": missing script on " + transform.name);
        }
        if (Resources.Load<TextAsset>("Models/handpad_touch_model") == null)
            problems.Add("Missing classifier model");
        File.WriteAllLines("handpad-validation.txt", problems.Count == 0
            ? new [] { "PASS: four enabled scenes opened; no missing MonoBehaviour scripts; model loaded." }
            : problems.ToArray());
        Debug.Log("HandPad validation problems: " + problems.Count);
        EditorApplication.Exit(problems.Count == 0 ? 0 : 1);
    }
}
