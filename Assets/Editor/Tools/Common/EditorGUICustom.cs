using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  public class EditorGUICustom
  {
    public static bool SelectButton()
    {
      return GUILayout.Button(new GUIContent("\ud83d\udd0d", "Select asset in Project Window"), GUILayout.Width(30));
    }
    
    public static bool EditButton()
    {
      return GUILayout.Button(new GUIContent("\u270f\ufe0f", "Edit"), GUILayout.Width(30));
    }

    public static bool CodeEditButton(float moveOnYAxis = 0)
    {
      Rect buttonRect = GUILayoutUtility.GetRect(30, 18, GUILayout.Width(30));
      buttonRect.y += moveOnYAxis;
      return GUI.Button(buttonRect,new GUIContent("\ud83d\udcdd", "Open in Code Editor"));
    }
  }
}