using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Configuration.Abstractions.SettingsTree;

namespace Vostok.Configuration.Sources.Vault.Tests
{
    [TestFixture]
    internal class VaultSourceState_Tests
    {
        private CancellationTokenSource localCancellation;
        private VaultSourceState state;

        [SetUp]
        public void TestSetup()
        {
            localCancellation = new CancellationTokenSource();
            state = new VaultSourceState(localCancellation.Token);
        }

        [Test]
        public void Should_not_be_canceled_initially()
        {
            state.IsCanceled.Should().BeFalse();
        }

        [Test]
        public void Should_become_canceled_after_an_expicit_call()
        {
            state.CancelSecretUpdates();

            state.IsCanceled.Should().BeTrue();
        }

        [Test]
        public void Should_become_canceled_after_signaling_external_token()
        {
            localCancellation.Cancel();

            state.IsCanceled.Should().BeTrue();
        }

        [Test]
        public void Should_initially_have_no_token_and_no_need_to_renew()
        {
            state.Token.Should().BeNull();
            state.TokenNeedsRenew.Should().BeFalse();
            state.TimeToRenew.Should().BeNull();
        }

        [Test]
        public void Should_initially_have_no_secret_data()
        {
            state.HasSecretData.Should().BeFalse();
        }

        [Test]
        public void RenewTokenImmediately_should_issue_a_renew_instantly()
        {
            state.SetToken("token");

            state.RenewTokenImmediately();

            state.TokenNeedsRenew.Should().BeTrue();
            state.TimeToRenew.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void RenewTokenAfter_should_issue_a_renew_after_given_time()
        {
            state.SetToken("token");

            state.RenewTokenAfter(500.Milliseconds());

            state.TokenNeedsRenew.Should().BeFalse();

            Action assertion = () =>
            {
                state.TokenNeedsRenew.Should().BeTrue();
                state.TimeToRenew.Should().Be(TimeSpan.Zero);
            };

            assertion.ShouldPassIn(5.Seconds());
        }

        [Test]
        public void SetToken_should_update_the_token()
        {
            state.SetToken("token1");
            state.Token.Should().Be("token1");

            state.SetToken("token2");
            state.Token.Should().Be("token2");
        }

        [Test]
        public void SetToken_should_cancel_any_pending_token_renew()
        {
            state.SetToken("token1");

            state.RenewTokenImmediately();
            state.TokenNeedsRenew.Should().BeTrue();

            state.SetToken("token2");

            state.TokenNeedsRenew.Should().BeFalse();
        }

        [Test]
        public void DtopToken_should_destroy_the_token()
        {
            state.SetToken("token1");
            state.Token.Should().Be("token1");

            state.DropToken().Should().BeTrue();
            state.Token.Should().BeNull();
        }

        [Test]
        public void DropToken_should_cancel_any_pending_token_renew()
        {
            state.SetToken("token1");

            state.RenewTokenImmediately();
            state.TokenNeedsRenew.Should().BeTrue();

            state.DropToken().Should().BeTrue();

            state.TokenNeedsRenew.Should().BeFalse();
        }

        [Test]
        public void DropToken_should_return_false_when_there_is_nothing_to_drop()
        {
            state.DropToken().Should().BeFalse();
            state.DropToken().Should().BeFalse();
        }

        [Test]
        public void UpdateSecretData_should_return_true_when_there_is_new_data()
        {
            state.UpdateSecretData(new ValueNode("value1")).Should().BeTrue();
            state.UpdateSecretData(new ValueNode("value2")).Should().BeTrue();
        }

        [Test]
        public void UpdateSecretData_should_return_false_when_there_is_new_data()
        {
            state.UpdateSecretData(new ValueNode("value1")).Should().BeTrue();
            state.UpdateSecretData(new ValueNode("value1")).Should().BeFalse();
        }

        [Test]
        public void UpdateSecretData_should_change_the_value_of_HasSecretData()
        {
            state.UpdateSecretData(new ValueNode("value"));

            state.HasSecretData.Should().BeTrue();
        }
    }
}
