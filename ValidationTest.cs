/**
 * NinjaTrader 8 TradingBot Strategy Validation Test
 * 
 * This file contains basic validation tests to ensure the strategy
 * meets NinjaTrader 8 compliance requirements.
 */

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace TradingBotValidation
{
    class ValidationTests
    {
        private static string strategyFilePath = "TradingBot.cs";
        
        static void Main(string[] args)
        {
            Console.WriteLine("NinjaTrader 8 TradingBot Validation Tests");
            Console.WriteLine("=========================================");
            
            if (!File.Exists(strategyFilePath))
            {
                Console.WriteLine("❌ FAIL: TradingBot.cs file not found");
                return;
            }
            
            string code = File.ReadAllText(strategyFilePath);
            
            RunValidationTests(code);
        }
        
        static void RunValidationTests(string code)
        {
            int passed = 0;
            int total = 0;
            
            // Test 1: Proper inheritance
            total++;
            if (code.Contains("public class TradingBot : Strategy"))
            {
                Console.WriteLine("✅ PASS: Proper Strategy inheritance");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Strategy inheritance missing or incorrect");
            }
            
            // Test 2: Correct namespace
            total++;
            if (code.Contains("namespace NinjaTrader.NinjaScript.Strategies"))
            {
                Console.WriteLine("✅ PASS: Correct namespace");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Incorrect or missing namespace");
            }
            
            // Test 3: OnStateChange implementation
            total++;
            if (code.Contains("protected override void OnStateChange()"))
            {
                Console.WriteLine("✅ PASS: OnStateChange method implemented");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: OnStateChange method missing");
            }
            
            // Test 4: OnBarUpdate implementation
            total++;
            if (code.Contains("protected override void OnBarUpdate()"))
            {
                Console.WriteLine("✅ PASS: OnBarUpdate method implemented");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: OnBarUpdate method missing");
            }
            
            // Test 5: Proper order management
            total++;
            if (code.Contains("OnOrderUpdate") && code.Contains("OnExecutionUpdate"))
            {
                Console.WriteLine("✅ PASS: Order management methods implemented");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Order management methods missing");
            }
            
            // Test 6: NinjaScriptProperty attributes
            total++;
            if (code.Contains("[NinjaScriptProperty]"))
            {
                Console.WriteLine("✅ PASS: NinjaScriptProperty attributes found");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: NinjaScriptProperty attributes missing");
            }
            
            // Test 7: Required using statements
            total++;
            if (code.Contains("using NinjaTrader.NinjaScript.Strategies;") ||
                code.Contains("using NinjaTrader.Cbi;"))
            {
                Console.WriteLine("✅ PASS: Required using statements found");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Required using statements missing");
            }
            
            // Test 8: State handling
            total++;
            if (code.Contains("State.SetDefaults") && 
                code.Contains("State.DataLoaded") && 
                code.Contains("State.Historical"))
            {
                Console.WriteLine("✅ PASS: Proper state handling");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Incomplete state handling");
            }
            
            // Test 9: Risk management
            total++;
            if (code.Contains("RiskPerTradePercent") && 
                code.Contains("MaxDailyLoss") && 
                code.Contains("CalculatePositionSize"))
            {
                Console.WriteLine("✅ PASS: Risk management implemented");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Risk management incomplete");
            }
            
            // Test 10: Generated code region
            total++;
            if (code.Contains("#region NinjaScript generated code"))
            {
                Console.WriteLine("✅ PASS: Generated code region present");
                passed++;
            }
            else
            {
                Console.WriteLine("❌ FAIL: Generated code region missing");
            }
            
            // Summary
            Console.WriteLine("\n=========================================");
            Console.WriteLine($"Validation Results: {passed}/{total} tests passed");
            Console.WriteLine($"Compliance Score: {(double)passed / total * 100:F1}%");
            
            if (passed == total)
            {
                Console.WriteLine("🎉 ALL TESTS PASSED - Strategy is NT8 compliant!");
            }
            else
            {
                Console.WriteLine("⚠️  Some tests failed - Review strategy code");
            }
            
            // Additional checks
            Console.WriteLine("\nAdditional Information:");
            Console.WriteLine($"Strategy file size: {new FileInfo(strategyFilePath).Length} bytes");
            Console.WriteLine($"Code lines: {code.Split('\n').Length}");
            Console.WriteLine($"Method count: {Regex.Matches(code, @"(public|private|protected|internal)\s+.*\s+\w+\(").Count}");
            Console.WriteLine($"Property count: {Regex.Matches(code, @"\[NinjaScriptProperty\]").Count}");
        }
    }
}

/*
 * To run this validation:
 * 1. Compile: csc ValidationTest.cs
 * 2. Run: ValidationTest.exe
 * 3. Review results
 * 
 * Note: This is a basic validation. Full compliance requires
 * compilation and runtime testing in NinjaTrader 8.
 */