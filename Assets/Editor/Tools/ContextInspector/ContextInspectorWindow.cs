using System;
using System.Collections.Generic;
using System.Linq;
using Com.Udogames.Core.Editor.Extensions;
using Com.Udogames.Core.Editor.Tools.Common;
using Com.Udogames.Core.Editor.Tools.ProjectBrowser;
using Newtonsoft.Json;
using strange.extensions.command.api;
using strange.extensions.command.impl;
using strange.extensions.context.api;
using strange.extensions.context.impl;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.implicitBind.api;
using strange.extensions.injector.api;
using strange.extensions.injector.impl;
using strange.extensions.mediation.api;
using strange.extensions.mediation.impl;
using strange.extensions.sequencer.api;
using strange.framework.api;
using strange.framework.impl;
using UnityEditor;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ContextInspector
{
  public class ContextInspectorWindow : EditorWindow
  {
    private Vector2 _scrollPos;
    private Vector2 _detailsScrollPos;

    private BindingCategory _selectedTab;

    private int _selectedContextIndex;

    private string _cachedJson;

    private object _cachedDetailObject;

    private Dictionary<BindingCategory, Action> _drawActionByType;

    private List<Type> _coreTypes = new List<Type>()
    {
      typeof(IInstanceProvider),
      typeof(IInjectionBinder),
      typeof(ISequencer),
      typeof(ICommandBinder),
      typeof(IMediationBinder),
      typeof(IImplicitBinder),
      typeof(ICommand),
      typeof(CrossContextBridge),
    };

    private ContextView[] _contextViews;

    [MenuItem("Window/Udo Core/Context Inspector")]
    public static void ShowWindow()
    {
      ContextInspectorWindow window = GetWindow<ContextInspectorWindow>("Context Inspector");
      window.SetEditorIcon("d_UnityEditor.InspectorWindow");
    }

    private void OnGUI()
    {
      if (CheckPanelActivation())
        return;

      DrawContextSelector();

      _selectedTab = (BindingCategory)GUILayout.Toolbar((int)_selectedTab, Enum.GetNames(typeof(BindingCategory)));

      _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

      var selectedContextView = _contextViews[_selectedContextIndex];

      if (CheckErrorCases(selectedContextView, out var bindings))
      {
        EditorGUILayout.EndScrollView();
        return;
      }

      if (_selectedTab == BindingCategory.Commands)
      {
        foreach (object type in bindings.Keys)
        {
          DrawCommandListItem(type, bindings);
        }
      }
      else if (_selectedTab == BindingCategory.Views)
      {
        foreach (Type type in bindings.Keys)
        {
          DrawViewListItem(type, bindings);
        }
      }
      else
      {
        foreach (Type type in bindings.Keys)
        {
          if (ShouldSkipType(type)) continue;

          switch (_selectedTab)
          {
            case BindingCategory.CoreTypes:
              DrawListItem(type, bindings);
              break;
            case BindingCategory.Models:
              DrawListItem(type, bindings);
              break;
            default:
              DrawListItem(type, bindings);
              break;
          }
        }
      }


      EditorGUILayout.EndScrollView();
    }

    private void DrawCommandListItem(object type, Dictionary<object, Dictionary<object, IBinding>> bindings)
    {
      Dictionary<object, IBinding> dictionary = bindings[type];

      EditorGUILayout.BeginVertical("box");
      EditorGUILayout.LabelField("Event Key: " + type, EditorStyles.boldLabel);

      foreach (KeyValuePair<object, IBinding> valuePair in dictionary)
      {
        object[] bindValue = (object[])valuePair.Value.value;

        foreach (Type bindType in bindValue)
        {
          GUILayout.Label(bindType.Name);
        }
      }

      EditorGUILayout.BeginHorizontal("box");

      if (GUILayout.Button("Dispatch"))
      {
        try
        {
          var selectedContextView = _contextViews[_selectedContextIndex];
          var context = selectedContextView.context as MVCSContext;
          if (context != null)
          {
            var injectionBinder = context.injectionBinder as InjectionBinder;
            if (injectionBinder != null)
            {
              IEventDispatcher eventDispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CONTEXT_DISPATCHER);
              eventDispatcher.Dispatch(type);
            }
          }
        }
        catch (Exception e)
        {
          Debug.LogException(e);
        }
      }

      if (GUILayout.Button("Cross Dispatch"))
      {
        try
        {
          var selectedContextView = _contextViews[_selectedContextIndex];
          var context = selectedContextView.context as MVCSContext;
          if (context != null)
          {
            var injectionBinder = context.injectionBinder as InjectionBinder;
            if (injectionBinder != null)
            {
              IEventDispatcher eventDispatcher = injectionBinder.GetInstance<IEventDispatcher>(ContextKeys.CROSS_CONTEXT_DISPATCHER);
              eventDispatcher.Dispatch(type);
            }
          }
        }
        catch (Exception e)
        {
          Debug.LogException(e);
        }
      }

      EditorGUILayout.EndHorizontal();

      EditorGUILayout.EndVertical();
    }

    private void DrawViewListItem(Type type, Dictionary<object, Dictionary<object, IBinding>> bindings)
    {
      EditorGUILayout.BeginVertical("box");
      EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);

      Dictionary<object, IBinding> dictionary = bindings[type];

      foreach (KeyValuePair<object, IBinding> valuePair in dictionary)
      {
        object[] bindValue = (object[])valuePair.Value.value;
        foreach (Type bindType in bindValue)
        {
          GUILayout.Label(bindType.Name);
        }
      }

      EditorGUILayout.EndVertical();
    }

    private void DrawListItem(Type type, Dictionary<object, Dictionary<object, IBinding>> bindings)
    {
      EditorGUILayout.BeginVertical("box");
      EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);

      Dictionary<object, IBinding> dictionary = bindings[type];

      foreach (KeyValuePair<object, IBinding> valuePair in dictionary)
      {
        EditorGUILayout.BeginHorizontal();

        BindingConst key = (BindingConst)valuePair.Key;
        if (key != BindingConst.NULLOID)
        {
          GUILayout.Label("Key:", GUILayout.Width(25));
          GUILayout.Label(valuePair.Key.ToString());
        }


        object bindValue = valuePair.Value.value;
        Type bindType = bindValue.GetType();

        GUILayout.Label("Type:", GUILayout.Width(40));
        GUILayout.Label(bindType.Name);

        EditorGUILayout.EndHorizontal();

        if (_selectedTab == BindingCategory.Models)
        {
          if (GUILayout.Button("View Data In Json"))
          {
            ViewDataInJson(bindValue, bindType);
          }
        }
      }

      EditorGUILayout.EndVertical();
    }

    private static void ViewDataInJson(object bindValue, Type bindType)
    {
      // Serialize bindValue to JSON
      var settings = new JsonSerializerSettings()
      {
        ContractResolver = new CustomFieldsContractResolver(),
        Formatting = Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        Error = (sender, args) => { args.ErrorContext.Handled = true; }
      };

      string json = JsonConvert.SerializeObject(bindValue, settings);

      // Save to a temp file
      string filePath = System.IO.Path.Combine(Application.dataPath, $"../Temp/{bindType.Name}.json");
      System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath) ?? string.Empty);
      System.IO.File.WriteAllText(filePath, json);

      // Get Unity's selected code editor path
      FileOpenHelper.OpenFileInIde(filePath);
    }

    private bool CheckPanelActivation()
    {
      if (!EditorApplication.isPlaying)
      {
        EditorGUILayout.HelpBox("Enter Play Mode to see bound models.", MessageType.Info);
        return true;
      }

      _contextViews = FindObjectsByType<ContextView>(FindObjectsSortMode.None);
      if (_contextViews == null || _contextViews.Length == 0)
      {
        EditorGUILayout.HelpBox("No ContextView found in scene.", MessageType.Warning);
        return true;
      }

      return false;
    }

    private void DrawContextSelector()
    {
      string[] contextNames = _contextViews.Select(cv => cv.gameObject.name).ToArray();
      _selectedContextIndex = EditorGUILayout.Popup("Select Context", _selectedContextIndex, contextNames);
    }

    private bool CheckErrorCases(ContextView selectedContextView, out Dictionary<object, Dictionary<object, IBinding>> bindings)
    {
      bindings = null;
      var context = selectedContextView.context as MVCSContext;
      if (context == null)
      {
        EditorGUILayout.LabelField("ContextView '" + selectedContextView.gameObject.name + "' has no context attached.", EditorStyles.helpBox);
        return true;
      }

      var injectionBinder = context.injectionBinder as InjectionBinder;
      if (injectionBinder == null)
      {
        EditorGUILayout.LabelField("InjectionBinder not found or invalid.", EditorStyles.helpBox);
        return true;
      }

      bindings = injectionBinder.bindings;

      if (_selectedTab == BindingCategory.Views)
      {
        Binder mediationBinder = injectionBinder.GetInstance<IMediationBinder>() as Binder;
        if (mediationBinder != null) bindings = mediationBinder.bindings;
      }

      if (_selectedTab == BindingCategory.Commands)
      {
        Binder commandBinder = injectionBinder.GetInstance<ICommandBinder>() as Binder;
        if (commandBinder != null) bindings = commandBinder.bindings;
      }

      if (bindings == null || bindings.Count == 0)
      {
        EditorGUILayout.LabelField("No bindings found.", EditorStyles.helpBox);
        return true;
      }

      return false;
    }

    private bool ShouldSkipType(Type type)
    {
      bool show = false;

      if (_selectedTab == BindingCategory.CoreTypes && _coreTypes.Contains(type)) show = true;
      else if (_selectedTab == BindingCategory.Views && typeof(View).IsAssignableFrom(type) && type != typeof(View)) show = true;
      else if (_selectedTab == BindingCategory.Commands && typeof(Command).IsAssignableFrom(type) && type != typeof(Command)) show = true;
      else if (_selectedTab == BindingCategory.Models && !(typeof(View).IsAssignableFrom(type) || typeof(Mediator).IsAssignableFrom(type) || typeof(Command).IsAssignableFrom(type)) &&
               !_coreTypes.Contains(type)) show = true;

      return !show;
    }
  }
}