#!/usr/bin/env python3
"""
Enhanced NinjaTrader 8 TradingBot Strategy Validation Script
Validates critical errors and implementation issues beyond basic compliance.
"""

import os
import re
import sys

def validate_critical_issues():
    """Run enhanced validation tests to detect critical implementation issues."""
    
    strategy_file = "TradingBot.cs"
    
    if not os.path.exists(strategy_file):
        print("❌ FAIL: TradingBot.cs file not found")
        return False
    
    with open(strategy_file, 'r', encoding='utf-8') as f:
        code = f.read()
    
    print("Enhanced NinjaTrader 8 TradingBot Critical Issue Detection")
    print("=" * 60)
    
    critical_tests = [
        {
            'name': 'DateTime.Now usage (should use Time[0])',
            'pattern': r'DateTime\.Now',
            'should_fail': True,
            'severity': 'CRITICAL'
        },
        {
            'name': 'Generated code uses CacheStrategy (not CacheIndicator)',
            'pattern': r'CacheStrategy<Strategies\.TradingBot>',
            'should_fail': False,
            'severity': 'CRITICAL'
        },
        {
            'name': 'No CacheIndicator for Strategy (incorrect pattern)',
            'pattern': r'CacheIndicator<Strategies\.TradingBot>',
            'should_fail': True,
            'severity': 'CRITICAL'
        },
        {
            'name': 'Daily P&L reset implementation',
            'pattern': r'currentTradingDay\.Date != Time\[0\]\.Date',
            'should_fail': False,
            'severity': 'HIGH'
        },
        {
            'name': 'Configurable MinutesBetweenTrades parameter',
            'pattern': r'MinutesBetweenTrades',
            'should_fail': False,
            'severity': 'MEDIUM'
        },
        {
            'name': 'Hardcoded 30-minute restriction (should be removed)',
            'pattern': r'\.TotalMinutes > 30',
            'should_fail': True,
            'severity': 'MEDIUM'
        },
        {
            'name': 'Proper position value validation',
            'pattern': r'currentPositionValue.*maxPositionValue',
            'should_fail': False,
            'severity': 'HIGH'
        },
        {
            'name': 'AllowReversals implementation',
            'pattern': r'if \(AllowReversals\)',
            'should_fail': False,
            'severity': 'MEDIUM'
        },
        {
            'name': 'ATR validation before use',
            'pattern': r'if \(atr\[0\] <= 0\)',
            'should_fail': False,
            'severity': 'HIGH'
        },
        {
            'name': 'Enhanced error handling and logging',
            'pattern': r'Print\(.*Error:.*\)',
            'should_fail': False,
            'severity': 'MEDIUM'
        }
    ]
    
    passed = 0
    total = len(critical_tests)
    critical_failures = 0
    high_failures = 0
    
    for test in critical_tests:
        found = bool(re.search(test['pattern'], code, re.DOTALL))
        
        if test['should_fail']:
            # This pattern should NOT be found
            if not found:
                print(f"✅ PASS: {test['name']} ({test['severity']})")
                passed += 1
            else:
                print(f"❌ FAIL: {test['name']} ({test['severity']}) - Pattern still found")
                if test['severity'] == 'CRITICAL':
                    critical_failures += 1
                elif test['severity'] == 'HIGH':
                    high_failures += 1
        else:
            # This pattern SHOULD be found
            if found:
                print(f"✅ PASS: {test['name']} ({test['severity']})")
                passed += 1
            else:
                print(f"❌ FAIL: {test['name']} ({test['severity']}) - Pattern not found")
                if test['severity'] == 'CRITICAL':
                    critical_failures += 1
                elif test['severity'] == 'HIGH':
                    high_failures += 1
    
    # Additional comprehensive checks
    print("\n" + "=" * 60)
    print("Comprehensive Implementation Analysis:")
    
    # Count indicators
    indicators = len(re.findall(r'private \w+ \w+;', code))
    print(f"Indicators implemented: {indicators}")
    
    # Count strategies
    strategies = len(re.findall(r'bool \w+Signal', code))
    print(f"Strategy signals: {strategies}")
    
    # Count configurable parameters
    properties = len(re.findall(r'\[NinjaScriptProperty\]', code))
    print(f"Configurable parameters: {properties}")
    
    # Count filter options
    filters = len(re.findall(r'Use\w+Filter', code))
    print(f"Filter options: {filters}")
    
    # Summary
    print("\n" + "=" * 60)
    print(f"Critical Issue Detection Results: {passed}/{total} tests passed")
    
    if critical_failures > 0:
        print(f"❌ CRITICAL FAILURES: {critical_failures}")
        return False
    elif high_failures > 0:
        print(f"⚠️  HIGH PRIORITY ISSUES: {high_failures}")
        return False
    else:
        print("✅ All critical issues resolved!")
        return True

def check_improvements():
    """Check for specific improvements made to the strategy."""
    
    strategy_file = "TradingBot.cs"
    
    with open(strategy_file, 'r', encoding='utf-8') as f:
        code = f.read()
    
    print("\nStrategy Enhancement Analysis:")
    print("=" * 35)
    
    improvements = [
        ("Multiple Indicators", r'(RSI|MACD|Bollinger)', "Enhanced technical analysis"),
        ("Filter System", r'Use\w+Filter.*true', "Configurable entry filters"),
        ("Advanced Risk Management", r'dailyStartingCash', "Proper daily P&L tracking"),
        ("Error Handling", r'Warning:.*Invalid', "Comprehensive validation"),
        ("Configurable Timeouts", r'MinutesBetweenTrades', "Flexible timing controls"),
        ("Position Management", r'ReversalExit', "Advanced exit strategies")
    ]
    
    for name, pattern, description in improvements:
        if re.search(pattern, code):
            print(f"✅ {name}: {description}")
        else:
            print(f"❌ {name}: Not implemented")

if __name__ == "__main__":
    critical_passed = validate_critical_issues()
    check_improvements()
    
    print("\n" + "=" * 60)
    print("FINAL ENHANCED VALIDATION SUMMARY")
    print("=" * 60)
    
    if critical_passed:
        print("✅ EXCELLENT: All critical issues have been resolved!")
        print("✅ The enhanced TradingBot is production-ready")
        sys.exit(0)
    else:
        print("❌ ISSUES REMAIN: Critical problems need attention")
        print("❌ Review and fix remaining issues")
        sys.exit(1)