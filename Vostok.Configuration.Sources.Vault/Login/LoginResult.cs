using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public class LoginResult
    {
        public LoginResult(ResponseCode code, string token, bool renewable, TimeSpan? ttl)
        {
            TTL = ttl;
            Code = code;
            Token = token;
            Renewable = renewable;
        }

        public static LoginResult Failure(ResponseCode code)
            => new LoginResult(code, null, false, null);

        public ResponseCode Code { get; }

        public string Token { get; }

        public bool Renewable { get; }

        public TimeSpan? TTL { get; }

        public bool IsSkipped { get; set; }
        
        public bool IsSuccessful => Code.IsSuccessful() && !string.IsNullOrWhiteSpace(Token);

        public bool IsAccessDenied => Code == ResponseCode.Forbidden;
    }
}
