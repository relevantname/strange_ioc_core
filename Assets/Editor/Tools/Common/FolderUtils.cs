using System.IO;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  public class FolderUtils
  {
    public static void CreateFolder(string fullPath)
    {
      try
      {
        if (!Directory.Exists(fullPath))
        {
          Directory.CreateDirectory(fullPath);
          // Debug.Log($"Created folder: {fullPath}");
        }
        else
        {
          Debug.LogWarning($"Folder already exists: {fullPath}");
        }
      }
      catch (System.Exception ex)
      {
        Debug.LogError($"Failed to create folder at {fullPath}: {ex}");
      }
    }
    
    public static bool FolderExists(string path)
    {
      return Directory.Exists(path);
    }
  }
}