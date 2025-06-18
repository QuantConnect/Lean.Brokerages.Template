/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Tests;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using System.Collections.Generic;
using QuantConnect.Tests.Brokerages;

namespace QuantConnect.Brokerages.Template.Tests
{
    [TestFixture, Ignore("Not implemented")]
    public partial class TemplateBrokerageTests : BrokerageTests
    {
        protected override Symbol Symbol { get; }
        protected override SecurityType SecurityType { get; }

        protected override IBrokerage CreateBrokerage(IOrderProvider orderProvider, ISecurityProvider securityProvider)
        {
            throw new NotImplementedException();
        }
        protected override bool IsAsync()
        {
            throw new NotImplementedException();
        }

        protected override decimal GetAskPrice(Symbol symbol)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Provides the data required to test each order type in various cases
        /// </summary>
        private static TestCaseData[] OrderParameters(string testName)
        {
            var testCases = new List<TestCaseData>()
            {
                new TestCaseData(new MarketOrderTestParameters(Symbols.BTCUSD)).SetName($"{testName}_MarketOrder_BTCUSD"),
                new TestCaseData(new LimitOrderTestParameters(Symbols.BTCUSD, 10000m, 0.01m)).SetName($"{testName}_LimitOrder_BTCUSD"),
                new TestCaseData(new StopMarketOrderTestParameters(Symbols.BTCUSD, 10000m, 0.01m)).SetName($"{testName}_StopMarketOrder_BTCUSD"),
                new TestCaseData(new StopLimitOrderTestParameters(Symbols.BTCUSD, 10000m, 0.01m)).SetName($"{testName}_StopLimitOrder_BTCUSD"),
                new TestCaseData(new LimitIfTouchedOrderTestParameters(Symbols.BTCUSD, 10000m, 0.01m)).SetName($"{testName}_LimitIfTouchedOrder_BTCUSD")
            };

            var optionSymbol = Symbol.CreateOption(Symbols.SPY, Market.USA, OptionStyle.American, OptionRight.Call, 200m, new DateTime(2029, 12, 19));
            testCases.Add(new TestCaseData(new MarketOrderTestParameters(optionSymbol)).SetName($"{testName}_MarketOrder_SPY_OPTION"));
            return testCases.ToArray();
        }
        private static TestCaseData[] CancelOrderParameters() => OrderParameters("Cancel");
        private static TestCaseData[] LongFromZeroOrderParameters() => OrderParameters("LongFromZero");
        private static TestCaseData[] CloseFromLongOrderParameters() => OrderParameters("CloseFromLong");
        private static TestCaseData[] ShortFromZeroOrderParameters() => OrderParameters("ShortFromZero");
        private static TestCaseData[] CloseFromShortOrderParameters() => OrderParameters("CloseFromShort");
        private static TestCaseData[] ShortFromLongOrderParameters() => OrderParameters("ShortFromLong");
        private static TestCaseData[] LongFromShortOrderParameters() => OrderParameters("LongFromShort");

        [Test, TestCaseSource(nameof(CancelOrderParameters))]
        public override void CancelOrders(OrderTestParameters parameters)
        {
            base.CancelOrders(parameters);
        }

        [Test, TestCaseSource(nameof(LongFromZeroOrderParameters))]
        public override void LongFromZero(OrderTestParameters parameters)
        {
            base.LongFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(CloseFromLongOrderParameters))]
        public override void CloseFromLong(OrderTestParameters parameters)
        {
            base.CloseFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(ShortFromZeroOrderParameters))]
        public override void ShortFromZero(OrderTestParameters parameters)
        {
            base.ShortFromZero(parameters);
        }

        [Test, TestCaseSource(nameof(CloseFromShortOrderParameters))]
        public override void CloseFromShort(OrderTestParameters parameters)
        {
            base.CloseFromShort(parameters);
        }

        [Test, TestCaseSource(nameof(ShortFromLongOrderParameters))]
        public override void ShortFromLong(OrderTestParameters parameters)
        {
            base.ShortFromLong(parameters);
        }

        [Test, TestCaseSource(nameof(LongFromShortOrderParameters))]
        public override void LongFromShort(OrderTestParameters parameters)
        {
            base.LongFromShort(parameters);
        }
    }
}