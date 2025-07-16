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
	/// Advanced Multi-Strategy Trading Bot
	/// Implements multiple trading methodologies with advanced risk management
	/// </summary>
	public class AdvancedTradingBot : Strategy
	{
		#region Variables
		private Order entryOrder = null;
		private Order stopLoss = null;
		private Order profitTarget = null;
		
		private TradingBotSignals signals;
		private ATR atr;
		private ADX adx;
		private VMA volumeMA;
		
		private bool isLongPositionActive = false;
		private bool isShortPositionActive = false;
		private DateTime lastTradeTime;
		private int barsSinceEntry = 0;
		private double dailyStartingCash = 0;
		private DateTime currentTradingDay = DateTime.MinValue;
		
		// Performance tracking
		private double totalProfit = 0;
		private int winningTrades = 0;
		private int losingTrades = 0;
		private double maxDrawdown = 0;
		private double highWaterMark = 0;
		
		// Advanced strategy variables
		private StrategyMode currentStrategy = StrategyMode.TrendFollowing;
		private MarketCondition marketCondition = MarketCondition.Neutral;
		#endregion

		#region Enums
		public enum StrategyMode
		{
			TrendFollowing,
			MeanReversion,
			Momentum,
			Adaptive
		}

		public enum MarketCondition
		{
			Trending,
			Ranging,
			Volatile,
			Neutral
		}
		#endregion

		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Advanced multi-strategy trading bot with adaptive market analysis and sophisticated risk management.";
				Name						= "AdvancedTradingBot";
				Calculate					= Calculate.OnBarClose;
				EntriesPerDirection			= 1;
				EntryHandling				= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds	= 30;
				IsFillLimitOnTouch			= false;
				MaximumBarsLookBack			= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution			= OrderFillResolution.Standard;
				Slippage					= 0;
				StartBehavior				= StartBehavior.WaitUntilFlat;
				TimeInForce					= TimeInForce.Gtc;
				TraceOrders					= false;
				RealtimeErrorHandling		= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling			= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade			= 50;
				
				// Strategy Parameters
				ATRPeriod					= 14;
				ATRMultiplier				= 2.0;
				RiskPerTradePercent			= 1.0;
				MaxDailyLoss				= 3.0;
				TradingStartTime			= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				TradingEndTime				= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				MinutesBetweenTrades		= 30;
				MaxBarsInPosition			= 50;
				MaxPositionSize				= 1000;
				MaxAccountExposurePercent	= 10.0;
				
				// Advanced parameters
				TrendThreshold				= 25;
				MomentumThreshold			= 30;
				VolatilityThreshold			= 20;
				ADXThreshold				= 25;
				VolumeMultiplier			= 1.5;
				UseAdaptiveStrategy			= true;
				UseVolumeFilter				= true;
				UseADXFilter				= true;
				MaxConsecutiveLosses		= 3;
				DrawdownLimit				= 5.0;
				
				currentStrategy = StrategyMode.Adaptive;
			}
			else if (State == State.DataLoaded)
			{
				// Initialize indicators
				signals = TradingBotSignals();
				atr = ATR(ATRPeriod);
				adx = ADX(14);
				volumeMA = VMA(20);
				
				// Add indicators to chart
				AddChartIndicator(signals);
				AddChartIndicator(atr);
				AddChartIndicator(adx);
				
				// Initialize tracking variables
				lastTradeTime = DateTime.MinValue;
				highWaterMark = Account.CashValue;
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
					Print(string.Format("{0}: Advanced strategy transitioning to real-time", Time[0]));
				}
			}
			else if (State == State.Realtime)
			{
				// Real-time initialization
				if (BarsInProgress == 0)
				{
					Print(string.Format("{0}: Advanced strategy running in real-time", Time[0]));
				}
			}
			else if (State == State.Terminated)
			{
				// Final performance report
				PrintPerformanceReport();
				Print(string.Format("{0}: Advanced strategy terminated", Time[0]));
			}
		}
		#endregion

		#region OnBarUpdate
		protected override void OnBarUpdate()
		{
			// Ensure minimum bars for indicators
			if (CurrentBars[0] < 50)
				return;

			// Check trading hours
			if (!IsInTradingHours())
				return;

			// Update market analysis
			UpdateMarketCondition();
			
			// Update performance tracking
			UpdatePerformanceMetrics();

			// Update position tracking
			UpdatePositionStatus();

			// Risk management checks
			if (!PassesRiskChecks())
				return;

			// Adaptive strategy selection
			if (UseAdaptiveStrategy)
				SelectOptimalStrategy();

			// Main trading logic
			ExecuteTradingLogic();
		}
		#endregion

		#region Market Analysis
		private void UpdateMarketCondition()
		{
			double trendSignal = signals.GetTrendSignal();
			double volatilitySignal = signals.GetVolatilitySignal();
			double adxValue = adx[0];
			
			if (adxValue > ADXThreshold && Math.Abs(trendSignal) > TrendThreshold)
			{
				marketCondition = MarketCondition.Trending;
			}
			else if (adxValue < ADXThreshold && Math.Abs(volatilitySignal) < VolatilityThreshold)
			{
				marketCondition = MarketCondition.Ranging;
			}
			else if (Math.Abs(volatilitySignal) > VolatilityThreshold * 1.5)
			{
				marketCondition = MarketCondition.Volatile;
			}
			else
			{
				marketCondition = MarketCondition.Neutral;
			}
		}

		private void SelectOptimalStrategy()
		{
			switch (marketCondition)
			{
				case MarketCondition.Trending:
					currentStrategy = StrategyMode.TrendFollowing;
					break;
				case MarketCondition.Ranging:
					currentStrategy = StrategyMode.MeanReversion;
					break;
				case MarketCondition.Volatile:
					currentStrategy = StrategyMode.Momentum;
					break;
				default:
					currentStrategy = StrategyMode.TrendFollowing; // Default
					break;
			}
		}
		#endregion

		#region Trading Logic
		private void ExecuteTradingLogic()
		{
			bool longSignal = false;
			bool shortSignal = false;

			switch (currentStrategy)
			{
				case StrategyMode.TrendFollowing:
					(longSignal, shortSignal) = GetTrendFollowingSignals();
					break;
				case StrategyMode.MeanReversion:
					(longSignal, shortSignal) = GetMeanReversionSignals();
					break;
				case StrategyMode.Momentum:
					(longSignal, shortSignal) = GetMomentumSignals();
					break;
				case StrategyMode.Adaptive:
					(longSignal, shortSignal) = GetAdaptiveSignals();
					break;
			}

			// Apply filters
			if (UseVolumeFilter && !PassesVolumeFilter())
			{
				longSignal = shortSignal = false;
			}

			if (UseADXFilter && adx[0] < ADXThreshold)
			{
				longSignal = shortSignal = false;
			}

			// Entry conditions
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (longSignal && CanEnterLong())
				{
					EnterLongPosition();
				}
				else if (shortSignal && CanEnterShort())
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

		private (bool longSignal, bool shortSignal) GetTrendFollowingSignals()
		{
			double trendSignal = signals.GetTrendSignal();
			double compositeSignal = signals.GetCompositeSignal();
			
			bool longSignal = trendSignal > TrendThreshold && compositeSignal > 0;
			bool shortSignal = trendSignal < -TrendThreshold && compositeSignal < 0;
			
			return (longSignal, shortSignal);
		}

		private (bool longSignal, bool shortSignal) GetMeanReversionSignals()
		{
			double trendSignal = signals.GetTrendSignal();
			double momentumSignal = signals.GetMomentumSignal();
			
			// Look for oversold/overbought conditions in ranging market
			bool longSignal = momentumSignal < -MomentumThreshold && trendSignal > -10;
			bool shortSignal = momentumSignal > MomentumThreshold && trendSignal < 10;
			
			return (longSignal, shortSignal);
		}

		private (bool longSignal, bool shortSignal) GetMomentumSignals()
		{
			double momentumSignal = signals.GetMomentumSignal();
			double compositeSignal = signals.GetCompositeSignal();
			
			bool longSignal = momentumSignal > MomentumThreshold && compositeSignal > MomentumThreshold;
			bool shortSignal = momentumSignal < -MomentumThreshold && compositeSignal < -MomentumThreshold;
			
			return (longSignal, shortSignal);
		}

		private (bool longSignal, bool shortSignal) GetAdaptiveSignals()
		{
			// Combine multiple strategy signals based on market condition
			var trend = GetTrendFollowingSignals();
			var meanRev = GetMeanReversionSignals();
			var momentum = GetMomentumSignals();
			
			bool longSignal = false;
			bool shortSignal = false;
			
			switch (marketCondition)
			{
				case MarketCondition.Trending:
					longSignal = trend.longSignal;
					shortSignal = trend.shortSignal;
					break;
				case MarketCondition.Ranging:
					longSignal = meanRev.longSignal;
					shortSignal = meanRev.shortSignal;
					break;
				case MarketCondition.Volatile:
					longSignal = momentum.longSignal;
					shortSignal = momentum.shortSignal;
					break;
				default:
					// Use composite approach
					longSignal = (trend.longSignal || momentum.longSignal) && !meanRev.shortSignal;
					shortSignal = (trend.shortSignal || momentum.shortSignal) && !meanRev.longSignal;
					break;
			}
			
			return (longSignal, shortSignal);
		}

		private bool PassesVolumeFilter()
		{
			return Volume[0] > volumeMA[0] * VolumeMultiplier;
		}

		private bool CanEnterLong()
		{
			return !isLongPositionActive && 
				   (Time[0] - lastTradeTime).TotalMinutes > MinutesBetweenTrades &&
				   GetConsecutiveLosses() < MaxConsecutiveLosses;
		}

		private bool CanEnterShort()
		{
			return !isShortPositionActive && 
				   (Time[0] - lastTradeTime).TotalMinutes > MinutesBetweenTrades &&
				   GetConsecutiveLosses() < MaxConsecutiveLosses;
		}
		#endregion

		#region Position Management
		private void EnterLongPosition()
		{
			if (atr[0] <= 0)
			{
				Print("Warning: Invalid ATR value, skipping trade.");
				return;
			}
			
			double stopPrice = Close[0] - (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] + (atr[0] * ATRMultiplier * GetRiskRewardRatio());
			
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
				
				Print(string.Format("{0}: {1} Long entry at {2:F2}, Stop: {3:F2}, Target: {4:F2}, Qty: {5}", 
					Time[0], currentStrategy, Close[0], stopPrice, targetPrice, quantity));
			}
		}

		private void EnterShortPosition()
		{
			if (atr[0] <= 0)
			{
				Print("Warning: Invalid ATR value, skipping trade.");
				return;
			}
			
			double stopPrice = Close[0] + (atr[0] * ATRMultiplier);
			double targetPrice = Close[0] - (atr[0] * ATRMultiplier * GetRiskRewardRatio());
			
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
				
				Print(string.Format("{0}: {1} Short entry at {2:F2}, Stop: {3:F2}, Target: {4:F2}, Qty: {5}", 
					Time[0], currentStrategy, Close[0], stopPrice, targetPrice, quantity));
			}
		}

		private void ManageActivePosition()
		{
			// Time-based exit
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
			
			// Implement trailing stops based on market condition
			if (marketCondition == MarketCondition.Trending)
			{
				ImplementTrailingStop();
			}
		}

		private void ImplementTrailingStop()
		{
			double trailAmount = atr[0] * ATRMultiplier * 0.75; // Tighter trail in trending markets
			
			if (Position.MarketPosition == MarketPosition.Long)
			{
				double newStopPrice = Close[0] - trailAmount;
				// Update stop if it's higher than current stop
				// This would require more complex order management
			}
			else if (Position.MarketPosition == MarketPosition.Short)
			{
				double newStopPrice = Close[0] + trailAmount;
				// Update stop if it's lower than current stop
			}
		}

		private double GetRiskRewardRatio()
		{
			// Adjust risk/reward based on market condition and strategy
			switch (currentStrategy)
			{
				case StrategyMode.TrendFollowing:
					return 2.0; // Higher reward in trending markets
				case StrategyMode.MeanReversion:
					return 1.5; // Moderate reward in ranging markets
				case StrategyMode.Momentum:
					return 1.8; // Good reward for momentum plays
				default:
					return 1.5;
			}
		}
		#endregion

		#region Risk Management and Performance
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

			// Check drawdown limit
			if (GetCurrentDrawdownPercent() > DrawdownLimit)
			{
				Print(string.Format("Drawdown limit exceeded: {0:F2}%. Trading halted.", GetCurrentDrawdownPercent()));
				return false;
			}

			// Check maximum position size
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

			// Adjust position size based on recent performance
			double performanceMultiplier = GetPerformanceAdjustment();
			double adjustedRiskPercent = RiskPerTradePercent * performanceMultiplier;
			
			double accountRisk = Account.CashValue * (adjustedRiskPercent / 100);
			int shares = (int)(accountRisk / riskPerShare);
			
			shares = Math.Max(1, shares);
			shares = Math.Min(shares, MaxPositionSize);
			
			double maxSharesByValue = (int)((Account.CashValue * 0.1) / Close[0]);
			shares = Math.Min(shares, (int)maxSharesByValue);
			
			return shares;
		}

		private double GetPerformanceAdjustment()
		{
			// Reduce position size after losses, increase after wins
			int consecutiveLosses = GetConsecutiveLosses();
			
			if (consecutiveLosses >= 2)
				return 0.5; // Reduce risk by 50%
			else if (consecutiveLosses == 1)
				return 0.75; // Reduce risk by 25%
			else if (GetConsecutiveWins() >= 3)
				return 1.25; // Increase risk by 25%
			else
				return 1.0; // Normal risk
		}

		private void UpdatePerformanceMetrics()
		{
			if (Account.CashValue > highWaterMark)
			{
				highWaterMark = Account.CashValue;
			}
			
			double currentDrawdown = (highWaterMark - Account.CashValue) / highWaterMark * 100;
			if (currentDrawdown > maxDrawdown)
			{
				maxDrawdown = currentDrawdown;
			}
		}

		private double GetCurrentDrawdownPercent()
		{
			return (highWaterMark - Account.CashValue) / highWaterMark * 100;
		}

		private int GetConsecutiveLosses()
		{
			// This would need to track actual trade results
			// Simplified implementation
			return 0;
		}

		private int GetConsecutiveWins()
		{
			// This would need to track actual trade results
			// Simplified implementation
			return 0;
		}

		private void PrintPerformanceReport()
		{
			Print("=== ADVANCED TRADING BOT PERFORMANCE REPORT ===");
			Print(string.Format("Total Profit: {0:C}", totalProfit));
			Print(string.Format("Winning Trades: {0}", winningTrades));
			Print(string.Format("Losing Trades: {0}", losingTrades));
			Print(string.Format("Win Rate: {0:F1}%", 
				winningTrades + losingTrades > 0 ? (double)winningTrades / (winningTrades + losingTrades) * 100 : 0));
			Print(string.Format("Maximum Drawdown: {0:F2}%", maxDrawdown));
			Print(string.Format("Market Condition: {0}", marketCondition));
			Print(string.Format("Final Strategy: {0}", currentStrategy));
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

		#region Order Event Handlers
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, ErrorCode error, string comment)
		{
			// Handle order updates
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

			Print(string.Format("{0}: Order Update - {1}, State: {2}, Filled: {3}", 
				time, order.Name, orderState, filled));
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (execution.Order != null)
			{
				Print(string.Format("{0}: Execution - {1}, Price: {2}, Quantity: {3}", 
					time, execution.Order.Name, price, quantity));
					
				// Track trade results for performance analysis
				if (execution.Order.Name == "StopLoss" || execution.Order.Name == "ProfitTarget" || execution.Order.Name.Contains("Exit"))
				{
					// This is an exit - calculate P&L and update statistics
					// Implementation would depend on specific trade tracking requirements
				}
			}
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Period", Description="Period for ATR calculation", Order=1, GroupName="Indicators")]
		public int ATRPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="ATR Multiplier", Description="Multiplier for ATR-based stops", Order=2, GroupName="Risk Management")]
		public double ATRMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name="Risk Per Trade %", Description="Risk percentage per trade", Order=3, GroupName="Risk Management")]
		public double RiskPerTradePercent { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 50.0)]
		[Display(Name="Max Daily Loss %", Description="Maximum daily loss percentage", Order=4, GroupName="Risk Management")]
		public double MaxDailyLoss { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Trading Start Time", Description="Start time for trading", Order=5, GroupName="Trading Hours")]
		public DateTime TradingStartTime { get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Trading End Time", Description="End time for trading", Order=6, GroupName="Trading Hours")]
		public DateTime TradingEndTime { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1440)]
		[Display(Name="Minutes Between Trades", Description="Minimum minutes between trades", Order=7, GroupName="Strategy")]
		public int MinutesBetweenTrades { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name="Max Bars In Position", Description="Maximum bars to hold a position", Order=8, GroupName="Strategy")]
		public int MaxBarsInPosition { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10000)]
		[Display(Name="Max Position Size", Description="Maximum position size in shares/contracts", Order=9, GroupName="Risk Management")]
		public int MaxPositionSize { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 50.0)]
		[Display(Name="Max Account Exposure %", Description="Maximum account exposure percentage", Order=10, GroupName="Risk Management")]
		public double MaxAccountExposurePercent { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Trend Threshold", Description="Threshold for trend signals", Order=11, GroupName="Strategy Signals")]
		public double TrendThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Momentum Threshold", Description="Threshold for momentum signals", Order=12, GroupName="Strategy Signals")]
		public double MomentumThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Volatility Threshold", Description="Threshold for volatility signals", Order=13, GroupName="Strategy Signals")]
		public double VolatilityThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="ADX Threshold", Description="Minimum ADX for trending market", Order=14, GroupName="Filters")]
		public double ADXThreshold { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 5.0)]
		[Display(Name="Volume Multiplier", Description="Volume filter multiplier", Order=15, GroupName="Filters")]
		public double VolumeMultiplier { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use Adaptive Strategy", Description="Enable adaptive strategy selection", Order=16, GroupName="Strategy")]
		public bool UseAdaptiveStrategy { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use Volume Filter", Description="Enable volume filter", Order=17, GroupName="Filters")]
		public bool UseVolumeFilter { get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use ADX Filter", Description="Enable ADX filter", Order=18, GroupName="Filters")]
		public bool UseADXFilter { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name="Max Consecutive Losses", Description="Maximum consecutive losses before reducing size", Order=19, GroupName="Risk Management")]
		public int MaxConsecutiveLosses { get; set; }

		[NinjaScriptProperty]
		[Range(1.0, 20.0)]
		[Display(Name="Drawdown Limit %", Description="Maximum drawdown before halting trading", Order=20, GroupName="Risk Management")]
		public double DrawdownLimit { get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Strategies.AdvancedTradingBot AdvancedTradingBot()
		{
			return AdvancedTradingBot(Input);
		}

		public Strategies.AdvancedTradingBot AdvancedTradingBot(ISeries<double> input)
		{
			if (cacheAdvancedTradingBot != null)
				for (int idx = 0; idx < cacheAdvancedTradingBot.Length; idx++)
					if (cacheAdvancedTradingBot[idx] != null && cacheAdvancedTradingBot[idx].EqualsInput(input))
						return cacheAdvancedTradingBot[idx];
			return CacheStrategy<Strategies.AdvancedTradingBot>(new Strategies.AdvancedTradingBot(), input, ref cacheAdvancedTradingBot);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Strategies.AdvancedTradingBot AdvancedTradingBot()
		{
			return AdvancedTradingBot(Input);
		}

		public Strategies.AdvancedTradingBot AdvancedTradingBot(ISeries<double> input)
		{
			if (cacheAdvancedTradingBot != null)
				for (int idx = 0; idx < cacheAdvancedTradingBot.Length; idx++)
					if (cacheAdvancedTradingBot[idx] != null && cacheAdvancedTradingBot[idx].EqualsInput(input))
						return cacheAdvancedTradingBot[idx];
			return CacheStrategy<Strategies.AdvancedTradingBot>(new Strategies.AdvancedTradingBot(), input, ref cacheAdvancedTradingBot);
		}
	}
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private Strategies.AdvancedTradingBot[] cacheAdvancedTradingBot;

		public Strategies.AdvancedTradingBot AdvancedTradingBot()
		{
			return AdvancedTradingBot(Input);
		}

		public Strategies.AdvancedTradingBot AdvancedTradingBot(ISeries<double> input)
		{
			if (cacheAdvancedTradingBot != null)
				for (int idx = 0; idx < cacheAdvancedTradingBot.Length; idx++)
					if (cacheAdvancedTradingBot[idx] != null && cacheAdvancedTradingBot[idx].EqualsInput(input))
						return cacheAdvancedTradingBot[idx];
			return CacheStrategy<Strategies.AdvancedTradingBot>(new Strategies.AdvancedTradingBot(), input, ref cacheAdvancedTradingBot);
		}
	}
}

#endregion