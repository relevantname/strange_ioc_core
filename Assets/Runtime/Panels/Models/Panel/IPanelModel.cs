
using Com.Udogames.Core.PromiseTool;

namespace Com.Udogames.Core.Panels.Models.Panel
{
  public interface IPanelModel
  {
    bool IsOpen(string key);

    IPromise Open(string name, string canvasKey);

    IPromise Close(string name, string canvasKey);
    
    void Clear(string canvasKey);
    
    IPromise ClearAll();
    void Reset();
  }
}