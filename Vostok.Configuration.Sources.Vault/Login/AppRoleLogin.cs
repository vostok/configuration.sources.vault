using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class AppRoleLogin : LoginMethod
    {
        public AppRoleLogin([NotNull] string roleId, [NotNull] string secretId)
            : this(() => roleId, () => secretId)
        {
        }

        public AppRoleLogin([NotNull] Func<string> roleIdProvider, [NotNull] Func<string> secretIdProvider)
            : base(
                () => Request
                    .Post("v1/auth/approle/login")
                    .WithContent($"{{ \"role_id\": \"{roleIdProvider()}\", \"secret_id\": \"{secretIdProvider()}\" }}"))
        {
        }
    }
}
