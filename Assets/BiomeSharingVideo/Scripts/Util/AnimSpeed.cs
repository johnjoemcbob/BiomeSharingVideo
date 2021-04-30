using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimSpeed : MonoBehaviour
{
	public float Speed = 0.1f;

    void Start()
    {
		GetComponent<Animation>()["PortfolioDay"].speed = Speed;
    }
}
