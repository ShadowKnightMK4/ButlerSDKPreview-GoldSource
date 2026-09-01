using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK.HttpClientStuff
{
    public class ButlerToolHttpTransport
    {

        /// <summary>
        /// A collection of URLs to request together
        /// </summary>
        public class CombinedCallContainer
        {
            public Dictionary<string, HttpResponseMessage?> Url = new();
        }


        private ButlerToolHttpTransport()
        {
            
            

        }


        static readonly ButlerToolHttpTransport Instance = new();
        static SocketsHttpHandler Sockets = new();
        static HttpClient Self = Self = new HttpClient(Sockets);




        public static CombinedCallContainer CreateCombinedCall()
        {
            return new CombinedCallContainer();
        }
        /// <summary>
        /// Request this page.
        /// </summary>
        /// <param name="Url">Target Page</param>
        /// <returns>Response of the request</returns>
        public static async Task<HttpResponseMessage> RequestPage(string Url)
        {
            return await Self.GetAsync(Url, HttpCompletionOption.ResponseContentRead); 
        }


        public static void AddCombinedCall(CombinedCallContainer ccc, string Url)
        {
            ccc.Url[Url] = null;
            //CombinedCalls.Add(RequestPage(Url));    
        }
        public static async Task CombinedCallsResolve(CombinedCallContainer calls)
        {
            
            List<Task> Tasks = new();
            foreach (var request in calls.Url.Keys)
            {
                Tasks.Add(Task.Run(() => {
                    var page = RequestPage(request);
                    page.Wait();
                    //var page_string = page.Result.Content.ReadAsStringAsync();
                    //await page_string;
                    calls.Url[request] = page.Result;
                }));
            }
            var Waiting = Task.WhenAll(Tasks);
            //Waiting.Start();
            Waiting.Wait();
            ;
            ;
            ;
            ;

        }
        /// <summary>
        /// If a class 
        /// </summary>
         static List<Task<HttpResponseMessage>> CombinedCalls = new List<Task<HttpResponseMessage>>();
    }
}
