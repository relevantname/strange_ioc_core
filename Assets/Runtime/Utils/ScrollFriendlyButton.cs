using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Com.Udogames.Core.Utils
{
  public class ScrollFriendlyButton : Button, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
  {
    private ScrollRect _scrollRect;
    private bool _dragging;

    protected override void Awake()
    {
      base.Awake();
      _scrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
      _dragging = true;
      if (_scrollRect != null)
        _scrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
      if (_scrollRect != null)
        _scrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
      if (_scrollRect != null)
        _scrollRect.OnEndDrag(eventData);
      _dragging = false;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
      if (!_dragging)
        base.OnPointerClick(eventData);
    }
    
    public void OnScroll(PointerEventData eventData)
    {
      if (_scrollRect != null)
        _scrollRect.OnScroll(eventData);
    }
  }
}