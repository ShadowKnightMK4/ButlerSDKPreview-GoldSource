using ButlerSDK.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static CoreUnitTests.Calavera.Calavera_Audited_GetPublicIP_v2_Tests;

namespace CoreUnitTests.Calavera.ToolGenTests.PubHolid
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using ButlerSDK.Tools;

    namespace CoreUnitTests.Calavera
    {
        [TestClass]
        public class Calavera_Audited_HolidayTool_Tests
        {
            [TestMethod]
            public void Cal_Attack_StringYear_InversionBug_MustAcceptValidStringNumber()
            {
                // WHAT THIS TESTS:
                // Sombra/Cal tests the inverted logic in ValidateToolArgs:
                // if (int.TryParse(Year.GetString(), out _)) { return false; }
                // Because of the missing '!', passing a valid string year like "2024"
                // causes ValidateToolArgs to return FALSE!

                var tool = new ButlerTool_RestAPI_GetPublicHolidays(null!);

                // Valid JSON with year as a string (standard LLM output)
                string validJsonWithStringYear = "{\"year\": \"2024\", \"country\": \"US\"}";
                using var doc = JsonDocument.Parse(validJsonWithStringYear);

                bool isValid = tool.ValidateToolArgs(null, doc);

                // ASSERTION:
                // PASS: "2024" is recognized as a valid numeric string.
                // FAIL: The inverted logic rejected the valid string year.
                Assert.IsTrue(isValid,
                    "CRITICAL FLAW CONFIRMED: ValidateToolArgs rejected a valid string year \"2024\" due to inverted int.TryParse logic.");
            }

            [TestMethod]
            public void Moira_Attack_NegativeYear_MustNotThrowUnhandledException()
            {
                // WHAT THIS TESTS:
                // In ValidateToolArgs: Year.GetUInt32() is called on numeric values.
                // If the LLM passes a negative number (e.g. -2024), GetUInt32() throws
                // an unhandled InvalidOperationException/FormatException instead of returning false.

                var tool = new ButlerTool_RestAPI_GetPublicHolidays(null!);

                string negativeYearJson = "{\"year\": -2024, \"country\": \"US\"}";
                using var doc = JsonDocument.Parse(negativeYearJson);

                bool threwException = false;
                bool validationResult = false;

                try
                {
                    validationResult = tool.ValidateToolArgs(null, doc);
                }
                catch (Exception ex)
                {
                    threwException = true;
                    Console.WriteLine($"[CRASH CONFIRMED] GetUInt32 threw: {ex.GetType().Name}");
                }

                Assert.IsFalse(threwException,
                    "MOIRA FLAW: Passing a negative number crashed ValidateToolArgs via Year.GetUInt32().");
                Assert.IsFalse(validationResult, "Validation should cleanly return false for negative years.");
            }
        }
    }


    [TestClass]
    public class ButlerTool_RestAPI_PubHoliday
    {
        public class TestablePublicHoliday : ButlerTool_RestAPI_GetPublicHolidays
        {
            public TestablePublicHoliday() : base(null!) { }

            public void SetMockClient(HttpClient client) => ForceHttpClient(client);
            public void SetTimeoutDuration(TimeSpan timeout) => SetTimeOut(timeout);
        }

        [TestMethod]
        [Timeout(1000)] // need that time out. we're gunna freeze if the code fials.
        public void SyncCode_HostileSync_DontFreeze()
        {
            // WHAT THIS ATTEMPTS TO TEST
            // Does the sync routine NOT DEAD LOCK
            // That's it. We don't care about anything elsei in the thing.
            var cur_context = SynchronizationContext.Current;

            try
            {
                SynchronizationContext.SetSynchronizationContext(new HostileSynchronizationContext());
                var mockHandler = new DeferredMockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
                var mockClient = new HttpClient(mockHandler);

                var tool = new TestablePublicHoliday();
                tool.SetMockClient(mockClient);
                Dictionary<string, string> x = new();
                x["country"] = "US";
                x["year"] = "2028";
                var result = tool.ResolveMyTool(JsonSerializer.Serialize(x), "sync_test", null);
                Assert.IsNotNull(result);

            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(cur_context);
            }
        }


        [TestMethod]
        public void LIFEACTION_RequestCurrentYearHolidays()
        {
            var tool = new ButlerTool_RestAPI_GetPublicHolidays(null);
            Dictionary<string, string> x = new();
            x["country"] = "US";
            x["year"] = "2028";
            var result = tool.ResolveMyTool(JsonSerializer.Serialize(x), "livefire", null);
            Assert.IsNotNull(result);
        }
    }
}
