using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ProjectBrowser
{
  public class CreateContextWizard : ScriptableWizard
  {
    private const string ViewFunctionTemplate = "public void On{0}()\r\t\t{{\r\t\t\tdispatcher.Dispatch({1}ViewEvent.{0});\r\t\t}}";
    private const string MediatorFunctionTemplate = "public void On{0}()\r\t\t{{\r\t\t\t\r\t\t}}";
    private const string AddListenerTemplate = "view.dispatcher.AddListener({1}ViewEvent.{0},On{0});";
    private const string RemoveListenerTemplate = "view.dispatcher.RemoveListener({1}ViewEvent.{0},On{0});";
    public const string ViewPath = "Assets/Scripts/Views";
    private const string WorkingDirectory = "Assets/Test";

    public string contextName = "TestPanel";

    public GameObject rootPrefab;

    public string[] eventList = { "Click" };

    public bool createView = true;

    private string _scriptRootPath;

    [UsedImplicitly]
    private void OnWizardCreate()
    {
      if (contextName == string.Empty)
        return;

      CreateFolders();

      CreateTestScripts(_scriptRootPath);

      if (createView)
        CreateScripts(ViewPath);

      EditorPrefs.SetBool("CreateTest", true);
      EditorPrefs.SetBool("CreateView", createView);
      EditorPrefs.SetString("Name", contextName);
      EditorPrefs.SetString("RootPrefabPath", AssetDatabase.GetAssetPath(rootPrefab));

      AssetDatabase.Refresh();
    }

    public static void Create()
    {
      var wizard = DisplayWizard<CreateContextWizard>("Create New Test Context", "Create");
      wizard.minSize = new Vector2(400, 300); // Minimum pencere boyutu
      wizard.maxSize = new Vector2(400, 300); // Maksimum pencere boyutu 
      wizard.rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Common/RootContext.prefab");
    }

    private void CreateFolders()
    {
      if (!AssetDatabase.IsValidFolder(WorkingDirectory + "/" + contextName))
        AssetDatabase.CreateFolder(WorkingDirectory, contextName);
      var rootPath = WorkingDirectory + "/" + contextName;

      // create scene
      var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
      EditorSceneManager.SaveScene(newScene, rootPath + "/" + contextName + "TestScene.unity");

      if (!AssetDatabase.IsValidFolder(rootPath + "/Scripts"))
        AssetDatabase.CreateFolder(rootPath, "Scripts");
      _scriptRootPath = rootPath + "/" + "Scripts";
      if (!AssetDatabase.IsValidFolder(_scriptRootPath + "/Config"))
        AssetDatabase.CreateFolder(_scriptRootPath, "Config");
      if (!AssetDatabase.IsValidFolder(_scriptRootPath + "/Commands"))
        AssetDatabase.CreateFolder(_scriptRootPath, "Commands");
    }

    private void CreateTestScripts(string scriptRootPath)
    {
      AddScript(scriptRootPath + "/Config", contextName, TemplateType.Bootstrap,TemplateType.Bootstrap.ToString());
      
      if (this.createView)
        AddScript(scriptRootPath + "/Config", contextName, TemplateType.ContextWithView,TemplateType.Context.ToString());
      else
        AddScript(scriptRootPath + "/Config", contextName, TemplateType.Context,TemplateType.Context.ToString());
      
      AddScript(scriptRootPath + "/Commands", contextName, TemplateType.Command,TemplateType.Command.ToString());
    }

    private void CreateScripts(string scriptRootPath)
    {
      AssetDatabase.CreateFolder(scriptRootPath, contextName);
      var data = CodeUtilities.LoadTemplate(TemplateType.View.ToString());
      var namespaceValue = "Views." + contextName;
      data = data.Replace(CodePlaceholder.NamespacePlaceholder, namespaceValue);
      data = data.Replace(CodePlaceholder.ClassnamePlaceholder, contextName);
      if (eventList.Length == 0)
      {
        data = data.Replace(CodePlaceholder.FunctionPlaceholder, "");
      }
      else
      {
        var functions = "";
        for (var i = 0; i < eventList.Length; i++)
        {
          functions += string.Format(ViewFunctionTemplate, eventList[i], contextName);
          if (i < eventList.Length - 1)
            functions += "\r\t\t\r\t\t";
        }

        data = data.Replace(CodePlaceholder.FunctionPlaceholder, functions);
      }

      CodeUtilities.SaveFile(data, scriptRootPath + "/" + contextName + "/" + contextName + "View.cs");

      data = CodeUtilities.LoadTemplate(TemplateType.Mediator.ToString());
      data = data.Replace(CodePlaceholder.NamespacePlaceholder, namespaceValue);
      data = data.Replace(CodePlaceholder.ClassnamePlaceholder, contextName);
      if (eventList.Length == 0)
      {
        data = data.Replace(CodePlaceholder.FunctionPlaceholder, "");
        data = data.Replace(CodePlaceholder.EventPlaceholder, "");
        data = data.Replace(CodePlaceholder.AddListenerPlaceholder, "");
        data = data.Replace(CodePlaceholder.RemoveListenerPlaceholder, "");
      }
      else
      {
        var events = "";
        for (var i = 0; i < eventList.Length; i++)
        {
          events += eventList[i];
          if (i < eventList.Length - 1)
            events += ",\r\t\t";
        }

        data = data.Replace(CodePlaceholder.EventPlaceholder, events);

        var addListeners = "";
        for (var i = 0; i < eventList.Length; i++)
        {
          addListeners += string.Format(AddListenerTemplate, eventList[i], contextName);
          if (i < eventList.Length - 1)
            addListeners += "\r\t\t\t";
        }

        data = data.Replace(CodePlaceholder.AddListenerPlaceholder, addListeners);

        var functions = "";
        for (var i = 0; i < eventList.Length; i++)
        {
          functions += string.Format(MediatorFunctionTemplate, eventList[i]);
          if (i < eventList.Length - 1)
            functions += "\r\t\t\r\t\t";
        }

        data = data.Replace(CodePlaceholder.FunctionPlaceholder, functions);

        var removeListeners = "";
        for (var i = 0; i < eventList.Length; i++)
        {
          removeListeners += string.Format(RemoveListenerTemplate, eventList[i], contextName);
          if (i < eventList.Length - 1)
            removeListeners += "\r\t\t\t";
        }

        data = data.Replace(CodePlaceholder.RemoveListenerPlaceholder, removeListeners);
      }

      CodeUtilities.SaveFile(data, scriptRootPath + "/" + contextName + "/" + contextName + "Mediator.cs");
    }

    private static void AddScript(string path, string name, TemplateType type, string filename)
    {
      var data = CodeUtilities.LoadTemplate(type.ToString());
      data = data.Replace(CodePlaceholder.NamespacePlaceholder, path.Replace("Assets/", "").Replace("/", "."));
      data = data.Replace(CodePlaceholder.ClassnamePlaceholder, name + "Test");
      data = data.Replace("%Name%", name);
      if (type == TemplateType.Command)
        CodeUtilities.SaveFile(data, path + "/Init" + name + "Test" + filename + ".cs");
      else
        CodeUtilities.SaveFile(data, path + "/" + name + "Test" + filename + ".cs");
    }
  }
}