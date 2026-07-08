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

namespace QuantConnect.Brokerages.Template.Tests
{
    [TestFixture, Ignore("Not implemented")]
    public class TemplateBrokerageSymbolMapperTests
    {
        [Test]
        public void ReturnsCorrectLeanSymbol()
        {

        }

        [Test]
        public void ReturnsCorrectBrokerageSymbol()
        {

        }

        // Equity tickers change over the life of a SID (e.g. GOOCV -> GOOG, BLBX -> ALOY).
        // A Symbol can still carry the old ticker in Value — for example an order reloaded
        // from disk, whose Value defaults to the SID permtick. The mapper must send the
        // ticker that is current today: resolve it with
        // SecurityIdentifier.Ticker(symbol, DateTime.UtcNow) for an equity and with
        // SecurityIdentifier.Ticker(symbol.Underlying, DateTime.UtcNow) for an equity
        // option root — never read it from Symbol.Value, Symbol.Underlying.Value or ID.Symbol.
        // Reference fix: https://github.com/QuantConnect/Lean.Brokerages.CharlesSchwab/pull/95
        [TestCase("GOOCV", "GOOG", Description = "GOOCV was renamed to GOOG on 2014/04/02")]
        [TestCase("GOOG", "GOOGL", Description = "GOOG was renamed to GOOGL on 2014/04/02")]
        public void ReturnsCurrentTickerAfterEquityTickerChange(string permtick, string currentTicker)
        {
            TestGlobals.Initialize();

            // TODO: replace with the symbol mapper of the new brokerage
            ISymbolMapper symbolMapper = null;

            var current = Symbol.Create(currentTicker, SecurityType.Equity, Market.USA);
            var historical = new Symbol(current.ID, permtick);

            Assert.AreEqual(historical, current);
            Assert.AreNotEqual(historical.Value, current.Value);

            // The historical Symbol goes first: if the mapper caches by Symbol (SID-based
            // equality), the old ticker must not poison the cache for the current Symbol.
            // Contains keeps the assertions valid for any brokerage symbol format
            // (plain ticker, OSI option string, ticker with market suffix, ...).
            var fromHistorical = symbolMapper.GetBrokerageSymbol(historical);
            Assert.IsTrue(fromHistorical.Contains(currentTicker), $"Expected the current ticker '{currentTicker}' in '{fromHistorical}'.");

            var fromCurrent = symbolMapper.GetBrokerageSymbol(current);
            Assert.IsTrue(fromCurrent.Contains(currentTicker), $"Expected the current ticker '{currentTicker}' in '{fromCurrent}'.");

            var option = Symbol.CreateOption(historical, Market.USA, OptionStyle.American, OptionRight.Call, 5m, new DateTime(2026, 6, 8));
            var fromOption = symbolMapper.GetBrokerageSymbol(option);
            Assert.IsTrue(fromOption.Contains(currentTicker), $"Expected the current ticker '{currentTicker}' in '{fromOption}'.");
        }
    }
}
