using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;
using Vostok.Configuration.Sources.Json;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class LoginMethod : ILoginMethod
    {
        private const string AuthScope = "auth";

        private readonly Func<Request> requestFactory;

        public LoginMethod(Func<Request> requestFactory)
            => this.requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));

        public async Task<LoginResult> LoginAsync(IClusterClient client, CancellationToken cancellation)
        {
            var request = requestFactory();

            var response = (await client.SendAsync(request, cancellationToken: cancellation).ConfigureAwait(false)).Response;
            if (response.IsSuccessful && response.HasContent)
            {
                var responseSource = new JsonStringSource(response.Content.ToString());
                var responseDto = ConfigurationProvider.Default.Get<LoginDto>(responseSource.ScopeTo(AuthScope));

                return new LoginResult(
                    response.Code,
                    responseDto.client_token,
                    responseDto.renewable ?? false,
                    responseDto.lease_duration?.Seconds());
            }

            return LoginResult.Failure(response.Code);
        }
        
        private class LoginDto
        {
            [UsedImplicitly]
            public string client_token { get; }

            [UsedImplicitly]
            public int? lease_duration { get;  }

            [UsedImplicitly]
            public bool? renewable { get; }
        }
    }
}
