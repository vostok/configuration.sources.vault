using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Clusterclient.Transport;
using Vostok.Commons.Threading;
using Vostok.Commons.Time;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Configuration.Sources.Vault.Login;
using Vostok.Configuration.Sources.Vault.Secrets;
using Vostok.Logging.Abstractions;

#pragma warning disable 4014

namespace Vostok.Configuration.Sources.Vault
{
    /// <summary>
    /// <para></para>
    /// <para></para>
    /// <para></para>
    /// <para></para>
    /// <para></para>
    /// <para></para>
    /// </summary>
    [PublicAPI]
    public class VaultSource : IConfigurationSource, IDisposable
    {
        private const string VaultServiceName = "Vault";

        private readonly VaultSourceSettings settings;
        private readonly VaultSourceState state;

        private readonly ILog log;
        private readonly IClusterClient client;

        private readonly AtomicBoolean startupGate;
        private readonly AtomicBoolean disposeGate;

        private readonly LoginHelper loginHelper;
        private readonly SecretUpdater secretUpdater;

        public VaultSource(VaultSourceSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            state = new VaultSourceState(settings.Cancellation);

            log = settings.Log.ForContext<VaultSource>().ForContext(settings.Path);

            client = new ClusterClient(
                log,
                config =>
                {
                    config.ClusterProvider = settings.Cluster;
                    config.SetupUniversalTransport();
                    config.Logging.LogReplicaRequests = false;
                    config.Logging.LogReplicaResults = false;
                    config.TargetServiceName = VaultServiceName;
                    config.DefaultTimeout = settings.RequestTimeout;
                    config.DefaultRequestStrategy = Strategy.Sequential1;
                });

            startupGate = new AtomicBoolean(false);
            disposeGate = new AtomicBoolean(false);

            loginHelper = new LoginHelper(state, client, log);
            secretUpdater = new SecretUpdater(state, client, log, settings.Path);
        }

        public IObservable<(ISettingsNode settings, Exception error)> Observe()
        {
            if (startupGate.TrySetTrue() && !state.IsCanceled)
                Task.Run(RunAsync);

            return state.SecretDataSource.Observe();
        }

        public void Dispose()
        {
            if (disposeGate.TrySetTrue())
                state.CancelSecretUpdates();
        }

        private async Task RunAsync()
        {
            while (!state.Cancellation.IsCancellationRequested)
            {
                var budget = TimeBudget.StartNew(settings.UpdatePeriod);

                try
                {
                    await EnsureTokenAsync().ConfigureAwait(false);

                    await UpdateSecretAsync().ConfigureAwait(false);

                    if (!settings.EnablePeriodicUpdates && state.HasSecretData)
                        return;

                    await WaitForNextIteration(budget).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception error)
                {
                    log.Error(error);

                    await WaitForNextIteration(budget).ContinueWith(_ => {}).ConfigureAwait(false);
                }
            }
        }

        private async Task EnsureTokenAsync()
        {
            if (state.Token == null)
                await loginHelper.LoginAsync(settings.Login).ConfigureAwait(false);

            state.Cancellation.ThrowIfCancellationRequested();

            if (state.Token != null && state.TokenNeedsRenew)
                await loginHelper.LoginAsync(new TokenLogin(state.Token)).ConfigureAwait(false);

            state.Cancellation.ThrowIfCancellationRequested();
        }

        private async Task UpdateSecretAsync()
        {
            if (state.Token == null)
                return;

            await secretUpdater.UpdateAsync().ConfigureAwait(false);

            state.Cancellation.ThrowIfCancellationRequested();
        }

        private async Task WaitForNextIteration(TimeBudget budget)
        {
            var waitDelay = budget.Remaining;

            var timeToRenew = state.TimeToRenew;
            if (timeToRenew.HasValue)
                waitDelay = TimeSpanArithmetics.Min(waitDelay, timeToRenew.Value);

            if (waitDelay > TimeSpan.Zero)
            {
                log.Info("Waiting {WaitDelay} for the next update..", waitDelay.ToPrettyString());

                await Task.Delay(waitDelay, state.Cancellation).ConfigureAwait(false);
            }
        }
    }
}
