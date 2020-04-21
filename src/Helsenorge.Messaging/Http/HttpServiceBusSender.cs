using Helsenorge.Messaging.Abstractions;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Helsenorge.Messaging.Http
{
    [Obsolete("Will be removed in 4.0.0", false)]
    public class HttpServiceBusSender : IMessagingSender
    {
        private readonly string _url;
        private readonly string _id;

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
            var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_url), _id));
            request.Headers.Add(HttpServiceBusReceiver.CLIENT_HEADER_NAME, HttpServiceBusReceiver.GetClientHeaderValue());
            request.Content = new StringContent(
                (message as OutgoingHttpMessage).CreateHttpBody().ToString()
            );
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new ArgumentException($"Error from AMQP/HTTP server: {responseContent}");
            }
        }
    }
}