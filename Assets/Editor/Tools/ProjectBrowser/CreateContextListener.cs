using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ProjectBrowser
{
  [InitializeOnLoad]
  public class CreateContextListener
  {
    static CreateContextListener()
    {
      if (!EditorPrefs.GetBool("CreateTest"))
        return;

      EditorPrefs.SetBool("CreateTest", false);

      string name = EditorPrefs.GetString("Name");
      bool createView = EditorPrefs.GetBool("CreateView");
      EditorPrefs.DeleteKey("Name");

      if (createView)
      {
        Type componentType = GetTypeFromName(name + "View");
        if (componentType == null)
        {
          Debug.LogError("Component type not found: " + name + "View");
          return;
        }

        GameObject panelSample = new GameObject(name);
        RectTransform rectTransform = panelSample.AddComponent<RectTransform>();
        panelSample.AddComponent(componentType);
        panelSample.transform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;


        // Ensure directory exists
        string prefabDir = "Assets/Prefabs/Panels";
        if (!AssetDatabase.IsValidFolder(prefabDir))
        {
          string[] parts = prefabDir.Split('/');
          string path = "Assets";
          for (int i = 1; i < parts.Length; i++)
          {
            if (!AssetDatabase.IsValidFolder(path + "/" + parts[i]))
            {
              AssetDatabase.CreateFolder(path, parts[i]);
            }

            path += "/" + parts[i];
          }
        }

        string prefabPath = prefabDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(panelSample, prefabPath);

        UnityEngine.Object.DestroyImmediate(panelSample);

        AssetDatabase.Refresh();
        EditorCoroutineUtility.StartCoroutineOwnerless(WaitForPrefabToAvailable(name, prefabPath));
      }
      else
      {
        string rootPrefabPath = EditorPrefs.GetString("RootPrefabPath");
        EditorPrefs.DeleteKey("RootPrefabPath");

        GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rootPrefabPath);
        if (rootPrefab == null)
        {
          Debug.LogError("Root prefab could not be found at path: " + rootPrefabPath);
          return;
        }

        GameObject contextRoot = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
        if (contextRoot != null)
        {
          Type bootstrapType = GetTypeFromName(name + "TestBootstrap");
          if (bootstrapType != null)
          {
            contextRoot.AddComponent(bootstrapType);
          }
          else
          {
            Debug.LogWarning("TestBootstrap type not found: " + name + "TestBootstrap");
          }
        }
        else
        {
          Debug.LogError("Cannot create context root from selected prefab.");
          return;
        }
      }
    }

    private static IEnumerator WaitForPrefabToAvailable(string name, string prefabPath)
    {
      string rootPrefabPath = EditorPrefs.GetString("RootPrefabPath");
      EditorPrefs.DeleteKey("RootPrefabPath");

      GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rootPrefabPath);
      if (rootPrefab == null)
      {
        Debug.LogError("Root prefab could not be found at path: " + rootPrefabPath);
        yield break;
      }

      GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      while (panelPrefab == null)
      {
        yield return new WaitForSeconds(1);
        panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
      }

      GameObject panelSample = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);

      GameObject contextRoot = PrefabUtility.InstantiatePrefab(rootPrefab) as GameObject;
      if (contextRoot != null)
      {
        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo != null)
        {
          panelSample.transform.SetParent(canvasGo.transform.GetChild(0));
          panelSample.transform.localScale = Vector3.one;
          RectTransform rectTransform = panelSample.GetComponent<RectTransform>();
          rectTransform.anchorMin = new Vector2(0, 0);
          rectTransform.anchorMax = new Vector2(1, 1);
          rectTransform.offsetMin = Vector2.zero;
          rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
          Debug.LogWarning("Canvas object not found in the scene.");
        }

        Type bootstrapType = GetTypeFromName(name + "TestBootstrap");
        if (bootstrapType != null)
        {
          contextRoot.AddComponent(bootstrapType);
        }
        else
        {
          Debug.LogWarning("TestBootstrap type not found: " + name + "TestBootstrap");
        }
      }
      else
      {
        Debug.LogError("Cannot create context root from selected prefab.");
        yield break;
      }

      EditorSceneManager.MarkAllScenesDirty();
      EditorSceneManager.SaveOpenScenes();
    }

    private static Type GetTypeFromName(string classNameWithNameSpace)
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        var type = assembly.GetType(classNameWithNameSpace);
        if (type != null)
          return type;
      }

      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        foreach (var type in assembly.GetTypes())
        {
          if (type.AssemblyQualifiedName != null && type.AssemblyQualifiedName.Contains(classNameWithNameSpace))
          {
            if (type.FullName != null && type.FullName.EndsWith("ViewEvent"))
              continue;
            return type;
          }
        }
      }

      return null;
    }
  }
}