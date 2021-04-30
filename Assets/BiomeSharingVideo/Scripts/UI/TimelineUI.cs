using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimelineUI : MonoBehaviour
{
	private Slider Slider;

	private bool HumanInput = true;

	void Start()
    {
		Slider = GetComponentInChildren<Slider>();
	}

    void Update()
	{
		HumanInput = false;
		Slider.value = Game.Instance.CurrentTime / ( (float) Game.Instance.VideoLength + Game.END_CARD_TIME );
	}

	public void SliderOnValueChange( float val )
	{
		if ( HumanInput )
		{
			Game.Instance.SetTime( val * ( (float) Game.Instance.VideoLength + Game.END_CARD_TIME ) );
		}
		HumanInput = true;
	}
}
