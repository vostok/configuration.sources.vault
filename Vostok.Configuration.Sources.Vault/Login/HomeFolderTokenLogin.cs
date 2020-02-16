using System.IO;
using JetBrains.Annotations;
using Vostok.Commons.Environment;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class HomeFolderTokenLogin : TokenLogin
    {
        public HomeFolderTokenLogin()
            : base(() => System.IO.File.ReadAllText(Path.Combine(EnvironmentInfo.HomeDirectory, ".vault-token")).Trim())
        {
        }
    }
}
