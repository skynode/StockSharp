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
using System.Windows.Input;
using StockSharp.Logging;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Common;
using Ecng.Localization;
using Ecng.Xaml;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Services;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		public const string LayoutFile = "layout.xml";
		private readonly ConnectorService _connectorService;

		public static MainWindow Instance { get; private set; }

		public MainWindow()
		{
			LocalizedStrings.ActiveLanguage = Languages.English;
			ConfigManager.RegisterService<IStudioCommandService>(new TerminalCommandService());

			InitializeComponent();
			Instance = this;

			Title = Title.Put("S# Terminal");

			_connectorService = new ConnectorService();
			_connectorService.ChangeConnectStatusEvent += ChangeConnectStatusEvent;
			_connectorService.ErrorEvent += OnError;

			var logManager = ConfigManager.GetService<LogManager>();
			logManager.Application.LogLevel = LogLevels.Debug;

			logManager.Listeners.Add(new FileLogListener("Terminal.log"));

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			Loaded += (sender, args) =>
			{
				cmdSvc.Register<RequestBindSource>(this, true, cmd => new BindConnectorCommand(ConfigManager.GetService<IConnector>(), cmd.Control).SyncProcess(this));
				_connectorService.InitConnector();
			};

			Closing += (sender, args) =>
			{
				new XmlSerializer<SettingsStorage>().Serialize(_workAreaControl.Save(), LayoutFile);
			};

			WindowState = WindowState.Maximized;

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
			this.GuiAsync(() => ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect);
		}

		private void OnError(string message, string caption)
		{
			this.GuiAsync(() => MessageBox.Show(this, message, caption));
		}

		private void LookupCode_OnKeyDown(object sender, KeyEventArgs e)
		{
			if(e.Key == Key.Enter)
				LookupSecurities();
		}

		private void LookupButton_OnClick(object sender, RoutedEventArgs e)
		{
			LookupSecurities();
		}

		private void LookupSecurities()
		{
			new LookupSecuritiesCommand(new Security
			{
				Code = LookupCode.Text.Trim(),
				Type = LookupType.SelectedType,
			}).Process(this);
		}
	}
}