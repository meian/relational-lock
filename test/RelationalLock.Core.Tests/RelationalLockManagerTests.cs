using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xbehave;
using Xunit.Abstractions;

namespace RelationalLock.Tests {

    public class RelationalLockManagerTests : TestBase {
        private IRelationalLockBuilder builder;
        private RelationalLockConfigurator configurator;

        #region setup

        public RelationalLockManagerTests(ITestOutputHelper helper) : base(helper) {
        }

        [Background]
        public void Setup() {
            configurator = new RelationalLockConfigurator();
            builder = new RelationalLockBuilder();
        }

        private (RelationalLockConfigurator configurator, IRelationalLockBuilder builder) NewRelationalBuilderSet() =>
            (new RelationalLockConfigurator(), new RelationalLockBuilder());

        #endregion setup

        [Scenario(DisplayName = "デフォルト有効期限が有効になること")]
        public void DefaultExpiredInTest() {
            IRelationalLockManager manager = default;
            "初期化"
                .x(c => {
                    configurator.DefaultExpireIn = TimeSpan.FromMilliseconds(500);
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                });
            "1つ目のロック取得"
                .x(() => {
                    manager.AcquireLock("key1").Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                });
            "関連キーのロック取得が時間内に処理されることの確認(TimeSpan)"
                .x(() => {
                    manager.AcquireLock("key2", (TimeSpan?)TimeSpan.FromMilliseconds(1000), (TimeSpan?)default).Should().BeTrue();
                    manager.Release("key2");
                });
            "1つ目のロック取得(再テスト用)"
               .x(() => {
                   manager.AcquireLock("key1").Should().BeTrue();
                   manager.GetState("key1").State.Should().Be(LockState.Locked);
               });
            "関連キーのロック取得が時間内に処理されることの確認(DateTime)"
                 .x(() => {
                     manager.AcquireLock("key2", TimeSpan.FromSeconds(2000), expireAt: (DateTime?)default).Should().BeTrue();
                     manager.Release("key2");
                 });
        }

        [Scenario(DisplayName = "デフォルトタイムアウトが有効になること")]
        public void DefaultTimeoutTest() {
            IRelationalLockManager manager = default;
            var source = new CancellationTokenSource();
            "初期化"
                .x(c => {
                    configurator.DefaultTimeout = TimeSpan.FromMilliseconds(1000);
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                    source.CancelAfter(2000);
                });
            "1つ目のロック取得"
                .x(() => {
                    manager.AcquireLock("key1").Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                });
            "関連キーのロック取得でタイムアウトが起こることの確認"
                .x(() => {
                    try {
                        var task = Task.Run(() => {
                            manager.AcquireLock("key2").Should().BeFalse();
                        });
                        task.Wait(source.Token);
                    }
                    catch (OperationCanceledException) {
                        Helper.WriteLine("too long wait for key2.");
                        throw;
                    }
                });
        }

        [Scenario(DisplayName = "関連性ないキーはロックされないことの確認")]
        public void IsolationTest() {
            IRelationalLockManager manager = null;
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

        [Scenario(DisplayName = "タイムアウトが起こることの確認(int)")]
        public void TimeoutIntTest() {
            IRelationalLockManager manager = default;
            "初期化"
                .x(c => {
                    configurator.RegisterRelation("key1", "key2");
                    manager = builder.Build(configurator).Using(c);
                });
            "ロック取得"
                .x(async () => {
                    manager.AcquireLock("key1", 100, 1000).Should().BeTrue();
                    manager.GetState("key1").State.Should().Be(LockState.Locked);
                    await Task.Delay(100);
                });
            "有効期間内は取得できない"
                .x(() => {
                    manager.AcquireLock("key2", 500).Should().BeFalse();
                });
            "有効期間が過ぎたら取得できる"
                .x(() => {
                    manager.AcquireLock("key2", 500).Should().BeTrue();
                });
        }

        [Scenario(DisplayName = "タイムアウトが起こることの確認")]
        public void TimeoutTest() {
            IRelationalLockManager manager = default;
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
