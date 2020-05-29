using Amqp;
using Helsenorge.Messaging.ServiceBus.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Messaging.ServiceBus
{
    internal class ServiceBusHttpClient
    {
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _authLock = new SemaphoreSlim(1);

        private const int DefaultStsPort = 9355;
        private const string StsPath = "$STS/OAuth/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Uri _stsEndpointUrl;
        private readonly string _username;
        private readonly string _password;
        private string _token;

        public ServiceBusHttpClient(Address address, ILogger logger) : this(address, logger, new DefaultHttpClientFactory())
        {
        }

        public ServiceBusHttpClient(Address address, ILogger logger, IHttpClientFactory httpClientFactory)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _stsEndpointUrl = new Uri($"{(address.UseSsl ? "https" : "http")}://{address.Host}:{DefaultStsPort}{address.Path}");
            _username = address.User;
            _password = address.Password;
        }

        public async Task RenewLockAsync(string queueName, long sequenceNumber, Guid lockToken, TimeSpan lockTimeout, TimeSpan timeout)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentException(nameof(queueName));
            }

            if (sequenceNumber <= 0)
            {
                throw new ArgumentException(nameof(sequenceNumber));
            }

            if (lockToken == Guid.Empty)
            {
                throw new ArgumentException(nameof(lockToken));
            }

            _logger.LogDebug("Renewing lock for message {queueName}/{sequenceNumber}/{lockToken}",
                queueName, sequenceNumber, lockToken);

            var remainingTime = await GetAuthTokenAsync(timeout);

            var url = new Uri(_stsEndpointUrl, $"{_stsEndpointUrl.LocalPath}/{queueName}/messages/{sequenceNumber:####}/{lockToken}?timeout={lockTimeout.TotalSeconds:####}");
            using (var client = _httpClientFactory.CreateClient())
            {
                client.Timeout = remainingTime;
                client.DefaultRequestHeaders.Add("Authorization", _token);

                var response = await client.PostAsync(url, new StringContent(""));
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Successfully renewed lock for message {queueName}/{sequenceNumber}/{lockToken",
                        queueName, sequenceNumber, lockToken);
                    return;
                }

                var content = await response.Content.ReadAsStringAsync();
                var message = $"Renew lock endpoint returned {response.StatusCode} status code ({content})";
                _logger.LogDebug(message);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedException(message);

                    case HttpStatusCode.NotFound:
                        throw new MessageNotFoundException($"Message with sequence number {sequenceNumber} and lock token {lockToken} not found in queue {queueName} ({content})");

                    case HttpStatusCode.Gone:
                        throw new MessageNotFoundException($"Queue {queueName} does not exist ({content})");

                    default:
                        throw new RecoverableServiceBusException(message);
                }
            }
        }

        private async Task<TimeSpan> GetAuthTokenAsync(TimeSpan timeout)
        {
            if (_token != null)
            {
                return timeout;
            }

            var deadlineUtc = DateTime.UtcNow + timeout;

            await _authLock.WaitAsync();

            try
            {
                if (_token == null)
                {
                    _token = await GetOAuthTokenFromSts(timeout);
                }
            }
            finally
            {
                _authLock.Release();
            }

            var remainingTime = deadlineUtc - DateTime.UtcNow;
            if (remainingTime <= TimeSpan.Zero)
            {
                throw new ServiceBusTimeoutException($"Timeout of {timeout} exceeded while obtaining auth token");
            }

            return remainingTime;
        }

        private async Task<string> GetOAuthTokenFromSts(TimeSpan timeout)
        {
            var requestUri = new Uri(_stsEndpointUrl, StsPath);
            _logger.LogDebug("Retrieving oauth token from STS endpoint {requestUri}", requestUri);

            using (var client = new HttpClient())
            {
                client.Timeout = timeout;
                var response = await client.PostAsync(requestUri, new FormUrlEncodedContent(
                    new List<KeyValuePair<string, string>>
                    {
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                        new KeyValuePair<string, string>("client_id", _username),
                        new KeyValuePair<string, string>("client_secret", _password),
                        new KeyValuePair<string, string>("scope", _stsEndpointUrl.AbsoluteUri)
                    }));

                var content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Oauth token successfully retrieved from STS endpoint {requestUri}", requestUri);
                    return string.Format(CultureInfo.InvariantCulture, "WRAP access_token=\"{0}\"", content);
                }

                var message = $"Sts auth returned {response.StatusCode} status code ({content})";
                _logger.LogDebug(message);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.Unauthorized:
                        throw new UnauthorizedException(message);

                    default:
                        throw new RecoverableServiceBusException(message);
                }
            }
        }
    }

    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 1
            });
        }
    }
}
