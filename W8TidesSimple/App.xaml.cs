using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using W8Tides.Data;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace W8Tides
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

			//uint waitIntervalMinutes = 60;

			SystemTrigger taskTrigger = new SystemTrigger(SystemTriggerType.InternetAvailable, false);

//			MaintenanceTrigger taskTrigger = new MaintenanceTrigger(waitIntervalMinutes, false);
//			SystemCondition taskCondition = new SystemCondition(SystemConditionType.InternetAvailable);


			string entryPoint = "Tasks.BackgroundTileUpdate";
			string taskName = "Irish Tides Background Tile Updater";

			BackgroundTaskRegistration task = RegisterBackgroundTask(entryPoint, taskName, taskTrigger, null);

			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);

			//set up wide tile as main tile
			XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideImage);

			var bindingElement = (XmlElement)tileXml.GetElementsByTagName("binding").Item(0);
			bindingElement.SetAttribute("branding", "none");


			XmlNodeList tileImageAttributes = tileXml.GetElementsByTagName("image");

			((XmlElement)tileImageAttributes[0]).SetAttribute("src", "ms-appx:///assets/WideLogo.png");
			((XmlElement)tileImageAttributes[0]).SetAttribute("alt", "red graphic");

			//make a square tile
			/*			XmlDocument squareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareImage);
						XmlNodeList squareTileImageAttributes = squareTileXml.GetElementsByTagName("image");

						((XmlElement)squareTileImageAttributes[0]).SetAttribute("src", "ms-appx:///assets/Logo.png");
						((XmlElement)squareTileImageAttributes[0]).SetAttribute("alt", "red graphic");
	
						//bind them together
						IXmlNode node = tileXml.ImportNode(squareTileXml.GetElementsByTagName("binding").Item(0), true);
						tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);*/

			TileNotification tileNotification = new TileNotification(tileXml);

			tileNotification.ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(10);
			Int16 dueTimeInSeconds = 10;
			DateTime dueTime = DateTime.Now.AddSeconds(dueTimeInSeconds);
			ScheduledTileNotification scheduledTile = new ScheduledTileNotification(tileXml, dueTime);
			scheduledTile.Id = "App_Tile";
			TileUpdateManager.CreateTileUpdaterForApplication().AddToSchedule(scheduledTile);
      }


	

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
			Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
		

        }

	
	


        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
			Debug.WriteLine("Main project suspended");
            deferral.Complete();
        }


		//
		// Register a background task with the specified taskEntryPoint, name, trigger,
		// and condition (optional).
		//
		// taskEntryPoint: Task entry point for the background task.
		// taskName: A name for the background task.
		// trigger: The trigger for the background task.
		// condition: Optional parameter. A conditional event that must be true for the task to fire.
		//
		public static BackgroundTaskRegistration RegisterBackgroundTask(string taskEntryPoint,
																		string taskName,
																		IBackgroundTrigger trigger,
																		IBackgroundCondition condition)
		{
			//
			// Check for existing registrations of this background task.
			//
			Debug.WriteLine("Registering background task: {0}", taskName);

			foreach (var cur in BackgroundTaskRegistration.AllTasks)
			{


				if (cur.Value.Name == taskName)
				{
					// 
					// The task is already registered.
					// 

					return (BackgroundTaskRegistration)(cur.Value);
				}
			}


			//
			// Register the background task.
			//

			var builder = new BackgroundTaskBuilder();

			builder.Name = taskName;
			builder.TaskEntryPoint = taskEntryPoint;
			builder.SetTrigger(trigger);

			if (condition != null)
			{
				builder.AddCondition(condition);
			}

			BackgroundTaskRegistration task = builder.Register();
			task.Completed += new BackgroundTaskCompletedEventHandler(OnTileUpdateCompleted);
			task.Progress += new BackgroundTaskProgressEventHandler(OnProgress);

			return task;
		}

		private static void OnProgress(BackgroundTaskRegistration sender, BackgroundTaskProgressEventArgs args)
		{

		}

	

		/// <summary> 
		/// Unregister background tasks with specified name. 
		/// </summary> 
		/// <param name="name">Name of the background task to unregister.</param> 
		public static void UnregisterBackgroundTasks(string name)
		{
			// 
			// Loop through all background tasks and unregister any with SampleBackgroundTaskName or 
			// SampleBackgroundTaskWithConditionName. 
			// 
			foreach (var cur in BackgroundTaskRegistration.AllTasks)
			{
				if (cur.Value.Name == name)
				{
					cur.Value.Unregister(true);
				}
			}
		}

		private static void OnTileUpdateCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
		{
			try
			{
				args.CheckResult();
			}
			catch (Exception)
			{
			}
		}
    }
}
