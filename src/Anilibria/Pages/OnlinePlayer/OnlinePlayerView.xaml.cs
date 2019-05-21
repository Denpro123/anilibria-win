﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Anilibria.Services.Implementations;
using Anilibria.Services.PresentationClasses;
using Windows.Devices.Input;
using Windows.Gaming.Input;
using Windows.Media.Casting;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;

namespace Anilibria.Pages.OnlinePlayer {

	/// <summary>
	/// Online player.
	/// </summary>
	public sealed partial class OnlinePlayerView : Page {

		private int m_TapCount = 0;

		private OnlinePlayerViewModel m_ViewModel;

		private DispatcherTimer m_DispatherTimer;

		private DispatcherTimer m_GamepadTimer;

		private bool m_MediaOpened = false;

		private TimeSpan m_Duration = new TimeSpan ();

		private double m_MouseX = 0;

		private double m_MouseY = 0;

		private double m_PreviousX = 0;

		private double m_PreviousY = 0;

		private int m_LastActivityTime = 0;

		private int m_LastRestoreActivityTime = 0;

		private bool m_TransportControlsCaptured = false;

		private GamepadButtons m_PreviousStateButtons = new GamepadButtons ();

		CastingDevicePicker castingPicker;

		private bool m_isXbox = false;

		public OnlinePlayerView () {
			InitializeComponent ();
			m_ViewModel = new OnlinePlayerViewModel ( new AnalyticsService () , StorageService.Current () , ApiService.Current () ) {
				ChangeVolumeHandler = ChangeVolumeHandler ,
				ChangePlayback = ChangePlaybackHandler ,
				ChangePosition = ChangePosition ,
				ScrollToSelectedPlaylist = ScrollToSelectedItemInPlaylist ,
				SetVisiblePlaybackButtons = SetVisiblePlaybackButtons ,
				ChangeOpenPlaylistButton = ChangeOpenPlaylistButton
			};
			DataContext = m_ViewModel;
			OnlinePlayer.MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
			OnlinePlayer.MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
			OnlinePlayer.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
			OnlinePlayer.MediaPlayer.SourceChanged += MediaPlayer_SourceChanged;
			OnlinePlayer.MediaPlayer.BufferingStarted += MediaPlayer_BufferingStarted;
			OnlinePlayer.MediaPlayer.BufferingEnded += MediaPlayer_BufferingEnded;
			OnlinePlayer.MediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;
			OnlinePlayer.MediaPlayer.VolumeChanged += MediaPlayer_VolumeChanged;
			OnlinePlayer.TransportControls.IsFastForwardButtonVisible = true;
			OnlinePlayer.TransportControls.IsFastForwardEnabled = true;
			OnlinePlayer.TransportControls.IsFastRewindButtonVisible = true;
			OnlinePlayer.TransportControls.IsFastRewindEnabled = true;
			//OnlinePlayer.TransportControls.IsPlaybackRateButtonVisible = true;
			//OnlinePlayer.TransportControls.IsPlaybackRateEnabled = true;
			OnlinePlayer.TransportControls.IsSkipBackwardButtonVisible = true;
			OnlinePlayer.TransportControls.IsSkipBackwardEnabled = true;
			OnlinePlayer.TransportControls.IsSkipForwardButtonVisible = true;
			OnlinePlayer.TransportControls.IsSkipForwardEnabled = true;
			OnlinePlayer.TransportControls.IsZoomButtonVisible = true;
			OnlinePlayer.TransportControls.IsZoomEnabled = true;
			OnlinePlayer.TransportControls.IsFullWindowEnabled = false;
			OnlinePlayer.TransportControls.IsFullWindowButtonVisible = false;

			RunTimer ();

			Loaded += OnlinePlayerView_Loaded;
			Unloaded += OnlinePlayerView_Unloaded;

			if ( SystemService.GetDeviceFamilyType () != DeviceFamilyType.Xbox ) {
				castingPicker = new CastingDevicePicker ();
				castingPicker.Filter.SupportsVideo = true;
				castingPicker.Filter.SupportedCastingSources.Add ( OnlinePlayer.MediaPlayer.GetAsCastingSource () );
				castingPicker.CastingDeviceSelected += CastingPicker_CastingDeviceSelected;
			}
			else {
				m_GamepadTimer = new DispatcherTimer ();
				m_GamepadTimer.Tick += GamepadTimer_Tick;
				m_GamepadTimer.Start ();
				m_isXbox = true;
			}

			Window.Current.CoreWindow.KeyUp += GlobalKeyUpHandler;
			Window.Current.CoreWindow.PointerMoved += CoreWindow_PointerMoved;
		}

		private void SetVisiblePlaybackButtons ( bool visible ) {
			OnlinePlayer.TransportControls.IsFastForwardButtonVisible = visible;
			OnlinePlayer.TransportControls.IsFastRewindButtonVisible = visible;
			OnlinePlayer.TransportControls.IsSkipBackwardButtonVisible = visible;
			OnlinePlayer.TransportControls.IsSkipForwardButtonVisible = visible;
			OnlinePlayer.TransportControls.IsZoomButtonVisible = visible;
		}

		private async void MediaPlayer_VolumeChanged ( MediaPlayer sender , object args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.Volume = OnlinePlayer.MediaPlayer.Volume;
				}
			);
		}

		private async void MediaPlayer_CurrentStateChanged ( MediaPlayer sender , object args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.MediaStateChanged ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState );
				}
			);
		}

		private void CoreWindow_PointerMoved ( CoreWindow sender , PointerEventArgs args ) {
			if ( Visibility != Visibility.Visible ) return;

			if ( args.CurrentPoint.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse ) {
				m_MouseX = args.CurrentPoint.Position.X;
				m_MouseY = args.CurrentPoint.Position.Y;
			}
		}

		/// <summary>
		/// Gamepad timer tick handler.
		/// </summary>
		private void GamepadTimer_Tick ( object sender , object e ) {
			if ( Visibility == Visibility.Collapsed || m_ViewModel == null || m_ViewModel.SelectedRelease == null ) return;

			if ( Gamepad.Gamepads.Count == 0 ) return;

			var firstGamepad = Gamepad.Gamepads.First ();
			var gamepadState = firstGamepad.GetCurrentReading ();

			var previousStateButtons = m_PreviousStateButtons;
			m_PreviousStateButtons = gamepadState.Buttons;

			if ( previousStateButtons.HasFlag ( GamepadButtons.X ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.X ) ) {
				OnlinePlayer_Tapped ( null , null );
				return;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.Y ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.Y ) ) {
				m_ViewModel.ShowPlaylistButton = !m_ViewModel.ShowPlaylistButton;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.DPadRight ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.DPadRight ) ) {
				if ( m_ViewModel.SelectedOnlineVideo != null ) m_ViewModel.IsHD = !m_ViewModel.IsHD;
				return;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.DPadUp ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.DPadUp ) ) {
				m_ViewModel.ChangeVolumeCommand.Execute ( .1 );
				return;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.DPadDown ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.DPadDown ) ) {
				m_ViewModel.ChangeVolumeCommand.Execute ( -.1 );
				return;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.LeftShoulder ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.LeftShoulder ) ) {
				if ( m_ViewModel.SelectedRelease.CountVideoOnline > 1 ) {
					var index = m_ViewModel.SelectedRelease.OnlineVideos.ToList ().IndexOf ( m_ViewModel.SelectedOnlineVideo );
					if ( index < m_ViewModel.SelectedRelease.OnlineVideos.Count () - 1 ) m_ViewModel.SelectedOnlineVideo = m_ViewModel.SelectedRelease.OnlineVideos.ElementAt ( index + 1 );
				}
				return;
			}
			if ( previousStateButtons.HasFlag ( GamepadButtons.RightShoulder ) && !gamepadState.Buttons.HasFlag ( GamepadButtons.RightShoulder ) ) {
				if ( m_ViewModel.SelectedRelease.CountVideoOnline > 1 ) {
					var index = m_ViewModel.SelectedRelease.OnlineVideos.ToList ().IndexOf ( m_ViewModel.SelectedOnlineVideo );
					if ( index > 0 ) m_ViewModel.SelectedOnlineVideo = m_ViewModel.SelectedRelease.OnlineVideos.ElementAt ( index - 1 );
				}
				return;
			}
			//TODO: change selected releases
			//if ( gamepadState.LeftTrigger == 1 ) {

			//	return;
			//}
			//if ( gamepadState.RightTrigger == 1 ) {

			//	return;
			//}
		}


		private void GlobalKeyUpHandler ( CoreWindow sender , KeyEventArgs args ) {
			if ( Visibility != Visibility.Visible ) return;

			if ( args.VirtualKey == VirtualKey.Space ) {
				switch ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState ) {
					case MediaPlaybackState.Playing:
						OnlinePlayer.MediaPlayer.Pause ();
						break;
					case MediaPlaybackState.Paused:
						OnlinePlayer.MediaPlayer.Play ();
						break;
				}
			}
			if ( args.VirtualKey == VirtualKey.PageUp ) m_ViewModel.NextTrackCommand.Execute ( null );
			if ( args.VirtualKey == VirtualKey.PageDown ) m_ViewModel.PreviousTrackCommand.Execute ( null );
			if ( m_ViewModel.IsCompactOverlayEnabled ) return;

			if ( args.VirtualKey == VirtualKey.Escape ) {
				var view = ApplicationView.GetForCurrentView ();
				if ( view.IsFullScreenMode ) view.ExitFullScreenMode ();
			}
			if ( args.VirtualKey == VirtualKey.F11 || args.VirtualKey == VirtualKey.F ) m_ViewModel.ToggleFullScreenCommand.Execute ( null );
			if ( args.VirtualKey == VirtualKey.Up ) m_ViewModel.ChangeVolumeCommand.Execute ( .05 );
			if ( args.VirtualKey == VirtualKey.Down ) m_ViewModel.ChangeVolumeCommand.Execute ( -.05 );
			if ( args.VirtualKey == VirtualKey.M ) OnlinePlayer.MediaPlayer.IsMuted = !OnlinePlayer.MediaPlayer.IsMuted;
			if ( args.VirtualKey == VirtualKey.Home ) m_ViewModel.ShowPlaylistCommand.Execute ( null );
			if ( args.VirtualKey == VirtualKey.End ) m_ViewModel.ShowPlaylistButton = true;
		}

		private async void CastingPicker_CastingDeviceSelected ( CastingDevicePicker sender , CastingDeviceSelectedEventArgs args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				async () => {
					var connection = args.SelectedCastingDevice.CreateCastingConnection ();

					//Hook up the casting events
					//connection.ErrorOccurred += Connection_ErrorOccurred;
					//connection.StateChanged += Connection_StateChanged;

					var videoSource = OnlinePlayer.MediaPlayer.GetAsCastingSource ();
					await connection.RequestStartCastingAsync ( videoSource );
				}
			);
		}

		private async void MediaPlayer_BufferingEnded ( MediaPlayer sender , object args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.BufferingEnd ();
				}
			);
		}

		private async void MediaPlayer_BufferingStarted ( MediaPlayer sender , object args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.BufferingStart ();
				}
			);
		}

		private async void MediaPlayer_MediaEnded ( MediaPlayer sender , object args ) {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.MediaEnded ();
				}
			);
		}

		private async void MediaPlayer_SourceChanged ( MediaPlayer sender , object args ) {
			m_Duration = TimeSpan.FromSeconds ( 0 );
			m_MediaOpened = false;
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_ViewModel.MediaClosed ();
				}
			);
		}

		private void OnlinePlayerView_Unloaded ( object sender , RoutedEventArgs e ) {
			Unloaded -= OnlinePlayerView_Unloaded;

			StopTimer ();
		}

		private void OnlinePlayerView_Loaded ( object sender , RoutedEventArgs e ) {
			Loaded -= OnlinePlayerView_Loaded;

			RunTimer ();
		}

		private void RunTimer () {
			m_DispatherTimer = new DispatcherTimer ();
			m_DispatherTimer.Tick += TimerTick;
			m_DispatherTimer.Interval = new TimeSpan ( 300 );
			m_DispatherTimer.Start ();
		}

		private void Grid_PointerMoved ( object sender , PointerRoutedEventArgs e ) {
		}

		private void MouseHidingTracker () {
			var windowHeight = ( (Frame) Window.Current.Content ).ActualHeight;
			if ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing && windowHeight - m_MouseY > 110 && !m_TransportControlsCaptured ) {
				m_LastActivityTime++;
				if ( !( m_PreviousX == m_MouseX && m_PreviousY == m_MouseY ) ) {
					RestoreCursor ();
					m_LastActivityTime = 0;
					m_PreviousX = m_MouseX;
					m_PreviousY = m_MouseY;
				}

				if ( m_LastActivityTime == 100 ) {
					m_LastActivityTime = 0;
					Window.Current.CoreWindow.PointerCursor = null;
				}
			}
			else {
				RestoreCursor ();
			}
		}

		private void SaveRestoreState () {
			if ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing ) return;

			m_LastRestoreActivityTime++;
			if ( m_LastRestoreActivityTime < 1000 ) return;

			m_LastRestoreActivityTime = 0;

			m_ViewModel.SavePlayerRestoreState ();
		}

		private void RestoreCursor () {
			Window.Current.CoreWindow.PointerCursor = new CoreCursor ( CoreCursorType.Arrow , 0 );
		}

		private void TimerTick ( object sender , object e ) {
			if ( m_MediaOpened ) {
				m_ViewModel.RefreshPosition ( OnlinePlayer.MediaPlayer.PlaybackSession.Position );

				MouseHidingTracker ();
				SaveRestoreState ();
			}

			if ( m_TransportControlsCaptured ) return;
			if ( m_ControlMediaBorder != null && PlaylistGrid != null ) PlaylistGrid.Opacity = m_ControlMediaBorder.Opacity;
		}

		private void StopTimer () {
			if ( m_DispatherTimer.IsEnabled ) m_DispatherTimer.Stop ();
		}

		private void ChangePlaybackHandler ( PlaybackState state , bool needAnimation = true ) {
			switch ( state ) {
				case PlaybackState.Stop:
					if ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ) OnlinePlayer.MediaPlayer.Pause ();
					OnlinePlayer.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds ( 0 );
					break;
				case PlaybackState.Pause:
					if ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing ) OnlinePlayer.MediaPlayer.Pause ();
					break;
				case PlaybackState.Play:
					if ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.Playing ) OnlinePlayer.MediaPlayer.Play ();
					break;
				default: throw new NotSupportedException ( $"State {state} not supporting." );
			}
		}

		/// <summary>
		/// Change position.
		/// </summary>
		/// <param name="position"></param>
		private void ChangePosition ( TimeSpan position ) {
			var playbackState = OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState;
			if ( playbackState == MediaPlaybackState.Playing || playbackState == MediaPlaybackState.Paused ) {
				OnlinePlayer.MediaPlayer.PlaybackSession.Position = position;
			}
		}

		private void MediaPlayer_MediaFailed ( MediaPlayer sender , MediaPlayerFailedEventArgs args ) => m_MediaOpened = false;

		private async void MediaPlayer_MediaOpened ( MediaPlayer sender , object args ) {
			m_MediaOpened = true;
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					m_Duration = OnlinePlayer.MediaPlayer.PlaybackSession.NaturalDuration;
					m_ViewModel.MediaOpened ( m_Duration );
				}
			);
		}
		private void ChangeVolumeHandler ( double value ) => OnlinePlayer.MediaPlayer.Volume = value;

		private async void OnlinePlayer_Tapped ( object sender , TappedRoutedEventArgs e ) {
			if ( m_TransportControlsCaptured ) return;

			m_TapCount = 1;

			await Task.Delay ( 300 );

			if ( m_TapCount > 1 ) return;

			if ( !m_ViewModel.ShowPlaylistButton ) {
				m_ViewModel.ShowPlaylistButton = true;
				return;
			}

			var mediaPlayer = OnlinePlayer.MediaPlayer;

			switch ( OnlinePlayer.MediaPlayer.PlaybackSession.PlaybackState ) {
				case MediaPlaybackState.Playing:
					if ( mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing && mediaPlayer.PlaybackSession.CanPause ) {
						mediaPlayer.Pause ();
					}
					break;
				case MediaPlaybackState.Paused:
					if ( mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused ) {
						mediaPlayer.Play ();
					}
					break;
			}
		}

		private void RunHidePauseAnimation () {
			var hideStoryboard = Resources["HidePause"] as Storyboard;
			hideStoryboard.Begin ();
		}

		private void RunShowPauseAnimation () {
			var storyboard = Resources["ShowPause"] as Storyboard;
			storyboard.Begin ();
		}

		private void OnlinePlayer_DoubleTapped ( object sender , DoubleTappedRoutedEventArgs e ) {
			if ( m_TransportControlsCaptured ) return;

			m_TapCount++;

			if ( m_ViewModel.IsCompactOverlayEnabled ) return;

			m_ViewModel.ToggleFullScreenCommand.Execute ( null );
		}

		private void RootGrid_PointerEntered ( object sender , PointerRoutedEventArgs e ) {
			m_TransportControlsCaptured = true;
		}

		private void RootGrid_PointerExited ( object sender , PointerRoutedEventArgs e ) {
			m_TransportControlsCaptured = false;
		}

		private Border m_ControlMediaBorder;

		private void ControlPanel_ControlPanelVisibilityStates_Border_Loaded ( object sender , RoutedEventArgs e ) {
			m_ControlMediaBorder = sender as Border;
		}

		private async void ScrollToSelectedItemInPlaylist () {
			await Dispatcher.RunAsync (
				CoreDispatcherPriority.Normal ,
				() => {
					if ( m_ViewModel.SelectedOnlineVideo == null ) return;

					PlaylistListView.ScrollIntoView ( m_ViewModel.SelectedOnlineVideo );
				}
			);
		}

		private void PlaylistListView_PointerEntered ( object sender , PointerRoutedEventArgs e ) {
			m_TransportControlsCaptured = true;
		}

		private void PlaylistListView_PointerExited ( object sender , PointerRoutedEventArgs e ) {
			m_TransportControlsCaptured = false;
		}

		private async void ComboBox_DataContextChanged ( FrameworkElement sender , DataContextChangedEventArgs args ) {
			var oldPosition = m_ViewModel.SelectedPlaylistButtonPosition;
			await Task.Delay ( 500 );

			if ( m_ViewModel.SelectedPlaylistButtonPosition == null ) m_ViewModel.SelectedPlaylistButtonPosition = oldPosition;
		}

		private void ChangeOpenPlaylistButton () {
			if ( m_ViewModel.SelectedPlaylistButtonPosition == null ) return;
			switch ( m_ViewModel.SelectedPlaylistButtonPosition.Position ) {
				case PresentationClasses.PlaylistButtonPosition.Center:
					OpenPlaylistButton.VerticalAlignment = VerticalAlignment.Center;
					OpenPlaylistButton.Margin = new Thickness ( 0 , 0 , 0 , 0 );
					break;
				case PresentationClasses.PlaylistButtonPosition.Top:
					OpenPlaylistButton.VerticalAlignment = VerticalAlignment.Top;
					OpenPlaylistButton.Margin = new Thickness ( 0 , 0 , 0 , 0 );
					break;
				case PresentationClasses.PlaylistButtonPosition.Bottom:
					OpenPlaylistButton.VerticalAlignment = VerticalAlignment.Bottom;
					OpenPlaylistButton.Margin = new Thickness ( 0 , 0 , 0 , m_isXbox ? 30 : 0 );
					break;
			}
		}

	}

}
