using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestDataTypes
{
    public class MockHttpClient: HttpMessageHandler
    {

        public HttpResponseMessage ReturnMessage = new HttpResponseMessage();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.Run(() => { return ReturnMessage; });
        }
    }
}
