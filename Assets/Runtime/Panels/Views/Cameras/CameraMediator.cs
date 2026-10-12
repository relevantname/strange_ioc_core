using Com.Udogames.Core.Panels.Models.Cameras;
using strange.extensions.injector;
using strange.extensions.mediation.impl;
using UnityEngine;

namespace Com.Udogames.Core.Panels.Views.Cameras
{
  public class CameraMediator : EventMediator
  {
    [Inject]
    public CameraView view { get; set; }

    [Inject]
    public ICameraModel cameraModel { get; set; }

    private bool _dontLoad;

    public override void OnRegister()
    {
      if (_dontLoad)
      {
        return;
      }

      if (!string.IsNullOrEmpty(view.key))
      {
        Camera currentCamera = GetComponent<Camera>();
        cameraModel.Add(view.key, currentCamera);
      }
    }

    public override void OnRemove()
    {
      if (_dontLoad)
      {
        return;
      }

      cameraModel.RemoveByName(view.key);
    }
  }
}