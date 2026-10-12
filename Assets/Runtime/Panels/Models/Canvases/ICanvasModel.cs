using System.Collections.Generic;
using UnityEngine;

namespace Com.Udogames.Core.Panels.Models.Canvases
{
  public interface ICanvasModel
  {
    void Add(string name, ICanvas camera);

    ICanvas GetByName(string name);

    void RemoveAndDestroyByName(string name);

    void RemoveByName(string name);

    void DisableAll();

    void EnableAllActives();

    void DisableByName(string name, bool silent = true);

    void EnableByName(string name, bool silent = true);
    List<ICanvas> GetAll();
  }
}