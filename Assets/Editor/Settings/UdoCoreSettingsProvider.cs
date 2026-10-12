using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Settings
{
  public static class UdoCoreSettingsProvider
  {
    [SettingsProvider]
    public static SettingsProvider CreateHammerSettingsProvider()
    {
      var provider = new SettingsProvider("Project/Udo Core", SettingsScope.Project)
      {
        label = "Udo Core",
        guiHandler = (searchContext) =>
        {
          DrawGui();
        },
        keywords = new HashSet<string>(new[] { "core", "udo", "sdk" })
      };

      return provider;
    }

    private static void DrawGui()
    {
      UdoCoreSettings settings = UdoCoreSettings.instance;
      SerializedObject so = new SerializedObject(settings);
      SerializedProperty multiplayerProp = so.FindProperty("multiplayer");

      EditorGUI.BeginChangeCheck();
      EditorGUILayout.PropertyField(multiplayerProp, new GUIContent("Multiplayer"));

      if (EditorGUI.EndChangeCheck())
      {
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(settings);
        settings.Save();
      }
    }
  }
}