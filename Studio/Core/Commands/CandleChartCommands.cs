using System;
using System.Collections.Generic;
using StockSharp.Algo.Candles;
using System.Windows.Controls;

namespace StockSharp.Studio.Core.Commands {
	public class SubscribeCandleChartCommand : BaseStudioCommand {
		public CandleSeries Series {get;}
		public Control Control {get;}

		public SubscribeCandleChartCommand(CandleSeries ser, Control control)
		{
			Series = ser;
			Control = control;
		}
	}

	public class ChartDataSubscriptionCommand : BaseStudioCommand {
		public CandleSeries Series {get;}
		public Func<IEnumerable<TimeFrameCandle>> AllCandlesGetter {get;} // tmp solution for indicators
		public Control Control {get;}

		public ChartDataSubscriptionCommand(CandleSeries ser, Func<IEnumerable<TimeFrameCandle>> getter, Control control)
		{
			Series = ser;
			AllCandlesGetter = getter;
			Control = control;
		}
	}

	public class UnsubscribeCandleChartCommand : BaseStudioCommand {
		public CandleSeries Series {get;}

		public UnsubscribeCandleChartCommand(CandleSeries ser)
		{
			Series = ser;
		}
	}

	public class CandleDataCommand : BaseStudioCommand
	{
		public CandleSeries Series {get;}
		public IEnumerable<TimeFrameCandle> Candles {get;}

		public CandleDataCommand(CandleSeries series, IEnumerable<TimeFrameCandle> candles)
		{
			Series = series;
			Candles = candles;
		}
	}
}
