#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleDiagram.Layout.SampleDiagramPublic
File: LayoutManager.cs
Created: 2015, 12, 14, 1:43 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Terminal.Layout
{
	using System;
	using System.Text;
	using System.Windows;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Studio.Controls;

	using DevExpress.Xpf.Docking;

	public sealed class LayoutManager : BaseLogReceiver
	{
		private readonly Dictionary<string, LayoutPanel> _panels = new Dictionary<string, LayoutPanel>();

		private DockLayoutManager DockCtl { get; }

		public LayoutManager(DockLayoutManager dockCtl)
		{
			if (dockCtl == null)
				throw new ArgumentNullException(nameof(dockCtl));

			DockCtl = dockCtl;
		}

		public void OpenToolWindow(BaseStudioControl content, bool canClose = true)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			var item = _panels.TryGetValue(content.Key);

			if (item == null)
			{
				item = CreatePanel(content.Key, string.Empty, content, canClose);
				item.SetBindings(BaseLayoutItem.CaptionProperty, content, "Title");

				_panels.Add(content.Key, item);
			}

			DockCtl.ActiveLayoutItem = item;
		}

		private LayoutPanel CreatePanel(string key, string title, object content, bool canClose = true)
		{
			var panel = DockCtl.DockController.AddPanel(new Point(100, 100), new Size(400, 300));

			panel.Name = key;
			panel.Caption = title;
			panel.Content = content;
			panel.AllowClose = canClose;

			return panel;
		}

		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			_panels.Clear();

			var controls = storage.GetValue<SettingsStorage[]>("Controls");
			var controlsDict = new Dictionary<string, BaseStudioControl>();

			foreach (var settings in controls)
			{
				try
				{
					var control = LoadBaseStudioControl(settings);
					controlsDict.Add(control.Key, control);
				}
				catch (Exception excp)
				{
					this.AddErrorLog(excp);
				}
			}

			var dockLayout = storage.GetValue<string>("Layout");
				
			if (!dockLayout.IsEmpty())
				LoadDockLayout(dockLayout, controlsDict);
		}

		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			var controls = DockCtl.GetItems()
								.OfType<LayoutPanel>()
								.Where(p => !p.IsClosed)
								.Select(p => p.Content)
								.OfType<BaseStudioControl>()
								.ToArray();

			storage.SetValue("Controls", controls.Select(SaveControl).ToArray());

			var layout = SaveDockLayout();
			storage.SetValue("Layout", layout);
		}

		private void LoadDockLayout(string settings, Dictionary<string, BaseStudioControl> controls)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));

			try
			{
				_panels.Clear();

				using(var stream = new MemoryStream(Encoding.UTF8.GetBytes(settings)))
					DockCtl.RestoreLayoutFromStream(stream);

				foreach (var panel in DockCtl.GetItems().OfType<LayoutPanel>())
				{
					if(panel.IsClosed || panel.Name.IsEmpty())
						continue;

					var content = controls.TryGetValue(panel.Name);

					if(content == null)
						continue;

					panel.Content = content;
					panel.SetBindings(BaseLayoutItem.CaptionProperty, panel.Content, "Title");
				}
			}
			catch (Exception excp)
			{
				this.AddErrorLog(excp, LocalizedStrings.Str3649);
			}
		}

		public string SaveDockLayout()
		{
			using (var stream = new MemoryStream())
			{
				DockCtl.SaveLayoutToStream(stream);
				stream.Position = 0;

				return Encoding.UTF8.GetString(stream.ReadBuffer());
			}
		}

		private SettingsStorage SaveControl(BaseStudioControl control)
		{
			var storage = new SettingsStorage();

			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				storage.SetValue("ControlType", control.GetType().GetTypeName(false));
				control.Save(storage);
			});

			return storage;
		}

		private static BaseStudioControl LoadBaseStudioControl(SettingsStorage settings)
		{
			var type = settings.GetValue<Type>("ControlType");
			var control = (BaseStudioControl)Activator.CreateInstance(type);

			control.Load(settings);

			return control;
		}
	}
}
