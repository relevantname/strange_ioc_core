using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ProjectBrowser
{
  public class CodeUtilities
  {
    private static Encoding _encoding = new UTF8Encoding(false);

    public static bool HasSelectedFolder()
    {
      if (Selection.objects.Length == 1)
      {
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
          string path = AssetDatabase.GetAssetPath(obj);
          if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
          {
            return true;
          }
        }
      }

      return false;
    }

    public static string GetSelectedFolder()
    {
      if (!HasSelectedFolder())
        throw new Exception("You need to select folder first.");

      if (Selection.objects.Length == 1)
      {
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
          string path = AssetDatabase.GetAssetPath(obj);
          if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
          {
            return path;
          }
        }
      }

      throw new Exception("You need to select single folder.");
    }

    public static void SaveFile(string data, string filename)
    {
//      if (File.Exists(filename) == false)
//      {
      using (StreamWriter outfile = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write),
               _encoding))
      {
        outfile.Write(data);
      }
//      }

//      AssetDatabase.Refresh();
    }

    public static string LoadTemplate(string path)
    {
      string fullPath = $"Packages/com.udogames.core/Editor/Resources/Template{path}.txt";
      TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
      return textAsset != null ? textAsset.text : null;
    }

    public static string LoadScript(string path)
    {
//      Debug.Log(path);
      try
      {
        _encoding = GetEncoding(path);

        string data;
        using (StreamReader theReader = new StreamReader(path, _encoding))
        {
          data = theReader.ReadToEnd();
          theReader.Close();
        }

        return data;
      }
      catch (Exception e)
      {
        Console.WriteLine("{0}\n", e.Message);
        return string.Empty;
      }
    }

    public static string SetTemplateVar(string template, string variableName, string value)
    {
      if (string.IsNullOrEmpty(template))
      {
        Debug.LogError("Template is null or empty.");
        return null;
      }
      return template.Replace("%" + variableName + "%", value);
    }

    public static Encoding GetEncoding(string filename)
    {
      // Read the BOM
      byte[] bom = new byte[4];
      using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
      {
        file.Read(bom, 0, 4);
      }

      // Analyze the BOM
      if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
      if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
      if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
      if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
      if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
      return Encoding.ASCII;
    }

    public static void ExcludeScript(string path)
    {
      string text = CodeUtilities.LoadTemplate(path);
      if (!text.StartsWith("#if UNITY_EDITOR"))
      {
        text = "#if UNITY_EDITOR \r\n" + text + "\r\n#endif";
        text = Regex.Replace(text, @"\r\n|\n\r|\n|\r", "\r\n");
        SaveFile(text, path);
      }
    }

    public static void EnsureFolderExists(string folderPath)
    {
      if (Directory.Exists(folderPath))
      {
        return;
      }

      Directory.CreateDirectory(folderPath);

      AssetDatabase.Refresh();
    }

    public static void DeleteFolder(string folderPath)
    {
      if (!Directory.Exists(folderPath))
      {
        return;
      }

      Directory.Delete(folderPath, true);

      AssetDatabase.Refresh();
    }
  }
}