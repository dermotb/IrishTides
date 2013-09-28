using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;


namespace PortsDaysTides
{
	public class TilePort
	{
		public string ID { get; set; }
		public string Title { get; set; }

		public List<Day> Days;

		public TilePort(string id, string title)
		{
			ID = id;
			Title = title;
			Days = new List<Day>();
		}

		public TilePort(XElement storedTp)
		{
			ID = storedTp.Attribute("ID").Value;
			Title = storedTp.Attribute("Title").Value;

			Days = new List<Day>();
		}

		public static TilePort TilePortFromXML(XElement p)
		{
			TilePort tp = new TilePort(p.Attribute("ID").Value, p.Attribute("Title").Value);
			var days = p.Descendants("Day");
			foreach (XElement d in days)
			{
				Day day = new Day(d.Attribute("DayDate").Value);
				var tides = d.Descendants("Tide");
				foreach (XElement t in tides)
				{
					Tide tide = new Tide((t.Attribute("HWLW").Value.Equals("HW")) ? true : false, t.Attribute("Time").Value, t.Attribute("Height").Value);
					day.Tides.Add(tide);
				}
				tp.Days.Add(day);
			}
			return tp;
		}
	}
}
