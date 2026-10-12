using Com.Udogames.Core.Panels.Models.Canvases;
using strange.extensions.mediation.impl;
using UnityEngine;

namespace Com.Udogames.Core.Panels.Views.Canvases
{
  [RequireComponent(typeof(Canvas))]
  public class CanvasView : EventView, ICanvas
  {
    public string key = "Default";

    public bool clearOnAwake;

    public bool isSinglePanel;
    
    public bool clearAll 
    {
      get => isSinglePanel;
    }

    public bool isEmpty { get => container.childCount == 0; }

    public Transform container;

    public Canvas canvas;

    protected override void Awake()
    {
      base.Awake();

      if (clearOnAwake)
      {
        var count = container.childCount;
        for (var i = 0; i < count; i++)
        {
          DestroyImmediate(container.GetChild(0).gameObject);
        }
      }
    }

    public bool xActive
    {
      get => canvas.enabled;
      set => canvas.enabled = value;
    }
    public string xKey
    {
      get => key;
    }
    public GameObject xGameObject
    {
      get { return gameObject; }
    }
    public Transform xContainer
    {
      get { return container; }
    }
    public Canvas xCanvas
    {
      get { return canvas; }
    }
  }
}