using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Configuration.Sources.Vault.Helpers
{
    internal static class RequestExtensions
    {
        public static Request WithToken(this Request request, [NotNull] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Provided token was either null or empty.", nameof(token));

            return request.WithHeader("X-Vault-Token", token);
        }
    }
}
