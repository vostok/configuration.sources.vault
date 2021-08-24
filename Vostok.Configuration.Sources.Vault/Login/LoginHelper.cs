using System;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

#pragma warning disable 4014

namespace Vostok.Configuration.Sources.Vault.Login
{
    internal class LoginHelper
    {
        private readonly VaultSourceState state;
        private readonly IClusterClient client;
        private readonly ILog log;

        public LoginHelper(VaultSourceState state, IClusterClient client, ILog log)
        {
            this.state = state;
            this.client = client;
            this.log = log;
        }

        public async Task LoginAsync(ILoginMethod login)
        {
            try
            {
                var loginResult = await login.LoginAsync(client, state.Cancellation).ConfigureAwait(false);
                if (loginResult.IsSuccessful)
                {
                    if (!loginResult.IsSkipped)
                        log.Info(
                            "Successfully logged in with method '{VaultLoginMethod}'. Got a token with TTL = {VaultTokenTTL}.",
                            login.GetType().Name,
                            loginResult.TTL?.ToPrettyString() ?? "unknown");

                    state.SetToken(loginResult.Token);

                    if (loginResult.Renewable && loginResult.TTL.HasValue)
                    {
                        var renewDelay = loginResult.TTL.Value.Multiply(0.9);
                        if (renewDelay > TimeSpan.Zero)
                        {
                            state.RenewTokenAfter(renewDelay);
                            log.Info("Scheduled a token renew after {VaultTokenRenewDelay}.", renewDelay.ToPrettyString());
                        }
                    }
                }
                else
                {
                    if (state.IsCanceled)
                        return;

                    log.Error("Failed to login with method '{VaultLoginMethod}'. Code = {VaultResponseCode}.", login.GetType().Name, (int)loginResult.Code);

                    if (loginResult.IsAccessDenied && state.DropToken())
                        log.Warn("Dropped current token.");

                    state.CancelTokenRenewal();
                }
            }
            catch (Exception error)
            {
                if (state.IsCanceled)
                    return;

                log.Error(error, "Failed to login with method '{VaultLoginMethod}'.", login.GetType().Name);

                state.CancelTokenRenewal();
            }
        }
    }
}