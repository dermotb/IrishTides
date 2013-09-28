using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.Xml.Linq;
using Windows.UI.Popups;

namespace W8Tides.Data
{



    /// <summary>
    /// Base class for <see cref="DataItem"/> and <see cref="DataGroup"/> that
    /// defines properties common to both.
    /// </summary>
    [Windows.Foundation.Metadata.WebHostHidden]
    public abstract class Port : W8Tides.Common.BindableBase
    {
        private static Uri _baseUri = new Uri("ms-appx:///");

        public Port(String uniqueId, String title, String subtitle, String imagePath, String description, string latitude, string longitude)
        {
			this._uniqueId = uniqueId;
			this._title = title;
			this._subtitle = subtitle;
			this._description = description;
			this._imagePath = imagePath;
			this._latitude = latitude;
			this._longitude = longitude;
        }

		public Port()
		{
			// TODO: Complete member initialization
		}

		private string _uniqueId = string.Empty;
		public string UniqueId
		{
			get { return this._uniqueId; }
			set { this.SetProperty(ref this._uniqueId, value); }
		}

		private string _title = string.Empty;
		public string Title
		{
			get { return this._title; }
			set { this.SetProperty(ref this._title, value); }
		}

		private string _subtitle = string.Empty;
		public string Subtitle
		{
			get { return this._subtitle; }
			set { this.SetProperty(ref this._subtitle, value); }
		}

		private string _description = string.Empty;
		public string Description
		{
			get { return this._description; }
			set { this.SetProperty(ref this._description, value); }
		}

		private ImageSource _image = null;
		private String _imagePath = null;
		public ImageSource Image
		{
			get
			{
				if (this._image == null && this._imagePath != null)
				{
					this._image = new BitmapImage(new Uri(Port._baseUri, this._imagePath));
				}
				return this._image;
			}

			set
			{
				this._imagePath = null;
				this.SetProperty(ref this._image, value);
			}
		}

		public void SetImage(String path)
		{
			this._image = null;
			this._imagePath = path;
			this.OnPropertyChanged("Image");
		}

		public override string ToString()
		{
			return this.Title;
		}

		private string _longitude = string.Empty;
		public string Longitude
		{
			get { return this._longitude; }
			set { this.SetProperty(ref this._longitude, value); }
		}

		private string _latitude = string.Empty;
		public string Latitude
		{
			get { return this._latitude; }
			set { this.SetProperty(ref this._latitude, value); }
		}
	}

    /// <summary>
    /// Generic item data model.
    /// </summary>
    public class DataItem : Port 
    {
        public DataItem(String name, String id, String subtitle, String imagePath, String description, String latitude, String longitude, String content, DataGroup group)
            : base(name, id, subtitle, imagePath, description, latitude, longitude)
        {
            this._content = content;
            this._group = group;
        }

        private string _content = string.Empty;
        public string Content
        {
            get { return this._content; }
            set { this.SetProperty(ref this._content, value); }
        }

        private DataGroup _group;
        public DataGroup Group
        {
            get { return this._group; }
            set { this.SetProperty(ref this._group, value); }
        }
    }

    /// <summary>
    /// Generic group data model.
    /// </summary>
    public class DataGroup : Port
    {
        public DataGroup(String uniqueId, String title, String subtitle, String imagePath, String description, String latitude, String longitude, String content)
            : base(uniqueId, title, subtitle, imagePath, description, latitude, longitude)
        {
            Items.CollectionChanged += ItemsCollectionChanged;
        }

        private void ItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Provides a subset of the full items collection to bind to from a GroupedItemsPage
            // for two reasons: GridView will not virtualize large items collections, and it
            // improves the user experience when browsing through groups with large numbers of
            // items.
            //
            // A maximum of 12 items are displayed because it results in filled grid columns
            // whether there are 1, 2, 3, 4, or 6 rows displayed

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex,Items[e.NewStartingIndex]);
                        if (TopItems.Count > 12)
                        {
                            TopItems.RemoveAt(12);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex < 12 && e.NewStartingIndex < 12)
                    {
                        TopItems.Move(e.OldStartingIndex, e.NewStartingIndex);
                    }
                    else if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        TopItems.Add(Items[11]);
                    }
                    else if (e.NewStartingIndex < 12)
                    {
                        TopItems.Insert(e.NewStartingIndex, Items[e.NewStartingIndex]);
                        TopItems.RemoveAt(12);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems.RemoveAt(e.OldStartingIndex);
                        if (Items.Count >= 12)
                        {
                            TopItems.Add(Items[11]);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex < 12)
                    {
                        TopItems[e.OldStartingIndex] = Items[e.OldStartingIndex];
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    TopItems.Clear();
                    while (TopItems.Count < Items.Count && TopItems.Count < 12)
                    {
                        TopItems.Add(Items[TopItems.Count]);
                    }
                    break;
            }
        }

        private ObservableCollection<DataItem> _items = new ObservableCollection<DataItem>();
        public ObservableCollection<DataItem> Items
        {
            get { return this._items; }
        }

        private ObservableCollection<DataItem> _topItem = new ObservableCollection<DataItem>();
        public ObservableCollection<DataItem> TopItems
        {
            get {return this._topItem; }
        }
    }

    /// <summary>
    /// Creates a collection of groups and items with hard-coded content.
    /// 
    /// SampleDataSource initializes with placeholder data rather than live production
    /// data so that sample data is provided at both design-time and run-time.
    /// </summary>
    public sealed class PortDataSource
    {
        private static PortDataSource _dataSource = new PortDataSource();

        private ObservableCollection<DataGroup> _allGroups = new ObservableCollection<DataGroup>();
        public ObservableCollection<DataGroup> AllGroups
        {
            get { return this._allGroups; }
        }

        public static IEnumerable<DataGroup> GetGroups(string uniqueId)
        {
            if (!uniqueId.Equals("AllGroups")) throw new ArgumentException("Only 'AllGroups' is supported as a collection of groups");
            
            return _dataSource.AllGroups;
        }

        public static DataGroup GetGroup(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _dataSource.AllGroups.Where((group) => group.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }

        public static DataItem GetItem(string uniqueId)
        {
            // Simple linear search is acceptable for small data sets
            var matches = _dataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.UniqueId.Equals(uniqueId));
            if (matches.Count() == 1) return matches.First();
            return null;
        }


		public static DataItem GetID(string title)
		{
			// Simple linear search is acceptable for small data sets
			var matches = _dataSource.AllGroups.SelectMany(group => group.Items).Where((item) => item.Title.Equals(title));
			if (matches.Count() == 1) return matches.First();
			return null;
		}


        public PortDataSource()
        {
			//try
			{
				var group1 = new DataGroup("Ireland",
				"Group Title: 1",
				"Group Subtitle: 1",
				"Assets/SplashScreen.png",
				"Group Description: Ports on the coast of Ireland",
				"", "", "");

				/*
				for (int i=0; i<20; i++)
				{
					group1.Items.Add(new DataItem(i.ToString(), string.Format("Name_{0}",i),
					"",
					"DataModel/Images/belfast.png",
					"Great spot",
					"ITEM_CONTENT",
                    group1));
				}
				*/
	
				XElement portFeeds = XElement.Load("DataModel\\ports.xml");
				foreach (XElement port in portFeeds.Elements())
				{
					group1.Items.Add(new DataItem(port.Attribute("ID").Value, port.Attribute("Name").Value,
					"",
					//"DataModel/Images/"+port.Attribute("Image").Value,
					"Assets/Logo.png",
					port.Attribute("Desc").Value,
					port.Attribute("Lat").Value,
					port.Attribute("Long").Value,
					"ITEM_CONTENT",
                    group1));
				}
				this.AllGroups.Add(group1);
		}
/*			catch (Exception e)
			{
				msgMethod(e);
			}*/
        }

		private async void msgMethod(Exception e)
		{
			MessageDialog msg = new MessageDialog("Exception: {0}", e.Message);
			await msg.ShowAsync();
		}
    }
}
