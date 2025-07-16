# NinjaTrader 8 TradingBot Installation Guide

## Quick Installation

### Step 1: Locate NT8 Strategies Folder
1. Open Windows Explorer
2. Navigate to: `Documents\NinjaTrader 8\bin\Custom\Strategies\`
3. If folder doesn't exist, create it

### Step 2: Copy Strategy File
1. Download `TradingBot.cs` from this repository
2. Copy it to the Strategies folder: `Documents\NinjaTrader 8\bin\Custom\Strategies\TradingBot.cs`

### Step 3: Compile in NinjaTrader 8
1. Open NinjaTrader 8
2. Go to **Tools** → **Edit NinjaScript** → **Strategy**
3. Find "TradingBot" in the list (should appear automatically)
4. Select it and press **F5** to compile
5. Or use **Tools** → **Compile** from the NinjaScript Editor

### Step 4: Verify Installation
1. Go to **Control Center** → **Strategies** tab
2. Look for "TradingBot" in the Available Strategies list
3. If visible, installation is successful

## Detailed Installation Steps

### Prerequisites
- NinjaTrader 8 Platform (any version)
- .NET Framework 4.8 or higher
- Active market data connection
- Valid trading account (for live trading)

### Alternative Installation Methods

#### Method 1: Direct File Copy (Recommended)
```
1. Close NinjaTrader 8 completely
2. Copy TradingBot.cs to Documents\NinjaTrader 8\bin\Custom\Strategies\
3. Open NinjaTrader 8
4. Strategy should auto-compile on startup
```

#### Method 2: NinjaScript Editor
```
1. Open NinjaTrader 8
2. Tools → Edit NinjaScript → Strategy
3. File → Open → Browse to TradingBot.cs
4. File → Save As → Save to Strategies folder
5. Press F5 to compile
```

#### Method 3: Import (if provided as .zip)
```
1. Tools → Import → NinjaScript Add-On
2. Select TradingBot.zip file
3. Follow import wizard
4. Restart NinjaTrader 8
```

## Post-Installation Setup

### 1. Strategy Configuration
1. Go to **Strategies** tab in Control Center
2. Right-click in empty area → **Strategies...**
3. Select "TradingBot" from dropdown
4. Configure parameters (see TradingBot.config for presets)

### 2. Backtest Setup (Recommended First Step)
1. Open **Strategy Analyzer**
2. Select "TradingBot" strategy
3. Choose instrument (e.g., ES 03-25)
4. Set date range for backtest
5. Configure parameters
6. Run backtest to verify functionality

### 3. Chart Setup (Optional)
1. Open price chart for your instrument
2. Right-click chart → **Strategies** → **TradingBot**
3. Configure parameters
4. Apply to chart for visual monitoring

## Verification Steps

### Compilation Check
```
1. Tools → Edit NinjaScript → Strategy
2. Find TradingBot in list
3. Should show "Compiled Successfully" status
4. No red error indicators
```

### Strategy Availability Check
```
1. Control Center → Strategies tab
2. Right-click → Strategies...
3. TradingBot appears in strategy dropdown
4. Parameters load correctly
```

### Basic Functionality Test
```
1. Create new Strategy Analyzer backtest
2. Select TradingBot strategy
3. Use ES instrument with 1-day range
4. Should execute without errors
5. Check for entry/exit signals
```

## Troubleshooting

### Common Installation Issues

#### Error: "Strategy not found"
- **Cause**: File not in correct folder
- **Solution**: Verify file is in `Documents\NinjaTrader 8\bin\Custom\Strategies\`
- **Check**: Folder path spelling and capitalization

#### Error: "Compilation failed"
- **Cause**: Missing dependencies or syntax errors
- **Solution**: Check Output Window for specific errors
- **Fix**: Ensure .NET Framework 4.8+ is installed

#### Error: "Cannot access indicators"
- **Cause**: NinjaTrader 8 permissions or installation issue
- **Solution**: Run NT8 as Administrator once
- **Alternative**: Reinstall NinjaTrader 8

#### Strategy appears but won't start
- **Cause**: Parameter validation failure
- **Solution**: Reset parameters to defaults
- **Check**: Verify all required parameters are set

### Advanced Troubleshooting

#### Clean Compilation
```
1. Close NinjaTrader 8
2. Delete: Documents\NinjaTrader 8\bin\Custom\Strategies\compiled\*
3. Restart NT8
4. Strategy will auto-recompile
```

#### Reset Strategy Settings
```
1. Tools → Options → Strategies
2. Reset to defaults
3. Restart NinjaTrader 8
4. Reconfigure strategy
```

#### Check Dependencies
```
1. Tools → References
2. Verify all NinjaTrader assemblies are present
3. Check .NET Framework version compatibility
```

## File Locations Reference

### Default NT8 Installation Paths
```
Program Files: C:\Program Files\NinjaTrader 8\
User Data: Documents\NinjaTrader 8\
Strategies: Documents\NinjaTrader 8\bin\Custom\Strategies\
Compiled: Documents\NinjaTrader 8\bin\Custom\Strategies\compiled\
Logs: Documents\NinjaTrader 8\trace\
```

### Required Files
- `TradingBot.cs` - Main strategy file
- `TradingBot.config` - Configuration template (optional)

## Support Resources

### Official Documentation
- NinjaTrader 8 Help Guide: Strategy Development
- NinjaScript Developer Reference
- Strategy Analyzer User Guide

### Community Support
- NinjaTrader Support Forum
- Strategy Development Community
- GitHub Issues (for this specific strategy)

### Professional Support
- NinjaTrader Technical Support
- Certified Strategy Developers
- Custom Development Services

## Next Steps After Installation

1. **Test in Simulation**: Always test with simulated trading first
2. **Optimize Parameters**: Use Strategy Analyzer to optimize settings
3. **Monitor Performance**: Track key metrics and drawdown
4. **Risk Management**: Set appropriate position sizes and limits
5. **Live Trading**: Deploy carefully with small position sizes initially

## Update Procedure

To update the strategy:
1. Close all charts using TradingBot
2. Stop any running strategy instances
3. Replace TradingBot.cs with new version
4. Recompile (F5) in NinjaScript Editor
5. Restart strategy instances

**Important**: Always backup your current working version before updating!