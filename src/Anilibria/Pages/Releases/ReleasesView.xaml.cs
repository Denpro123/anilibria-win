﻿using System;
using Anilibria.Services.Implementations;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Anilibria.Pages.Releases {

	/// <summary>
	/// Releases view.
	/// </summary>
	public sealed partial class ReleasesView : UserControl {

		public ReleasesView () {
			InitializeComponent ();

			DataContext = new ReleasesViewModel ( ApiService.Current () , StorageService.Current () , SyncService.Current () );

			Window.Current.CoreWindow.KeyUp += GlobalKeyUpHandler;
		}

		private void GlobalKeyUpHandler ( CoreWindow sender , KeyEventArgs args ) {
			if ( Visibility != Visibility.Visible ) return;

			if ( args.VirtualKey == VirtualKey.Escape ) Rectangle_Tapped ( null , null );
		}

		private void UserControl_Loaded ( object sender , RoutedEventArgs e ) {
			var dataContext = (ReleasesViewModel) DataContext;
			dataContext.Initialize ();
		}

		private void Rectangle_Tapped ( object sender , TappedRoutedEventArgs e ) {
			var dataContext = (ReleasesViewModel) DataContext;
			dataContext.HideReleaseCardCommand.Execute ( null );
		}

		private void PointingGridControl_Tapped ( object sender , TappedRoutedEventArgs e ) {
			var dataContext = (ReleasesViewModel) DataContext;
			dataContext.AddCardFavoriteCommand.Execute ( null );

		}

		private void RemoveFavoriteButton_Tapped ( object sender , TappedRoutedEventArgs e ) {
			var dataContext = (ReleasesViewModel) DataContext;
			dataContext.RemoveCardFavoriteCommand.Execute ( null );
		}

		private void ShowComments_Tapped ( object sender , TappedRoutedEventArgs e ) {
			var dataContext = (ReleasesViewModel) DataContext;
			dataContext.ShowCommentsCommand.Execute ( null );
		}
	}

}
