using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbehave;
using Xunit;
using Xunit.Abstractions;

namespace RelationalLock.Tests {

    public class RelationalLockBuilderTests : TestBase {
        private RelationalLockBuilder builder;
        private RelationalLockConfigurator configurator;

        #region init

        public RelationalLockBuilderTests(ITestOutputHelper helper) : base(helper) {
            Setup();
        }

        [Background]
        public void Setup() {
            configurator = new RelationalLockConfigurator();
            builder = new RelationalLockBuilder();
        }

        #endregion init

        [Fact(DisplayName = "キーを追加しないとBuildでエラーになることの確認")]
        public void NoKeyTest() {
            var builder = new RelationalLockBuilder();
            new Action(() => builder.Build(configurator)).Should().Throw<ArgumentException>();
        }

        [Scenario(DisplayName = "ビルド結果と有効なキーの確認まで")]
        public void SuccessTest() {
            IRelationalLockManager manager = default;
            "初期化"
                .x(() => {
                    configurator.RegisterRelation("key1", "key3", "key2");
                    configurator.RegisterRelation("key2", "key4");
                    manager = builder.Build(configurator);
                });
            "nullでないこと"
                .x(() => manager.Should().NotBeNull());
            "有効なキーがリレーションを追加したキーの昇順であること"
                .x(() => manager.AvailableKeys.Should().Equal("key1", "key2", "key3", "key4"));
        }
    }
}
