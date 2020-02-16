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
                    log.Info(
                        "Successfully logged in with method '{LoginMethod}'. Got a token with TTL = {TokenTTL}.",
                        login.GetType().Name,
                        loginResult.TTL?.ToPrettyString() ?? "unknown");

                    state.SetToken(loginResult.Token);
                    state.CancelTokenRenewal();

                    if (loginResult.Renewable && loginResult.TTL.HasValue)
                    {
                        var renewDelay = loginResult.TTL.Value.Multiply(0.9);
                        if (renewDelay > TimeSpan.Zero)
                        {
                            state.RenewTokenAfter(renewDelay);
                            log.Info("Scheduled a token renew after {TokenRenewDelay}.", renewDelay.ToPrettyString());
                        }
                    }
                }
                else
                {
                    if (state.IsCanceled)
                        return;

                    log.Error("Failed to login with method '{LoginMethod}'. Code = {ResponseCode}.", login.GetType().Name, (int)loginResult.Code);

                    if (loginResult.IsAccessDenied && state.DropToken())
                        log.Warn("Dropped current token.");
                }
            }
            catch (Exception error)
            {
                if (state.IsCanceled)
                    return;

                log.Error(error, "Failed to login with method '{LoginMethod}'.", login.GetType().Name);
            }
        }
    }
}
