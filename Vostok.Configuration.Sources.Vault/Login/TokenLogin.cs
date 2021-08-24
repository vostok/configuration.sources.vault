using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Configuration.Sources.Vault.Helpers;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class TokenLogin : ILoginMethod
    {
        private readonly Func<string> tokenProvider;
        private LoginMethod renew;

        public TokenLogin([NotNull] string token)
            : this(() => token)
        {
        }

        public TokenLogin([NotNull] Func<string> tokenProvider)
        {
            this.tokenProvider = tokenProvider;
            renew = new LoginMethod(() => Request.Post("v1/auth/token/renew-self").WithToken(tokenProvider()));
        }

        public bool Renew { get; set; }

        public Task<LoginResult> LoginAsync(IClusterClient client, CancellationToken cancellation) =>
            Renew
                ? renew.LoginAsync(client, cancellation)
                : Task.FromResult(new LoginResult(ResponseCode.Ok, tokenProvider(), false, null));
    }
}