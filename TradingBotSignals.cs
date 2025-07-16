#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// Enhanced Multi-Strategy Signal Indicator for TradingBot
	/// Combines multiple technical analysis methods for comprehensive market analysis
	/// </summary>
	public class TradingBotSignals : Indicator
	{
		#region Variables
		private EMA fastEMA;
		private EMA slowEMA;
		private RSI rsi;
		private MACD macd;
		private Bollinger bollinger;
		private Stochastics stochastic;
		private Williams williamsR;
		private CCI cci;
		
		private Series<double> trendSignal;
		private Series<double> momentumSignal;
		private Series<double> volatilitySignal;
		private Series<double> compositeSignal;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Enhanced multi-strategy signal indicator for comprehensive market analysis";
				Name						= "TradingBotSignals";
				Calculate					= Calculate.OnBarClose;
				IsOverlay					= false;
				DisplayInDataBox			= true;
				DrawOnPricePanel			= false;
				DrawHorizontalGridLines		= true;
				DrawVerticalGridLines		= true;
				PaintPriceMarkers			= true;
				ScaleJustification			= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive	= true;
				
				// Parameters
				FastPeriod		= 9;
				SlowPeriod		= 21;
				RSIPeriod		= 14;
				MACDFast		= 12;
				MACDSlow		= 26;
				MACDSmooth		= 9;
				BollingerPeriod	= 20;
				StdDev			= 2.0;
				StochasticK		= 14;
				StochasticD		= 3;
				WilliamsRPeriod	= 14;
				CCIPeriod		= 14;
				
				AddPlot(Brushes.Blue, "TrendSignal");
				AddPlot(Brushes.Red, "MomentumSignal");
				AddPlot(Brushes.Green, "VolatilitySignal");
				AddPlot(Brushes.Purple, "CompositeSignal");
				
				AddLine(Brushes.Gray, 0, "Zero");
				AddLine(Brushes.DarkBlue, 50, "Bullish");
				AddLine(Brushes.DarkRed, -50, "Bearish");
			}
			else if (State == State.DataLoaded)
			{
				// Initialize indicators
				fastEMA = EMA(FastPeriod);
				slowEMA = EMA(SlowPeriod);
				rsi = RSI(RSIPeriod, 3);
				macd = MACD(MACDFast, MACDSlow, MACDSmooth);
				bollinger = Bollinger(BollingerPeriod, StdDev);
				stochastic = Stochastics(StochasticK, StochasticD, 3);
				williamsR = Williams(WilliamsRPeriod);
				cci = CCI(CCIPeriod);
				
				// Initialize series
				trendSignal = new Series<double>(this);
				momentumSignal = new Series<double>(this);
				volatilitySignal = new Series<double>(this);
				compositeSignal = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBars[0] < Math.Max(SlowPeriod, Math.Max(RSIPeriod, BollingerPeriod)))
				return;

			// Calculate trend signals
			double trend = CalculateTrendSignal();
			trendSignal[0] = trend;
			Values[0][0] = trend;

			// Calculate momentum signals
			double momentum = CalculateMomentumSignal();
			momentumSignal[0] = momentum;
			Values[1][0] = momentum;

			// Calculate volatility signals
			double volatility = CalculateVolatilitySignal();
			volatilitySignal[0] = volatility;
			Values[2][0] = volatility;

			// Calculate composite signal
			double composite = (trend + momentum + volatility) / 3.0;
			compositeSignal[0] = composite;
			Values[3][0] = composite;
		}

		#region Signal Calculation Methods
		private double CalculateTrendSignal()
		{
			double signal = 0;
			
			// EMA trend
			if (fastEMA[0] > slowEMA[0])
				signal += 30;
			else
				signal -= 30;
				
			// EMA momentum
			if (fastEMA[0] > fastEMA[1] && slowEMA[0] > slowEMA[1])
				signal += 20;
			else if (fastEMA[0] < fastEMA[1] && slowEMA[0] < slowEMA[1])
				signal -= 20;
				
			// Price relative to EMAs
			if (Close[0] > fastEMA[0] && Close[0] > slowEMA[0])
				signal += 15;
			else if (Close[0] < fastEMA[0] && Close[0] < slowEMA[0])
				signal -= 15;
				
			return Math.Max(-100, Math.Min(100, signal));
		}

		private double CalculateMomentumSignal()
		{
			double signal = 0;
			
			// RSI signals
			if (rsi[0] > 70)
				signal -= 25;
			else if (rsi[0] < 30)
				signal += 25;
			else if (rsi[0] > 50)
				signal += 10;
			else
				signal -= 10;
				
			// MACD signals
			if (macd[0] > macd.Avg[0])
				signal += 20;
			else
				signal -= 20;
				
			// Stochastic signals
			if (stochastic.K[0] > 80)
				signal -= 15;
			else if (stochastic.K[0] < 20)
				signal += 15;
				
			// Williams %R signals
			if (williamsR[0] > -20)
				signal -= 10;
			else if (williamsR[0] < -80)
				signal += 10;
				
			// CCI signals
			if (cci[0] > 100)
				signal -= 15;
			else if (cci[0] < -100)
				signal += 15;
				
			return Math.Max(-100, Math.Min(100, signal));
		}

		private double CalculateVolatilitySignal()
		{
			double signal = 0;
			
			// Bollinger position
			double bbPosition = (Close[0] - bollinger.Lower[0]) / (bollinger.Upper[0] - bollinger.Lower[0]);
			
			if (bbPosition > 0.8)
				signal -= 30;
			else if (bbPosition < 0.2)
				signal += 30;
			else if (bbPosition > 0.5)
				signal += 10;
			else
				signal -= 10;
				
			// Bollinger squeeze detection
			double bbWidth = (bollinger.Upper[0] - bollinger.Lower[0]) / bollinger.Middle[0];
			double avgBBWidth = 0;
			for (int i = 0; i < 10; i++)
			{
				avgBBWidth += (bollinger.Upper[i] - bollinger.Lower[i]) / bollinger.Middle[i];
			}
			avgBBWidth /= 10;
			
			if (bbWidth < avgBBWidth * 0.8) // Squeeze
				signal += 20; // Expect breakout
			else if (bbWidth > avgBBWidth * 1.2) // Expansion
				signal -= 10; // Expect consolidation
				
			return Math.Max(-100, Math.Min(100, signal));
		}
		#endregion

		#region Public Methods
		public double GetTrendSignal(int barsAgo = 0)
		{
			return trendSignal[barsAgo];
		}

		public double GetMomentumSignal(int barsAgo = 0)
		{
			return momentumSignal[barsAgo];
		}

		public double GetVolatilitySignal(int barsAgo = 0)
		{
			return volatilitySignal[barsAgo];
		}

		public double GetCompositeSignal(int barsAgo = 0)
		{
			return compositeSignal[barsAgo];
		}

		public bool IsStrongBullish(int barsAgo = 0)
		{
			return compositeSignal[barsAgo] > 50;
		}

		public bool IsStrongBearish(int barsAgo = 0)
		{
			return compositeSignal[barsAgo] < -50;
		}

		public bool IsTrendingUp(int barsAgo = 0)
		{
			return trendSignal[barsAgo] > 0;
		}

		public bool IsTrendingDown(int barsAgo = 0)
		{
			return trendSignal[barsAgo] < 0;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fast Period", Description="Fast EMA period", Order=1, GroupName="Trend")]
		public int FastPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Slow Period", Description="Slow EMA period", Order=2, GroupName="Trend")]
		public int SlowPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI Period", Description="RSI period", Order=3, GroupName="Momentum")]
		public int RSIPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Fast", Description="MACD fast period", Order=4, GroupName="Momentum")]
		public int MACDFast { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Slow", Description="MACD slow period", Order=5, GroupName="Momentum")]
		public int MACDSlow { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Smooth", Description="MACD smooth period", Order=6, GroupName="Momentum")]
		public int MACDSmooth { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Bollinger Period", Description="Bollinger bands period", Order=7, GroupName="Volatility")]
		public int BollingerPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name="Std Dev", Description="Standard deviation multiplier", Order=8, GroupName="Volatility")]
		public double StdDev { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stochastic K", Description="Stochastic %K period", Order=9, GroupName="Momentum")]
		public int StochasticK { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stochastic D", Description="Stochastic %D period", Order=10, GroupName="Momentum")]
		public int StochasticD { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Williams R Period", Description="Williams %R period", Order=11, GroupName="Momentum")]
		public int WilliamsRPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CCI Period", Description="CCI period", Order=12, GroupName="Momentum")]
		public int CCIPeriod { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradingBotSignals[] cacheTradingBotSignals;

		public TradingBotSignals TradingBotSignals()
		{
			return TradingBotSignals(Input);
		}

		public TradingBotSignals TradingBotSignals(ISeries<double> input)
		{
			if (cacheTradingBotSignals != null)
				for (int idx = 0; idx < cacheTradingBotSignals.Length; idx++)
					if (cacheTradingBotSignals[idx] != null &&  cacheTradingBotSignals[idx].EqualsInput(input))
						return cacheTradingBotSignals[idx];
			return CacheIndicator<TradingBotSignals>(new TradingBotSignals(), input, ref cacheTradingBotSignals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		private TradingBotSignals[] cacheTradingBotSignals;

		public TradingBotSignals TradingBotSignals()
		{
			return TradingBotSignals(Input);
		}

		public TradingBotSignals TradingBotSignals(ISeries<double> input)
		{
			if (cacheTradingBotSignals != null)
				for (int idx = 0; idx < cacheTradingBotSignals.Length; idx++)
					if (cacheTradingBotSignals[idx] != null && cacheTradingBotSignals[idx].EqualsInput(input))
						return cacheTradingBotSignals[idx];
			return CacheIndicator<TradingBotSignals>(new TradingBotSignals(), input, ref cacheTradingBotSignals);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		private TradingBotSignals[] cacheTradingBotSignals;

		public TradingBotSignals TradingBotSignals()
		{
			return TradingBotSignals(Input);
		}

		public TradingBotSignals TradingBotSignals(ISeries<double> input)
		{
			if (cacheTradingBotSignals != null)
				for (int idx = 0; idx < cacheTradingBotSignals.Length; idx++)
					if (cacheTradingBotSignals[idx] != null && cacheTradingBotSignals[idx].EqualsInput(input))
						return cacheTradingBotSignals[idx];
			return CacheIndicator<TradingBotSignals>(new TradingBotSignals(), input, ref cacheTradingBotSignals);
		}
	}
}

#endregion