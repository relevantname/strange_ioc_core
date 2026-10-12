using System.Collections.Generic;
using System.Linq;
using Com.Udogames.Core.DebugLogger;
using Com.Udogames.Core.Editor.Tools.Common;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.DebugLogger
{
  public class LoggerSettingsWindow : EditorWindow
  {
    private DebugX _loggerSettings;

    private string _searchQuery = "";

    private Vector2 _scrollPos;

    private List<Preset> _presets = new();

    private int _selectedPresetIndex = -1;

    [MenuItem("Window/Udo Core/Logger Settings")]
    public static void ShowWindow()
    {
      LoggerSettingsWindow window = GetWindow<LoggerSettingsWindow>("Logger Settings");
      window.SetCustomEditorIcon("logger");
    }

    private void OnEnable()
    {
      _loggerSettings = AssetDatabase.LoadAssetAtPath<DebugX>(DebugX.DataPath);
      if (_loggerSettings == null)
      {
        _loggerSettings = ScriptableObject.CreateInstance<DebugX>();
        _loggerSettings.list = new List<LoggerVo>();
        AssetDatabase.CreateAsset(_loggerSettings, DebugX.DataPath);
        AssetDatabase.SaveAssets();
      }

      LoadPresets();
    }

    private void LoadPresets()
    {
      _presets = AssetDatabase.FindAssets("t:Preset")
        .Select(guid => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(guid)))
        .ToList();
    }

    private void OnGUI()
    {
      EditorGUILayout.BeginHorizontal();

      EditorGUILayout.LabelField("Preset:", GUILayout.Width(50));
      string[] presetNames = _presets.Select(p => p.name).ToArray();
      _selectedPresetIndex = EditorGUILayout.Popup(_selectedPresetIndex, presetNames);

      if (_selectedPresetIndex >= 0 && _selectedPresetIndex < _presets.Count)
      {
        if (GUILayout.Button("Load Preset"))
        {
          ApplyPreset(_presets[_selectedPresetIndex]);
        }
      }

      GUIContent saveIcon = EditorGUIUtility.IconContent("SaveActive");
      saveIcon.tooltip = "Save Current as Preset";
      if (GUILayout.Button(saveIcon, GUILayout.Width(25), GUILayout.Height(18)))
      {
        CreatePresetFromActives();
      }

      if (GUILayout.Button("Activate All"))
      {
        foreach (var logger in _loggerSettings.list)
        {
          logger.active = true;
        }

        EditorUtility.SetDirty(_loggerSettings);
      }

      if (GUILayout.Button("Deactivate All"))
      {
        foreach (var logger in _loggerSettings.list)
        {
          logger.active = false;
        }

        EditorUtility.SetDirty(_loggerSettings);
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space();

      // Search Field
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
      _searchQuery = EditorGUILayout.TextField(_searchQuery);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space();

      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
      for (int i = 0; i < _loggerSettings.list.Count; i++)
      {
        var logger = _loggerSettings.list[i];
        if (!string.IsNullOrEmpty(_searchQuery) && !logger.tag.ToLower().Contains(_searchQuery.ToLower()))
          continue;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tag", GUILayout.Width(40));
        logger.tag = EditorGUILayout.TextField(logger.tag);
        GUIContent buttonContent = new GUIContent("X", "Remove");
        if (GUILayout.Button(buttonContent, GUILayout.Width(50)))
        {
          _loggerSettings.list.RemoveAt(i);
          EditorUtility.SetDirty(_loggerSettings);
          GUIUtility.ExitGUI();
          return;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Color", GUILayout.Width(40));
        logger.color = EditorGUILayout.ColorField(logger.color);
        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Active", GUILayout.Width(40));
        logger.active = EditorGUILayout.Toggle(logger.active);
        EditorGUILayout.EndHorizontal();


        if (logger.ignoreKeys == null)
          logger.ignoreKeys = new LoggerIgnoreKeyVo[0];

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Ignore Keys:");
        buttonContent = new GUIContent("+", "Add Key");
        if (GUILayout.Button(buttonContent, GUILayout.Width(50)))
        {
          var keys = new List<LoggerIgnoreKeyVo>(logger.ignoreKeys);
          keys.Add(new LoggerIgnoreKeyVo { key = "NewKey", active = true });
          logger.ignoreKeys = keys.ToArray();
          EditorUtility.SetDirty(_loggerSettings);
        }

        EditorGUILayout.EndHorizontal();
        for (int j = 0; j < logger.ignoreKeys.Length; j++)
        {
          EditorGUILayout.BeginHorizontal();
          logger.ignoreKeys[j].key = EditorGUILayout.TextField(logger.ignoreKeys[j].key);
          logger.ignoreKeys[j].active = EditorGUILayout.Toggle(logger.ignoreKeys[j].active);
          if (GUILayout.Button("X", GUILayout.Width(20)))
          {
            var keys = new List<LoggerIgnoreKeyVo>(logger.ignoreKeys);
            keys.RemoveAt(j);
            logger.ignoreKeys = keys.ToArray();
            EditorUtility.SetDirty(_loggerSettings);
            GUIUtility.ExitGUI();
            return;
          }

          EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
      }

      EditorGUILayout.EndScrollView();

      EditorGUILayout.Space();

      GUI.backgroundColor = Color.green;
      if (GUILayout.Button(new GUIContent("+ Add New Tag", "Add New Tag"), GUILayout.Height(30)))
      {
        _loggerSettings.list.Add(new LoggerVo { tag = "NewTag", active = true, color = Color.white, ignoreKeys = new LoggerIgnoreKeyVo[0] });
        EditorUtility.SetDirty(_loggerSettings);
      }

      GUI.backgroundColor = Color.white;

      if (GUI.changed)
      {
        EditorUtility.SetDirty(_loggerSettings);
      }
    }

    private void CreatePresetFromActives()
    {
      List<LoggerVo> actives = new List<LoggerVo>();
      foreach (LoggerVo loggerVo in _loggerSettings.list)
      {
        if (loggerVo.active)
          actives.Add(loggerVo);
      }

      DebugX asset = CreateInstance<DebugX>();
      asset.list = actives;
      var preset = new Preset(asset);
      const string folder = "Assets/Settings/LoggerPresets";
      const string saveTitle = "Save Logger Preset";
      string defaultName = asset.list.Count > 0 ? asset.list[0].tag : "LoggerPreset";
      string path = EditorUtility.SaveFilePanelInProject(saveTitle, defaultName, "preset", "Save preset", folder);
      if (!string.IsNullOrEmpty(path))
      {
        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        LoadPresets();
      }
    }

    private void ApplyPreset(Preset preset)
    {
      DebugX asset = CreateInstance<DebugX>();
      preset.ApplyTo(asset);

      Dictionary<string, LoggerVo> map = new Dictionary<string, LoggerVo>();
      asset.list.ForEach(vo => map.Add(vo.tag, vo));

      foreach (var logger in _loggerSettings.list)
      {
        logger.active = false;

        if (map.ContainsKey(logger.tag))
        {
          logger.active = true;
          logger.ignoreKeys = map[logger.tag].ignoreKeys;
        }
      }

      EditorUtility.SetDirty(_loggerSettings);
    }
  }
}