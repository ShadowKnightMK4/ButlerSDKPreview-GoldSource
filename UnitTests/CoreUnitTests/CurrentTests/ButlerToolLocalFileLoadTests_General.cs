using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ButlerSDK.Tools;
namespace CoreUnitTests.CurrentTests
{
    [TestClass]
    public class ButlerToolLocalFileLoadTests_General_UNICODE
    {
        [TestMethod]
        public void LOAD_ValidatePassingNonOwnedJsonDoc_DontDispose()
        {
            bool fail = false;
            ButlerTool_LocalFile_Load testme = new(null);
            Dictionary<string, string> args = new();
            args["baseaction"] = "load";
            args["formatdiff"] = "utf8-txt";
            args["target"] = "C:\\Windows\\Temp\\doesnothing.txt";
            JsonDocument doc = null;
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
                var result = testme.ValidateToolArgs(null, doc);
                Assert.IsTrue(result); // also it accepting valid function list
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed
            }
            catch (ObjectDisposedException)
            {
                fail = true;
                Assert.Fail("Validate in file tool disposd of json it didn't own!");
            }
            finally
            {
                if (fail)
                {
                    Console.WriteLine("FAIL: File loader tool disposed json passed as an argument!");
                }
                else
                {
                    Console.WriteLine("YES! Fail load tool did NOT dispose of json it didn't own as arugment");
                }
                doc?.Dispose();
            }
        }

        [TestMethod]
        public void SAVE_ValidatePassingNonOwnedJsonDoc_DontDispose()
        {
            bool fail = false;
            ButlerTool_LocalFile_Load testme = new(null);
            Dictionary<string, string> args = new();
            args["baseaction"] = "save";
            args["formatdiff"] = "utf8-txt";
            args["target"] = "C:\\Windows\\Temp\\doesnothing.txt";
            args["data"] = "HELLO WORLD!";
            JsonDocument doc = null;
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
                var result = testme.ValidateToolArgs(null, doc);
                Assert.IsTrue(result); // also it accepting valid function list
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed
            }
            catch (ObjectDisposedException)
            {
                fail = true;
                Assert.Fail("Validate in file tool disposd of json it didn't own!");
            }
            finally
            {
                if (fail)
                {
                    Console.WriteLine("FAIL: File loader tool disposed json passed as an argument!");
                }
                else
                {
                    Console.WriteLine("YES! Fail load tool did NOT dispose of json it didn't own as arugment");
                }
                doc?.Dispose();
            }
        }
    }



    [TestClass]
    public class ButlerToolLocalFileLoadTests_General_ANSI
    {
        [TestMethod]
        public void LOAD_ValidatePassingNonOwnedJsonDoc_DontDispose()
        {
            bool fail = false;
            ButlerTool_LocalFile_Load testme = new(null);
            Dictionary<string, string> args = new();
            args["baseaction"] = "load";
            args["formatdiff"] = "format_ansi_text";
            args["target"] = "C:\\Windows\\Temp\\doesnothing.txt";
            JsonDocument doc = null;
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
                var result = testme.ValidateToolArgs(null, doc);
                Assert.IsTrue(result, "The validate function returned false on thing that it should have treated as valid"); // also it accepting valid function list
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed
            }
            catch (ObjectDisposedException)
            {
                fail = true;
                Assert.Fail("Validate in file tool disposd of json it didn't own!");
            }
            finally
            {
                if (fail)
                {
                    Console.WriteLine("FAIL: File loader tool disposed json passed as an argument!");
                }
                else
                {
                    Console.WriteLine("YES! Fail load tool did NOT dispose of json it didn't own as arugment");
                }
                doc?.Dispose();
            }
        }

        [TestMethod]
        public void SAVE_ValidatePassingNonOwnedJsonDoc_DontDispose()
        {
            bool fail = false;
            ButlerTool_LocalFile_Load testme = new(null);
            Dictionary<string, string> args = new();
            args["baseaction"] = "save";
            args["formatdiff"] = "format_ansi_text";
            args["target"] = "C:\\Windows\\Temp\\doesnothing.txt";
            args["data"] = "HELLO WORLD!";
            JsonDocument doc = null;
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(args));
                var result = testme.ValidateToolArgs(null, doc);
                Assert.IsTrue(result); // also it accepting valid function list
                var _ = doc.RootElement.ValueKind; // should trigger disposeal exception if disposed
            }
            catch (ObjectDisposedException)
            {
                fail = true;
                Assert.Fail("Validate in file tool disposd of json it didn't own!");
            }
            finally
            {
                if (fail)
                {
                    Console.WriteLine("FAIL: File loader tool disposed json passed as an argument!");
                }
                else
                {
                    Console.WriteLine("YES! Fail load tool did NOT dispose of json it didn't own as arugment");
                }
                doc?.Dispose();
            }
        }
    }
}

