using Com.Udogames.Core.Panels.Models.Canvases;
using strange.extensions.injector;
using strange.extensions.mediation.impl;

namespace Com.Udogames.Core.Panels.Views.Canvases
{
  public class CanvasMediator : EventMediator
  {
    [Inject]
    public CanvasView view { get; set; }

    [Inject]
    public ICanvasModel canvasModel { get; set; }

    private bool _dontLoad;

    public override void OnRegister()
    {
      if (_dontLoad)
      {
        return;
      }

      if (!string.IsNullOrEmpty(view.key))
      {
        canvasModel.Add(view.key, view);
      }
    }

    public override void OnRemove()
    {
      if (_dontLoad)
      {
        return;
      }

      canvasModel.RemoveByName(view.key);
    }
  }
}