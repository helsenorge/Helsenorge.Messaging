using Helsenorge.Messaging.Abstractions;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Http
{
    [Obsolete("Will be removed in 4.0.0", false)]
    class HttpServiceBusReceiver : IMessagingReceiver
    {
        private readonly string _url;
        private readonly string _id;

        public HttpServiceBusReceiver(string url, string id)
        {
            _url = url;
            _id = id;   
        }

        public bool IsClosed => false;

        public void Close()
        {
            
        }


        public const string CLIENT_HEADER_NAME = "AMQP_HTTP_CLIENT";

        public static string GetClientHeaderValue()
        {
            var cp = Process.GetCurrentProcess();
            return $"{cp.ProcessName} pid:{cp.Id} on {Environment.MachineName}";
        }

        public async Task<IMessagingMessage> ReceiveAsync(TimeSpan serverWaitTime)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_url), _id));
            request.Headers.Add(CLIENT_HEADER_NAME, GetClientHeaderValue());
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await Task.Delay(serverWaitTime);
                return null;
            }
            else
            {
                await Task.Delay(serverWaitTime);
                var responseString = await response.Content.ReadAsStringAsync();
                return new IncomingHttpMessage
                {
                    AMQPMessage = XElement.Parse(responseString)
                };
            }            
        }
    }
}
