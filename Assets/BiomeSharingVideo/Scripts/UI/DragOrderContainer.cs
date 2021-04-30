using UnityEngine;
public class DragOrderContainer : MonoBehaviour
{
	public GameObject objectBeingDragged { get; set; }

	void Awake()
	{
		objectBeingDragged = null;
	}
}
