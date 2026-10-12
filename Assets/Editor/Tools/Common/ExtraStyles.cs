using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  public class ExtraStyles
  {
    public static GUIStyle foldoutHeader = new(EditorStyles.foldoutHeader)
    {
      fontStyle = FontStyle.Bold,
      fontSize = 12,
      fixedHeight = 23,
      fixedWidth = 5000,
      alignment = TextAnchor.MiddleLeft,
      margin = new RectOffset(-3, 0, -2, 4),
    };

    private static Texture2D MakeTex(int width, int height, Color col)
    {
      Color[] pix = new Color[width * height];
      for (int i = 0; i < pix.Length; i++)
        pix[i] = col;

      Texture2D result = new Texture2D(width, height);
      result.SetPixels(pix);
      result.Apply();
      return result;
    }
    
    public static void DrawHorLine()
    {
      Rect rect = EditorGUILayout.GetControlRect(false, 1);
      EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
      GUILayout.Space(4);
    }
  }
}