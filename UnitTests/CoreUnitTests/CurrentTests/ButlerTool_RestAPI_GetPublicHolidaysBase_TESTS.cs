
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ButlerSDK.Tools;
using ButlerToolContract.DataTypes;
using UnitTestDataTypes;
using ButlerSDK.ApiKeyMgr.Contract;



/// <summary>
/// The purpose of this code here is to NOT PUMP. We use it to test if the TPL context capture thing the GetPubIP tool uses in async over async code does nOT LOCK
/// </summary>
sealed class HostileSynchronizationContext : SynchronizationContext
{
    
    public override void Post(
        SendOrPostCallback d,
        object? state)
    {
        // Continuation is stranded.
        // This thread is intentionally not pumping it.
    }
}

/// <summary>
/// awaits then reutrns the response set. Used to try to deadlock a unit test
/// </summary>
/// <remarks>Learned from gemini -  if the code don't return an incomplete request - the sync context is never captured. This class does an incplete request.</remarks>
sealed class DeferredMockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public DeferredMockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Force an asynchronous yield so the task is NOT completed immediately
        await Task.Yield();

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "text/plain")
        };
    }
}


/// <summary>
/// HTTP Handler to mock http returns without fluff.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _responseContent;
    private readonly HttpStatusCode _statusCode;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
    {
        _responseContent = responseContent;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent, Encoding.UTF8, "text/plain")
        };


        {
            return Task.FromResult(response);
        }
    }
}

namespace CoreUnitTests.Calavera
    {
        [TestClass]
        public class Calavera_Audited_GetPublicIP_v2_Tests
        {
            // Subclass exposing the protected test hooks
            public class TestablePublicIPTool : ButlerTool_RestAPI_GetPublicIP
            {
                public TestablePublicIPTool() : base(null!) { }

                public void SetMockClient(HttpClient client) => ForceHttpClient(client);
                public void SetTimeoutDuration(TimeSpan timeout) => SetTimeOut(timeout);
            }
        [DataRow("NOT_AN_IP", null)]
        [DataRow("192.168.1.1", "192.168.1.1")]
            [DataRow("29.24.4.1", "29.24.4.1")]
        [DataRow("29.24.4.1                    Skip this stuff", "29.24.4.1")]
        [TestMethod]
            public void Moira_Attack_CleanIP_LoopTypo_MustNotThrowIndexOutOfRangeException(string testIPIN, string? GOODREPLY)
            {
                // WHAT THIS TESTS:
                // Sombra/Moira tests the loop: for (int j = 0; i < results.Length; j++)
                // When the remote endpoint returns a clean IP with NO whitespace ("192.168.1.1"),
                // checking 'i < results.Length' causes j to increment past the end of the string, (FIXED)
                // throwing an unhandled IndexOutOfRangeException and crashing the tool.
                // This TEST ALSO shows that the tool will sucessfully ignore anything beyond the first ip it gets returned that's not white spice
                var mockHandler = new MockHttpMessageHandler(testIPIN, HttpStatusCode.OK);
                var mockClient = new HttpClient(mockHandler);

                var tool = new TestablePublicIPTool();
                tool.SetMockClient(mockClient);

                ButlerChatToolResultMessage? result = null;


                try
                {
                    // Direct call to target with clean IP
                    result = tool.ResolveMyToolAsync("{}", "func_clean_ip_test", null).GetAwaiter().GetResult();
                }
                catch (IndexOutOfRangeException ex)
                {
                    Console.WriteLine($"[CRITICAL CRASH CONFIRMED] {ex.Message}");
                }


            if (result != null)
            {
                Assert.IsNotNull(result, "Tool returned null on a valid 200 OK IP response.");
                Assert.AreEqual(GOODREPLY, result.GetCombinedText());
            }
            else
            {
                Assert.IsNull(GOODREPLY);
            }
                
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

                var tool = new TestablePublicIPTool();
                tool.SetMockClient(mockClient);
                var result = tool.ResolveMyTool("{}", "sync_test", null);
                Assert.IsNotNull(result);

            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(cur_context);
            }
        }

            [TestMethod]
            public void Reaper_Attack_AllEndpointsFail_MustReturnDiagnosticError_NotSilentNull()
            {
                // WHAT THIS TESTS:
                // Tests behavior when all endpoints in AddressList return HTTP 500 or timeout.
                // FAULTY ASSUMPTION PROVEN: The target returns 'null' at the end of the method,
                // blinding autonomous LLM agents into infinite retry loops.
                // Note whie butlersdk informally treats null as an errror for the llm, the tool might not actually be used in that envirment
                var mockHandler = new MockHttpMessageHandler("Internal Server Error", HttpStatusCode.InternalServerError);
                var mockClient = new HttpClient(mockHandler);

                var tool = new TestablePublicIPTool();
                tool.SetMockClient(mockClient);

                var result = tool.ResolveMyToolAsync("{}", "func_fail_test", null).GetAwaiter().GetResult();

                // PASS: Tool returns a ButlerChatToolResultMessage explaining that all endpoints failed.
                // FAIL: Tool returns null, hiding the failure state from the LLM.
                Assert.IsNotNull(result,
                    "REAPER CONFIRMED: Tool returned NULL when all endpoints failed, inducing potential LLM agent retry loops. Note Butler's handling treats null as error code");

            /*    Assert.IsTrue(result.ResultMessage.Contains("Error", StringComparison.OrdinalIgnoreCase),
                    $"Expected error diagnostic, but got: '{result.ResultMessage}'");*/
            }
        }

    }
