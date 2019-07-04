using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xbehave;
using Xbehave.Sdk;
using Xunit.Abstractions;

namespace RelationalLock.Tests {

    public class RelationalLockManagerTests : TestBase {

        #region setup

        public RelationalLockManagerTests(ITestOutputHelper helper) : base(helper) {
        }

        [Background]
        public void Setup() {
        }

        private (RelationalLockConfigurator configurator, IRelationalLockBuilder builder) NewRelationalBuilderSet() =>
            (new RelationalLockConfigurator(), new RelationalLockBuilder());

        #endregion setup

        [Scenario(DisplayName = "関連性ないキーはロックされないことの確認")]
        public void IsolationTest() {
            IRelationalLockManager manager = null;
            var (configurator, builder) = NewRelationalBuilderSet();
            "初期化"
                .x(c => {
                    configurator.RegisterRelation("key1", "key2");
                    configurator.RegisterRelation("key1", "key3");
                    configurator.RegisterRelation("key3", "key4");
                    manager = builder.Build(configurator).Using(c);
                });
            "キーのロック"
                .x(() => manager.AcquireLock("key2", TimeSpan.FromMilliseconds(100)).Should().BeTrue());
            "関連性のあるキーはロックされる"
                .x(() => manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100)).Should().BeFalse());
            "関連性のないキーはロックを取得できる"
                .x(() => {
                    manager.AcquireLock("key4", TimeSpan.FromMilliseconds(100)).Should().BeTrue();
                    manager.Release("key4");
                });
            "直接関連性のないキーはロックを取得できる"
                .x(() => manager.AcquireLock("key3", TimeSpan.FromMilliseconds(100)).Should().BeTrue());
            "最初のキーリリース後も関連キーはロックされている"
                .x(() => {
                    manager.Release("key2");
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100)).Should().BeFalse();
                });
            "関連キーをすべてリリースするとロックが取得できる"
                .x(() => {
                    manager.Release("key3");
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100)).Should().BeTrue();
                    manager.Release("key1");
                });
            Thread.Sleep(1000);
        }

        [Scenario(DisplayName = "同じキーをロック試行してタイムアウトが起こることの確認")]
        public void SelfTimeoutTest() {
            IRelationalLockManager manager = default;
            var (configurator, builder) = NewRelationalBuilderSet();
            "初期化"
                .x(c => {
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                });
            "ロック取得"
                .x(async () => {
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(3000)).Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                    await Task.Delay(100);
                });
            "有効期間内は取得できない"
                .x(() => {
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(500)).Should().BeFalse();
                });
            "ロックのリリース"
                .x(() => {
                    manager.Release("key1");
                    manager.GetState("key1").State.Should().Be(LockState.Unlocked);
                });
            "リリース以降は取得できる"
                .x(() => {
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(500)).Should().BeTrue();
                    Helper.WriteLine($"{DateTime.Now:HH:mm:ss.fff}");
                });
        }

        [Scenario(DisplayName = "正常に関連ロックが取得できることの確認")]
        public void SuccessTest() {
            IRelationalLockManager manager = default;
            var (configurator, builder) = NewRelationalBuilderSet();
            "初期化"
                .x(c => {
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                });
            "ロック取得"
                .x(() => {
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100)).Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                });
            "関連ロックの取得試行"
                .x(() => {
                    manager.AcquireLock("key2", TimeSpan.FromSeconds(1)).Should().BeFalse();
                    manager.GetState("key2").State.Should().Be(LockState.Locked);
                });
            "ロック開放"
                .x(() => {
                    manager.Release("key1");
                    manager.GetState("key1").State.Should().Be(LockState.Unlocked);
                    manager.GetState("key2").State.Should().Be(LockState.Unlocked);
                });
            "関連ロックの取得再試行"
                .x(() => {
                    manager.AcquireLock("key2", TimeSpan.FromMilliseconds(100)).Should().BeTrue();
                    manager.GetState("key2").State.Should().Be(LockState.Locked);
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100)).Should().BeFalse();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                    manager.Release("key2");
                });
        }

        [Scenario(DisplayName = "タイムアウトが起こることの確認")]
        public void TimeoutTest() {
            IRelationalLockManager manager = default;
            var (configurator, builder) = NewRelationalBuilderSet();
            "初期化"
                .x(c => {
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                });
            "ロック取得"
                .x(async () => {
                    manager.AcquireLock("key1", TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)).Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                    await Task.Delay(100);
                });
            "有効期間内は取得できない"
                .x(() => {
                    manager.AcquireLock("key2", TimeSpan.FromMilliseconds(500)).Should().BeFalse();
                });
            "有効期間が過ぎたら取得できる"
                .x(() => {
                    manager.AcquireLock("key2", TimeSpan.FromSeconds(500)).Should().BeTrue();
                });
        }
    }
}
