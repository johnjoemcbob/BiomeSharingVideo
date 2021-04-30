using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class AnimPlayOnPress : MonoBehaviour
{
    void Update()
    {
        if ( Input.GetKeyDown( KeyCode.Space ) )
		{
			GetComponent<Animation>().Stop();
			GetComponent<Animation>().Play();

			foreach ( var vid in FindObjectsOfType<VideoPlayer>() )
			{
				vid.Stop();
				vid.Play();
			}
		}
	}
}
