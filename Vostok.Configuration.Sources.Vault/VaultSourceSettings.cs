using System;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Commons.Time;
using Vostok.Configuration.Sources.Vault.Login;
using Vostok.Logging.Abstractions;

namespace Vostok.Configuration.Sources.Vault
{
    [PublicAPI]
    public class VaultSourceSettings
    {
        public VaultSourceSettings(
            [NotNull] IClusterProvider cluster,
            [NotNull] ILoginMethod login,
            [CanBeNull] ILog log,
            [NotNull] string path)
        {
            Cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));
            Login = login ?? throw new ArgumentNullException(nameof(login));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Log = log ?? LogProvider.Get();
        }

        [NotNull]
        public IClusterProvider Cluster { get; }

        [NotNull]
        public ILoginMethod Login { get; }

        [NotNull]
        public string Path { get; }

        [NotNull]
        public ILog Log { get; }

        public bool EnablePeriodicUpdates { get; set; } = true;

        public TimeSpan UpdatePeriod { get; set; } = 1.Minutes();

        public TimeSpan RequestTimeout { get; set; } = 30.Seconds();

        public CancellationToken Cancellation { get; set; } = CancellationToken.None;
    }
}
