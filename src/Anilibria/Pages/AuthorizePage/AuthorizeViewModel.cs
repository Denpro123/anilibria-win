﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Anilibria.MVVM;
using Anilibria.Services;

namespace Anilibria.Pages.AuthorizePage {

	/// <summary>
	/// Authorize view model.
	/// </summary>
	public class AuthorizeViewModel : ViewModel, INavigation {

		private string m_Email;

		private string m_Password;

		private readonly IAnilibriaApiService m_AnilibriaApiService;

		/// <summary>
		/// Constructor injection.
		/// </summary>
		/// <param name="anilibriaApiService">Anilibria api service.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public AuthorizeViewModel ( IAnilibriaApiService anilibriaApiService ) {
			m_AnilibriaApiService = anilibriaApiService ?? throw new ArgumentNullException ( nameof ( anilibriaApiService ) );

			CreateCommands ();
		}

		private void CreateCommands () {
			SigninCommand = CreateCommand ( Signin , () => !string.IsNullOrEmpty ( Email ) && !string.IsNullOrEmpty ( Password ) );
			ShowSidebarCommand = CreateCommand ( OpenSidebar );
		}

		private void OpenSidebar () {
			ShowSidebar?.Invoke ();
		}

		private async void Signin () {
			var result = await m_AnilibriaApiService.Authentification ( Email , Password );
			if ( result ) {
				ChangePage ( "Releases" , null );
				RefreshOptions?.Invoke ();
				await ChangeUserSession ();
			}
			else {
				//TODO: show error on page
			}
		}

		public void NavigateFrom () {
			Email = "";
			Password = "";
			RaiseCommands ();
		}

		public void NavigateTo ( object parameter ) {
			Email = "";
			Password = "";
			RaiseCommands ();
		}

		/// <summary>
		/// Email.
		/// </summary>
		public string Email
		{
			get => m_Email;
			set
			{
				if ( !Set ( ref m_Email , value ) ) return;

				RaiseCommands ();
			}
		}

		/// <summary>
		/// Password.
		/// </summary>
		public string Password
		{
			get => m_Password;
			set
			{
				if ( !Set ( ref m_Password , value ) ) return;

				RaiseCommands ();
			}
		}

		/// <summary>
		/// Refresh options.
		/// </summary>
		public Action RefreshOptions
		{
			get;
			set;
		}

		/// <summary>
		/// Change user session.
		/// </summary>
		public Func<Task> ChangeUserSession
		{
			get;
			set;
		}

		/// <summary>
		/// Change page handler.
		/// </summary>
		public Action<string , object> ChangePage
		{
			get;
			set;
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
		/// Signin.
		/// </summary>
		public ICommand SigninCommand
		{
			get;
			set;
		}

		/// <summary>
		/// Show sidebar.
		/// </summary>
		public ICommand ShowSidebarCommand
		{
			get;
			set;
		}

	}

}
