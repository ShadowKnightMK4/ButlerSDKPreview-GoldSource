using ButlerSDK.ToolSupport.DiscoverTool;
using ButlerToolContract.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiscoverToolUnitTests
{

    [TestClass]
    public sealed class DiscoverValidateTests
    {
        [TestMethod]
        public void RemoveArgument_ValidateCleanUp_OnButlerCallMessage()
        {

            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Remove";
            dummy_check["SearchTerm"] = "Any";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var msg = new ButlerChatToolCallMessage("Any", "RemoveMe", JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(msg, null);
                Assert.IsTrue(result);
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");
                doc = null;
            }

            finally
            {
                doc?.Dispose();
            }
        }


        [TestMethod]
        public void RemoveArgument_ValidateCleanUp_OnNonOwnedJsonDoc_SHOULDNOTDISPOSE()
        {
            bool yes = false;
            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Remove";
            dummy_check["SearchTerm"] = "ALL";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(null, doc);
                Assert.IsTrue(result);
                yes = true;

                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");
                yes = false;
                doc = null;
            }

            finally
            {
                if (yes)
                {
                    Console.WriteLine("The jsondocument we created then passed to validate was NOT disposed. YES, correct");
                }
                else
                {
                    Console.WriteLine("FAILED. Jsondocument we created and then passed was dipsoed by validateargs. NOOOO");
                }
                doc?.Dispose();
            }
        }


        [TestMethod]
        public void ActivateArgument_ValidateCleanUp_OnButlerCallMessage()
        {

            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Activate";
            dummy_check["SearchTerm"] = "Any";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var msg = new ButlerChatToolCallMessage("Any", "findme", JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(msg, null);
                Assert.IsTrue(result);
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");
                doc = null;
            }

            finally
            {
                doc?.Dispose();
            }
        }


        [TestMethod]
        public void ActivateArgument_ValidateCleanUp_OnNonOwnedJsonDoc_SHOULDNOTDISPOSE()
        {
            bool yes = false;
            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Activate";
            dummy_check["SearchTerm"] = "ALL";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(null, doc);
                Assert.IsTrue(result);
                yes = true;

                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                yes = false;
                doc = null;
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");

            }

            finally
            {
                if (yes)
                {
                    Console.WriteLine("The jsondocument we created then passed to validate was NOT disposed. YES, correct");
                }
                else
                {
                    Console.WriteLine("FAILED. Jsondocument we created and then passed was dipsoed by validateargs. NOOOO");
                }
                doc?.Dispose();
            }
        }


        [TestMethod]
        public void SearchArgument_ValidateCleanUp_OnNonOwnedJsonDoc_SHOULDNOTDISPOSE()
        {
            bool yes = false;
            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Search";
            dummy_check["SearchTerm"] = "Any";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(null, doc);
                Assert.IsTrue(result);
                yes = true;

                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                yes = false;
                doc = null;
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");

            }

            finally
            {
                if (yes)
                {
                    Console.WriteLine("The jsondocument we created then passed to validate was NOT disposed. YES, correct");
                }
                else
                {
                    Console.WriteLine("FAILED. Jsondocument we created and then passed was dipsoed by validateargs. NOOOO");
                }
                doc?.Dispose();
            }
        }

        [TestMethod]
        public void SearchArgument_ValidateCleanUp_OnButlerCallMessage()
        {

            // we checked if the cleanup does not rigger in the jsondoc we pass cause discover don't own it
            ButlerTool_DiscoverTools demo = new(null);
            JsonDocument doc = null;
            Dictionary<string, string> dummy_check = new();
            dummy_check["action"] = "Search";
            dummy_check["SearchTerm"] = "Any";
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(dummy_check));
                var msg = new ButlerChatToolCallMessage("Any", "findme", JsonSerializer.Serialize(dummy_check));
                var result = demo.ValidateToolArgs(msg, null);
                Assert.IsTrue(result);
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed

            }
            catch (ObjectDisposedException)
            {
                doc = null;
                Assert.Fail("The discoveror validate function disposed of a json document it didn't own!");
            }

            finally
            {
                doc?.Dispose();
            }
        }
    }
}
