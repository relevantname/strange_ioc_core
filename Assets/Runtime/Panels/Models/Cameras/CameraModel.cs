using System;
using System.Collections.Generic;
using Com.Udogames.Core.Panels.Views.Cameras;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Panels.Models.Cameras
{
  public class CameraModel : ICameraModel, IDisposable
  {
    private Dictionary<string, Camera> _map = new Dictionary<string, Camera>();

    public Dictionary<string, Camera> actives = new Dictionary<string, Camera>();

    private bool _disabled;

    public void Dispose()
    {
      _map.Clear();
      
      actives.Clear();

      _disabled = false;
    }

    public void DisableAll()
    {
      actives.Clear();
      foreach (KeyValuePair<string, Camera> pair in _map)
      {
        if (pair.Value == null)
        {
          continue;
        }

        if (pair.Value.enabled)
        {
          actives.Add(pair.Key, pair.Value);
          pair.Value.enabled = false;
        }
      }

      _disabled = true;
    }

    public void EnableAllActives()
    {
      foreach (KeyValuePair<string, Camera> pair in actives)
      {
        if (pair.Value != null)
        {
          pair.Value.enabled = true;
        }
      }

      actives.Clear();
      _disabled = false;
    }

    public void EnableByName(string name, bool silent = true)
    {
      if (!_map.ContainsKey(name))
        return;

      if (_disabled)
      {
        if (!actives.ContainsKey(name))
        {
          actives.Add(name, _map[name]);
        }
      }
      else
      {
        _map[name].enabled = true;
      }
    }

    public void DisableByName(string name, bool silent = true)
    {
      if (!_map.ContainsKey(name))
        return;

      if (_disabled)
      {
        if (actives.ContainsKey(name))
          actives.Remove(name);
      }
      else
      {
        _map[name].enabled = false;
      }
    }

    public void Add(string name, Camera camera)
    {
      if (_map.ContainsKey(name))
        return;

      _map.Add(name, camera);
      
      if(camera.enabled)
        actives.Add(name, _map[name]);
    }

    public void RemoveAndDestroyByName(string name)
    {
      if (!_map.ContainsKey(name))
        return;

      if (actives.ContainsKey(name))
      {
        actives.Remove(name);
      }

      Object.Destroy(_map[name].gameObject);

      _map.Remove(name);
    }

    public Camera GetByName(string name)
    {
      if (_map.ContainsKey(name))
        return _map[name];

      return null;
    }

    public GameObject Create(string name, Transform parent)
    {
      GameObject camera = new(name, typeof(Camera), typeof(CameraView));
      camera.GetComponent<CameraView>().key = name;
      camera.transform.SetParent(parent);
      camera.transform.position = Vector3.zero;
      Add(name, camera.GetComponent<Camera>());
      return camera;
    }

    public void RemoveByName(string name)
    {
      if (!_map.ContainsKey(name))
        return;

      if (actives.ContainsKey(name))
      {
        actives.Remove(name);
      }

      _map.Remove(name);
    }

    public Camera GetFirstActiveCamera()
    {
      foreach (KeyValuePair<string, Camera> pair in _map)
      {
        if (pair.Value == null)
        {
          continue;
        }

        if (pair.Value.enabled)
        {
          return pair.Value;
        }
      }

      return null;
    }
  }
}