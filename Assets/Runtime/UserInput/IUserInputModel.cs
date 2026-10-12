using UnityEngine;

namespace Com.Udogames.Core.UserInput
{
  public interface IUserInputModel
  {
    bool IfClose();

    bool IfPressed();

    Vector2 mousePosition { get; }
    
    bool IfReleased();
  }
}