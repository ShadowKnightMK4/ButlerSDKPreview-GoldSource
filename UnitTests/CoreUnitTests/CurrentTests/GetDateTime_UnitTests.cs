using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using ButlerSDK.Tools;
using ButlerToolContract.DataTypes;
using ButlerSDK.ApiKeyMgr.Contract;

namespace CoreUnitTests.Calavera
{
    class custom_datetime: ButlerTool_DeviceAPI_GetLocalDateTime
    {
        public custom_datetime(IButlerVaultKeyCollection x):base(x)
        {

        }
        protected override void AssignDefaultPattern()
        {
            DefaultPattern = "ShortDatePattern";
        }
    }
    [TestClass]
    public class Calavera_DateTimeTool_Tests
    {

        [TestInitialize]
        public void Setup()
        {
            Assert.IsNotNull(new ButlerTool_DeviceAPI_GetLocalDateTime(null));
        }



        [TestMethod]
        public void Sombra_Attack_CustomFormatString_MalformedHandle_AcceptedAndUsesCustomDefaultPattern()
        {
            // WHAT THIS TESTS:
            // The tool advertises support for standard .NET DateTime format strings.
            // Sombra requests a standard ISO format: "yyyy-MM-dd".
            // FAULTY ASSUMPTION: SpecialCaseChecks falls back to SpecialCases["FullDateTimePattern"]
            // for any format not in its dictionary, silently destroying the user's custom format!
            var _targetTool = new custom_datetime(null!);
            _targetTool.Initialize(); // part of the contract of the default running is it DOES calls initalize.
            string customIsoFormat = "{\"format\": \"1\"}";

            var result = _targetTool.ResolveMyTool(customIsoFormat, "func_time_001", null);

            Assert.IsNotNull(result, "Tool returned null result.");
            string outputText = result.GetCombinedText();

            // We passed a invalid format to DateTime.Now.ToString(), fallback is FUllDateTime/the default
            Assert.IsFalse(outputText.Contains("Monday")
                           || outputText.Contains("Tuesday")
                           || outputText.Contains("Wednesday")
                           || outputText.Contains("Thursday")
                           || outputText.Contains("Friday")
                           || outputText.Contains("Saturday")
                           || outputText.Contains("Sunday"),
                           outputText);

        }


        [TestMethod]
        public void Sombra_Attack_CustomFormatString_MalformedHandle_UsesDefaultPattern()
        {
            // WHAT THIS TESTS:
            // The tool advertises support for standard .NET DateTime format strings.
            // Sombra requests a standard ISO format: "yyyy-MM-dd".
            // FAULTY ASSUMPTION: SpecialCaseChecks falls back to SpecialCases["FullDateTimePattern"]
            // for any format not in its dictionary, silently destroying the user's custom format!
            var _targetTool = new ButlerTool_DeviceAPI_GetLocalDateTime(null);
            _targetTool.Initialize(); // part of the contract of the default running is it DOES calls initalize.
            string customIsoFormat = "{\"format\": \"1\"}";

            var result = _targetTool.ResolveMyTool(customIsoFormat, "func_time_001", null);

            Assert.IsNotNull(result, "Tool returned null result.");
            string outputText = result.GetCombinedText();

            // We passed a invalid format to DateTime.Now.ToString(), fallback is FUllDateTime/the default
            Assert.IsTrue(outputText.Contains("Monday")
                           || outputText.Contains("Tuesday")
                           || outputText.Contains("Wednesday")
                           || outputText.Contains("Thursday")
                           || outputText.Contains("Friday")
                           || outputText.Contains("Saturday")
                           || outputText.Contains("Sunday"),
                           outputText);

        }

        [TestMethod]
        public void Sombra_Attack_CustomFormatString_MustNotBeHijackedByFullDateTimePattern()
        {
            // WHAT THIS TESTS:
            // The tool advertises support for standard .NET DateTime format strings.
            // Sombra requests a standard ISO format: "yyyy-MM-dd".
            // FAULTY ASSUMPTION: SpecialCaseChecks falls back to SpecialCases["FullDateTimePattern"]
            // for any format not in its dictionary, silently destroying the user's custom format!
            var _targetTool = new ButlerTool_DeviceAPI_GetLocalDateTime(null);
            _targetTool.Initialize(); // part of the contract of the default running is it DOES calls initalize.
            string customIsoFormat = "{\"format\": \"yyyy-MM-dd\"}";

            var result = _targetTool.ResolveMyTool(customIsoFormat, "func_time_001", null);

            Assert.IsNotNull(result, "Tool returned null result.");
            string outputText = result.GetCombinedText();

            // Expected: "2026-08-17" (length 10)
            // Buggy Actual: "Monday, August 17, 2026 8:00:00 AM"
            bool isCustomFormatRespected = outputText.Length == 10 && outputText.Contains("-");

            // PASS: "yyyy-MM-dd" was respected.
            // FAIL: Tool hijacked the format with FullDateTimePattern.
            Assert.IsTrue(isCustomFormatRespected,
                $"SOMBRA FLAW CONFIRMED: Custom format 'yyyy-MM-dd' was hijacked and returned: '{outputText}'");
        }

        [TestMethod]
        public void Moira_Attack_SpecialCaseAlias_WithBraces_ResolvesCorrectly()
        {
            // WHAT THIS TESTS:
            // Verifies that passing an alias with braces like "{ShortDatePattern}" resolves cleanly.
            var _targetTool = new ButlerTool_DeviceAPI_GetLocalDateTime(null);
            _targetTool.Initialize(); // part of the contract of the default running is it DOES calls initalize.
            string aliasWithBraces = "{\"format\": \"{ShortDatePattern}\"}";

            var result = _targetTool.ResolveMyTool(aliasWithBraces, "func_time_002", null);

            Assert.IsNotNull(result);
            string outputText = result.GetCombinedText();

            // Short date should not contain full day name like "Monday"
            Assert.IsFalse(outputText.Contains("Monday") 
                           || outputText.Contains("Tuesday") 
                           || outputText.Contains("Wednesday") 
                           || outputText.Contains("Thursday") 
                           || outputText.Contains("Friday")
                           || outputText.Contains("Saturday") 
                           || outputText.Contains("Sunday"),
                $"Alias with braces failed to resolve to ShortDatePattern: '{outputText}'");
        }

        [TestMethod]
        public void Moira_Attack_SpecialCaseAlias_WithNoBraces_ResolvesCorrectly()
        {
            // WHAT THIS TESTS:
            // Verifies that passing an alias with braces like "{ShortDatePattern}" resolves cleanly.
            var _targetTool = new ButlerTool_DeviceAPI_GetLocalDateTime(null);
            _targetTool.Initialize(); // part of the contract of the default running is it DOES calls initalize.
            string aliasWithBraces = "{\"format\": \"ShortDatePattern\"}";

            var result = _targetTool.ResolveMyTool(aliasWithBraces, "func_time_002", null);

            Assert.IsNotNull(result);
            string outputText = result.GetCombinedText();

            // Short date should not contain full day name like "Monday"
            Assert.IsFalse(outputText.Contains("Monday")
                           || outputText.Contains("Tuesday")
                           || outputText.Contains("Wednesday")
                           || outputText.Contains("Thursday")
                           || outputText.Contains("Friday")
                           || outputText.Contains("Saturday")
                           || outputText.Contains("Sunday"),
                $"Alias with braces failed to resolve to ShortDatePattern: '{outputText}'");
        }
    }
}