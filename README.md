# NinjaTrader 8 Trading Bot

This repository contains a fully compliant NinjaTrader 8 automated trading strategy that follows all NT8 framework rules and best practices.

## Features

### NinjaTrader 8 Compliance
- ✅ Proper inheritance from `NinjaTrader.NinjaScript.Strategies.Strategy`
- ✅ Complete `OnStateChange()` implementation for all states
- ✅ Proper order management using NT8 order methods
- ✅ Thread-safe data access following NT8 guidelines
- ✅ Correct namespace and assembly structure
- ✅ NT8 property decorations and parameter handling

### Trading Strategy
- **EMA Crossover Strategy**: Uses fast and slow EMA crossovers for entry signals
- **ATR-Based Risk Management**: Dynamic stop losses and profit targets based on Average True Range
- **Position Sizing**: Risk-based position sizing with maximum exposure limits
- **Time-Based Filters**: Configurable trading hours with session management
- **Multiple Timeframe Support**: Compatible with any chart timeframe

### Risk Management
- Daily loss limits with automatic trading halt
- Position size limits based on account equity
- ATR-based stop losses for market volatility adaptation
- Configurable risk per trade percentage
- Time-based exit after maximum bars in position

## Installation

### Requirements
- NinjaTrader 8 (any version)
- .NET Framework 4.8 or higher
- Valid market data subscription

### Installation Steps

1. **Copy Strategy File**:
   ```
   Copy TradingBot.cs to:
   Documents\NinjaTrader 8\bin\Custom\Strategies\
   ```

2. **Compile Strategy**:
   - Open NinjaTrader 8
   - Go to Tools → Edit NinjaScript → Strategy
   - Find "TradingBot" in the list
   - Press F5 to compile or use Tools → Compile

3. **Verify Installation**:
   - Go to Control Center → Strategies tab
   - Look for "TradingBot" in the strategy list

## Configuration

### Strategy Parameters

#### Indicators
- **Fast EMA Period** (Default: 9): Period for the fast moving average
- **Slow EMA Period** (Default: 21): Period for the slow moving average  
- **ATR Period** (Default: 14): Period for Average True Range calculation

#### Risk Management
- **ATR Multiplier** (Default: 2.0): Multiplier for ATR-based stops and targets
- **Risk Per Trade %** (Default: 1.0): Maximum risk per trade as % of account
- **Max Daily Loss %** (Default: 3.0): Maximum daily loss before trading halt

#### Trading Hours
- **Trading Start Time** (Default: 09:30): Start time for trading
- **Trading End Time** (Default: 15:30): End time for trading

#### Strategy Settings
- **Allow Reversals** (Default: False): Whether to allow position reversals

### Recommended Settings by Market

#### Stock Index Futures (ES, NQ)
```
Fast EMA Period: 9
Slow EMA Period: 21
ATR Period: 14
ATR Multiplier: 2.0
Risk Per Trade %: 1.0
Trading Hours: 09:30 - 15:30 EST
```

#### Forex Major Pairs
```
Fast EMA Period: 12
Slow EMA Period: 26
ATR Period: 14
ATR Multiplier: 1.5
Risk Per Trade %: 0.5
Trading Hours: 24 hours (adjust for sessions)
```

#### Crude Oil (CL)
```
Fast EMA Period: 8
Slow EMA Period: 18
ATR Period: 12
ATR Multiplier: 2.5
Risk Per Trade %: 1.5
Trading Hours: 09:00 - 14:30 EST
```

## Usage

### Running the Strategy

1. **Backtesting**:
   - Open Strategy Analyzer
   - Select "TradingBot" strategy
   - Configure parameters and instrument
   - Run backtest to verify performance

2. **Live Trading**:
   - Open Strategies tab in Control Center
   - Right-click and select "Strategies..."
   - Choose "TradingBot" and configure parameters
   - Select instrument and account
   - Enable strategy for live trading

### Monitoring

The strategy provides detailed logging for:
- Entry and exit signals with prices
- Order status updates and executions
- Risk management decisions
- Daily performance statistics

Monitor the Output Window and Strategy Performance for real-time updates.

## NT8 Framework Compliance Details

### State Management
The strategy properly handles all NinjaTrader 8 states:
- `State.SetDefaults`: Parameter initialization
- `State.DataLoaded`: Indicator initialization  
- `State.Historical`: Historical data processing
- `State.Transition`: Historical to real-time transition
- `State.Realtime`: Live trading mode
- `State.Terminated`: Cleanup and termination

### Order Management
- Uses proper NT8 order methods (`EnterLong`, `ExitLongStopMarket`, etc.)
- Implements `OnOrderUpdate()` and `OnExecutionUpdate()` for order tracking
- Follows NT8 order naming conventions and lifecycle management
- Proper handling of order states and error conditions

### Threading and Data Access
- All data access follows NT8 threading rules
- No direct manipulation of Orders collection
- Proper use of BarsInProgress for multi-timeframe strategies
- Thread-safe indicator and price data access

### Performance and Memory
- Efficient indicator usage with proper initialization
- Minimal memory footprint with appropriate lookback settings
- Proper disposal of resources in State.Terminated

## Risk Warnings

⚠️ **Important Risk Disclaimers**:
- This is an automated trading system that can lose money
- Always test thoroughly in simulation before live trading
- Monitor the strategy actively during live trading
- Understand all parameters before deployment
- Past performance does not guarantee future results
- Trading involves substantial risk of loss

## Support and Customization

### Common Modifications
- Adjust EMA periods for different market conditions
- Modify ATR multiplier for different volatility environments
- Add additional entry filters (volume, momentum, etc.)
- Implement trailing stop functionality
- Add multiple timeframe confirmations

### Troubleshooting
- **Compilation Errors**: Ensure proper NT8 installation and .NET Framework
- **No Trades**: Check trading hours, minimum bars, and entry conditions
- **Order Errors**: Verify account permissions and margin requirements
- **Performance Issues**: Reduce lookback periods and optimize indicator usage

For technical support or custom modifications, please refer to the NinjaTrader 8 documentation and community forums.

## License

This strategy is provided as-is for educational and research purposes. Use at your own risk in live trading environments.