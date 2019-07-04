using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbehave;
using Xunit.Abstractions;

namespace RelationalLock.Tests {

    public class RelationalLockConfiguratorTests : TestBase {
        private RelationalLockConfigurator cfg;

        #region init

        public RelationalLockConfiguratorTests(ITestOutputHelper helper) : base(helper) {
            Setup();
        }

        [Background]
        public void Setup() {
            cfg = new RelationalLockConfigurator();
        }

        #endregion init

        [Scenario(DisplayName = "TimeSpan系のDefaultプロパティ")]
        public void DefaultTimeSpanTest() {
            "初期値はint.MaxValueの最大値"
                .x(() => {
                    var span = TimeSpan.FromMilliseconds(int.MaxValue);
                    cfg.DefaultTimeout.Should().Be(span);
                    cfg.DefaultExpireIn.Should().Be(span);
                });
            "タイムアウトを変更"
                .x(() => cfg.DefaultTimeout = TimeSpan.FromSeconds(10));
            "有効期限を変更"
                .x(() => cfg.DefaultExpireIn = TimeSpan.FromMinutes(1));
            "既定値が変更されている"
                .x(() => {
                    cfg.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(10));
                    cfg.DefaultExpireIn.Should().Be(TimeSpan.FromMinutes(1));
                });
            "不正なタイムアウトはエラー"
                .x(() => {
                    cfg.DefaultTimeout = TimeSpan.FromSeconds(1);
                    new Action(
                        () => cfg.DefaultTimeout = LockTimeConstants.MinTimeSpan.Add(-TimeSpan.FromMilliseconds(1))
                        ).Should().Throw<ArgumentException>();
                    new Action(
                        () => cfg.DefaultTimeout = LockTimeConstants.MaxTimeSpan.Add(TimeSpan.FromMilliseconds(1))
                        ).Should().Throw<ArgumentException>();
                    cfg.DefaultTimeout.Should().Be(TimeSpan.FromSeconds(1), $"{nameof(cfg.DefaultTimeout)} has changed.");
                });
            "不正な有効期限はエラー"
                .x(() => {
                    cfg.DefaultExpireIn = TimeSpan.FromSeconds(1);
                    new Action(
                        () => cfg.DefaultExpireIn = LockTimeConstants.MinTimeSpan.Add(-TimeSpan.FromMilliseconds(1))
                        ).Should().Throw<ArgumentException>();
                    new Action(
                        () => cfg.DefaultExpireIn = LockTimeConstants.MaxTimeSpan.Add(TimeSpan.FromMilliseconds(1))
                        ).Should().Throw<ArgumentException>();
                    cfg.DefaultExpireIn.Should().Be(TimeSpan.FromSeconds(1), $"{nameof(cfg.DefaultExpireIn)} has changed.");
                });
        }

        [Scenario(DisplayName = "例外が出るケースいろいろ")]
        public void ExceptionScenarios() {
            "キー配列が空"
                .x(() => new Action(() => cfg.RegisterRelation(null)).Should().Throw<ArgumentNullException>());
            "キーが空"
                .x(() => new Action(() => cfg.RegisterRelation()).Should().Throw<ArgumentException>());
            "キーが1つ"
                .x(() => new Action(() => cfg.RegisterRelation("key1")).Should().Throw<ArgumentException>());
            "1つ目のキーがnull"
                .x(() => new Action(() => cfg.RegisterRelation(null, "key2")).Should().Throw<ArgumentException>());
            "1つ目のキーが空文字"
                .x(() => new Action(() => cfg.RegisterRelation("", "key2", "key3")).Should().Throw<ArgumentException>());
            "2つ目のキーがnull"
                .x(() => new Action(() => cfg.RegisterRelation("key1", null, "key3")).Should().Throw<ArgumentException>());
            "2つ目のキーが空文字"
                .x(() => new Action(() => cfg.RegisterRelation("key1", "")).Should().Throw<ArgumentException>());
            "同じキーが含まれる"
                .x(() => new Action(() => cfg.RegisterRelation("key1", "key2", "key1")).Should().Throw<ArgumentException>());
            "例外の時は登録されない"
                .x(() => cfg.RelationCount.Should().Be(0));
            "登録なしで情報取得しようとする"
                .x(() => new Action(() => cfg.GetInfos()).Should().Throw<InvalidOperationException>());
        }

        [Scenario(DisplayName = "キーペアの関連性を登録する")]
        public void SimpleSuccessScenario() {
            object registerRelation = null;
            IEnumerable<RelationalInfo> infos = null;
            "key1-key2のリレーション追加"
                .x(() => registerRelation = cfg.RegisterRelation("key2", "key1"));
            "registerの結果はnullにはならない"
                .x(() => {
                    registerRelation.Should().NotBeNull();
                    Helper.WriteLine($"{nameof(cfg.RegisterRelation)} result type: {registerRelation.GetType()}");
                });
            "キーは2件、関連は1件"
                .x(() => {
                    cfg.KeyCount.Should().Be(2);
                    cfg.RelationCount.Should().Be(1);
                });
            "key1とkey2は関連性を持っている"
                .x(() => {
                    infos = cfg.GetInfos().ToList();
                    var info1 = infos.InfoByKey("key1");
                    info1.RelatedKeys.Should().BeEquivalentTo("key2");
                    infos.InfoByKey("key2").RelatedKeys.Should().BeEquivalentTo("key1");
                    Helper.WriteLine($"lockKey: {info1.LockKeys[0]}");
                });
            "key1-key3のリレーションを追加"
                .x(() => infos = cfg.RegisterRelation("key1", "key3").GetInfos().ToList());
            "キーは3件、関連は2件"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(2);
                });
            "key1は関連キーが2件"
                .x(() => infos.InfoByKey("key1").RelatedKeys.Should().BeEquivalentTo("key2", "key3"));
            "key3は関連キーが1件"
                .x(() => infos.InfoByKey("key3").RelatedKeys.Should().BeEquivalentTo("key1"));
            "key2はkey3と紐付かない"
                .x(() => {
                    infos.InfoByKey("key2").RelatedKeys.Should().NotContain("key3");
                    infos.InfoByKey("key3").RelatedKeys.Should().NotContain("key2");
                });
            "登録済のリレーションを追加"
                .x(() => infos = cfg.RegisterRelation("key1", "key2").GetInfos().ToList());
            "件数は変わらない"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(2);
                });
        }

        [Scenario(DisplayName = "サブセットを登録しても関連が増えないことの確認")]
        public void SubsetTest() {
            "複数キーの関連の追加"
                .x(() => cfg.RegisterRelation("key1", "key3", "key2"));
            "キーは3件、関連は1件"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(1);
                });
            "サブセットの追加"
                .x(() => {
                    cfg.RegisterRelation("key1", "key2");
                    cfg.RegisterRelation("key3", "key2");
                });
            "キーは3件、関連は1件"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(1);
                });
        }

        [Scenario(DisplayName = "スーパーセットを登録すると関連が統合されることの確認")]
        public void SupersetTest() {
            "複数の個別関連の追加"
                .x(() => {
                    cfg.RegisterRelation("key1", "key2");
                    cfg.RegisterRelation("key3", "key2");
                });
            "キーは3件、関連は2件"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(2);
                });
            "スーパーセットの追加"
                .x(() => {
                    cfg.RegisterRelation("key1", "key2", "key3");
                });
            "キーは3件、関連は1件"
                .x(() => {
                    cfg.KeyCount.Should().Be(3);
                    cfg.RelationCount.Should().Be(1);
                });
        }
    }

    internal static class Extensions {

        public static RelationalInfo InfoByKey(this IEnumerable<RelationalInfo> infos, string key) => infos.First(info => info.Key == key);
    }
}
