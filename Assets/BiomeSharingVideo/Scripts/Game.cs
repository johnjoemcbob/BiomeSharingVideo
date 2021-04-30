using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Game : MonoBehaviour
{
	public static Game Instance;

	public const float END_CARD_TIME = 6;

	#region Struct/Enum Defines
	[Serializable]
	public struct Sharing
	{
		public VideoClip Clip;
		public string Credit;
	}
	[Serializable]
	public struct SharingMusic
	{
		public AudioClip Clip;
		public string Credit;
	}

	public enum State
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
	public float CrossFadeSpeed = 2;
	public int TestVideoStart = 0;

	[Header( "References" )]
	public VideoPlayer[] Players;
	public AudioSource[] MusicSources;
	public Text SharingCredit;
	public Text MusicCredit;
	public GameObject CreditBar;
	public GameObject MusicCreditBar;

	[Header( "Assets" )]
	public Sharing[] Sharings;
	public SharingMusic[] Musics;
	public VideoClip[] EndCards;
	#endregion

	#region Variables - Private
	private State CurrentState;
	private MusicState CurrentMusicState;

	private int CurrentVideo = 0;
	private int CurrentMusic = 0;

	private int CurrentFrontPlayer = 0;
	private int CurrentFrontMusicPlayer = 0;

	private int[] VideoOrder;
	private int[] MusicOrder;

	[HideInInspector]
	public float CurrentTime = 0;
	private float CurrentStateTime = 0;
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
		SetState( State.Playing );

		AddVideo( "E:/Projects/BiomeSharingVideo/Assets/BiomeSharingVideo/Videos/ally_cross.mp4" );

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
			VideoLength += share.Clip.length;
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

	#region States
	public void SwitchState( State newstate )
	{
		FinishState( CurrentState );
		SetState( newstate );
	}

	void SetState( State newstate )
	{
		CurrentState = newstate;
		CurrentStateTime = 0;
		StartState( CurrentState );
	}

	void StartState( State state )
	{
		switch ( state )
		{
			case State.Playing:
				break;
			case State.CrossFading:
				break;
			case State.Endcard:
				// Endcard
				CreditBar.SetActive( false );
				MusicCreditBar.SetActive( false );

				SwitchPlayer();
				Players[CurrentFrontPlayer].clip = EndCards[0]; // TODO randomise
				Players[CurrentFrontPlayer].Play();

				SwitchState( State.CrossFading );

				break;
			default:
				break;
		}
	}

	void UpdateState( State state )
	{
		CurrentStateTime += Time.deltaTime;

		switch ( state )
		{
			case State.Playing:
				// Finished, play next clip or endcard
				double curtime = Players[CurrentFrontPlayer].time;
				double endtime = Players[CurrentFrontPlayer].clip.length - ( 1.0f / CrossFadeSpeed / 2 );

				if ( curtime >= endtime && CurrentStateTime > 0.1f )
				{
					CurrentVideo++;
					if ( CurrentVideo < Sharings.Length )
					{
						SwitchPlayer();
						PlayCurrentClip();

						SwitchState( State.CrossFading );
					}
					else
					{
						SwitchState( State.Endcard );
						Ended = true;
					}
				}
				break;
			case State.CrossFading:
				var img = Players[CurrentFrontPlayer].GetComponentInParent<RawImage>();
				float prog = CurrentStateTime * CrossFadeSpeed;
				img.color = Color.Lerp( Color.clear, Color.white, prog );

				if ( prog >= 1 && !Ended )
				{
					SwitchState( State.Playing );
				}
				
				if ( Ended )
				{
					MusicSources[CurrentFrontMusicPlayer].volume = Mathf.Lerp( 1, 0, CurrentStateTime / END_CARD_TIME );
				}

				break;
			case State.Endcard:
				break;
			default:
				break;
		}
	}

	void FinishState( State state )
	{
		switch ( state )
		{
			case State.Playing:
				break;
			case State.CrossFading:
				break;
			case State.Endcard:
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
		Players[CurrentFrontPlayer].clip = Sharings[ind].Clip;
		Players[CurrentFrontPlayer].Play();

		SharingCredit.text = Sharings[ind].Credit;
		CurrentStateTime = 0;
	}

	void SwitchPlayer()
	{
		Players[CurrentFrontPlayer].transform.localPosition = new Vector3( 0, 0, -1 );

		CurrentFrontPlayer = CurrentFrontPlayer == 0 ? 1 : 0;
		Players[CurrentFrontPlayer].transform.localPosition = new Vector3( 0, 0, 0 );
		Players[CurrentFrontPlayer].transform.parent.SetAsLastSibling();

		// Make transparent when move to front
		Players[CurrentFrontPlayer].GetComponentInParent<RawImage>().color = Color.clear;
	}

	public void SetVideoOrder( int raw, int current )
	{
		VideoOrder[current] = raw;
	}

	public void PopulateVideoMenu()
	{
		// Initial order (TODO move to when videos are added manually)
		VideoOrder = new int[Sharings.Length];
		for ( int i = 0; i < Sharings.Length; i++ )
		{
			VideoOrder[i] = i;
		}

		// TODO TEMP REMOVE
		int index = 0;
		foreach ( var video in Sharings )
		{
			Editor.Instance.AddVideo( index, "", video.Credit ); // TODO add url
			index++;
		}
	}

	public void AddVideo( string url )
	{

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
		if ( false ) // TODO
		{
			// If none defined, play default and hide credits
			MusicCreditBar.SetActive( false );

			NextMusicPlay = -1;
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
		for ( int share = 0; share < Sharings.Length; share++ )
		{
			var ind = VideoOrder[share];
			if ( Sharings[ind].Clip.length <= remaining )
			{
				remaining -= (float) Sharings[ind].Clip.length;
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
			SwitchState( State.Endcard );
			Ended = true;
			Players[CurrentFrontPlayer].time = timeleft;

			CurrentStateTime = timeleft;
		}
	}
	#endregion
}
