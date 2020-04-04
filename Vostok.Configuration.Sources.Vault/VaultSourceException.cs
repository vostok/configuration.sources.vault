using System;

namespace Vostok.Configuration.Sources.Vault
{
    internal class VaultSourceException : Exception
    {
        public VaultSourceException(string message)
            : base(message)
        {
        }
    }
}
