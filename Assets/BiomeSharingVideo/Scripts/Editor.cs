using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Editor : MonoBehaviour
{
	public static Editor Instance;

	[Header( "References" )]
	public Transform VideoParent;
	public Transform MusicParent;

	[Header( "Assets" )]
	public GameObject VideoEntryPrefab;
	public GameObject MusicEntryPrefab;

	private Dictionary<int, GameObject> Videos = new Dictionary<int, GameObject>();
	private Dictionary<int, GameObject> Musics = new Dictionary<int, GameObject>();

	private Vector3 BasePos;

	private void Awake()
	{
		Instance = this;

		BasePos = transform.localPosition;

		// TODO TEMP
		Game.Instance.PopulateVideoMenu();
		Game.Instance.PopulateMusicMenu();
	}

	void Start()
    {

	}

    void Update()
    {
        if ( Input.GetKeyDown( KeyCode.Escape ) )
		{
			transform.localPosition = ( transform.localPosition.y == BasePos.y ) ? Vector3.one * 10000 : BasePos;
		}
    }

	public void AddVideo( int rawindex, string url, string name )
	{
		GameObject obj = Instantiate( VideoEntryPrefab, VideoParent );
		obj.GetComponentInChildren<DragOrderObject>().OnOrderChange.AddListener( OnVideoReorder );
		obj.name = rawindex.ToString();
		Videos.Add( rawindex, obj );

		obj.GetComponentInChildren<Text>().text = name;

		// TODO url for preview
		//obj.
	}

	public void OnVideoReorder()
	{
		foreach ( var entry in Videos )
		{
			var obj = entry.Value;
			Game.Instance.SetVideoOrder( entry.Key, obj.transform.GetSiblingIndex() );
		}

		// Reload with new video order at current time
		Game.Instance.SetTime( Game.Instance.CurrentTime );
	}

	public void AddMusic( int rawindex, AudioClip clip, string name )
	{
		GameObject obj = Instantiate( MusicEntryPrefab, MusicParent );
		obj.GetComponentInChildren<DragOrderObject>().OnOrderChange.AddListener( OnMusicReorder );
		obj.name = rawindex.ToString();
		Musics.Add( rawindex, obj );

		obj.GetComponentInChildren<Text>().text = name;

		// TODO url for preview
		//obj.
	}

	public void OnMusicReorder()
	{
		foreach ( var entry in Musics )
		{
			var obj = entry.Value;
			Game.Instance.SetMusicOrder( entry.Key, obj.transform.GetSiblingIndex() );
		}

		// Reload with new music order at current time
		Game.Instance.SetTime( Game.Instance.CurrentTime );
	}
}
