using System;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Commons.Time;
using Vostok.Configuration.Abstractions.SettingsTree;
using Vostok.Configuration.Sources.Manual;

namespace Vostok.Configuration.Sources.Vault
{
    internal class VaultSourceState
    {
        [NotNull]
        private readonly CancellationTokenSource localCancellation;

        [CanBeNull]
        private volatile string token;

        [CanBeNull]
        private volatile TimeBudget renewBudget;

        [CanBeNull]
        private volatile ISettingsNode currentSecretData;

        public VaultSourceState(CancellationToken cancellation)
        {
            localCancellation = new CancellationTokenSource();
            Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation, localCancellation.Token).Token;
            SecretDataSource = new ManualFeedSource();
        }

        public CancellationToken Cancellation { get; }

        public ManualFeedSource SecretDataSource { get; }

        public string Token => token;

        public bool TokenNeedsRenew => renewBudget?.HasExpired ?? false;

        public TimeSpan? TimeToRenew => renewBudget?.Remaining;

        public bool IsCanceled => Cancellation.IsCancellationRequested;

        public bool HasSecretData => currentSecretData != null;

        public void SetToken(string newToken)
        {
            CancelTokenRenewal();

            Interlocked.Exchange(ref token, newToken);
        }

        public bool DropToken()
        {
            CancelTokenRenewal();

            return Interlocked.Exchange(ref token, null) != null;
        }

        public void RenewTokenAfter(TimeSpan delay)
            => Interlocked.Exchange(ref renewBudget, TimeBudget.StartNew(delay));

        public void RenewTokenImmediately()
            => Interlocked.Exchange(ref renewBudget, TimeBudget.Expired);

        public void CancelSecretUpdates()
            => localCancellation.Cancel();

        public bool UpdateSecretData(ISettingsNode newSecretData)
        {
            if (Equals(newSecretData, currentSecretData))
                return false;

            SecretDataSource.Push(currentSecretData = newSecretData);
            return true;
        }

        public void PushError(Exception error)
            => SecretDataSource.Push(currentSecretData, error);

        public void CancelTokenRenewal()
            => Interlocked.Exchange(ref renewBudget, null);
    }
}
