using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;

namespace Vostok.Configuration.Sources.Vault.Login
{
    [PublicAPI]
    public interface ILoginMethod
    {
        [ItemNotNull]
        Task<LoginResult> LoginAsync([NotNull] IClusterClient client, CancellationToken cancellation);
    }
}
