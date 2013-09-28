using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Notifications;

namespace PortsDaysTides
{
	public class TilePortUtils
	{
		const string TileFileName = @"IrishTidesTiles.xml";

		public static async Task<string> GetTidePage(string portID)
		{
			StringBuilder portRequestUrl = new StringBuilder();
			portRequestUrl.Append(string.Format(CultureInfo.InvariantCulture, @"http://easytide.ukho.gov.uk/EASYTIDE/EasyTide/ShowPrediction.aspx?PortID={0}", portID));
			portRequestUrl.Append(@"&PredictionLength=7");

			//In the summer, get the tide times in IST/BST
			if (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now))
			{
				portRequestUrl.Append(@"&DaylightSavingOffset=60");
			}

			var httpClient = new HttpClient();
			var response = await httpClient.GetAsync(portRequestUrl.ToString());

			response.EnsureSuccessStatusCode();

			return await response.Content.ReadAsStringAsync();
		}


		public static async Task<Queue<TilePort>> ReadTileDataXml()
		{
			Queue<TilePort> portsQueue = new Queue<TilePort>();

			try
			{
				//StorageFolder folder = KnownFolders.DocumentsLibrary;
				StorageFolder folder = ApplicationData.Current.LocalFolder;
				XDocument xmldoc = null;

				StorageFile xmlfile = await folder.GetFileAsync(TileFileName);

				IRandomAccessStream readStream = await xmlfile.OpenAsync(FileAccessMode.Read);
				if (readStream == null)
				{
					return portsQueue;
				}
				if (readStream.Size > 0)
				{
					xmldoc = XDocument.Load(readStream.AsStreamForRead());

					var portTides = xmldoc.Descendants("Port");

					Debug.WriteLine(portTides.Count() + " nodes selected");

					foreach (XElement p in portTides)
					{
						TilePort tp = TilePort.TilePortFromXML(p);
						portsQueue.Enqueue(tp);
					}
				}
				return portsQueue;
			}
			catch (Exception)
			{
				return portsQueue;
			}
		}


		public static async void WriteTileDataXml(Queue<TilePort> portsQueue)
		{
			//StorageFolder folder = KnownFolders.DocumentsLibrary;
			StorageFolder folder = ApplicationData.Current.LocalFolder;
			// save answers
			StorageFile file = await folder.CreateFileAsync(TileFileName, CreationCollisionOption.ReplaceExisting);

			XmlDocument xmlDoc = new XmlDocument();

			// Manipulate the xml document.
			var root = xmlDoc.CreateElement("ROOT");
			xmlDoc.AppendChild(root);

			foreach (TilePort tp in portsQueue)
			{
				var portTag = xmlDoc.CreateElement("Port");
				root.AppendChild(portTag);
				portTag.SetAttribute("ID", tp.ID);
				portTag.SetAttribute("Title", tp.Title);

				foreach (Day d in tp.Days)
				{
					var dayTag = xmlDoc.CreateElement("Day");
					portTag.AppendChild(dayTag);
					dayTag.SetAttribute("DayDate", d.dayDate);
					foreach (Tide t in d.Tides)
					{
						var tideTag = xmlDoc.CreateElement("Tide");
						dayTag.AppendChild(tideTag);
						tideTag.SetAttribute("HWLW", (t.isHigh)?"HW":"LW");
						tideTag.SetAttribute("Time", t.tideTime);
						tideTag.SetAttribute("Height", t.tideHeight);
					}
				}
			}
			await xmlDoc.SaveToFileAsync(file);
		}


		public static TilePort TilePortFromHtml(string portID, string portTitle, string htmlPage)
		{
			HtmlDocument tideFile = new HtmlDocument();
			tideFile.LoadHtml(htmlPage);
			TilePort tp = new TilePort(portID, portTitle);

			var tableNodes = tideFile.DocumentNode.Descendants("table");

			var tideTables = tideFile.DocumentNode.Descendants("table").Where(p=>p.HasAttributes && p.Attributes["class"].Value.Contains("HWLWTable"));

			foreach (HtmlNode table in tideTables)
			{
				HtmlNode tableHead = table.Descendants("th").FirstOrDefault(p => p.HasAttributes && p.Attributes[0].Value.Equals("HWLWTableHeaderCell"));
				if (tableHead != null)
				{
					Day oneDay = new Day(tableHead.InnerText);
					var tideTitles = table.Descendants("th").Where(p => p.HasAttributes && p.Attributes[0].Value.Equals("HWLWTableHWLWCell"));

					if (tideTitles != null)
					{
						int tideCount = tideTitles.Count();
						
						for (int i = 0; i < tideCount; i++)
						{
							Tide oneTide = new Tide();

							if (tideTitles.ElementAt(i).InnerText.Contains("HW"))
							{
								oneTide.isHigh = true;
							}
							oneTide.tideTime = table.Descendants("td").ElementAt(i).InnerText.TrimStart().Replace("&nbsp;", " "); 
							oneTide.tideHeight = table.Descendants("td").ElementAt(i + tideCount).InnerText.Replace("&nbsp;", " ");
							oneDay.Tides.Add(oneTide);
						}
					}
					tp.Days.Add(oneDay);
				}
			}
			return tp;
		}

		public static void PushTileUpdateData(string portID, string portName, Day today)
		{
			TileUpdateManager.CreateTileUpdaterForApplication().EnableNotificationQueue(true);

			//set up wide tile as main tile
			XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideText10);
			XmlNodeList tileTextAttributes = tileXml.GetElementsByTagName("text");
			tileTextAttributes[0].InnerText = portName;
			tileTextAttributes[1].InnerText = "";
			tileTextAttributes[2].InnerText = string.Format("High Tides - {0}", today.dayDate);
			tileTextAttributes[3].InnerText = "";

			bool haveOne = false;
			foreach (Tide t in today.Tides)
			{
				if (t.isHigh && !haveOne)
				{
					tileTextAttributes[4].InnerText = string.Format("{0}	{1}", t.tideTime, t.tideHeight);
					haveOne = true;
				}

				if (t.isHigh && haveOne)
				{
					tileTextAttributes[6].InnerText = string.Format("{0}	{1}", t.tideTime, t.tideHeight);
				}
			}

			//XmlNodeList tileImageAttributes = tileXml.GetElementsByTagName("image");

			//((XmlElement)tileImageAttributes[0]).SetAttribute("src", "ms-appx:///assets/logo.png");
			//((XmlElement)tileImageAttributes[0]).SetAttribute("alt", "red graphic");

			/*			//make a square tile
						XmlDocument squareTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText04);
						XmlNodeList squareTileTextAttributes = squareTileXml.GetElementsByTagName("text");
						//squareTileTextAttributes[0].AppendChild(squareTileXml.CreateTextNode("Burtonport"+e.GetHashCode().ToString()));

						//bind the contents of both
						IXmlNode node = tileXml.ImportNode(squareTileXml.GetElementsByTagName("binding").Item(0), true);
						tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);*/

			TileNotification tileNotification = new TileNotification(tileXml);

			tileNotification.ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(10);

			Int16 dueTimeInSeconds = 10;
			DateTime dueTime = DateTime.Now.AddSeconds(dueTimeInSeconds);
			ScheduledTileNotification scheduledTile = new ScheduledTileNotification(tileXml, dueTime);
			scheduledTile.Id = portID;
			TileUpdateManager.CreateTileUpdaterForApplication().AddToSchedule(scheduledTile);
		}

	}
}
