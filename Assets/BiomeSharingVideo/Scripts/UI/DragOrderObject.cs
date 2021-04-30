using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DragOrderObject : MonoBehaviour, IPointerEnterHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public UnityEvent OnOrderChange;

	DragOrderContainer container = null;

	void Start()
	{
		container = GetComponentInParent<DragOrderContainer>();

		if ( OnOrderChange == null )
			OnOrderChange = new UnityEvent();
	}

	public void OnBeginDrag( PointerEventData eventData )
	{
		container.objectBeingDragged = this.gameObject;
	}
	public void OnDrag( PointerEventData data )
	{
		// Do nothing
		// Apparently this interface needs to exist in order for BeginDrag and EndDrag to work,
		// but we don't actually have anything to do here
	}
	public void OnEndDrag( PointerEventData eventData )
	{
		if ( container.objectBeingDragged == this.gameObject ) container.objectBeingDragged = null;
	}

	public void OnPointerEnter( PointerEventData eventData )
	{
		GameObject objectBeingDragged = container.objectBeingDragged;
		if ( objectBeingDragged != null && objectBeingDragged != this.gameObject )
		{
			objectBeingDragged.transform.SetSiblingIndex( this.transform.GetSiblingIndex() );

			if ( OnOrderChange != null )
			{
				OnOrderChange.Invoke();
			}
		}
	}
}
