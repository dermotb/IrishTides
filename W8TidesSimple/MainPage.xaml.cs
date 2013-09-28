using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using W8Tides.Data;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HtmlAgilityPack;
using System.Net.Http;
using System.Text;
using Windows.UI.ApplicationSettings;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Text;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using PortsDaysTides;
using Windows.UI.ViewManagement;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace W8Tides
{
	/// <summary>
	/// A basic page that provides characteristics common to most applications.
	/// </summary>
	public sealed partial class MainPage : W8Tides.Common.LayoutAwarePage
	{
		public static string CurrentPortID { get; set; }
		public static string CurrentPortTitle { get; set; }
		bool isScraping = false;
		static Queue<TilePort> TilesQueue;

		public MainPage()
		{
			this.InitializeComponent();
			SettingsPane.GetForCurrentView().CommandsRequested += W8Tides_CommandsRequested;
			ApplicationViewStates.CurrentStateChanging += ApplicationViewStates_CurrentStateChanging;

			DataTransferManager manager = DataTransferManager.GetForCurrentView();
			manager.DataRequested += manager_DataRequested;
			InitializeData();
		}

	


		private async void InitializeData()
		{
				await LoadData();
				ScrapeTidepage();
		}

		private async Task<bool> LoadData()
		{
			try
			{
				cboItemTitle.ItemsSource = PortDataSource.GetGroup("Ireland").Items.Select(x => x.Title);
				cboItemTitle.SelectedItem = CurrentPortTitle;
			}
			catch (Exception)
			{
				ShowWarningMessage("Failed to initialise  data model");
				return false;
			}

			try
			{
				CurrentPortID = ApplicationData.Current.LocalSettings.Values["currentPortID"].ToString();
				CurrentPortTitle = ApplicationData.Current.LocalSettings.Values["currentPortName"].ToString();
			}
			catch (NullReferenceException)
			{
				CurrentPortID = PortDataSource.GetGroup("Ireland").Items.ToList()[0].UniqueId;
				CurrentPortTitle = PortDataSource.GetGroup("Ireland").Items.ToList()[0].Title;
			}

			try
			{
				TilesQueue = await TilePortUtils.ReadTileDataXml();
			}
			catch (FileNotFoundException)
			{
				ShowWarningMessage("No saved data found - first use of app");
			}
			catch (Exception)
			{
				ShowWarningMessage("Issues occured reading saved data - Recent port and live tile updates may be affected");
			}
			cboItemTitle.SelectedItem = CurrentPortTitle;
			return true;
		}


		private void AddToTPQueue(TilePort tp)
		{
			try
			{
				if (TilesQueue.ToArray().FirstOrDefault(p => p.ID.Equals(tp.ID)) != null)
				{
					UpdateTileQueue(tp);
				}
				else
				{
					if (TilesQueue.Count == 5)
					{
						TilesQueue.Dequeue();
					}
					TilesQueue.Enqueue(tp);
				}
			}
			catch (Exception)
			{
				savedDataWarning.Text = "Internal Error - Failed to add current port to queue";
				savedDataWarning.Visibility = Windows.UI.Xaml.Visibility.Visible;
			}
				 
		}

		private void UpdateTileQueue(TilePort tp)
		{
			for (int i = 0; i < TilesQueue.Count; i++)
			{
				if (TilesQueue.ToArray()[i].ID.Equals(tp.ID))
				{
					tp.Days.CopyTo(TilesQueue.ToArray()[i].Days.ToArray());
				}
			}
		}

		void manager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
		{
			DataRequest request = args.Request;

			request.Data.Properties.Title = string.Format("Irish Tides - {0}", CurrentPortTitle);
			request.Data.Properties.Description = "Sharing tide data";

			TilePort sharePort = TilesQueue.ToArray().FirstOrDefault(p => p.ID.Equals(CurrentPortID));
			string htmlMail = BuildHtml(sharePort, CurrentPortID, CurrentPortTitle);
			string htmlFormat = HtmlFormatHelper.CreateHtmlFormat(htmlMail);
			request.Data.SetHtmlFormat(htmlFormat);
		}


		private void cboItemTitle_Changed(object sender, SelectionChangedEventArgs e)
		{
			string selectedPort = cboItemTitle.SelectedItem.ToString();
			if (!selectedPort.Equals(CurrentPortTitle))
			{
				DataItem newPort = PortDataSource.GetID(selectedPort);
				ApplicationData.Current.LocalSettings.Values["currentPortName"] = selectedPort;
				ApplicationData.Current.LocalSettings.Values["currentPortID"] = newPort.UniqueId;
				CurrentPortID = newPort.UniqueId;
				CurrentPortTitle = newPort.Title;
			}
			ScrapeTidepage();
		}

		void W8Tides_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
		{
			// Add an About command
			SettingsCommand commandAbout = new SettingsCommand("about", "About This App", (x) =>
			{
				Popup popup = BuildSettingsItem(new AboutPage(), 346);
				popup.IsOpen = true;
			});

			args.Request.ApplicationCommands.Add(commandAbout);

			// Add an Privacy command
			SettingsCommand commandPrivacy = new SettingsCommand("privacy", "Privacy Policy", (x) =>
			{
				Popup popup = BuildSettingsItem(new PrivacyPage(), 346);
				popup.IsOpen = true;
			});

			args.Request.ApplicationCommands.Add(commandPrivacy);
		}

		private Popup BuildSettingsItem(UserControl u, int w)
		{
			Popup p = new Popup();
			p.IsLightDismissEnabled = true;
			p.ChildTransitions = new TransitionCollection();
			p.ChildTransitions.Add(new PaneThemeTransition()
			{
				Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ?
						EdgeTransitionLocation.Right :
						EdgeTransitionLocation.Left
			});

			u.Width = w;
			u.Height = Window.Current.Bounds.Height;
			p.Child = u;

			p.SetValue(Canvas.LeftProperty, SettingsPane.Edge == SettingsEdgeLocation.Right ? (Window.Current.Bounds.Width - w) : 0);
			p.SetValue(Canvas.TopProperty, 0);

			return p;
		}

		public async void ScrapeTidepage()
		{
			if (isScraping)
			{
				return;
			}

			savedDataWarning.Visibility = Visibility.Collapsed; //remove old error messages
			isScraping = true;

			try
			{
				TilePort tpWeek;
				PortDataGrid.DataContext = PortDataSource.GetItem(CurrentPortID);

				string content = await TilePortUtils.GetTidePage(CurrentPortID);

				if (content.Length > 0)
				{
					tpWeek = TilePortUtils.TilePortFromHtml(CurrentPortID, CurrentPortTitle, content);
					TilePortUtils.PushTileUpdateData(CurrentPortID, CurrentPortTitle, tpWeek.Days[0]);
				}
				else
				{
					ShowWarningMessage("Failed to connect - attempting to use locally stored tide data");
					tpWeek = TilesQueue.ToArray().FirstOrDefault(p => p.ID.Equals(CurrentPortID));
				}

				if ((tpWeek != null) && (tpWeek.Days.Count > 0))
				{
					AddTables(tpWeek);
					AddToTPQueue(tpWeek);
				}
				else
				{
					ShowWarningMessage("Failed to connect or to retrieve stored tide data");
				}

			}
			catch (Exception)
			{
				ShowWarningMessage("An error occurred retrieving tide data");
				return;
			}
			finally
			{
				isScraping = false;
			}
			TilePortUtils.WriteTileDataXml(TilesQueue);
		}

		private void AddTables(TilePort tpWeek)
		{
			try
			{
				tablesGridView.Items.Clear();
				tablesListView.Items.Clear();
				tablesGridView.FlowDirection = Windows.UI.Xaml.FlowDirection.LeftToRight;

				foreach (Day day in tpWeek.Days)
				{
					StackPanel dayPanel = new StackPanel();
					dayPanel.Height = 300;
					dayPanel.Width = 500;
					dayPanel.Background = new SolidColorBrush(Colors.WhiteSmoke);

					DayTable dayTable = new DayTable(day.dayDate);

					for (int i = 0; i < 4; i++ )
					{
						if (i < day.Tides.Count)
						{
							dayTable.SetColumnValue(day.Tides[i], i);
						}
						else
						{
							dayTable.SetColumnBlank(i);
						}
					}

					//need to make a copy of the table to add it to both lists safely
					DayTable d2 = new DayTable(dayTable);

					double snapWidth = GetSnapWidth();
					d2.SetWidth(snapWidth);
					d2.SetTitleHAlignment(Windows.UI.Xaml.HorizontalAlignment.Left);

					tablesGridView.Items.Add(dayTable);
					tablesListView.Items.Add(d2);
				}
			}
			catch (Exception)
			{
				savedDataWarning.Text = "Error adding tables to gridview!";
				savedDataWarning.Visibility = Visibility.Visible;
			}
		}

		private double GetSnapWidth()
		{
			if (ApplicationView.Value == ApplicationViewState.FullScreenLandscape)
			{
				return ((this.ActualWidth / 100) * 23);
			}
			if (ApplicationView.Value == ApplicationViewState.Filled)
			{
				return ((this.ActualWidth / 100) * 31);
			}
			if (ApplicationView.Value == ApplicationViewState.FullScreenPortrait)
			{
				return ((this.ActualWidth / 100) * 41);
			}
			return 0;

		}


		private void ShowWarningMessage(string msg)
		{
			savedDataWarning.Text = msg;
			savedDataWarning.Visibility = Visibility.Visible;
		}


		private string BuildHtml(TilePort sharePort, string CurrentPortID, string CurrentPortTitle)
		{
			StringBuilder htmlBuilder = new StringBuilder();
			string headText = GetHtmlHead(CurrentPortTitle);

			htmlBuilder.Append(headText);

			foreach (Day d in sharePort.Days)
			{
				htmlBuilder.Append(MakeHtmlTable(d));
			}
			return htmlBuilder.ToString();
		}

		private string MakeHtmlTable(Day d)
		{
			StringBuilder tableBuilder = new StringBuilder();

			tableBuilder.Append("<table class=\"HWLWTable\" border=\"1\" style=\"background-color: lightgray;filter:alpha(opacity=60);margin-bottom:2em;\"> <tr>");
			tableBuilder.Append("<th class=\"HWLWTableHeaderCell\" colspan=\"4\" style=\"font-size:30px;font-weight:bold\">" + d.dayDate + "</th></tr>");
			tableBuilder.Append("<tr>");
			foreach (Tide t in d.Tides)
			{
				string colTitle = (t.isHigh) ? "HW" : "LW";
				tableBuilder.Append("<th class=\"HWLWTableHWLWCell\">"+colTitle+"</th>");
			}
			
			tableBuilder.Append("</tr><tr>");

			foreach (Tide t in d.Tides)
			{
				tableBuilder.Append("<td class=\"HWLWTableCell\" align=\"center\" style=\"font-size:20px\" style=\"font-weight:bold\">"+t.tideTime+"</td>");
			}
			
			tableBuilder.Append("</tr><tr>");

			foreach (Tide t in d.Tides)
			{
				tableBuilder.Append("<td class=\"HWLWTableCell\" align=\"center\" style=\"font-size:20px\" style=\"font-weight:bold\">"+t.tideHeight+"</td>");
			}

			tableBuilder.Append("</tr>");
			tableBuilder.Append("</table>");
			return tableBuilder.ToString();
			
		}


		private string GetHtmlHead(string name)
		{
			StringBuilder head = new StringBuilder();

			head.Append("<html style=\"background-color: none\"/>\n<meta name=\"viewport\" content=\"width=300, user-scalable=no\"/>");
			string header1 = string.Format("<p style=\"color:navy;font-size:16px;font-weight:bold\">Data for port: {0}<BR>", name);
			head.Append(header1);
			string header2 = string.Format("<p style=\"color:navy;font-size:12px\">Time shown are local (UTC+1 in summer)<BR>");
			head.Append(header2);
			head.Append("</p>");

			return head.ToString();
		}


		void ApplicationViewStates_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
		{
			//the actual view size 
			var visualState = ApplicationView.Value;

			if (visualState.ToString() == "Snapped")
			{
				Preamble.Visibility = Visibility.Collapsed;
				LatTitle.Visibility = Visibility.Collapsed;
				LongTitle.Visibility = Visibility.Collapsed;
				cboItemTitle.IsEnabled = false;
				GVertical.Visibility = Visibility.Visible;
				GHorizontal.Visibility = Visibility.Collapsed;
				return;
			}
			else
			{
				Preamble.Visibility = Visibility.Visible;
				LatTitle.Visibility = Visibility.Visible;
				LongTitle.Visibility = Visibility.Visible;
				cboItemTitle.IsEnabled = true;
				GHorizontal.Visibility = Visibility.Visible;
				GVertical.Visibility = Visibility.Collapsed;
				cboItemTitle.Width = 700; //visible and useable in all states except snapped

				return;
				}
		}
 

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
		}


		/// <summary>
		/// Populates the page with content passed during navigation.  Any saved state is also
		/// provided when recreating a page from a prior session.
		/// </summary>
		/// <param name="navigationParameter">The parameter value passed to
		/// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
		/// </param>
		/// <param name="pageState">A dictionary of state preserved by this page during an earlier
		/// session.  This will be null the first time a page is visited.</param>
		protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
		{
		}

		/// <summary>
		/// Preserves state associated with this page in case the application is suspended or the
		/// page is discarded from the navigation cache.  Values must conform to the serialization
		/// requirements of <see cref="SuspensionManager.SessionState"/>.
		/// </summary>
		/// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
		protected override void SaveState(Dictionary<String, Object> pageState)
		{
		}

	
		private void cboItemTitle_DropDownOpened(object sender, object e)
		{
			cboItemTitle.FontWeight = FontWeights.Normal;
			cboItemTitle.FontSize = 20;
			cboItemTitle.Height = 25;
		}

		private void cboItemTitle_DropDownClosed(object sender, object e)
		{
			cboItemTitle.FontWeight = FontWeights.Bold;
			cboItemTitle.FontSize = 50;
			cboItemTitle.Height = 75;
		}
	}
}
