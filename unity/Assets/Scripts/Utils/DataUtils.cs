using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class DataUtils
{
    public static string ListToCSVString(List<string[]> strings)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < strings.Count; i++)
        {
            sb.AppendLine(string.Join(",", strings[i]));
        }
        return sb.ToString();
    }

    public static void SaveDataToFile(string filename, string data)
    {
        string directory = Path.Combine(Application.persistentDataPath, "Data");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, filename + "_" + string.Format("{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now) + ".csv");
        StreamWriter streamWriter = File.CreateText(path);
        streamWriter.Write(data);
        streamWriter.Close();
        Debug.Log("Data Saved To: " + path);
    }
}
