using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  public static class CustomIconUtility
  {
    public static GUIContent CustomIconContent(string iconName, string tooltip = "")
    {
      Texture2D tex = EditorGUIUtility.Load($"Icons/{iconName}.png") as Texture2D;

      if (tex == null)
      {
        string[] guids = AssetDatabase.FindAssets(iconName);
        foreach (string guid in guids)
        {
          string path = AssetDatabase.GUIDToAssetPath(guid);
          if (path.EndsWith($"{iconName}.png"))
          {
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) break;
          }
        }
      }

      if (tex == null)
      {
        Debug.LogWarning($"[CustomIconUtility] Icon not found: {iconName}");
        return new GUIContent(iconName, tooltip); // fallback: show text
      }

      return new GUIContent(tex, tooltip);
    }


    /// <summary>
    /// Sets the icon of an EditorWindow.
    /// </summary>
    public static void SetCustomEditorIcon(this EditorWindow window, string iconName, string tooltip = "")
    {
      GUIContent icon = CustomIconContent(iconName, tooltip);
      if (icon.image == null)
      {
        Debug.LogWarning($"[SetCustomEditorIcon] Icon not found: {iconName}");
        return;
      }

      window.titleContent = new GUIContent(window.titleContent.text, icon.image, tooltip);
    }
  }
}