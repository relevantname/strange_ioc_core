using Com.Udogames.Core.Editor.Tools.Common;
using Com.Udogames.Core.Editor.Tools.ProjectBrowser;
using strange.extensions.context.impl;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ContextInspector
{
  [CustomEditor(typeof(ContextView), true)]
  public class ContextViewEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      // Draw default inspector fields
      DrawDefaultInspector();

      GUILayout.Space(10);

      if (GUILayout.Button("Open Context & Edit Bindings"))
      {
        OpenContextScript();
      }
    }

    private void OpenContextScript()
    {
      MonoScript bootstrapScript = MonoScript.FromMonoBehaviour((ContextView)target);
      string bootstrapPath = AssetDatabase.GetAssetPath(bootstrapScript);

      if (string.IsNullOrEmpty(bootstrapPath))
      {
        Debug.LogWarning("Could not find bootstrap script path.");
        return;
      }

      bootstrapPath = bootstrapPath.Replace("Bootstrap.cs", "Context.cs");
      
      FileOpenHelper.OpenFileInIde(bootstrapPath);
    }
  }
}