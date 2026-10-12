using System.Collections;
using UnityEngine;

namespace Com.Udogames.Core.Timer
{
  public interface ITimerModel
  {
    Coroutine StartCoroutine(IEnumerator routine);

    void StopCoroutine(Coroutine routine);
  }
}