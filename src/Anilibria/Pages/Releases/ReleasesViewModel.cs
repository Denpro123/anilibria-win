﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Anilibria.Collections;
using Anilibria.MVVM;
using Anilibria.Pages.Releases.PresentationClasses;
using Anilibria.Services;

namespace Anilibria.Pages.Releases {

	/// <summary>
	/// Release view model.
	/// </summary>
	public class ReleasesViewModel : ViewModel {

		private bool m_IsMultipleSelect;

		private IncrementalLoadingCollection<ReleaseModel> m_Collection;

		private ObservableCollection<ReleaseModel> m_SelectedReleases;

		private ReleaseModel m_OpenedRelease;

		private bool m_IsShowReleaseCard;

		private readonly IAnilibriaApiService m_AnilibriaApiService;		

		/// <summary>
		/// Constructor injection.
		/// </summary>
		/// <param name="anilibriaApiService">Anilibria Api Service.</param>
		public ReleasesViewModel ( IAnilibriaApiService anilibriaApiService ) {
			m_AnilibriaApiService = anilibriaApiService ?? throw new ArgumentNullException ( nameof ( anilibriaApiService ) );

			CreateCommands ();
			RefreshSelectedReleases ();
		}

		private void CreateCommands () {
			ShowSidebarCommand = CreateCommand ( ToggleSidebar );
		}

		private void ToggleSidebar () {
			ShowSidebar?.Invoke ();
		}

		private void RefreshSelectedReleases () {
			m_SelectedReleases = new ObservableCollection<ReleaseModel> ();
			m_SelectedReleases.CollectionChanged += SelectedReleasesChanged;
		}

		private void SelectedReleasesChanged ( object sender , NotifyCollectionChangedEventArgs e ) {
			RaiseCommands ();

			if ( !IsMultipleSelect && SelectedReleases.Count == 1 ) {
				OpenedRelease = SelectedReleases.First ();
				IsShowReleaseCard = true;
			}
		}

		/// <summary>
		/// Refresh groups.
		/// </summary>
		private void RefreshGroups () {
			m_Collection = new IncrementalLoadingCollection<ReleaseModel> {
				PageSize = 20 ,
				GetPageFunction = GetItemsPageAsync
			};
			RaisePropertyChanged ( () => Collection );

			//RaiseSelectableCommands ();
		}

		/// <summary>
		/// Get items page.
		/// </summary>
		/// <param name="page">Page.</param>
		/// <param name="pageSize">Page size.</param>
		/// <returns>Items on current page.</returns>
		private async Task<IEnumerable<ReleaseModel>> GetItemsPageAsync ( int page , int pageSize ) {
			//TODO: network error handling
			var releases = await m_AnilibriaApiService.GetPage ( page , pageSize );

			return releases.Select (
				a => new ReleaseModel {
					Id = a.Id ,
					AddToFavorite = a.Favorite?.Added ?? false ,
					Code = a.Code ,
					Description = a.Description ,
					Genres = string.Join ( ", " , a.Genres ) ,
					Title = a.Names.FirstOrDefault () ,
					Names = a.Names ,
					Poster = m_AnilibriaApiService.GetUrl ( a.Poster.Replace ( "default" , a.Id.ToString () ) ) ,
					//PosterFull = m_AnilibriaApiService.GetUrl ( a.PosterFull.Replace ( "default" , a.Id.ToString () ) ) ,
					Rating = a.Favorite?.Rating ?? 0 ,
					Series = a.Series ,
					Status = a.Status ,
					Type = a.Type ,
					Voices = string.Join ( ", " , a.Voices ) ,
					Year = a.Year
				}
			);
		}

		/// <summary>
		/// Initialize view model.
		/// </summary>
		public void Initialize () {
			RefreshGroups ();
		}

		/// <summary>
		/// Collection.
		/// </summary>
		public IncrementalLoadingCollection<ReleaseModel> Collection
		{
			get => m_Collection;
			set => Set ( ref m_Collection , value );
		}

		/// <summary>
		/// Is multiple select.
		/// </summary>
		public bool IsMultipleSelect
		{
			get => m_IsMultipleSelect;
			set => Set ( ref m_IsMultipleSelect , value );
		}

		/// <summary>
		/// Release for show in Release Card.
		/// </summary>
		public ReleaseModel OpenedRelease
		{
			get => m_OpenedRelease;
			set => Set ( ref m_OpenedRelease , value );
		}

		/// <summary>
		/// Release for show in Release Card.
		/// </summary>
		public bool IsShowReleaseCard
		{
			get => m_IsShowReleaseCard;
			set => Set ( ref m_IsShowReleaseCard , value );
		}

		/// <summary>
		/// Selected releases.
		/// </summary>
		public ObservableCollection<ReleaseModel> SelectedReleases
		{
			get => m_SelectedReleases;
			set => Set ( ref m_SelectedReleases , value );
		}

		/// <summary>
		/// Show sidebar.
		/// </summary>
		public Action ShowSidebar
		{
			get;
			set;
		}

		/// <summary>
		/// Show sidebar command.
		/// </summary>
		public ICommand ShowSidebarCommand
		{
			get;
			set;
		}

	}

}
