using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortsDaysTides
{
	public class Day
	{
		public string dayDate {get; set;}
		public List<Tide> Tides {get; set;}

		public Day(string day)
		{
			dayDate = day;
			Tides = new List<Tide>();
		}
	}
}
