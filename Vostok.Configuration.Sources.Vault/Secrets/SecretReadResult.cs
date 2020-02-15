using Vostok.Clusterclient.Core.Model;

namespace Vostok.Configuration.Sources.Vault.Secrets
{
    internal class SecretReadResult
    {
        public SecretReadResult(ResponseCode code, string payload)
        {
            Code = code;
            Payload = payload;
        }

        public string Payload { get; }

        public bool IsSuccessful => Code.IsSuccessful() && !string.IsNullOrWhiteSpace(Payload);

        public bool IsSecretNotFound => Code == ResponseCode.NotFound;

        public bool IsAccessDenied => Code == ResponseCode.Forbidden;

        private ResponseCode Code { get; }
    }
}
