using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwitchBot
{
    public class SwitchBotHandler : DelegatingHandler
    {
        private readonly string _token;
        private readonly byte[] _secretBytes;

        public SwitchBotHandler(IOptions<SwitchBotOptions> options)
        {
            _token = options.Value.Token;
            _secretBytes = Encoding.UTF8.GetBytes(options.Value.Secret);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nonce = Guid.NewGuid().ToString();

            using var signer = new HMACSHA256()
            {
                Key = _secretBytes
            };

            var stringToSign = string.Format("{0}{1}{2}", _token, time, nonce);
            var signature = Convert.ToBase64String(signer.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stringToSign)));
            request.Headers.TryAddWithoutValidation("Authorization", _token);
            request.Headers.TryAddWithoutValidation("t", time.ToString());
            request.Headers.TryAddWithoutValidation("sign", signature);
            request.Headers.TryAddWithoutValidation("nonce", nonce);
                        
            return await Policy<HttpResponseMessage>
                .HandleResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests || r.StatusCode == System.Net.HttpStatusCode.Forbidden)
                .WaitAndRetryAsync([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120)])
                .ExecuteAsync(async () => await base.SendAsync(request, cancellationToken));
        }
    }
}
