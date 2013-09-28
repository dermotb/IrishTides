using PortsDaysTides;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace W8Tides
{
	public sealed partial class DayTable : UserControl
	{
		public string dayDate { get; set; }

		public DayTable()
		{
		}

		public DayTable(string date)
		{
			this.InitializeComponent();
			PanelTitle.Text = date;
			dayDate = date;
	
		}

		public DayTable(DayTable copyTable)
		{
			this.InitializeComponent();

			PanelTitle.Text = copyTable.PanelTitle.Text;
			dayDate = copyTable.dayDate;

			HWLW0.Text = copyTable.HWLW0.Text;
			Time0.Text = copyTable.Time0.Text;
			Height0.Text = copyTable.Height0.Text;

			HWLW1.Text = copyTable.HWLW1.Text;
			Time1.Text = copyTable.Time1.Text;
			Height1.Text = copyTable.Height1.Text;

			HWLW2.Text = copyTable.HWLW2.Text;
			Time2.Text = copyTable.Time2.Text;
			Height2.Text = copyTable.Height2.Text;

			HWLW3.Text = copyTable.HWLW3.Text;
			Time3.Text = copyTable.Time3.Text;
			Height3.Text = copyTable.Height3.Text;
		}

		public void SetTitleHAlignment(Windows.UI.Xaml.HorizontalAlignment alignment)
		{
			this.PanelTitle.HorizontalAlignment = alignment;
		}


		public void SetWidth(double width)
		{
			double textWidth = width - 5; //Border is 4 wide
			this.InitializeComponent();
			HWLW0.Width = textWidth / 5;
			HWLW1.Width = textWidth / 5;
			HWLW2.Width = textWidth / 5;
			HWLW3.Width = textWidth / 5;
			TideTablePanel.Width = textWidth;
			DayGrid.Width = width;
			this.Width = width;
			DayTideBorder.Padding = new Thickness((width / 100) * 2);
			DayTideBorder.Width = textWidth;

			SetFontWeight(textWidth / 10);
		}

		private void SetFontWeight(double titleWeight)
		{
			double regWeight = (titleWeight/5)*3;
			
			PanelTitle.FontSize = titleWeight;
			HWLW0.FontSize = regWeight;
			HWLW1.FontSize = regWeight;
			HWLW2.FontSize = regWeight;
			HWLW3.FontSize = regWeight;
			Time0.FontSize = regWeight;
			Time1.FontSize = regWeight;
			Time2.FontSize = regWeight;
			Time3.FontSize = regWeight;
			Height0.FontSize = regWeight;
			Height1.FontSize = regWeight;
			Height2.FontSize = regWeight;
			Height3.FontSize = regWeight;
		}


		internal void SetColumnValue(Tide tide, int col)
		{
			switch (col)
			{
				case 0:
					HWLW0.Text = (tide.isHigh) ? "HW":"LW";
					Time0.Text = tide.tideTime;
					Height0.Text = tide.tideHeight;
					break;
				case 1:
					HWLW1.Text = (tide.isHigh) ? "HW" : "LW";
					Time1.Text = tide.tideTime;
					Height1.Text = tide.tideHeight;
					break;
				case 2:
					HWLW2.Text = (tide.isHigh) ? "HW" : "LW";
					Time2.Text = tide.tideTime;
					Height2.Text = tide.tideHeight;
					break;
				case 3:
					HWLW3.Text = (tide.isHigh) ? "HW" : "LW";
					Time3.Text = tide.tideTime;
					Height3.Text = tide.tideHeight;
					break;
			}
		}

		internal void SetColumnBlank(int col)
		{
			switch (col)
			{
				case 0:
					HWLW0.Text = "";
					Time0.Text = "";
					Height0.Text = "";
					break;
				case 1:
					HWLW1.Text = "";
					Time1.Text = "";
					Height1.Text = "";
					break;
				case 2:
					HWLW2.Text = "";
					Time2.Text = "";
					Height2.Text = "";
					break;
				case 3:
					HWLW3.Text = "";
					Time3.Text = "";
					Height3.Text = "";
					break;
			}
		}
	}
}
