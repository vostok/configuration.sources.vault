using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Configuration.Sources.Json;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class LoginMethod : ILoginMethod
    {
        private const string AuthScope = "auth";
        private const string TokenField = "client_token";
        private const string RenewableField = "renewable";
        private const string LeaseDurationField = "lease_duration";

        private readonly Func<Request> requestFactory;

        public LoginMethod(Func<Request> requestFactory)
            => this.requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));

        public async Task<LoginResult> LoginAsync(IClusterClient client, CancellationToken cancellation)
        {
            var request = requestFactory();

            var response = (await client.SendAsync(request, cancellationToken: cancellation).ConfigureAwait(false)).Response;
            if (response.IsSuccessful && response.HasContent)
            {
                var authData = JsonConfigurationParser.Parse(response.Content.ToString())?.ScopeTo(AuthScope);

                return new LoginResult(
                    response.Code,
                    authData?[TokenField]?.Value,
                    bool.TryParse(authData?[RenewableField]?.Value, out var renewable) && renewable,
                    int.TryParse(authData?[LeaseDurationField]?.Value, out var duration) ? duration.Seconds() : null as TimeSpan?);
            }

            return LoginResult.Failure(response.Code);
        }
       
    }
}
