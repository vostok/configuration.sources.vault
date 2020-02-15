using System.IO;
using JetBrains.Annotations;
using Vostok.Commons.Environment;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class HomeFolderTokenLogin : TokenLogin
    {
        public HomeFolderTokenLogin()
            : base(ReadHomeFolderToken)
        {
        }

        private static string ReadHomeFolderToken()
        {
            var path = Path.Combine(EnvironmentInfo.HomeDirectory, ".vault-token");

            if (!System.IO.File.Exists(path))
                return null;

            var content = System.IO.File.ReadAllText(path).Trim();

            return string.IsNullOrWhiteSpace(content) ? null : content;
        }
    }
}
