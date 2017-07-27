using Helsenorge.Messaging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Http
{
    public class HttpServiceBusSender : IMessagingSender
    {
        private string _url;
        private string _id;

        public HttpServiceBusSender(string url, string id)
        {
            _url = url;
            _id = id;
        }

        public bool IsClosed => false;

        public void Close()
        {
            //
        }

        public async Task SendAsync(IMessagingMessage message)
        {
            Debug.Assert(message is OutgoingHttpMessage);
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(
                new Uri(new Uri(_url), _id), 
                new StringContent(
                    (message as OutgoingHttpMessage).CreateHttpBody().ToString()
                )
            );
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Error from AMQP/HTTP server: {responseContent}");
            }
        }
    }
}