using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Basic
{
  public class ClearNameNumbersTool : UnityEditor.Editor
  {
    [MenuItem("Udo Core/Tools/Clear Name Numbers #&%c")]
    [UsedImplicitly]
    private static void ClearNameNumbersOperation()
    {
      var replaces = Selection.objects;

      foreach (var o in replaces)
      {
        var t = (GameObject)o;
        var str = t.name.Split(' ');

        if (str.Length > 1) t.name = t.name.Remove(t.name.Length - str[str.Length - 1].Length - 1);
      }

      EditorSceneManager.MarkAllScenesDirty();
    }
  }
}