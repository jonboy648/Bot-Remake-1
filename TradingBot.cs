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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	/// <summary>
	/// NinjaTrader 8 Compliant Trading Bot Strategy
	/// Implements proper NT8 lifecycle, order management, and risk controls
	/// </summary>
	public class TradingBot : Strategy
	{
		#region Variables
		private Order entryOrder = null;
		private Order stopLoss = null;
		private Order profitTarget = null;
		
		private EMA fastEMA;
		private EMA slowEMA;
		private ATR atr;
		private RSI rsi;
		private MACD macd;
		private Bollinger bollinger;
		
		private bool isLongPositionActive = false;
		private bool isShortPositionActive = false;
		private DateTime lastTradeTime;
		private int barsSinceEntry = 0;
		private double dailyStartingCash = 0;
		private DateTime currentTradingDay = DateTime.MinValue;
		#endregion

		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"NinjaTrader 8 compliant automated trading strategy with proper risk management and order handling.";
				Name										= "TradingBot";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				
				// Strategy Parameters - Following NT8 conventions
				FastEMAPeriod								= 9;
				SlowEMAPeriod								= 21;
				ATRPeriod									= 14;
				ATRMultiplier								= 2.0;
				RiskPerTradePercent							= 1.0;
				MaxDailyLoss								= 3.0;
				TradingStartTime							= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				TradingEndTime								= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				AllowReversals								= false;
				MinutesBetweenTrades						= 30;
				MaxBarsInPosition							= 50;
				MaxPositionSize								= 1000;
				MaxAccountExposurePercent					= 10.0;
				RSIPeriod									= 14;
				RSIOverbought								= 70;
				RSIOversold									= 30;
				MACDFast									= 12;
				MACDSlow									= 26;
				MACDSmooth									= 9;
				BollingerPeriod								= 20;
				BollingerStdDev								= 2.0;
				UseRSIFilter								= true;
				UseMACDFilter								= true;
				UseBollingerFilter							= false;
			}
			else if (State == State.DataLoaded)
			{
				// Initialize indicators
				fastEMA = EMA(FastEMAPeriod);
				slowEMA = EMA(SlowEMAPeriod);
				atr = ATR(ATRPeriod);
				rsi = RSI(RSIPeriod, 3);
				macd = MACD(MACDFast, MACDSlow, MACDSmooth);
				bollinger = Bollinger(BollingerPeriod, BollingerStdDev);
				
				// Add indicators to chart for visualization
				AddChartIndicator(fastEMA);
				AddChartIndicator(slowEMA);
				AddChartIndicator(rsi);
				AddChartIndicator(macd);
				AddChartIndicator(bollinger);
				
				// Initialize tracking variables
				lastTradeTime = DateTime.MinValue;
			}
			else if (State == State.Historical)
			{
				// Historical data processing setup
				SetProfitTarget("", CalculationMode.Ticks, 0);
				SetStopLoss("", CalculationMode.Ticks, 0, false);
			}
			else if (State == State.Transition)
			{
				// Transition from historical to real-time
				if (BarsInProgress == 0)
				{
					Print(string.Format("{0}: Strategy transitioning from historical to real-time at {1}", 
						Name, Time[0]));
				}
			}
			else if (State == State.Realtime)
			{
				// Real-time initialization
				if (BarsInProgress == 0)
				{
					Print(string.Format("{0}: Strategy now running in real-time at {1}", 
						Name, Time[0]));
				}
			}
			else if (State == State.Terminated)
			{
				// Cleanup when strategy terminates
				Print(string.Format("{0}: Strategy terminated at {1}", Name, Time[0]));
			}
		}
		#endregion

		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
			// Ensure minimum bars for indicators
			int minBars = Math.Max(Math.Max(SlowEMAPeriod, ATRPeriod), Math.Max(RSIPeriod, BollingerPeriod));
			if (CurrentBars[0] < minBars)
				return;

			// Check trading hours
			if (!IsInTradingHours())
				return;

			// Update position tracking
			UpdatePositionStatus();

			// Risk management checks
			if (!PassesRiskChecks())
				return;

			// Main trading logic
			ExecuteTradingLogic();
		}
		#endregion

		#region Trading Logic Methods
		private void ExecuteTradingLogic()
		{
			// EMA crossover strategy with additional filters
			bool bullishSignal = fastEMA[0] > slowEMA[0] && fastEMA[1] <= slowEMA[1];
			bool bearishSignal = fastEMA[0] < slowEMA[0] && fastEMA[1] >= slowEMA[1];

			// Apply additional filters if enabled
			if (UseRSIFilter)
			{
				bullishSignal = bullishSignal && rsi[0] < RSIOverbought && rsi[0] > RSIOversold;
				bearishSignal = bearishSignal && rsi[0] > RSIOversold && rsi[0] < RSIOverbought;
			}

			if (UseMACDFilter)
			{
				bullishSignal = bullishSignal && macd[0] > macd.Avg[0];
				bearishSignal = bearishSignal && macd[0] < macd.Avg[0];
			}

			if (UseBollingerFilter)
			{
				// Only trade when price is not at extreme bands
				bullishSignal = bullishSignal && Close[0] > bollinger.Lower[0] && Close[0] < bollinger.Upper[0];
				bearishSignal = bearishSignal && Close[0] > bollinger.Lower[0] && Close[0] < bollinger.Upper[0];
			}

			// Entry conditions
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (bullishSignal && CanEnterLong())
				{
					EnterLongPosition();
				}
				else if (bearishSignal && CanEnterShort())
				{
					EnterShortPosition();
				}
			}
			
			// Position management
			if (Position.MarketPosition != MarketPosition.Flat)
			{
				barsSinceEntry++;
				ManageActivePosition();
			}
		}

		private bool CanEnterLong()
		{
			// Additional filters for long entries
			return !isLongPositionActive && 
				   (Time[0] - lastTradeTime).TotalMinutes > MinutesBetweenTrades &&
				   Close[0] > Open[0]; // Bullish candle
		}

		private bool CanEnterShort()
		{
			// Additional filters for short entries
			return !isShortPositionActive && 
				   (Time[0] - lastTradeTime).TotalMinutes > MinutesBetweenTrades &&
				   Close[0] < Open[0]; // Bearish candle
		}

		private void EnterLongPosition()
		{
			// Validate ATR value
			if (atr[0] <= 0)
			{
				Print("Warning: Invalid ATR value, skipping trade.");
				return;
			}
			
			double stopPrice = Close[0] - (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] + (atr[0] * ATRMultiplier * 1.5);
			
			// Validate prices
			if (stopPrice >= Close[0] || targetPrice <= Close[0])
			{
				Print("Warning: Invalid stop/target prices, skipping trade.");
				return;
			}
			
			int quantity = CalculatePositionSize(Close[0] - stopPrice);
			
			if (quantity <= 0)
			{
				Print("Warning: Invalid position size calculated, skipping trade.");
				return;
			}
			
			entryOrder = EnterLong(quantity, "LongEntry");
			
			if (entryOrder != null)
			{
				stopLoss = ExitLongStopMarket(0, true, quantity, stopPrice, "StopLoss", "LongEntry");
				profitTarget = ExitLongLimit(0, true, quantity, targetPrice, "ProfitTarget", "LongEntry");
				
				isLongPositionActive = true;
				lastTradeTime = Time[0];
				barsSinceEntry = 0;
				
				Print(string.Format("{0}: Long entry at {1:F2}, Stop: {2:F2}, Target: {3:F2}, Qty: {4}", 
					Time[0], Close[0], stopPrice, targetPrice, quantity));
			}
			else
			{
				Print("Error: Failed to place long entry order.");
			}
		}

		private void EnterShortPosition()
		{
			// Validate ATR value
			if (atr[0] <= 0)
			{
				Print("Warning: Invalid ATR value, skipping trade.");
				return;
			}
			
			double stopPrice = Close[0] + (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] - (atr[0] * ATRMultiplier * 1.5);
			
			// Validate prices
			if (stopPrice <= Close[0] || targetPrice >= Close[0])
			{
				Print("Warning: Invalid stop/target prices, skipping trade.");
				return;
			}
			
			int quantity = CalculatePositionSize(stopPrice - Close[0]);
			
			if (quantity <= 0)
			{
				Print("Warning: Invalid position size calculated, skipping trade.");
				return;
			}
			
			entryOrder = EnterShort(quantity, "ShortEntry");
			
			if (entryOrder != null)
			{
				stopLoss = ExitShortStopMarket(0, true, quantity, stopPrice, "StopLoss", "ShortEntry");
				profitTarget = ExitShortLimit(0, true, quantity, targetPrice, "ProfitTarget", "ShortEntry");
				
				isShortPositionActive = true;
				lastTradeTime = Time[0];
				barsSinceEntry = 0;
				
				Print(string.Format("{0}: Short entry at {1:F2}, Stop: {2:F2}, Target: {3:F2}, Qty: {4}", 
					Time[0], Close[0], stopPrice, targetPrice, quantity));
			}
			else
			{
				Print("Error: Failed to place short entry order.");
			}
		}

		private void ManageActivePosition()
		{
			// Time-based exit using configurable parameter
			if (barsSinceEntry > MaxBarsInPosition)
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					ExitLong("TimeExit");
					Print(string.Format("{0}: Time-based long exit after {1} bars", Time[0], barsSinceEntry));
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					ExitShort("TimeExit");
					Print(string.Format("{0}: Time-based short exit after {1} bars", Time[0], barsSinceEntry));
				}
			}
			
			// Reversal logic if enabled
			if (AllowReversals)
			{
				bool bullishSignal = fastEMA[0] > slowEMA[0] && fastEMA[1] <= slowEMA[1];
				bool bearishSignal = fastEMA[0] < slowEMA[0] && fastEMA[1] >= slowEMA[1];
				
				if (Position.MarketPosition == MarketPosition.Long && bearishSignal)
				{
					ExitLong("ReversalExit");
					Print(string.Format("{0}: Reversal signal - exiting long position", Time[0]));
				}
				else if (Position.MarketPosition == MarketPosition.Short && bullishSignal)
				{
					ExitShort("ReversalExit");
					Print(string.Format("{0}: Reversal signal - exiting short position", Time[0]));
				}
			}
		}
		#endregion

		#region Risk Management
		private bool PassesRiskChecks()
		{
			// Reset daily tracking if new trading day
			if (currentTradingDay.Date != Time[0].Date)
			{
				currentTradingDay = Time[0].Date;
				dailyStartingCash = Account.CashValue;
			}

			// Check daily loss limit
			double dailyPnL = Account.CashValue - dailyStartingCash;
			double maxDailyLossAmount = dailyStartingCash * (MaxDailyLoss / 100);
			
			if (dailyPnL < -maxDailyLossAmount)
			{
				Print(string.Format("Daily loss limit reached. P&L: {0:C}, Limit: {1:C}. Trading halted.", 
					dailyPnL, -maxDailyLossAmount));
				return false;
			}

			// Check maximum position size (convert to currency value for proper comparison)
			double currentPositionValue = Math.Abs(Position.Quantity * Position.AveragePrice);
			double maxPositionValue = Account.CashValue * (MaxAccountExposurePercent / 100);
			
			if (currentPositionValue >= maxPositionValue)
			{
				Print(string.Format("Position size limit reached. Current: {0:C}, Max: {1:C}", 
					currentPositionValue, maxPositionValue));
				return false;
			}

			return true;
		}

		private int CalculatePositionSize(double riskPerShare)
		{
			if (riskPerShare <= 0)
			{
				Print("Warning: Invalid risk per share, using minimum position size.");
				return 1;
			}

			double accountRisk = Account.CashValue * (RiskPerTradePercent / 100);
			int shares = (int)(accountRisk / riskPerShare);
			
			// Apply limits and validation
			shares = Math.Max(1, shares); // Minimum 1 share
			shares = Math.Min(shares, MaxPositionSize); // Maximum configurable size
			
			// Additional safety check - don't exceed 10% of available cash
			double maxSharesByValue = (int)((Account.CashValue * 0.1) / Close[0]);
			shares = Math.Min(shares, (int)maxSharesByValue);
			
			Print(string.Format("Position size calculated: {0} shares, Risk per share: {1:C}, Account risk: {2:C}", 
				shares, riskPerShare, accountRisk));
			
			return shares;
		}

		private bool IsInTradingHours()
		{
			TimeSpan currentTime = Time[0].TimeOfDay;
			return currentTime >= TradingStartTime.TimeOfDay && 
				   currentTime <= TradingEndTime.TimeOfDay;
		}

		private void UpdatePositionStatus()
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				isLongPositionActive = false;
				isShortPositionActive = false;
				barsSinceEntry = 0;
			}
		}
		#endregion

		#region OnOrderUpdate
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
		{
			// Handle order updates following NT8 best practices
			if (order.Name == "LongEntry" || order.Name == "ShortEntry")
			{
				entryOrder = order;
				
				if (orderState == OrderState.Cancelled && Position.MarketPosition == MarketPosition.Flat)
				{
					entryOrder = null;
					stopLoss = null;
					profitTarget = null;
				}
			}
			else if (order.Name == "StopLoss")
			{
				stopLoss = order;
			}
			else if (order.Name == "ProfitTarget")
			{
				profitTarget = order;
			}

			// Log order events for debugging
			Print(string.Format("{0}: Order Update - {1}, State: {2}, Filled: {3}", 
				time, order.Name, orderState, filled));
		}
		#endregion

		#region OnExecutionUpdate
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			// Handle execution updates
			if (execution.Order != null)
			{
				Print(string.Format("{0}: Execution - {1}, Price: {2}, Quantity: {3}", 
					time, execution.Order.Name, price, quantity));
			}
		}
		#endregion

		#region Properties - Following NT8 Parameter Conventions
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fast EMA Period", Description="Period for fast EMA", Order=1, GroupName="Indicators")]
		public int FastEMAPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Slow EMA Period", Description="Period for slow EMA", Order=2, GroupName="Indicators")]
		public int SlowEMAPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Period", Description="Period for ATR calculation", Order=3, GroupName="Indicators")]
		public int ATRPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="ATR Multiplier", Description="Multiplier for ATR-based stops", Order=4, GroupName="Risk Management")]
		public double ATRMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="Risk Per Trade %", Description="Risk percentage per trade", Order=5, GroupName="Risk Management")]
		public double RiskPerTradePercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 50.0)]
		[Display(Name="Max Daily Loss %", Description="Maximum daily loss percentage", Order=6, GroupName="Risk Management")]
		public double MaxDailyLoss { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Trading Start Time", Description="Start time for trading", Order=7, GroupName="Trading Hours")]
		public DateTime TradingStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Trading End Time", Description="End time for trading", Order=8, GroupName="Trading Hours")]
		public DateTime TradingEndTime { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Allow Reversals", Description="Allow position reversals", Order=9, GroupName="Strategy")]
		public bool AllowReversals { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1440)]
		[Display(Name="Minutes Between Trades", Description="Minimum minutes between trades", Order=10, GroupName="Strategy")]
		public int MinutesBetweenTrades { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name="Max Bars In Position", Description="Maximum bars to hold a position", Order=11, GroupName="Strategy")]
		public int MaxBarsInPosition { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10000)]
		[Display(Name="Max Position Size", Description="Maximum position size in shares/contracts", Order=12, GroupName="Risk Management")]
		public int MaxPositionSize { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 50.0)]
		[Display(Name="Max Account Exposure %", Description="Maximum account exposure percentage", Order=13, GroupName="Risk Management")]
		public double MaxAccountExposurePercent { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="RSI Period", Description="Period for RSI calculation", Order=14, GroupName="Indicators")]
		public int RSIPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(50, 100)]
		[Display(Name="RSI Overbought", Description="RSI overbought level", Order=15, GroupName="Indicators")]
		public double RSIOverbought { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name="RSI Oversold", Description="RSI oversold level", Order=16, GroupName="Indicators")]
		public double RSIOversold { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Fast", Description="MACD fast period", Order=17, GroupName="Indicators")]
		public int MACDFast { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Slow", Description="MACD slow period", Order=18, GroupName="Indicators")]
		public int MACDSlow { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MACD Smooth", Description="MACD smoothing period", Order=19, GroupName="Indicators")]
		public int MACDSmooth { get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Bollinger Period", Description="Bollinger bands period", Order=20, GroupName="Indicators")]
		public int BollingerPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name="Bollinger Std Dev", Description="Bollinger bands standard deviation", Order=21, GroupName="Indicators")]
		public double BollingerStdDev { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use RSI Filter", Description="Enable RSI filter for entries", Order=22, GroupName="Filters")]
		public bool UseRSIFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use MACD Filter", Description="Enable MACD filter for entries", Order=23, GroupName="Filters")]
		public bool UseMACDFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use Bollinger Filter", Description="Enable Bollinger bands filter for entries", Order=24, GroupName="Filters")]
		public bool UseBollingerFilter { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Strategies.TradingBot TradingBot()
		{
			return TradingBot(Input);
		}

		public Strategies.TradingBot TradingBot(ISeries<double> input)
		{
			if (cacheTradingBot != null)
				for (int idx = 0; idx < cacheTradingBot.Length; idx++)
					if (cacheTradingBot[idx] != null && cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheStrategy<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Strategies.TradingBot TradingBot()
		{
			return TradingBot(Input);
		}

		public Strategies.TradingBot TradingBot(ISeries<double> input)
		{
			if (cacheTradingBot != null)
				for (int idx = 0; idx < cacheTradingBot.Length; idx++)
					if (cacheTradingBot[idx] != null && cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheStrategy<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
		}
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Strategies.TradingBot[] cacheTradingBot;

		public Strategies.TradingBot TradingBot()
		{
			return TradingBot(Input);
		}

		public Strategies.TradingBot TradingBot(ISeries<double> input)
		{
			if (cacheTradingBot != null)
				for (int idx = 0; idx < cacheTradingBot.Length; idx++)
					if (cacheTradingBot[idx] != null && cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheStrategy<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
		}
	}
}

#endregion