using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Com.Udogames.Core.Panels.Models.Canvases
{
  public class CanvasModel : ICanvasModel, IDisposable
  {
    private Dictionary<string, ICanvas> _map = new ();

    public Dictionary<string, ICanvas> actives = new ();

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
      foreach (KeyValuePair<string, ICanvas> pair in _map)
      {
        if (pair.Value == null)
        {
          continue;
        }

        if (pair.Value.xActive)
        {
          actives.Add(pair.Key, pair.Value);
          pair.Value.xActive = false;
        }
      }

      _disabled = true;
    }

    public void EnableAllActives()
    {
      foreach (KeyValuePair<string, ICanvas> pair in actives)
      {
        if (pair.Value != null)
        {
          pair.Value.xActive = true;
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
        _map[name].xActive = true;
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
        _map[name].xActive = false;
      }
    }

    public void Add(string name, ICanvas canvas)
    {
      if (_map.ContainsKey(name))
        return;

      _map.Add(name, canvas);
      
      if(canvas.xActive)
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

      Object.Destroy(_map[name].xGameObject);

      _map.Remove(name);
    }

    public ICanvas GetByName(string name)
    {
      if (_map.ContainsKey(name))
        return _map[name];

      return null;
    }

    public List<ICanvas> GetAll()
    {
      List<ICanvas> all = _map.Values.ToList();
      return all;
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
  }
}