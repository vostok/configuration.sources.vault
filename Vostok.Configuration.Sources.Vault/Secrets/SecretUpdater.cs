using System.Collections.Generic;
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
        private static readonly string[] SecretDataScope = {"data", "data"};
        private static readonly string[] SecretKeysScope = {"data", "keys"};

        private readonly VaultSourceState state;
        private readonly IClusterClient client;
        private readonly ILog log;
        private readonly string mountPoint;
        private readonly string path;

        private volatile TimeBudget tokenRenewCooldown = TimeBudget.Expired;

        public SecretUpdater(VaultSourceState state, IClusterClient client, ILog log, string mountPoint, string path)
        {
            this.state = state;
            this.client = client;
            this.log = log;
            this.mountPoint = mountPoint;

            var mountPointSlash = $"{mountPoint}/";
            if (path.StartsWith(mountPointSlash))
                path = path.Substring(mountPointSlash.Length);

            this.path = path;
        }

        public async Task UpdateAsync()
        {
            var data = await GetSettingsNodeAsync(path);
            if(data != null && state.UpdateSecretData(data))
                log.Info("Updated secrets to a new value");

        }

        private async Task<ISettingsNode> GetSettingsNodeAsync(string pathSegment)
        {
            ISettingsNode node;
            if (pathSegment.EndsWith("/"))
            {
                node = await GetSegmentSettingsNode(pathSegment);
            }
            else
            {
                node = await GetLeafSettingsNode(pathSegment);
            }

            return node != null ? new ObjectNode(GetLastSegmentOf(pathSegment), node.Children) : null;
        }

        private static string GetLastSegmentOf(string pathSegment)
        {
            var segments = pathSegment.Split('/');
            return segments[segments.Length - 1];
        }

        private async Task<ISettingsNode> GetSegmentSettingsNode(string pathSegment)
        {
            var getListRequest = Request.Get($"v1/{mountPoint}/metadata/{pathSegment}?list=true").WithToken(state.Token);
            var getListResponse = (await client.SendAsync(getListRequest, cancellationToken: state.Cancellation)
                .ConfigureAwait(false)).Response;

            var getListResult = new SecretReadResult(getListResponse.Code, getListResponse.Content.ToString());
            if (getListResult.IsSuccessful || getListResult.IsSecretNotFound)
            {
                var parsed = ParseSecretKeys(getListResult);
                var segmentNodes = new List<ISettingsNode>();
                foreach (var child in parsed.Children)
                {
                    var nextSegment = $"{pathSegment}{child.Value}";
                    var nextNode = await GetSettingsNodeAsync(nextSegment);
                    if (nextNode != null)
                        segmentNodes.Add(nextNode);
                }

                return new ArrayNode("", segmentNodes);
            }
            else
            {
                if(!state.IsCanceled)
                    log.Warn("Failed to list secret's path '{VaultSecretPath}'. Response code = {VaultResponseCode}.", pathSegment, (int)getListResponse.Code);

                if (getListResult.IsAccessDenied && tokenRenewCooldown.HasExpired)
                {
                    state.RenewTokenImmediately();
                    tokenRenewCooldown = TimeBudget.StartNew(1.Minutes());
                }

                return null;
            }
        }

        private async Task<ISettingsNode> GetLeafSettingsNode(string pathSegment)
        {
            var request = Request.Get($"v1/{mountPoint}/data/{pathSegment}").WithToken(state.Token);

            var response = (await client.SendAsync(request, cancellationToken: state.Cancellation).ConfigureAwait(false)).Response;

            var result = new SecretReadResult(response.Code, response.Content.ToString());

            if (result.IsSuccessful || result.IsSecretNotFound)
            {
                return ParseSecretData(result);
            }
            else
            {
                if (!state.IsCanceled)
                    log.Warn("Failed to read secret '{VaultSecretPath}'. Response code = {VaultResponseCode}.", pathSegment, (int)response.Code);

                if (result.IsAccessDenied && tokenRenewCooldown.HasExpired)
                {
                    state.RenewTokenImmediately();
                    tokenRenewCooldown = TimeBudget.StartNew(1.Minutes());
                }

                return null;
            }
        }

        private ISettingsNode ParseSecretKeys(SecretReadResult result)
        {
            if (result.IsSecretNotFound)
                return new ObjectNode(null, Enumerable.Empty<ISettingsNode>());

            try
            {
                return JsonConfigurationParser.Parse(result.Payload).ScopeTo(SecretKeysScope);
            }
            catch
            {
                log.Error("Failed to parse secret data from server response.");

                return null;
            }
        }

        private ISettingsNode ParseSecretData(SecretReadResult result)
        {
            if (result.IsSecretNotFound)
                return new ObjectNode(null, Enumerable.Empty<ISettingsNode>());

            try
            {
                return JsonConfigurationParser.Parse(result.Payload).ScopeTo(SecretDataScope);
            }
            catch
            {
                log.Error("Failed to parse secret data from server response.");

                return null;
            }
        }
    }
}