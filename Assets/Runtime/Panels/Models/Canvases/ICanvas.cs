using UnityEngine;

namespace Com.Udogames.Core.Panels.Models.Canvases
{
  public interface ICanvas
  {
    bool xActive { get; set; }

    string xKey { get; }

    GameObject xGameObject { get; }

    Transform xContainer { get; }

    Canvas xCanvas { get; }
    
    bool clearAll { get; }
    
    bool isEmpty { get; }
  }
}