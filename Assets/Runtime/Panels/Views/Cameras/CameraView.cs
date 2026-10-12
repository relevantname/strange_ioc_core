using strange.extensions.mediation.impl;
using UnityEngine;
using UnityEngine.Serialization;

namespace Com.Udogames.Core.Panels.Views.Cameras
{
  [RequireComponent(typeof(Camera))]
  public class CameraView : EventView
  {
    public string key = "Main";
  }
}