using System.Collections;
using strange.extensions.context.impl;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  public class FileOpenHelper
  {
    private static string _scenePath;
    private static bool _playAfterOpen;
    
    public static void PlaySceneSafe(string scenePath)
    {
      _scenePath = scenePath;
      _playAfterOpen = true;

      if (EditorApplication.isPlaying)
      {
        // Stop play mode first — callback will handle opening
        EditorApplication.isPlaying = false;
      }
      else
      {
        DoOpen();
      }
    }

    public static void OpenSceneSafe(string scenePath)
    {
      _scenePath = scenePath;
      _playAfterOpen = false;

      if (EditorApplication.isPlaying)
      {
        // Stop play mode first — callback will handle opening
        EditorApplication.isPlaying = false;
      }
      else
      {
        DoOpen();
      }
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
      EditorApplication.playModeStateChanged += state =>
      {
        if (state == PlayModeStateChange.EnteredEditMode && !string.IsNullOrEmpty(_scenePath))
        {
          DoOpen();
        }
      };
    }

    private static void DoOpen()
    {
      if (!string.IsNullOrEmpty(_scenePath))
      {
        EditorSceneManager.OpenScene(_scenePath);

        if (_playAfterOpen)
          EditorCoroutineUtility.StartCoroutineOwnerless(EnterPlayMode());

        _scenePath = null; // reset
      }
    }

    private static IEnumerator EnterPlayMode()
    {
      yield return new WaitForSeconds(2);
      EditorApplication.EnterPlaymode();
    }

    public static void OpenFileInIde(string filePath)
    {
      string editorPath = EditorPrefs.GetString("kScriptsDefaultApp");

      if (!string.IsNullOrEmpty(editorPath) && System.IO.File.Exists(editorPath))
      {
        // Launch code editor with the file
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
          FileName = editorPath,
          Arguments = $"\"{System.IO.Path.GetFullPath(filePath)}\"",
          UseShellExecute = false
        });
      }
      else
      {
        // Fallback: Open with OS default app
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
          FileName = filePath,
          UseShellExecute = true
        });
      }
    }
  }
}