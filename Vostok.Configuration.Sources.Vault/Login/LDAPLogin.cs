using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class LDAPLogin : LoginMethod
    {
        public LDAPLogin([NotNull] string user, [NotNull] string password)
            : this(() => user, () => password)
        {
        }

        public LDAPLogin([NotNull] Func<string> userProvider, [NotNull] Func<string> passwordProvider)
            : base(
                () => Request
                    .Post($"v1/auth/ldap/login/{userProvider()}")
                    .WithContent($"{{ \"password\": \"{passwordProvider()}\" }}"))
        {
        }
    }
}
