using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Game : MonoBehaviour
{
	public static Game Instance;

	#region Variables - Constants
	public const float END_CARD_TIME = 7;
	public const float END_CARD_MUSIC_FADE_MULT = 3;
	public const float CLIP_LENGTH = 12;
	#endregion

	#region Struct/Enum Defines
	[Serializable]
	public enum ShareType
	{
		Video,
		AudioVisualiser,
		Image,
	}
	[Serializable]
	public struct Sharing
	{
		public ShareType Type;
		public string ClipURL;
		public string Credit;
		public float Length;
		public float Speed;
		public float StartTime;
	}
	[Serializable]
	public struct SharingMusic
	{
		public AudioClip Clip;
		public string Credit;
	}
	[Serializable]
	public struct EndCard
	{
		public string ClipURL;
		public string Credit;
	}
	//file:///E:/Projects/BiomeSharingVideo/Assets/BiomeSharingVideo/Videos/Endcards/04_tom.mp4

	public enum State
	{
		Menu,
		Playing,
		AddVideo,
		AddMusic,
		EditVideo,
	}

	public enum PlayState
	{
		Playing,
		CrossFading,
		Endcard,
	}

	public enum MusicState
	{
		Default,
		Playing,
		CrossFading,
	}
	#endregion

	#region Variables - Inspector
	[Header( "Variables" )]
	public int CurrentEndCard = 0;
	public float CrossFadeSpeed = 2;
	public int TestVideoStart = 0;

	[Header( "References" )]
	public VideoPlayer[] Players;
	public AudioSource[] MusicSources;
	public Text SharingCredit;
	public Text MusicCredit;
	public Text EndCardCredit;
	public GameObject CreditBar;
	public GameObject MusicCreditBar;
	public GameObject EndCardCreditBar;
	public GameObject AddVideoPanel;
	public GameObject AddMusicPanel;
	public GameObject EditVideoPanel;

	[Header( "Assets" )]
	public AudioClip GenerativeMusic;
	public SharingMusic[] Musics;
	public EndCard[] EndCards;
	public RenderTexture Video1RenderTexture;
	public RenderTexture Video2RenderTexture;
	public RenderTexture AudioVisualiserRenderTexture;
	#endregion

	#region Variables - Private
	[HideInInspector]
	private List<Sharing> Sharings = new List<Sharing>();

	private State CurrentState;
	private PlayState CurrentPlayState;
	private MusicState CurrentMusicState;

	private int CurrentVideo = 0;
	private int CurrentMusic = 0;

	private int CurrentFrontPlayer = 0;
	private int CurrentFrontMusicPlayer = 0;

	private List<int> VideoOrder = new List<int>();
	private int[] MusicOrder;

	[HideInInspector]
	public float CurrentTime = 0;
	private float CurrentPlayStateTime = 0;
	private float CurrentMusicStateTime = 0;
	private float NextMusicPlay = -1;

	[HideInInspector]
	public double VideoLength = 0;

	private bool Paused = false;
	private bool LastPaused = false;

	private bool Ended = false;
	#endregion

	#region MonoBehaviour
	private void Awake()
	{
		Instance = this;
	}

	void Start()
    {
		SetState( State.Menu );
		SetPlayState( PlayState.Playing );

		CreditBar.SetActive( true );
		MusicCreditBar.SetActive( true );
		EndCardCreditBar.SetActive( false );

		// TODO TEMP
		if ( Sharings.Count == 0 )
		{
			// johnjoemcbob Wallmasterr NiallEM jctwizard Henwuar DouglasFlinders
			// shimmerwitch AllThingsTruly _kaymay
			// susepicious tomdemajo GhostTyrant psalmlab
			var root = "file:///E:/zOBS Video Recordings/biome sharings/21-11-19/";
			AddVideo( root + "smart1.mp4", "@psalmlab", 6 );
			AddVideo( root + "smart2.mp4", "@psalmlab", 6 );
			AddVideo( root + "john1.mp4", "@johnjoemcbob", 6, 3, 120 );
			AddVideo( root + "niall1.mp4", "@NiallEM", 6, 1, 30 );
			AddAudioVisualiser( "@Henwuar", 6 );
			AddVideo( root + "john2.mp4", "@johnjoemcbob", 6, 2, 35 );
			AddVideo( root + "niall2.mp4", "@NiallEM", 6, 8, 20 );
			AddVideo( root + "james1.mp4", "@jctwizard", 6 );
			AddVideo( root + "james2.mp4", "@jctwizard", 6 );
			AddVideo( root + "caspar.mp4", "@GhostTyrant", 12 );
			AddAudioVisualiser( "@Henwuar", 6 );
			AddVideo( root + "tom.mp4", "@tomdemajo", 12 );
		}

		CurrentVideo = TestVideoStart;
		PlayCurrentClip();
		//CurrentVideo = -1;

		// Reset transparencies
		Players[CurrentFrontPlayer == 1 ? 0 : 1].GetComponentInParent<RawImage>().color = Color.clear;
		Players[CurrentFrontPlayer].GetComponentInParent<RawImage>().color = Color.white;

		// Reset audio levels
		MusicSources[0].volume = 1;
		MusicSources[1].volume = 1;

		// Calculate video length
		VideoLength = 0;
		foreach ( var share in Sharings )
		{
			VideoLength += share.Length; // TODO Calculate by divisor of exact credit occurances
		}

		CurrentTime = 0;

		StartMusic();
	}

    void Update()
    {
		CurrentTime += Time.deltaTime;

		UpdateState( CurrentState );
		UpdateMusic();

		if ( Input.GetKeyDown( KeyCode.Space ) )
		{
			CurrentVideo = TestVideoStart;
			CurrentMusic = 0;
			Start();
		}
    }
	#endregion

	#region Buttons
	public void ButtonAddVideo()
	{
		if ( CurrentState == State.Playing )
		{
			SwitchState( State.AddVideo );
		}
	}

	public void ButtonAddMusic()
	{
		if ( CurrentState == State.Playing )
		{
			SwitchState( State.AddMusic );
		}
	}
	#endregion

	#region States
	public void SwitchState( State state )
	{
		FinishState( CurrentState );
		SetState( state );
	}

	void SetState( State state )
	{
		CurrentState = state;
		StartState( CurrentState );
	}

	void StartState( State state )
	{
		AddVideoPanel.SetActive( false );
		AddMusicPanel.SetActive( false );
		EditVideoPanel.SetActive( false );

		switch ( state )
		{
			case State.Menu:
				// TODO TEMP REMOVE
				SwitchState( State.Playing );
				break;
			case State.Playing:
				TogglePaused( LastPaused );
				break;
			case State.AddVideo:
				AddVideoPanel.SetActive( true );
				break;
			case State.AddMusic:
				AddMusicPanel.SetActive( true );
				break;
			case State.EditVideo:
				EditVideoPanel.SetActive( true );
				break;
			default:
				break;
		}
	}

	void UpdateState( State state )
	{
		switch ( state )
		{
			case State.Menu:
				break;
			case State.Playing:
				UpdatePlayState( CurrentPlayState );
				break;
			case State.AddVideo:
				break;
			case State.AddMusic:
				break;
			case State.EditVideo:
				break;
			default:
				break;
		}
	}

	void FinishState( State state )
	{
		switch ( state )
		{
			case State.Menu:
				break;
			case State.Playing:
				LastPaused = Paused;
				TogglePaused( true );
				break;
			case State.AddVideo:
				break;
			case State.AddMusic:
				break;
			case State.EditVideo:
				break;
			default:
				break;
		}
	}
	#endregion

	#region Video Play States
	public void SwitchPlayState( PlayState newstate )
	{
		FinishPlayState( CurrentPlayState );
		SetPlayState( newstate );
	}

	void SetPlayState( PlayState newstate )
	{
		CurrentPlayState = newstate;
		CurrentPlayStateTime = 0;
		StartPlayState( CurrentPlayState );
	}

	void StartPlayState( PlayState state )
	{
		switch ( state )
		{
			case PlayState.Playing:
				break;
			case PlayState.CrossFading:
				break;
			case PlayState.Endcard:
				// Endcard
				CreditBar.SetActive( false );
				MusicCreditBar.SetActive( false );
				EndCardCreditBar.SetActive( true );
				EndCardCredit.text = EndCards[CurrentEndCard].Credit;

				SwitchPlayer();
				Players[CurrentFrontPlayer].GetComponentInParent<RawImage>().texture = Players[CurrentFrontPlayer].targetTexture; // Return to video player
				Players[CurrentFrontPlayer].url = EndCards[CurrentEndCard].ClipURL; // TODO randomise

				Players[CurrentFrontPlayer].audioOutputMode = VideoAudioOutputMode.AudioSource;
				var src = Players[CurrentFrontPlayer].gameObject.GetComponent<AudioSource>();
				Players[CurrentFrontPlayer].SetTargetAudioSource( 0, src );

				Players[CurrentFrontPlayer].Play();

				SwitchPlayState( PlayState.CrossFading );

				break;
			default:
				break;
		}
	}

	void UpdatePlayState( PlayState state )
	{
		CurrentPlayStateTime += Time.deltaTime;
		switch ( state )
		{
			case PlayState.Playing:
				int ind = VideoOrder[CurrentVideo];
				// Finished, play next clip or endcard
				double curtime = CurrentPlayStateTime;
				double endtime = Sharings[ind].Length - ( 1.0f / CrossFadeSpeed / 2 );

				if ( curtime >= endtime && CurrentPlayStateTime > 0.1f )
				{
					CurrentVideo++;
					if ( CurrentVideo < Sharings.Count )
					{
						SwitchPlayer();
						PlayCurrentClip();

						SwitchPlayState( PlayState.CrossFading );
					}
					else
					{
						SwitchPlayState( PlayState.Endcard );
						Ended = true;
					}
				}
				break;
			case PlayState.CrossFading:
				float prog = CurrentPlayStateTime * CrossFadeSpeed;

				// Get image to fade based on sharing type
				// TODO need multiple ShareImageRawImage to use that properly!!
				RawImage img = Players[CurrentFrontPlayer].GetComponentInParent<RawImage>();
				img.color = Color.Lerp( Color.clear, Color.white, prog );

				if ( prog >= 1 && !Ended )
				{
					SwitchPlayState( PlayState.Playing );
				}

				if ( Ended )
				{
					// Lerp music track volume out
					MusicSources[CurrentFrontMusicPlayer].volume = Mathf.Lerp( 1, 0, CurrentPlayStateTime / END_CARD_TIME * END_CARD_MUSIC_FADE_MULT );

					// Lerp the end card credits in/out
					float progress = CurrentPlayStateTime / END_CARD_TIME;
					float a = 0;
					float start = 0.5f;
					float duration = 0.25f;
					if ( progress >= start )
					{
						a = ( progress - start ) / duration;
					}
					if ( progress >= start + duration )
					{
						a = 1 - ( ( progress - start - duration ) / duration );
					}
					EndCardCredit.color = new Color( 1, 1, 1, a );
					if ( CurrentEndCard == 4 ) // Ally background white, hardcode for now TODO
					{
						EndCardCredit.color = new Color( 0.1f, 0.1f, 0.1f, a );
					}
				}

				break;
			case PlayState.Endcard:
				break;
			default:
				break;
		}
	}

	void FinishPlayState( PlayState state )
	{
		switch ( state )
		{
			case PlayState.Playing:
				break;
			case PlayState.CrossFading:
				break;
			case PlayState.Endcard:
				break;
			default:
				break;
		}
	}
	#endregion

	#region Video
	void PlayCurrentClip()
	{
		// Reset to video player
		Texture tex = Players[CurrentFrontPlayer].targetTexture;

		var ind = VideoOrder[CurrentVideo];
		switch ( Sharings[ind].Type )
		{
			case ShareType.Video:
				Players[CurrentFrontPlayer].url = Sharings[ind].ClipURL;
				Players[CurrentFrontPlayer].playbackSpeed = Sharings[ind].Speed;
				Players[CurrentFrontPlayer].Play();
				StartCoroutine( WaitForPlayerPrepared( Players[CurrentFrontPlayer], Sharings[ind].StartTime ) );
				Players[CurrentFrontPlayer].audioOutputMode = VideoAudioOutputMode.None;

				break;
			case ShareType.AudioVisualiser:
				Players[CurrentFrontPlayer].playbackSpeed = 1; // Set to normal speed for other logic safety
				tex = AudioVisualiserRenderTexture;

				break;
			case ShareType.Image:
				break;
			default:
				break;
		}

		// Apply the correct texture
		Players[CurrentFrontPlayer].GetComponentInParent<RawImage>().texture = tex;

		SharingCredit.text = Sharings[ind].Credit;
		CurrentPlayStateTime = 0;
	}

	IEnumerator WaitForPlayerPrepared( VideoPlayer player, float time = 0 )
	{
		while ( !player.canSetTime || !player.isPrepared || player.frame == -1 )
		{
			yield return new WaitForEndOfFrame();
		}

		Debug.Log( "prepped, set time; " + time );
		var ind = VideoOrder[CurrentVideo];
		player.time = time;
		//player.frame = (long) ( time * player.frameRate );

		Debug.Log( player.time );
		Debug.Log( player.frame );

		//yield return new WaitForSeconds( 1 );

		//Debug.Log( "set" );
		//Debug.Log( time );
		//Debug.Log( player.time );
		//player.time = time;
		//Debug.Log( player.time );
		//Debug.Log( player.frame );

		yield break;
	}

	void SwitchPlayer()
	{
		Players[CurrentFrontPlayer].transform.localPosition = new Vector3( 0, 0, -1 );
		Players[CurrentFrontPlayer].audioOutputMode = VideoAudioOutputMode.None;

		CurrentFrontPlayer = CurrentFrontPlayer == 0 ? 1 : 0;
		Players[CurrentFrontPlayer].transform.localPosition = new Vector3( 0, 0, 0 );
		Players[CurrentFrontPlayer].transform.parent.SetAsLastSibling();
		Players[CurrentFrontPlayer].audioOutputMode = VideoAudioOutputMode.None;

		// Make transparent when move to front
		Players[CurrentFrontPlayer].GetComponentInParent<RawImage>().color = Color.clear;
	}

	public void SetVideoOrder( int raw, int current )
	{
		VideoOrder[current] = raw;
	}
	#endregion

	#region Add Visual Element
	public void AddVideo( string url, string credits, float length = -1, float speed = 1, float starttime = 0 )
	{
		Sharing share = new Sharing();
		{
			share.Type = ShareType.Video;
			share.ClipURL = url;
			share.Credit = credits;
			share.Length = length == -1 ? CLIP_LENGTH : length;
			share.Speed = speed;
			share.StartTime = starttime;
		}
		Sharings.Add( share );
		VideoOrder.Add( VideoOrder.Count );

		Editor.Instance.AddVideo( VideoOrder.Count - 1, share.ClipURL, share.Credit );
	}

	public void AddAudioVisualiser( string credits, float length = -1 )
	{
		Sharing share = new Sharing();
		{
			share.Type = ShareType.AudioVisualiser;
			share.Credit = credits;
			share.Length = length == -1 ? CLIP_LENGTH : length;
		}
		Sharings.Add( share );
		VideoOrder.Add( VideoOrder.Count );

		Editor.Instance.AddVideo( VideoOrder.Count - 1, "Audio Visualiser", share.Credit );
	}
	#endregion

	#region Music
	void SwitchMusicState( MusicState state )
	{
		CurrentMusicState = state;
		CurrentMusicStateTime = 0;
	}

	void StartMusic()
	{
		if ( Musics.Length == 0 )
		{
			// If none defined, play default and hide credits
			MusicCreditBar.SetActive( false );

			NextMusicPlay = -1;

			float remaining = CurrentTime % GenerativeMusic.length;
			MusicSources[CurrentFrontMusicPlayer].clip = GenerativeMusic;
			MusicSources[CurrentFrontMusicPlayer].Play();
			MusicSources[CurrentFrontMusicPlayer].time = remaining;
		}
		else
		{
			NextMusicPlay = (float) VideoLength / Musics.Length;

			CurrentMusic = 0;
			float remaining = CurrentTime;
			while ( remaining >= NextMusicPlay )
			{
				remaining -= NextMusicPlay;
				if ( CurrentMusic + 1 < Musics.Length )
				{
					CurrentMusic++;
				}
			}
			NextMusicPlay += CurrentMusic * (float) VideoLength / Musics.Length;

			PlayCurrentMusic();
			MusicSources[CurrentFrontMusicPlayer].time = remaining;

			SwitchMusicState( MusicState.Playing );
		}
	}

	void UpdateMusic()
	{
		if ( Ended ) return;

		CurrentMusicStateTime += Time.deltaTime;

		if ( NextMusicPlay != -1 && CurrentTime >= NextMusicPlay )
		{
			CurrentMusic++;
			SwitchMusicPlayer();
			PlayCurrentMusic();
			SwitchMusicState( MusicState.CrossFading );

			if ( CurrentMusic + 1 < Musics.Length )
			{
				NextMusicPlay += (float) VideoLength / Musics.Length;
			}
			else
			{
				NextMusicPlay = -1;
			}
		}

		// In case of multiple tracks, cross fade here
		int other = CurrentFrontMusicPlayer == 0 ? 1 : 0;
		switch ( CurrentMusicState )
		{
			case MusicState.Default:
				break;
			case MusicState.Playing:
				// Ensure its completely silent after crossfading
				MusicSources[other].volume = 0;

				break;
			case MusicState.CrossFading:
				MusicSources[CurrentFrontMusicPlayer].volume = Mathf.Lerp( 0, 1, CurrentMusicStateTime );
				MusicSources[other].volume = Mathf.Lerp( 1, 0, CurrentMusicStateTime );

				if ( CurrentMusicStateTime >= 1 )
				{
					SwitchMusicState( MusicState.Playing );
				}

				break;
			default:
				break;
		}
	}

	void PlayCurrentMusic()
	{
		// Clamp
		if ( CurrentMusic >= Musics.Length )
		{
			CurrentMusic = Musics.Length - 1;
		}

		int ind = MusicOrder[CurrentMusic];
		MusicSources[CurrentFrontMusicPlayer].clip = Musics[ind].Clip;
		MusicSources[CurrentFrontMusicPlayer].Play();

		MusicCreditBar.SetActive( true );
		MusicCredit.text = Musics[ind].Credit;
	}

	void SwitchMusicPlayer()
	{
		CurrentFrontMusicPlayer = CurrentFrontMusicPlayer == 0 ? 1 : 0;

		MusicSources[CurrentFrontMusicPlayer].transform.parent.SetAsLastSibling();

		MusicSources[CurrentFrontMusicPlayer].volume = 0;
	}

	public void SetMusicOrder( int raw, int current )
	{
		MusicOrder[current] = raw;
	}

	public void PopulateMusicMenu()
	{
		// Initial order (TODO move to when videos are added manually)
		MusicOrder = new int[Musics.Length];
		for ( int i = 0; i < Musics.Length; i++ )
		{
			MusicOrder[i] = i;
		}

		// TODO TEMP REMOVE
		int index = 0;
		foreach ( var music in Musics )
		{
			Editor.Instance.AddMusic( index, music.Clip, music.Credit );
			index++;
		}
	}

	public AudioSource GetCurrentAudioSource()
	{
		return MusicSources[CurrentFrontMusicPlayer];
	}
	#endregion

	#region Time
	public void SetTime( float time )
	{
		CurrentVideo = TestVideoStart;
		CurrentMusic = 0;
		Start();

		CurrentTime = time;

		// Get correct video at that time
		float remaining = CurrentTime;
		for ( int share = 0; share < Sharings.Count; share++ )
		{
			var ind = VideoOrder[share];
			if ( Sharings[ind].Length <= remaining )
			{
				remaining -= (float) Sharings[ind].Length;
			}
			else
			{
				CurrentVideo = share;
				break;
			}
		}
		// Play it
		PlayCurrentClip();

		var curr = VideoOrder[CurrentVideo];
		switch ( Sharings[curr].Type )
		{
			case ShareType.Video:
				// Set correct seconds through video
				StartCoroutine( WaitForPlayerPrepared( Players[CurrentFrontPlayer], Sharings[curr].StartTime + remaining * Sharings[curr].Speed ) );

				break;
			case ShareType.AudioVisualiser:

				break;
			case ShareType.Image:
				break;
			default:
				break;
		}

		// Get correct music, play
		StartMusic();

		// If at end, custom logic
		float timeleft = CurrentTime - (float) VideoLength;
		if ( timeleft >= 0 )
		{
			SwitchPlayState( PlayState.Endcard );
			Ended = true;
			Players[CurrentFrontPlayer].time = timeleft;

			CurrentPlayStateTime = timeleft;
		}
	}
	#endregion

	#region Pause
	public void TogglePaused( bool pause )
	{
		Paused = pause;

		if ( Paused )
		{
			Players[CurrentFrontPlayer].Pause();
			MusicSources[CurrentFrontMusicPlayer].Pause();
		}
		else
		{
			Players[CurrentFrontPlayer].Play();
			MusicSources[CurrentFrontMusicPlayer].Play();
		}
	}
	#endregion
}
