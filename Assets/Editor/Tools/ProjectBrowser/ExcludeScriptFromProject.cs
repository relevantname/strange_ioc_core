using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ProjectBrowser
{
  public class ExcludeScriptFromProject : UnityEditor.Editor
  {
    [MenuItem("Assets/Exclude Script From Project")]
    [UsedImplicitly]
    public static void ExcludeScriptFromProjectOperation()
    {
      foreach (var item in Selection.objects)
      {
        if (!(item is TextAsset)) continue;

        var script = item as TextAsset;
        var path = AssetDatabase.GetAssetPath(script);
        var text = CodeUtilities.LoadScript(path);
        if (!text.StartsWith("#if UNITY_EDITOR"))
        {
          text = "#if UNITY_EDITOR \r\n" + text + "\r\n#endif";
          text = Regex.Replace(text, @"\r\n|\n\r|\n|\r", "\r\n");
          CodeUtilities.SaveFile(text, path);
          AssetDatabase.Refresh();
        }
      }
    }
  }
}