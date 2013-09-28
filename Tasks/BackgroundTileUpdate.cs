using PortsDaysTides;
//
// ExampleBackgroundTask.cs
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;



namespace Tasks
{
	public sealed class BackgroundTileUpdate : IBackgroundTask
	{
		Queue<TilePort> TilesQueue;
		volatile bool _CancelRequested = false;
		 IBackgroundTaskInstance _taskInstance = null;


		public async void Run(IBackgroundTaskInstance taskInstance)
		{
			BackgroundTaskDeferral _deferral = taskInstance.GetDeferral();
			taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
			_taskInstance = taskInstance;

			Debug.WriteLine("BackgroundTask running");

			//if Tile was not updated today, update it now
			if (!SameDay())
			{
				await UpdateTiles();
			}
			_deferral.Complete();
		}

		private bool SameDay()
		{
			var key = string.Format("{0} Last Run Day",_taskInstance.Task.Name); 

			var settings = ApplicationData.Current.LocalSettings;
			int lastRunDay = 0;

			if (settings.Values.ContainsKey(key))
			{
				lastRunDay = (int)settings.Values[key];
			}
			else
			{
				lastRunDay = 0;
			}

			settings.Values[key] = DateTime.Now.DayOfYear;

			if (lastRunDay == DateTime.Today.DayOfYear)
			{
				return true;
			}

			return false;
		}

		private async System.Threading.Tasks.Task<uint> UpdateTiles()
		{
			if (_CancelRequested)
			{
				return 0;
			}

			uint noPorts = 0;
			Day closestDay;
			TilesQueue = await TilePortUtils.ReadTileDataXml();
			bool synced = false;

			for (int i=0; i< TilesQueue.Count; i++)
			{
				TilePort tp = TilesQueue.ToArray()[i];
				closestDay = GetNextHW(tp);

				//if today or tomorrow's data not in xml, then we need to sync with web site
				if (closestDay == null && !_CancelRequested) //if app is aborting, don't start this job
				{
					string content = await TilePortUtils.GetTidePage(tp.ID);
					TilePort tpWeek = TilePortUtils.TilePortFromHtml(tp.ID, tp.Title, content);
					tpWeek.Days.CopyTo(TilesQueue.ToArray()[i].Days.ToArray());
					synced = true;
					closestDay = GetNextHW(tp);
				}

				if (closestDay != null)
				{
					TilePortUtils.PushTileUpdateData(tp.ID, tp.Title, closestDay);
				}
			}
			//if we synced with web site, save the new data
			if (synced)
			{
				TilePortUtils.WriteTileDataXml(TilesQueue);
			}
			return noPorts;
		}

	

		private Day GetNextHW(TilePort tp)
		{
			DateTime current = DateTime.Today;
			string today = current.Day.ToString();
			string tomorrow = (current.Day+1).ToString();

			foreach (Day d in tp.Days)
			{
				if (d.dayDate.Contains(today))
				{
					return (d);
				}
				
				if (d.dayDate.Contains(tomorrow))
				{
					return (d);
				}
			}
			//if we get here we have not found it and need to sync with the web site
			return null;
		}


		private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
		{
			//
			// Indicate that the background task is canceled.
			//

			_CancelRequested = true;

			Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested...");
		}

	}
}