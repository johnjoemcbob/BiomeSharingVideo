using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HideEditPanel : MonoBehaviour
{
	public float LerpSpeed = 5;

	public RectTransform Panel;

	private float Base = 0;
	private float BaseY = 0;
	private float Target = 0;

	private void Start()
	{
		Base = Panel.transform.localPosition.x;
		BaseY = Panel.transform.localPosition.y;
		Target = Base;
	}

	void Update()
    {
		Panel.transform.localPosition = Vector3.Lerp( Panel.transform.localPosition, new Vector3( Target, BaseY, 0 ), Time.deltaTime * LerpSpeed );
		
		if ( Input.GetKeyDown( KeyCode.Tab ) )
		{
			ButtonHideShow();
		}
	}

	public void ButtonHideShow()
	{
		bool show = true;
			if ( Target == Base )
			{
				show = false;
				Target = Base - 512;
			}
			else
			{
				Target = Base;
			}
		GetComponentInChildren<Text>().text = show ? "<" : ">";
	}
}
