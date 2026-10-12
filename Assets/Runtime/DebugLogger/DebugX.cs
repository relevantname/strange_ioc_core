#if UNITY_EDITOR

using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Com.Udogames.Core.PromiseTool;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = UnityEngine.Debug;

namespace Com.Udogames.Core.DebugLogger
{
  [Serializable]
  public class DebugX : ScriptableObject
  {
    [NonSerialized]
    public const string DataPath = "Assets/Settings/LoggerSettings.asset";

    public List<LoggerVo> list;

    private static Dictionary<string, LoggerVo> _logMap;

    private static bool _inited;

    public static Action<string> onLog;

    [Conditional("UNITY_EDITOR")]
    // [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
      Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    // [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, string tag = "")
    {
      Debug.LogError(message);
    }

    [Conditional("UNITY_EDITOR")]
    // [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, string tag = "")
    {
      Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    // [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, string tag)
    {
      if (message.Length > 16000)
      {
        IEnumerable<string> split = Split(message, 16000);
        foreach (string s in split)
        {
          Log(s, tag);
        }

        return;
      }

      if (!_inited)
      {
        Init();
      }

      if (string.IsNullOrEmpty(tag))
      {
        Debug.Log(tag);
      }
      else
      {
        if (!_logMap.ContainsKey(tag))
        {
          Log(message);
          return;
        }

        LoggerVo loggerVo = _logMap[tag];

        if (!loggerVo.active) return;
        if (loggerVo.ignoreKeys.Where(vo => vo.active).Any(vo => message.Contains(vo.key))) return;

        string s = "<color=#" + ColorUtility.ToHtmlStringRGBA(loggerVo.color) + ">" + tag + "</color>: " +
                   message;

        Debug.Log("[" + Thread.CurrentThread.ManagedThreadId + "] " + s);
        onLog?.Invoke(s);
      }
    }


    public static IPromise Init()
    {
      Promise promise = new Promise();
      _logMap = new Dictionary<string, LoggerVo>();
      AsyncOperationHandle<DebugX> handle = Addressables.LoadAssetAsync<DebugX>("LoggerSettings");
      handle.Completed += operationHandle =>
      {
        if (operationHandle.IsDone)
        {
          DebugX debugX = operationHandle.Result;
          foreach (LoggerVo vo in debugX.list)
          {
            _logMap[vo.tag] = vo;
          }

          _inited = true;
          promise.Resolve();
        }
        else
        {
          promise.Reject(operationHandle.OperationException);
        }
      };

      return promise;
    }

    static IEnumerable<string> Split(string str, int chunkSize)
    {
      return Enumerable.Range(0, str.Length / chunkSize)
        .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

#if UNITY_EDITOR
    [
      MenuItem("Assets/Create/Udo Core/Logger Settings")]
    public static void CreateMyAsset()
    {
      DebugX old = AssetDatabase.LoadAssetAtPath<DebugX>(DataPath);
      if (old != null)
      {
        Selection.activeObject = old;
        return;
      }

      DebugX asset = CreateInstance<DebugX>();
      AssetDatabase.CreateAsset(asset, DataPath);
      AssetDatabase.SaveAssets();
      EditorUtility.FocusProjectWindow();
      Selection.activeObject = asset;
    }
#endif
  }
}