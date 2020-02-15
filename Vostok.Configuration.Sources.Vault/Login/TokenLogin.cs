using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Configuration.Sources.Vault.Helpers;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class TokenLogin : LoginMethod
    {
        public TokenLogin([NotNull] string token)
            : this(() => token)
        {
        }

        public TokenLogin([NotNull] Func<string> tokenProvider)
            : base(() => Request.Post("v1/auth/token/renew-self").WithToken(tokenProvider()))
        {
        }
    }
}
