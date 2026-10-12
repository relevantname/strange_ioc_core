using System;
using System.Collections.Generic;
using Com.Udogames.Core.DebugLogger;
using Com.Udogames.Core.Panels.Enums;
using Com.Udogames.Core.Panels.Models.Canvases;
using Com.Udogames.Core.PromiseTool;
using strange.extensions.context.api;
using strange.extensions.dispatcher.eventdispatcher.api;
using strange.extensions.injector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Panels.Models.Panel
{
  public class PanelModel : IPanelModel
  {
    [Inject]
    public ICanvasModel canvasModel { get; set; }
    
    [Inject(ContextKeys.CONTEXT_DISPATCHER)]
    public IEventDispatcher dispatcher { get; set; }

    private readonly Dictionary<string, IPromise> _promiseByPageKey = new();

    private readonly HashSet<string> _openPageList = new();

    public IPromise Open(string name, string canvasKey)
    {
      return OpenNextPanelAsync(name, canvasKey);
    }

    private IPromise OpenNextPanelAsync(string name, string canvasKey)
    {
      DebugX.Log($"Openning panel with name {name} on {canvasKey} canvas.", "PanelModel");
      Promise promise = new();

      ICanvas canvas = canvasModel.GetByName(canvasKey);
      if (canvas == null)
      {
        Exception exception = new Exception($"Canvas not found with key {canvasKey} to open {name}.");
        DebugX.LogError(exception.Message, "PanelModel");
        promise.Reject(exception);
        _promiseByPageKey.Remove(name);
      }
      else
      {
        if (canvas.clearAll)
        {
          Clear(canvas.xKey);
        }
        
        if (_promiseByPageKey.ContainsKey(name))
        {
          CancelPromise(name);
          _promiseByPageKey.Remove(name);
        }

        _promiseByPageKey.Add(name, promise);

        _openPageList.Add(name);

        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(name, canvas.xContainer);

        handle.Completed += operationHandle =>
        {
          if (operationHandle.Status == AsyncOperationStatus.Succeeded)
          {
            if (operationHandle.Result == null)
            {
              if (_openPageList.Contains(name))
              {
                _openPageList.Remove(name);
              }

              if (_promiseByPageKey.ContainsKey(name))
              {
                _promiseByPageKey.Remove(name);
              }

              if (promise.CurState == PromiseState.Pending)
              {
                Exception exception = new Exception($"Current state is {promise.CurState} but had load error {name}");
                DebugX.LogWarning(exception.Message, "PanelModel");
                promise.Reject(exception);
              }

              return;
            }

            if (promise.CurState != PromiseState.Pending)
            {
              DebugX.LogWarning($"Current state is {promise.CurState} skipped loading panel with name {name}", "PanelModel");
              if (_openPageList.Contains(name))
                _openPageList.Remove(name);
              Object.DestroyImmediate(operationHandle.Result);
              return;
            }

            operationHandle.Result.name = name;
            DebugX.Log($"Panel loaded with name {name} to canvas {canvasKey}", "PanelModel");
            promise.Resolve();
            dispatcher.Dispatch(PanelEvent.PanelOpened, name);

            _promiseByPageKey.Remove(name);
          }
          else
          {
            Addressables.Release(operationHandle);
            DebugX.LogError(operationHandle.OperationException.Message, "PanelModel");
            promise.Reject(operationHandle.OperationException);
            _promiseByPageKey.Remove(name);
          }
        };
      }

      return promise;
    }

    private void RemoveOpenScreen(Transform screen)
    {
      if (screen == null)
        return;

      string name = screen.gameObject.name;
      _openPageList.Remove(name);

      DebugX.Log($"Open Panel with name {name} destroyed.", "PanelModel");
      if (_promiseByPageKey.ContainsKey(name))
      {
        _promiseByPageKey[name].Cancel();
        _promiseByPageKey.Remove(name);
      }

      Object.DestroyImmediate(screen.gameObject);
    }

    public bool IsOpen(string key)
    {
      return _openPageList.Contains(key);
    }

    public IPromise Close(string name, string canvasKey)
    {
      Promise promise = new Promise();
      DebugX.Log($"Panel with name {name} closed from {canvasKey} canvas.", "PanelModel");

      ICanvas canvas = canvasModel.GetByName(canvasKey);
      if (canvas == null)
      {
        if (_openPageList.Contains(name))
        {
          _openPageList.Remove(name);
        }
        promise.Resolve();
        return promise;
      }

      if (_promiseByPageKey.ContainsKey(name))
      {
        _promiseByPageKey[name].Cancel();
        _promiseByPageKey.Remove(name);
      }

      foreach (Transform screen in canvas.xContainer)
      {
        if (screen.name == name)
        {
          RemoveOpenScreen(screen);
        }
      }
      
      dispatcher.Dispatch(PanelEvent.PanelClosed, name);
      promise.Resolve();
      return promise;
    }

    public void Clear(string canvasKey)
    {
      DebugX.Log($"Clear {canvasKey} canvas.", "PanelModel");

      ICanvas canvas = canvasModel.GetByName(canvasKey);
      if (canvas == null)
      {
        return;
      }

      var count = canvas.xContainer.childCount;
      for (var i = 0; i < count; i++)
      {
        Transform screen = canvas.xContainer.GetChild(0);
        string screenName = screen.name;
        
        RemoveOpenScreen(screen);

        if (_openPageList.Contains(screenName))
        {
          _openPageList.Remove(screenName);
        }

        if (_promiseByPageKey.ContainsKey(screenName))
        {
          _promiseByPageKey[screenName].Cancel();
          _promiseByPageKey.Remove(screenName);
        }
      }
    }

    public IPromise ClearAll()
    { 
      Promise promise = new Promise();
      
      List<ICanvas> allCanvases = canvasModel.GetAll();
      if (allCanvases == null || allCanvases.Count <= 0)
      {
        promise.Resolve();
        return promise;
      }

      foreach (var canvas in allCanvases)
      {
        Clear(canvas.xKey);
      }
      
      promise.Resolve();
      return promise;
    }

    public void Reset()
    {
      _openPageList.Clear();
      foreach (var keyValue in _promiseByPageKey)
      {
        Promise promise = (Promise)keyValue.Value;
        if (promise.CurState == PromiseState.Pending)
          promise.Cancel();
      }
      _promiseByPageKey.Clear();
    }

    private void CancelPromise(string name)
    {
      DebugX.Log($"Panel with name {name} loading cancelled.", "PanelModel");
      Promise promise = (Promise)_promiseByPageKey[name];
      if (promise == null) return;
      if (promise.CurState == PromiseState.Pending)
        promise.Cancel();
    }
  }
}