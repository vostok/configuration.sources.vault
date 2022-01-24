using System.Linq;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Time;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Configuration.Sources.Json;
using Vostok.Configuration.Sources.Vault.Helpers;
using Vostok.Logging.Abstractions;

namespace Vostok.Configuration.Sources.Vault.Secrets
{
    internal class SecretUpdater
    {
        private const string SecretPrefix = "secret/";

        private readonly VaultSourceState state;
        private readonly IClusterClient client;
        private readonly ILog log;
        private readonly string root;
        private readonly string path;
        private readonly string[] scope;

        private volatile TimeBudget tokenRenewCooldown = TimeBudget.Expired;

        public SecretUpdater(VaultSourceState state, IClusterClient client, ILog log, string root, string path, string[] scope)
        {
            this.state = state;
            this.client = client;
            this.log = log;
            this.root = root;

            if (path.StartsWith(SecretPrefix))
                path = path.Substring(SecretPrefix.Length);

            this.path = path;
            this.scope = scope;
        }

        public async Task UpdateAsync()
        {
            var request = Request.Get($"v1/{root}/{path}").WithToken(state.Token);

            var response = (await client.SendAsync(request, cancellationToken: state.Cancellation).ConfigureAwait(false)).Response;

            var result = new SecretReadResult(response.Code, response.Content.ToString());

            if (result.IsSuccessful || result.IsSecretNotFound)
            {
                if (state.UpdateSecretData(ParseSecretData(result)))
                    log.Info("Updated the secret to a new value.");
            }
            else
            {
                if (!state.IsCanceled)
                    log.Warn("Failed to read secret '{VaultSecretPath}'. Response code = {VaultResponseCode}.", path, (int)response.Code);

                if (result.IsAccessDenied && tokenRenewCooldown.HasExpired)
                {
                    state.RenewTokenImmediately();
                    tokenRenewCooldown = TimeBudget.StartNew(1.Minutes());
                }
            }
        }

        private ISettingsNode ParseSecretData(SecretReadResult result)
        {
            if (result.IsSecretNotFound)
                return new ObjectNode(null, Enumerable.Empty<ISettingsNode>());

            try
            {
                return JsonConfigurationParser.Parse(result.Payload).ScopeTo(scope);
            }
            catch
            {
                log.Error("Failed to parse secret data from server response.");

                return null;
            }
        }
    }
}
