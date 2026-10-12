using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Com.Udogames.Core.Editor.Extensions
{
  public static class EditorWindowExtensions
  {
    public static void SetEditorIcon(this EditorWindow window, string iconName)
    {
      GUIContent iconContent = EditorGUIUtility.IconContent(iconName);
      if (iconContent != null && iconContent.image != null)
      {
        window.titleContent = new GUIContent(window.titleContent.text, iconContent.image);
        window.Repaint();
      }
      else
      {
        Debug.LogWarning($"Icon '{iconName}' not found.");
      }
    }
  }
}