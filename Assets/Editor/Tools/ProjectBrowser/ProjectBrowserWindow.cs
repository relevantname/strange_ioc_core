using System.Collections.Generic;
using System.IO;
using System.Linq;
using Com.Udogames.Core.Editor.Extensions;
using Com.Udogames.Core.Editor.Tools.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ProjectBrowser
{
  public class ProjectBrowserWindow : EditorWindow
  {
    private List<string> _testScenes;
    private Dictionary<string, List<string>> _categorizedPrefabs;

    private readonly Dictionary<string, bool> _categoryFoldouts = new();

    private bool _showStartGame = true;
    private bool _showTestScenes = true;
    private bool _showPrefabs = true;

    private string _sceneSearch = "";
    private string _prefabSearch = "";

    private Vector2 _sceneScroll;
    private Vector2 _prefabScroll;
    private Vector2 _windowScroll;

    [MenuItem("Window/Udo Core/Project Browser")]
    public static void ShowWindow()
    {
      ProjectBrowserWindow window = GetWindow<ProjectBrowserWindow>("Project Browser");
      window.SetEditorIcon("Prefab Icon");
      window.Show();
    }

    private void OnEnable()
    {
      LoadTestScenes();
      LoadPrefabs();
    }

    private void OnGUI()
    {
      _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll, GUILayout.ExpandHeight(true));

      StartGame();

      TestScenes();

      Prefabs();

      EditorGUILayout.EndScrollView();
    }

    private void StartGame()
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      _showStartGame = EditorGUILayout.Foldout(_showStartGame, "Game", true, ExtraStyles.foldoutHeader);
      if (_showStartGame)
      {
        ExtraStyles.DrawHorLine();

        EditorGUI.indentLevel++;

        const string scenePath = "Assets/Scenes/MainScene.unity";
        if (GUILayout.Button("Open Game Scene"))
        {
          FileOpenHelper.OpenSceneSafe(scenePath);
        }

        if (GUILayout.Button("Play Fresh Game"))
        {
          PlayerPrefs.DeleteAll();
          FileOpenHelper.PlaySceneSafe(scenePath);
        }

        if (GUILayout.Button("Play Game (continue)"))
        {
          FileOpenHelper.PlaySceneSafe(scenePath);
        }

        if (GUILayout.Button("Open GDD"))
        {
          FileOpenHelper.OpenFileInIde("Assets/../README.md");
        }

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
    }

    private void TestScenes()
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox,  GUILayout.ExpandHeight(true));
      _showTestScenes = EditorGUILayout.Foldout(_showTestScenes, "Test Scenes (" + _testScenes.Count + ")", true, ExtraStyles.foldoutHeader);

      if (_showTestScenes)
      {
        ExtraStyles.DrawHorLine();

        EditorGUILayout.BeginHorizontal();
        _sceneSearch = GUILayout.TextField(_sceneSearch, GUI.skin.FindStyle("ToolbarSearchTextField"));
        if (GUILayout.Button("Create", GUILayout.Width(60)))
        {
          CreateContextWizard.Create();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel++;

        List<string> filteredScenes = _testScenes
          .Where(path => Path.GetFileNameWithoutExtension(path).ToLower().Contains(_sceneSearch.ToLower()))
          .ToList();

        _sceneScroll = EditorGUILayout.BeginScrollView(_sceneScroll, GUILayout.ExpandHeight(true));

        foreach (string scenePath in filteredScenes)
        {
          EditorGUILayout.BeginHorizontal();

          GUILayout.Label(" \u2726 " + Path.GetFileNameWithoutExtension(scenePath).Replace("TestScene",""));

          if (GUILayout.Button("\u25b6", GUILayout.Width(30)))
          {
            FileOpenHelper.PlaySceneSafe(scenePath);
          }
          
          if (EditorGUICustom.EditButton())
          {
            FileOpenHelper.OpenSceneSafe(scenePath);
          }

          if (EditorGUICustom.SelectButton())
          {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
          }

          EditorGUILayout.EndHorizontal();
        }

        if (filteredScenes.Count == 0)
        {
          EditorGUILayout.LabelField("No scenes found.");
        }

        EditorGUILayout.EndScrollView();

        EditorGUI.indentLevel--;
      }

      EditorGUILayout.EndVertical();
    }

    private void Prefabs()
    {
      EditorGUILayout.BeginVertical(EditorStyles.helpBox,  GUILayout.ExpandHeight(true));
      int totalPrefabCount = _categorizedPrefabs.Sum(pair => pair.Value.Count);
      _showPrefabs = EditorGUILayout.Foldout(_showPrefabs, "Prefabs (" + totalPrefabCount + ")", true, ExtraStyles.foldoutHeader);

      if (_showPrefabs)
      {
        ExtraStyles.DrawHorLine();

        EditorGUILayout.BeginHorizontal();
        _prefabSearch = GUILayout.TextField(_prefabSearch, GUI.skin.FindStyle("ToolbarSearchTextField"));
        EditorGUILayout.EndHorizontal();

        _prefabScroll = EditorGUILayout.BeginScrollView(_prefabScroll, GUILayout.ExpandHeight(true));

        foreach (var category in _categorizedPrefabs)
        {
          var filteredPrefabs = category.Value
            .Where(path => Path.GetFileNameWithoutExtension(path).ToLower().Contains(_prefabSearch.ToLower()))
            .ToList();

          if (filteredPrefabs.Count == 0)
            continue;

          if (!_categoryFoldouts.ContainsKey(category.Key))
            _categoryFoldouts[category.Key] = true;

          GUIStyle boldFoldout = new GUIStyle(EditorStyles.foldout);
          boldFoldout.fontStyle = FontStyle.Bold;

          _categoryFoldouts[category.Key] = EditorGUILayout.Foldout(
            _categoryFoldouts[category.Key],
            category.Key + " (" + filteredPrefabs.Count + ")",
            true,
            boldFoldout
          );

          if (_categoryFoldouts[category.Key])
          {
            foreach (var prefabPath in filteredPrefabs)
            {
              EditorGUILayout.BeginHorizontal();

              GUILayout.Label("   \u2726 " + Path.GetFileNameWithoutExtension(prefabPath));

              if (GUILayout.Button(new GUIContent("\u2795", "Adds this prefab under the selected GameObject in the scene."), GUILayout.Width(30)))
              {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (Selection.activeGameObject != null)
                {
                  PrefabUtility.InstantiatePrefab(prefab, Selection.activeGameObject.transform);
                }
                else
                {
                  PrefabUtility.InstantiatePrefab(prefab);
                }
              }

              if (EditorGUICustom.EditButton())
              {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                  AssetDatabase.OpenAsset(prefab);
                }
                else
                {
                  Debug.LogWarning("Failed to load prefab at path: " + prefabPath);
                }
              }
              
              if (EditorGUICustom.SelectButton())
              {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
              }

              EditorGUILayout.EndHorizontal();
            }
          }
        }

        EditorGUILayout.EndScrollView();
      }

      EditorGUILayout.EndVertical();
    }

    private void LoadTestScenes()
    {
      _testScenes = Directory.GetFiles("Assets/Test", "*.unity", SearchOption.AllDirectories)
        .Select(path => path.Replace("\\", "/"))
        .ToList();
    }

    private void LoadPrefabs()
    {
      var allPrefabs = Directory.GetFiles("Assets/Prefabs", "*.prefab", SearchOption.AllDirectories)
        .Select(path => path.Replace("\\", "/"))
        .ToList();

      _categorizedPrefabs = new Dictionary<string, List<string>>();

      foreach (var prefabPath in allPrefabs)
      {
        string relativeFolder = Path.GetDirectoryName(prefabPath)?.Replace("\\", "/").Replace("Assets/Prefabs", "").Trim('/');
        if (string.IsNullOrEmpty(relativeFolder))
        {
          relativeFolder = "Root";
        }

        if (!_categorizedPrefabs.ContainsKey(relativeFolder))
        {
          _categorizedPrefabs[relativeFolder] = new List<string>();
        }

        _categorizedPrefabs[relativeFolder].Add(prefabPath);
      }
    }
  }
}