using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Game : MonoBehaviour
{
	public static Game Instance;

	public const float END_CARD_TIME = 7;
	public const float END_CARD_MUSIC_FADE_MULT = 3;
	public const float CLIP_LENGTH = 12;

	#region Struct/Enum Defines
	[Serializable]
	public struct Sharing
	{
		public string ClipURL;
		public string Credit;
		public float Length;
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

	[Header( "Assets" )]
	public AudioClip GenerativeMusic;
	public SharingMusic[] Musics;
	public EndCard[] EndCards;
	#endregion

	#region Variables - Private
	[HideInInspector]
	private List<Sharing> Sharings = new List<Sharing>();

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

	private bool Ended = false;
	#endregion

	#region MonoBehaviour
	private void Awake()
	{
		Instance = this;
	}

	void Start()
    {
		SetPlayState( PlayState.Playing );

		CreditBar.SetActive( true );
		MusicCreditBar.SetActive( true );
		EndCardCreditBar.SetActive( false );

		// TODO TEMP
		if ( Sharings.Count == 0 )
		{
			// johnjoemcbob NiallEM Wallmasterr DouglasFlinders Henwuar jctwizard shimmerwitch AllThingsTruly GhostTyrant _kaymay tomdemajo
			var root = "file:///E:/zOBS Video Recordings/biome sharings/21-06-18/";
			AddVideo( root + "john.mp4", "@johnjoemcbob" );
			AddVideo( root + "vrchat1.mp4", "@johnjoemcbob\n@GhostTyrant\n@DrMelon\n@leafcodes", 4 );
			AddVideo( root + "vrchat2.mp4", "@johnjoemcbob\n@GhostTyrant\n@DrMelon\n@leafcodes", 4 );
			AddVideo( root + "vrchat3.mp4", "@johnjoemcbob\n@GhostTyrant\n@DrMelon\n@leafcodes", 4 );
			AddVideo( root + "caspar.mp4", "@GhostTyrant" );
			AddVideo( root + "ally1.mp4", "@Wallmasterr", 4 );
			AddVideo( root + "ally2.mp4", "@Wallmasterr", 8 );
			AddVideo( root + "henry.mp4", "@Henwuar" );
			AddVideo( root + "niall.mp4", "@NiallEM" );
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

		UpdatePlayState( CurrentPlayState );
		UpdateMusic();

		if ( Input.GetKeyDown( KeyCode.Space ) )
		{
			CurrentVideo = TestVideoStart;
			CurrentMusic = 0;
			Start();
		}
    }
	#endregion

	#region States
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
				// Finished, play next clip or endcard
				double curtime = Players[CurrentFrontPlayer].time;
				int ind = VideoOrder[CurrentVideo];
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
				var img = Players[CurrentFrontPlayer].GetComponentInParent<RawImage>();
				float prog = CurrentPlayStateTime * CrossFadeSpeed;
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
		var ind = VideoOrder[CurrentVideo];
		Players[CurrentFrontPlayer].url = Sharings[ind].ClipURL;
		Players[CurrentFrontPlayer].Play();
		Players[CurrentFrontPlayer].audioOutputMode = VideoAudioOutputMode.None;

		SharingCredit.text = Sharings[ind].Credit;
		CurrentPlayStateTime = 0;
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

	public void AddVideo( string url, string credits, float length = -1 )
	{
		Sharing share = new Sharing();
		{
			share.ClipURL = url;
			share.Credit = credits;
			share.Length = length == -1 ? CLIP_LENGTH : length;
		}
		Sharings.Add( share );
		VideoOrder.Add( VideoOrder.Count );

		Editor.Instance.AddVideo( VideoOrder.Count - 1, share.ClipURL, share.Credit );
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
		switch ( CurrentMusicState )
		{
			case MusicState.Default:
				break;
			case MusicState.Playing:
				break;
			case MusicState.CrossFading:
				int other = CurrentFrontMusicPlayer == 0 ? 1 : 0;
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
		// Set correct seconds through video
		Players[CurrentFrontPlayer].time = remaining;

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
}
