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

using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Securities.Future;
using QuantConnect.Securities.FutureOption;
using QuantConnect.Securities.IndexOption;
using QuantConnect.Securities.Option;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Template.Tests
{
    public abstract class TemplateFeatureRegressionAlgorithm: QCAlgorithm
    {
        public abstract BrokerageName Brokerage { get; set; }
        private int _testCaseIndex;
        private bool _submittedMarketOnCloseToday;
        private DateTime _last = DateTime.MinValue;
        private List<OrderType> _orderTypes;
        private Dictionary<OrderType, Func<Slice, List<OrderTicket>>> _orderTypeMethods;
        private bool _symbolsHaveBeenSetup;
        protected BrokerageAlgorithmSettings BrokerageAlgorithmSettings;
        private Dictionary<Symbol, int> _pointsFoundPerSymbol;
        protected abstract string BrokerageSettingsURL { get; set; }

        public override void Initialize()
        {
            SetStartDate(2024, 07, 20);
            SetEndDate(2024, 07, 29);
            SetCash(100000000);
            SetCash("USDT", 10000);

            BrokerageAlgorithmSettings = new BrokerageAlgorithmSettings(BrokerageSettingsURL);
            BrokerageAlgorithmSettings.BrokerageName = Brokerage;
            SetBrokerageModel(BrokerageAlgorithmSettings.BrokerageName);
            AddSymbols();

            _orderTypeMethods = new()
            {
                { OrderType.Market, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.Market)) },
                { OrderType.Limit, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.Limit)) },
                { OrderType.StopMarket, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.StopMarket)) },
                { OrderType.StopLimit, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.StopLimit)) },
                { OrderType.MarketOnOpen, new Func<Slice, List<OrderTicket>>(slice => ExecuteMarketOnOpenOrders()) },
                { OrderType.MarketOnClose, new Func<Slice, List<OrderTicket>>(slice => ExecuteMarketOnCloseOrders()) },
                { OrderType.OptionExercise, new Func<Slice, List<OrderTicket>>(slice => ExecuteOptionExerciseOrder()) },
                { OrderType.LimitIfTouched, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.LimitIfTouched)) },
                { OrderType.ComboMarket, new Func<Slice, List<OrderTicket>>(slice => ExecuteComboOrder(slice, OrderType.ComboMarket)) },
                { OrderType.ComboLimit, new Func<Slice, List<OrderTicket>>(slice => ExecuteComboOrder(slice, OrderType.ComboLimit)) },
                { OrderType.ComboLegLimit, new Func<Slice, List<OrderTicket>>(slice => ExecuteComboOrder(slice, OrderType.ComboLegLimit)) },
                { OrderType.TrailingStop, new Func<Slice, List<OrderTicket>>(slice => ExecuteOrder(OrderType.TrailingStop)) },
            };
        }

        protected virtual void AddSymbols()
        {
            foreach(var symbol in BrokerageAlgorithmSettings.SecurityTypesToAdd)
            {
                if (symbol.Underlying?.SecurityType == SecurityType.Future)
                {
                    AddFutureOption(symbol.Underlying, BrokerageAlgorithmSettings.FutureOptionFilter);
                    continue;
                }

                var security = AddSecurity(symbol, BrokerageAlgorithmSettings.Resolution);
                dynamic filter;
                switch (symbol.SecurityType)
                {
                    case SecurityType.Option:
                        filter = BrokerageAlgorithmSettings.OptionFilter;
                        break;
                    case SecurityType.Future:
                        filter = BrokerageAlgorithmSettings.FutureFilter;
                        break;
                    case SecurityType.IndexOption:
                        filter = BrokerageAlgorithmSettings.IndexOptionFilter;
                        break;
                    case SecurityType.FutureOption:
                        filter = BrokerageAlgorithmSettings.FutureOptionFilter;
                        break;
                    default:
                        filter = null;
                        break;
                }

                if (filter != null)
                {
                    switch (symbol.SecurityType)
                    {
                        case SecurityType.Option:
                            (security as Option).SetFilter(filter);
                            break;
                        case SecurityType.Future:
                            (security as Future).SetFilter(filter);
                            break;
                        case SecurityType.FutureOption:
                            (security as FutureOption).SetFilter(filter);
                            break;
                        case SecurityType.IndexOption:
                            (security as IndexOption).SetFilter(filter);
                            break;
                    }
                }
            }
        }

        protected virtual void SetupSymbols()
        {
            BrokerageAlgorithmSettings.InitializeSymbols();
            _orderTypes = _orderTypeMethods.Keys.ToList();
            _pointsFoundPerSymbol = BrokerageAlgorithmSettings.SecurityTypes.ToDictionary(x => x, x => 0);
            _symbolsHaveBeenSetup = true;
        }

        public override void OnData(Slice slice)
        {
            if (!SetFutureContract(slice))
            {
                Debug($"{Time}: Waiting for future contract to be set...");
                return;
            }

            if (!SetOptionContract(BrokerageAlgorithmSettings.CanonicalOptionSymbol, slice.OptionChains.Values, SecurityType.Option, ref BrokerageAlgorithmSettings.OptionContract))
            {
                Debug($"{Time}: Waiting for option contract to be set...");
                return;
            }

            if (!SetOptionContract(BrokerageAlgorithmSettings.CanonicalIndexOptionSymbol, slice.OptionChains.Values, SecurityType.IndexOption, ref BrokerageAlgorithmSettings.IndexOptionContract))
            {
                Debug($"{Time}: Waiting for index option contract to be set...");
                return;
            }

            if (!SetOptionContract(BrokerageAlgorithmSettings.CanonicalFutureOptionSymbol, slice.OptionChains.Values, SecurityType.FutureOption, ref BrokerageAlgorithmSettings.FutureOptionContract))
            {
                Debug($"{Time}: Waiting for future option contract to be set...");
                return;
            }

            if (!_symbolsHaveBeenSetup)
            {
                SetupSymbols();
            }

            foreach (var symbol in BrokerageAlgorithmSettings.SecurityTypes)
            {
                if (!symbol.IsCanonical() && Securities[symbol].Price == 0)
                {
                    Debug($"{Time}: Waiting for {symbol} to have price...");
                    return;
                }
            }

            foreach(var symbol in slice.Keys)
            {
                if (BrokerageAlgorithmSettings.SecurityTypes.Contains(symbol))
                {
                    _pointsFoundPerSymbol[symbol]++;
                }
            }

            if (Portfolio.Invested)
            {
                Debug($"{Time}: Liquidating so we start from scratch");
                Liquidate();
                return;
            }

            if (Transactions.GetOpenOrders().Count > 0)
            {
                Debug($"{Time}: Cancelling open orders so we start from scratch");
                Transactions.CancelOpenOrders();
                return;
            }

            var testCase = _orderTypes[_testCaseIndex];
            var result = _orderTypeMethods[testCase](slice);

            if (result != null && result.Any(x => x.Status == OrderStatus.Invalid) && (BrokerageAlgorithmSettings.SymbolToTestPerOrderType.ContainsKey(testCase)))
            {
                throw new RegressionTestException($"Brokerage was supposed to accept orders of type {testCase} but one order was invalid: " +
                    $"{string.Join(", ", result.Where(x => x.Status == OrderStatus.Invalid))}");
            }

            _testCaseIndex++;
            if (_testCaseIndex == _orderTypes.Count)
            {
                AssertHistory();
                Quit();
            }
        }

        public override void OnEndOfAlgorithm()
        {
            foreach(var symbol in _pointsFoundPerSymbol.Where(x => x.Value == 0).Select(x => x.Key))
            {
                throw new RegressionTestException($"No data was found for {symbol} symbol");
            }
        }

        protected virtual void AssertHistory()
        {
            IEnumerable<IBaseData> history = default;
            foreach (var symbol in BrokerageAlgorithmSettings.SecurityTypes)
            {
                foreach(var resolution in BrokerageAlgorithmSettings.ResolutionsPerSecurity[symbol.SecurityType])
                {
                    foreach(var type in BrokerageAlgorithmSettings.DataTypesPerSecurity[symbol.SecurityType])
                    {
                        Debug($"{type.Name} history request for {symbol.SecurityType} symbol {symbol} at {resolution} resolution ");
                        if (type == typeof(QuoteBar))
                        {
                            history = History<QuoteBar>(symbol, 50, resolution).ToList();
                        }
                        else if (type == typeof(TradeBar))
                        {
                            history = History<TradeBar>(symbol, 50, resolution).ToList();
                        }

                        if (history.Count() != 50)
                        {
                            throw new Exception($"50 {type.Name}'s were expected for {symbol.SecurityType} symbol {symbol} at {resolution} resolution, but just obtained {history.Count()}");
                        }
                    }
                }
            }
        }

        protected virtual decimal GetOrderPrice(Symbol symbol, bool aboveTheMarket)
        {
            var assetPrice = Securities[symbol].Price;

            if (aboveTheMarket)
            {
                if (symbol.SecurityType.IsOption() && assetPrice >= 2.95m)
                {
                    return (assetPrice + 0.05m).DiscretelyRoundBy(0.05m);
                }
                assetPrice = assetPrice + Math.Min(assetPrice * 0.001m, 0.25m);
            }
            else
            {
                if (symbol.SecurityType.IsOption() && assetPrice >= 2.95m)
                {
                    return (assetPrice - 0.05m).DiscretelyRoundBy(0.05m);
                }
                assetPrice = assetPrice - Math.Min(assetPrice * 0.001m, 0.25m);
            }
            return assetPrice;
        }

        protected virtual bool SetOptionContract(Symbol canonical, ICollection<OptionChain> chains, SecurityType securityType, ref Symbol contract)
        {
            if (canonical == null)
            {
                return true;
            }

            if (contract  == null)
            {
                foreach(var chain in chains)
                {
                    var atmContract = chain
                        .Where(x => x.Symbol.SecurityType == securityType)
                        .OrderByDescending(x => x.Expiry)
                        .ThenBy(x => Math.Abs(chain.Underlying.Price - x.Strike))
                        .ThenByDescending(x => x.Right)
                        .FirstOrDefault();

                    if (atmContract != null)
                    {
                        contract = atmContract.Symbol;
                        break;
                    }
                }
            }

            return contract != null;
        }

        protected virtual bool SetFutureContract(Slice slice)
        {
            if (BrokerageAlgorithmSettings.CanonicalFutureSymbol == null)
            {
                return true;
            }

            if (BrokerageAlgorithmSettings.FutureContract == null)
            {
                foreach (var chain in slice.FutureChains.Values)
                {
                    var contract = (from futuresContract in chain.OrderBy(x => x.Expiry)
                                    where futuresContract.Expiry > Time.Date.AddDays(29)
                                    select futuresContract
                    ).FirstOrDefault();
                    if (contract != null)
                    {
                        BrokerageAlgorithmSettings.FutureContract = contract.Symbol;
                        break;
                    }
                }
            }
            return BrokerageAlgorithmSettings.FutureContract != null;
        }

        protected virtual List<OrderTicket> ExecuteOrder(OrderType orderType)
        {
            if (!BrokerageAlgorithmSettings.SymbolToTestPerOrderType.TryGetValue(orderType, out var symbols))
            {
                symbols = BrokerageAlgorithmSettings.SecurityTypes; ;
            }

            Debug($"{Time}: Sending {orderType} orders");
            var result = new List<OrderTicket>();
            foreach (var symbol in symbols)
            {
                OrderTicket orderTicket = default;
                decimal aboveTheMarket;
                switch (orderType)
                {
                    case OrderType.Market:
                        orderTicket = MarketOrder(symbol, GetOrderQuantity(symbol));
                        break;
                    case OrderType.Limit:
                        orderTicket = LimitOrder(symbol, -GetOrderQuantity(symbol), GetOrderPrice(symbol, aboveTheMarket: false));
                        break;
                    case OrderType.StopMarket:
                        orderTicket = StopMarketOrder(symbol, GetOrderQuantity(symbol), GetOrderPrice(symbol, aboveTheMarket: true));
                        break;
                    case OrderType.StopLimit:
                        aboveTheMarket = GetOrderPrice(symbol, aboveTheMarket: false);
                        orderTicket = StopLimitOrder(symbol, -GetOrderQuantity(symbol), aboveTheMarket, aboveTheMarket);
                        break;
                    case OrderType.LimitIfTouched:
                        aboveTheMarket = GetOrderPrice(symbol, aboveTheMarket: false);
                        orderTicket = LimitIfTouchedOrder(symbol, GetOrderQuantity(symbol), GetOrderPrice(symbol, aboveTheMarket: false), aboveTheMarket);
                        break;
                    case OrderType.TrailingStop:
                        orderTicket = TrailingStopOrder(symbol, (int)GetOrderQuantity(symbol), 0.1m, true);
                        break;
                }
                result.Add(orderTicket);
            }

            return result;
        }

        protected virtual List<OrderTicket> ExecuteMarketOnOpenOrders()
        {
            var result = new List<OrderTicket>();
            if (Time.Date != _last.Date) // each morning submit a market on open order
            {
                Debug($"{Time}: Sending MarketOnOpen orders");
                
                if (!BrokerageAlgorithmSettings.SymbolToTestPerOrderType.TryGetValue(OrderType.MarketOnOpen, out var symbols))
                {
                    symbols = BrokerageAlgorithmSettings.SecurityTypes; ;
                }
                foreach (var symbol in symbols)
                {
                    if (!Securities[symbol].Exchange.Hours.IsMarketAlwaysOpen)
                    {
                        result.Add(MarketOnOpenOrder(symbol, GetOrderQuantity(symbol)));
                    }
                }

                _submittedMarketOnCloseToday = false;
                _last = Time;
            }

            return result;
        }

        protected virtual List<OrderTicket> ExecuteMarketOnCloseOrders()
        {
            var result = new List<OrderTicket>();
            if (!_submittedMarketOnCloseToday) // once the exchange opens submit a market on close order
            {
                _submittedMarketOnCloseToday = true;
                _last = Time;
                Debug($"{Time}: Sending MarketOnClose orders");
                if (!BrokerageAlgorithmSettings.SymbolToTestPerOrderType.TryGetValue(OrderType.MarketOnClose, out var symbols))
                {
                    symbols = BrokerageAlgorithmSettings.SecurityTypes; ;
                }

                foreach (var symbol in symbols)
                {
                    if (Securities[symbol].Exchange.ExchangeOpen && !Securities[symbol].Exchange.Hours.IsMarketAlwaysOpen)
                    {
                        result.Add(MarketOnCloseOrder(symbol, GetOrderQuantity(symbol)));
                    }
                }
            }

            return result;
        }

        protected virtual List<OrderTicket> ExecuteOptionExerciseOrder()
        {
            if (BrokerageAlgorithmSettings.OptionContract == null)
            {
                return null;
            }

            MarketOrder(BrokerageAlgorithmSettings.OptionContract, 1);

            // Exercise option
            Debug($"{Time}: Exercising option contract");
            var result = new List<OrderTicket>();
            result.Add(ExerciseOption(BrokerageAlgorithmSettings.OptionContract, (int)GetOrderQuantity(BrokerageAlgorithmSettings.OptionContract)));
            return result;
        }

        protected virtual List<OrderTicket> ExecuteComboOrder(Slice slice, OrderType orderType)
        {
            if (BrokerageAlgorithmSettings.OptionContract == null)
            {
                return null;
            }

            OptionChain chain;
            if (IsMarketOpen(BrokerageAlgorithmSettings.OptionContract) && slice.OptionChains.TryGetValue(BrokerageAlgorithmSettings.CanonicalOptionSymbol, out chain))
            {
                var callContracts = chain.Where(contract => contract.Right == OptionRight.Call)
                    .GroupBy(x => x.Expiry)
                    .OrderBy(grouping => grouping.Key)
                    .First()
                    .OrderBy(x => x.Strike)
                    .ToList();

                // Let's wait until we have at least three contracts
                if (callContracts.Count < 2)
                {
                    return null;
                }

                Debug($"{Time}: Sending combo market orders");
                var orderLegs = new List<Leg>()
                    {
                        Leg.Create(callContracts[0].Symbol, (int)GetOrderQuantity(callContracts[0].Symbol)),
                        Leg.Create(callContracts[1].Symbol, -(int)GetOrderQuantity(callContracts[1].Symbol)),
                    };
                switch (orderType)
                {
                    case OrderType.ComboMarket:
                        return ComboMarketOrder(orderLegs, 2);
                    case OrderType.ComboLimit:
                        return ComboLimitOrder(orderLegs, 2, GetOrderPrice(callContracts[0].Symbol, false));
                    case OrderType.ComboLegLimit:
                        orderLegs = new List<Leg>()
                        {
                            Leg.Create(callContracts[0].Symbol, (int)GetOrderQuantity(callContracts[0].Symbol), GetOrderPrice(callContracts[0].Symbol, aboveTheMarket: false)),
                            Leg.Create(callContracts[1].Symbol, -(int)GetOrderQuantity(callContracts[1].Symbol), GetOrderPrice(callContracts[1].Symbol, aboveTheMarket: false)),
                        };
                        return ComboLegLimitOrder(orderLegs, 2);
                }
            }

            return null;
        }

        protected virtual decimal GetOrderQuantity(Symbol symbol)
        {
            return Securities[symbol].SymbolProperties.MinimumOrderSize ?? 1;
        }
    }
}
