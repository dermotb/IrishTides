using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortsDaysTides
{
	public class Tide
	{
		public bool isHigh {get; set;}
		public string tideTime { get; set; }
		public string tideHeight { get; set; }

		public Tide(bool highValue, string time, string height)
		{
			isHigh=highValue;
			tideTime = time;
			tideHeight = height;
		}

		public Tide()
		{
			isHigh = false;
			tideTime = string.Empty;
			tideHeight = string.Empty;
		}
	}
}
