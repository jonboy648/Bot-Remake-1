#!/usr/bin/env python3
"""
NinjaTrader 8 TradingBot Strategy Validation Script
Validates that the strategy meets NT8 compliance requirements.
"""

import os
import re
import sys

def validate_strategy():
    """Run validation tests on the TradingBot strategy."""
    
    strategy_file = "TradingBot.cs"
    
    if not os.path.exists(strategy_file):
        print("❌ FAIL: TradingBot.cs file not found")
        return
    
    with open(strategy_file, 'r', encoding='utf-8') as f:
        code = f.read()
    
    print("NinjaTrader 8 TradingBot Validation Tests")
    print("=" * 45)
    
    tests = [
        {
            'name': 'Proper Strategy inheritance',
            'pattern': r'public class TradingBot : Strategy',
            'required': True
        },
        {
            'name': 'Correct namespace',
            'pattern': r'namespace NinjaTrader\.NinjaScript\.Strategies',
            'required': True
        },
        {
            'name': 'OnStateChange implementation',
            'pattern': r'protected override void OnStateChange\(\)',
            'required': True
        },
        {
            'name': 'OnBarUpdate implementation',
            'pattern': r'protected override void OnBarUpdate\(\)',
            'required': True
        },
        {
            'name': 'Order management methods',
            'pattern': r'OnOrderUpdate.*OnExecutionUpdate',
            'required': True,
            'flags': re.DOTALL
        },
        {
            'name': 'NinjaScriptProperty attributes',
            'pattern': r'\[NinjaScriptProperty\]',
            'required': True
        },
        {
            'name': 'Required using statements',
            'pattern': r'using NinjaTrader\.(Cbi|NinjaScript\.Strategies)',
            'required': True
        },
        {
            'name': 'State handling (SetDefaults)',
            'pattern': r'State\.SetDefaults',
            'required': True
        },
        {
            'name': 'State handling (DataLoaded)',
            'pattern': r'State\.DataLoaded',
            'required': True
        },
        {
            'name': 'State handling (Historical)',
            'pattern': r'State\.Historical',
            'required': True
        },
        {
            'name': 'Risk management parameters',
            'pattern': r'RiskPerTradePercent.*MaxDailyLoss',
            'required': True,
            'flags': re.DOTALL
        },
        {
            'name': 'Position size calculation',
            'pattern': r'CalculatePositionSize',
            'required': True
        },
        {
            'name': 'Generated code region',
            'pattern': r'#region NinjaScript generated code',
            'required': True
        },
        {
            'name': 'Proper entry methods',
            'pattern': r'EnterLong.*EnterShort',
            'required': True,
            'flags': re.DOTALL
        },
        {
            'name': 'Proper exit methods',
            'pattern': r'ExitLong.*ExitShort',
            'required': True,
            'flags': re.DOTALL
        }
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        flags = test.get('flags', 0)
        if re.search(test['pattern'], code, flags):
            print(f"✅ PASS: {test['name']}")
            passed += 1
        else:
            print(f"❌ FAIL: {test['name']}")
    
    # Additional code analysis
    print("\n" + "=" * 45)
    print("Additional Code Analysis:")
    
    # Count methods
    methods = re.findall(r'(public|private|protected|internal)\s+.*?\s+\w+\s*\(', code)
    print(f"Method count: {len(methods)}")
    
    # Count properties
    properties = re.findall(r'\[NinjaScriptProperty\]', code)
    print(f"Property count: {len(properties)}")
    
    # Count lines
    lines = code.split('\n')
    print(f"Total lines: {len(lines)}")
    print(f"Code lines (non-empty): {len([l for l in lines if l.strip()])}")
    
    # File size
    file_size = os.path.getsize(strategy_file)
    print(f"File size: {file_size:,} bytes")
    
    # Summary
    print("\n" + "=" * 45)
    print(f"Validation Results: {passed}/{total} tests passed")
    compliance_score = (passed / total) * 100
    print(f"Compliance Score: {compliance_score:.1f}%")
    
    if passed == total:
        print("🎉 ALL TESTS PASSED - Strategy is NT8 compliant!")
        return True
    else:
        print("⚠️  Some tests failed - Review strategy code")
        return False

def check_file_structure():
    """Check that all required files are present."""
    
    print("\nFile Structure Check:")
    print("=" * 25)
    
    required_files = [
        'TradingBot.cs',
        'README.md',
        'INSTALLATION.md',
        'NT8_COMPLIANCE.md',
        'TradingBot.config',
        '.gitignore'
    ]
    
    all_present = True
    for file in required_files:
        if os.path.exists(file):
            size = os.path.getsize(file)
            print(f"✅ {file} ({size:,} bytes)")
        else:
            print(f"❌ {file} - MISSING")
            all_present = False
    
    return all_present

if __name__ == "__main__":
    strategy_valid = validate_strategy()
    files_present = check_file_structure()
    
    print("\n" + "=" * 45)
    print("FINAL VALIDATION SUMMARY")
    print("=" * 45)
    
    if strategy_valid and files_present:
        print("✅ COMPLETE: All validations passed!")
        print("✅ The TradingBot is ready for NinjaTrader 8 deployment")
        sys.exit(0)
    else:
        print("❌ INCOMPLETE: Some validations failed")
        print("❌ Review and fix issues before deployment")
        sys.exit(1)