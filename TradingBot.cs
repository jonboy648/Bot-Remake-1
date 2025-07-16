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
		
		private bool isLongPositionActive = false;
		private bool isShortPositionActive = false;
		private DateTime lastTradeTime;
		private int barsSinceEntry = 0;
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
			}
			else if (State == State.DataLoaded)
			{
				// Initialize indicators
				fastEMA = EMA(FastEMAPeriod);
				slowEMA = EMA(SlowEMAPeriod);
				atr = ATR(ATRPeriod);
				
				// Add indicators to chart for visualization
				AddChartIndicator(fastEMA);
				AddChartIndicator(slowEMA);
				
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
				Print(string.Format("{0}: Strategy terminated at {1}", Name, DateTime.Now));
			}
		}
		#endregion

		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
			// Ensure minimum bars for indicators
			if (CurrentBars[0] < Math.Max(SlowEMAPeriod, ATRPeriod))
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
			// EMA crossover strategy with ATR-based stops
			bool bullishSignal = fastEMA[0] > slowEMA[0] && fastEMA[1] <= slowEMA[1];
			bool bearishSignal = fastEMA[0] < slowEMA[0] && fastEMA[1] >= slowEMA[1];

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
				   (DateTime.Now - lastTradeTime).TotalMinutes > 30 &&
				   Close[0] > Open[0]; // Bullish candle
		}

		private bool CanEnterShort()
		{
			// Additional filters for short entries
			return !isShortPositionActive && 
				   (DateTime.Now - lastTradeTime).TotalMinutes > 30 &&
				   Close[0] < Open[0]; // Bearish candle
		}

		private void EnterLongPosition()
		{
			double stopPrice = Close[0] - (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] + (atr[0] * ATRMultiplier * 1.5);
			
			int quantity = CalculatePositionSize(Close[0] - stopPrice);
			
			entryOrder = EnterLong(quantity, "LongEntry");
			
			if (entryOrder != null)
			{
				stopLoss = ExitLongStopMarket(0, true, quantity, stopPrice, "StopLoss", "LongEntry");
				profitTarget = ExitLongLimit(0, true, quantity, targetPrice, "ProfitTarget", "LongEntry");
				
				isLongPositionActive = true;
				lastTradeTime = Time[0];
				barsSinceEntry = 0;
				
				Print(string.Format("{0}: Long entry at {1}, Stop: {2}, Target: {3}", 
					Time[0], Close[0], stopPrice, targetPrice));
			}
		}

		private void EnterShortPosition()
		{
			double stopPrice = Close[0] + (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] - (atr[0] * ATRMultiplier * 1.5);
			
			int quantity = CalculatePositionSize(stopPrice - Close[0]);
			
			entryOrder = EnterShort(quantity, "ShortEntry");
			
			if (entryOrder != null)
			{
				stopLoss = ExitShortStopMarket(0, true, quantity, stopPrice, "StopLoss", "ShortEntry");
				profitTarget = ExitShortLimit(0, true, quantity, targetPrice, "ProfitTarget", "ShortEntry");
				
				isShortPositionActive = true;
				lastTradeTime = Time[0];
				barsSinceEntry = 0;
				
				Print(string.Format("{0}: Short entry at {1}, Stop: {2}, Target: {3}", 
					Time[0], Close[0], stopPrice, targetPrice));
			}
		}

		private void ManageActivePosition()
		{
			// Trail stops or implement additional exit logic here
			if (barsSinceEntry > 50) // Max bars in position
			{
				if (Position.MarketPosition == MarketPosition.Long)
					ExitLong("TimeExit");
				else if (Position.MarketPosition == MarketPosition.Short)
					ExitShort("TimeExit");
			}
		}
		#endregion

		#region Risk Management
		private bool PassesRiskChecks()
		{
			// Check daily loss limit
			if (SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit < -(Account.CashValue * MaxDailyLoss / 100))
			{
				Print("Daily loss limit reached. Trading halted.");
				return false;
			}

			// Check maximum position size
			if (Position.Quantity >= Account.CashValue * 0.1) // Max 10% of account
			{
				Print("Position size limit reached.");
				return false;
			}

			return true;
		}

		private int CalculatePositionSize(double riskPerShare)
		{
			if (riskPerShare <= 0)
				return 1;

			double accountRisk = Account.CashValue * (RiskPerTradePercent / 100);
			int shares = (int)(accountRisk / riskPerShare);
			
			return Math.Max(1, Math.Min(shares, 1000)); // Min 1, Max 1000 shares
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
					if (cacheTradingBot[idx] != null &&  cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheIndicator<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
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
					if (cacheTradingBot[idx] != null &&  cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheIndicator<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
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
					if (cacheTradingBot[idx] != null &&  cacheTradingBot[idx].EqualsInput(input))
						return cacheTradingBot[idx];
			return CacheIndicator<Strategies.TradingBot>(new Strategies.TradingBot(), input, ref cacheTradingBot);
		}
	}
}

#endregion