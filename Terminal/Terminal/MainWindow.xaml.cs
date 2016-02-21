#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using DevExpress.Xpf.Bars;
using StockSharp.Logging;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Localization;
using Ecng.Xaml;
using StockSharp.Alerts;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Studio.Controls;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Controls;
using StockSharp.Terminal.Services;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		public const string LayoutFile = "layout.xml";
		private readonly ConnectorService _connectorService;

		public static MainWindow Instance { get; private set; }

		public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register("IsConnected", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		public static readonly Type[] ControlTypes = {
			typeof(TradesPanel),
			typeof(MyTradesTable),
			typeof(OrdersPanel),
			typeof(SecuritiesPanel),
			typeof(ScalpingMarketDepthControl),
			typeof(NewsPanel),
			typeof(PortfoliosPanel),
			typeof(CandleChartPanel),
		};

		public MainWindow()
		{
			LocalizedStrings.ActiveLanguage = Languages.English;
			ConfigManager.RegisterService<IStudioCommandService>(new TerminalCommandService());
			ConfigManager.RegisterService<IAlertService>(new AlertService("AlertSvc"));

			InitializeComponent();
			Instance = this;

			Title = Title.Put("S# Terminal");

			var logManager = ConfigManager.GetService<LogManager>();
			logManager.Application.LogLevel = LogLevels.Debug;

			logManager.Listeners.Add(new FileLogListener("Terminal.log"));

			_connectorService = new ConnectorService();
			_connectorService.ChangeConnectStatusEvent += ChangeConnectStatusEvent;

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<ErrorCommand>(this, true, cmd => OnError(cmd.ToString(), "Error"));

			Loaded += (sender, args) =>
			{
	            if(Properties.Settings.Default.WindowMaximized)
					WindowState = WindowState.Maximized;

				cmdSvc.Register<RequestBindSource>(this, true, cmd => new BindConnectorCommand(ConfigManager.GetService<IConnector>(), cmd.Control).SyncProcess(this));
				_connectorService.InitConnector();

				foreach (var c in ControlTypes)
				{
					var ctlType = c;

					_ribbonGroupWindows.Items.Add(new BarButtonItem
					{
						Glyph = new BitmapImage(ctlType.GetIconUrl()),
						Content = ctlType.GetDisplayName(),
						Command = new DelegateCommand(o => OnCreateWindowClick(ctlType))
					});
				}
			};

			Closing += (sender, args) =>
			{
		        Properties.Settings.Default.WindowMaximized = WindowState == WindowState.Maximized;
	            Properties.Settings.Default.Save();
				new XmlSerializer<SettingsStorage>().Serialize(_workAreaControl.Save(), LayoutFile);
			};

			try
			{
				if (File.Exists(LayoutFile))
					_workAreaControl.Load(new XmlSerializer<SettingsStorage>().Deserialize(LayoutFile));
			}
			catch (Exception ex)
			{
				OnError(ex.ToString(), $"Ошибка при чтении файла {LayoutFile}");
			}
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			_connectorService.Configure(this);
			new XmlSerializer<SettingsStorage>().Serialize(_connectorService.Save(), ConnectorService.SETTINGS_FILE);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_connectorService.IsConnected)
				_connectorService.Connect();
			else
				_connectorService.Disconnect();
		}

		private void ChangeConnectStatusEvent(bool isConnected)
		{
			this.GuiAsync(() => IsConnected = isConnected);
		}

		private void OnError(string message, string caption)
		{
			this.GuiAsync(() => MessageBox.Show(this, message, caption));
		}

		private void LookupButton_OnClick(object sender, RoutedEventArgs e)
		{
			var w = new FindSecurityWindow();

			if(w.ShowModal())
				new LookupSecuritiesCommand(w.Criteria).Process(this);
		}

		private void OnCreateWindowClick(Type ctlType)
		{
			if(ctlType != null)
				_workAreaControl.HandleNewPanelSelection(ctlType);
		}
	}
}