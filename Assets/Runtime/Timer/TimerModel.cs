using System.Collections;
using strange.extensions.context.api;
using strange.extensions.injector;
using UnityEngine;

namespace Com.Udogames.Core.Timer
{
  public class TimerModel : ITimerModel
  {
    private MonoBehaviour _monoBehaviour;

    [Inject(ContextKeys.CONTEXT_VIEW)]
    public GameObject contextView { get; set; }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
      return _monoBehaviour.StartCoroutine(routine);
    }

    public void StopCoroutine(Coroutine routine)
    {
      if (routine == null) return;

      _monoBehaviour.StopCoroutine(routine);
    }

    [PostConstruct]
    public void OnPostConstruct()
    {
      _monoBehaviour = contextView.GetComponent<MonoBehaviour>();
    }
  }
}