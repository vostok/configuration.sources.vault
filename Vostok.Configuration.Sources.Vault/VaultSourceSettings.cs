using System;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Commons.Time;
using Vostok.Configuration.Sources.Vault.Login;
using Vostok.Logging.Abstractions;

namespace Vostok.Configuration.Sources.Vault
{
    /// <summary>
    /// Configuration options of <see cref="VaultSource"/>.
    /// </summary>
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

        /// <summary>
        /// Topology of the Vault cluster.
        /// </summary>
        [NotNull]
        public IClusterProvider Cluster { get; }

        /// <summary>
        /// Login method used to obtain a Vault token.
        /// </summary>
        [NotNull]
        public ILoginMethod Login { get; }

        /// <summary>
        /// Path to the secret engine was mounted on.
        /// </summary>
        [NotNull]
        public string MountPoint { get; set; } = "secret";
        
        /// <summary>
        /// Path to the secret being read (omit the <c>secret/</c> prefix here).
        /// </summary>
        [NotNull]
        public string Path { get; }

        /// <summary>
        /// Optional diagnostic log. <see cref="LogProvider"/> is used by default.
        /// </summary>
        [NotNull]
        public ILog Log { get; }

        /// <summary>
        /// If enabled (default), polls Vault with <see cref="UpdatePeriod"/> to discover new versions of the secret.
        /// </summary>
        public bool EnablePeriodicUpdates { get; set; } = true;

        /// <summary>
        /// Polling period used when <see cref="EnablePeriodicUpdates"/> is <c>true</c>.
        /// </summary>
        public TimeSpan UpdatePeriod { get; set; } = 1.Minutes();

        /// <summary>
        /// Default timeout for all Vault requests (auth and secret reads).
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = 30.Seconds();

        /// <summary>
        /// Optional cancellation token that can be used to stop periodic updates (equivalent to disposing the <see cref="VaultSource"/> instance).
        /// </summary>
        public CancellationToken Cancellation { get; set; } = CancellationToken.None;
    }
}
