using System;
using System.Collections;
using UnityEngine;

namespace Com.Udogames.Core.Assets.Utils
{
  /// <summary>
  /// Utility for running coroutines from non-MonoBehaviour classes
  /// </summary>
  public class CoroutineRunner : MonoBehaviour
  {
    public static CoroutineRunner GetOrCreate(GameObject gameObject)
    {
      var component = gameObject.GetComponent<CoroutineRunner>();
      if (component == null)
        component = gameObject.AddComponent<CoroutineRunner>();

      return component;
    }

    /// <summary>
    /// Runs a coroutine
    /// </summary>
    public Coroutine RunCoroutine(IEnumerator routine)
    {
      return StartCoroutine(routine);
    }

    /// <summary>
    /// Runs a coroutine and waits for its completion before continuing.
    /// Use this when you need to run a coroutine from within another coroutine and wait for it to complete.
    /// </summary>
    /// <param name="routine">The coroutine to run and wait for</param>
    /// <returns>A yield instruction that can be yielded to wait for the coroutine's completion</returns>
    public CustomYieldInstruction RunCoroutineAndWait(IEnumerator routine)
    {
      bool isComplete = false;
      RunCoroutine(WrapCoroutineForWaiting(routine, () => isComplete = true));
      return new WaitUntil(() => isComplete);
    }

    /// <summary>
    /// Runs a coroutine with completion callback
    /// </summary>
    public void RunCoroutine(IEnumerator routine, Action onComplete)
    {
      StartCoroutine(WrapCoroutine(routine, onComplete));
    }

    /// <summary>
    /// Runs an action on the main thread
    /// </summary>
    public void RunOnMainThread(Action action)
    {
      if (action == null) return;
      StartCoroutine(ExecuteOnMainThread(action));
    }

    /// <summary>
    /// Runs an action after a delay
    /// </summary>
    public void RunDelayed(Action action, float delayInSeconds)
    {
      if (action == null) return;
      StartCoroutine(ExecuteWithDelay(action, delayInSeconds));
    }

    /// <summary>
    /// Stops a coroutine
    /// </summary>
    public void StopRunningCoroutine(Coroutine coroutine)
    {
      if (coroutine != null)
      {
        StopCoroutine(coroutine);
      }
    }

    private IEnumerator WrapCoroutine(IEnumerator routine, Action onComplete)
    {
      yield return StartCoroutine(routine);
      onComplete?.Invoke();
    }

    private IEnumerator WrapCoroutineForWaiting(IEnumerator routine, Action onComplete)
    {
      yield return StartCoroutine(routine);
      onComplete?.Invoke();
    }

    private IEnumerator ExecuteOnMainThread(Action action)
    {
      yield return null;
      action();
    }

    private IEnumerator ExecuteWithDelay(Action action, float delayInSeconds)
    {
      yield return new WaitForSeconds(delayInSeconds);
      action();
    }
  }
}