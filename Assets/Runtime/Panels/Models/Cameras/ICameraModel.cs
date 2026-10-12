using UnityEngine;

namespace Com.Udogames.Core.Panels.Models.Cameras
{
  public interface ICameraModel
  {
    void Add(string name, Camera camera);

    Camera GetByName(string name);

    GameObject Create(string name, Transform parent);

    void RemoveAndDestroyByName(string name);

    void RemoveByName(string name);

     void DisableAll();

    void EnableAllActives();

    void DisableByName(string name, bool silent = true);

    void EnableByName(string name, bool silent = true);

    Camera GetFirstActiveCamera();
  }
}