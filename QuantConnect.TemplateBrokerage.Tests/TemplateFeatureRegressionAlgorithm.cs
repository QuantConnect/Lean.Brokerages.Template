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
    /// <summary>
    /// This algorithm tests the order types, security types, data types
    /// and resolutions supported by a given brokerage. In order to use it,
    /// you must first create a new algorithm that inherits from this one and
    /// overrides BrokerageSettingsURL and Brokerage properties.
    /// BrokerageSettingsURL must be the URL of the brokerage settings json
    /// file from its Github repository (in raw) and Brokerage is just the
    /// name of the brokerage to test.
    ///
    /// Using the brokerage settings json file, this algorithm creates an
    /// instance of the class BrokerageAlgorithmSettings, containing the
    /// order types, security types, data types and resolutions to test.
    ///
    /// First of all, this algorithm starts adding a symbol for each security type
    /// supported by the brokerage. This is done in the AddSymbols() method. For the
    /// options, futures, index options and future options, it first add a canonical
    /// symbol so that we can later find contracts for each of these securities.
    /// Besides, it also adds a filter for these securities if it has been defined.
    ///
    /// In the second step, when we start receiving data points in the OnData()
    /// method, the algorithm tries to find contracts for the futures, options
    /// index options and future options added previously. For all these securities,
    /// if a contract is found, it saves it into a property of the 
    /// BrokerageAlgorithmSettings. For example, if an index option contract is
    /// found, it saves the contract in IndexOptionContract. However, if no
    /// one of these security types was not added, the algorithm won't search
    /// contracts for it.
    ///
    /// Once we have already added the contracts into the BrokerageAlgorithmSettings,
    /// we need to call SetupSymbols() method. This method will intialize the final
    /// dictionary of order types and symbols that will be tested in the algorithm.
    /// For each order type, the algorithm will submit an order of that type for the
    /// list of symbols associated with that order in the dictionary. For example,
    /// if the order type is StopMarket and the associated symbols associated with
    /// it in the dictionary are AAPL, EURGBP and BTCETH, the algorithm will submit
    /// a stop market order for each of those symbols. It's also worth saying, that
    /// this method can be overriden in case certain order type does not accept
    /// all the security types allowed by the brokerage. Finally, this method will
    /// also initialize a dictionary that will count the number of data points
    /// received by each symbol.
    ///
    /// In the next step the algo will wait until all the symbols added have a price.
    /// Once finished, it will increase by one the value of each symbol in the dictionary
    /// _pointsFoundPerSymbol if a data point for it is found.
    ///
    /// At this point, the algo will liquidate the holdings and cancel the open
    /// orders to then select an order type to test. For each order type, an order
    /// will be submitted for each of the symbols associated with it in
    /// BrokerageAlgorithmSettings.SymbolToTestPerOrderType. Once the orders
    /// have been submitted, the algorithm will assert that no order was invalid
    /// if the order type was supported by the brokerage. Then, the same process
    /// will be repeated for all the remaining order types. The order types that
    /// will be tested will be:
    ///
    /// 1. Market
    /// 2. Limit
    /// 3. Stop Market
    /// 4. Stop Limit
    /// 5. Market On Open
    /// 6. Market On Close
    /// 7. Option Exercise
    /// 8. LimitIfTouched
    /// 9. ComboMarket
    /// 10. ComboLimit
    /// 11. ComboLegLimit
    /// 12. TrailingStop
    ///
    /// Once all of the order types are tested, the algorithm will made a history
    /// request for 50 data points for each symbol, using the allowed resolutions
    /// and data types for it, defined in
    /// BrokerageAlgorithmSettings.ResolutionsPerSecurity and
    /// BrokerageAlgorithmSettings.DataTypesPerSecurity.
    ///
    /// Finally, at the end of the algorithm, it will be asserted that the algorithm
    /// has received at least one data point for each of the symbols added in the
    /// beggining.
    /// </summary>
    public abstract class TemplateFeatureRegressionAlgorithm: QCAlgorithm
    {
        /// <summary>
        /// Index of the order type to test from the _orderTypes list
        /// </summary>
        private int _testCaseIndex;

        /// <summary>
        /// Flag indicating that a market on open order has been submitted
        /// </summary>
        private bool _submittedMarketOnCloseToday = true;

        /// <summary>
        /// Last time an market on open order was submitted
        /// </summary>
        private DateTime _last = DateTime.MinValue;

        /// <summary>
        /// List of order types to test in this algorithm
        /// </summary>
        private List<OrderType> _orderTypes;

        /// <summary>
        /// Tests to execute for each order type
        /// </summary>
        private Dictionary<OrderType, Func<Slice, List<OrderTicket>>> _orderTypeMethods;

        /// <summary>
        /// Flag indicating that we already have the symbols that will be used in the tests.
        /// At the beginning of the algorithm we didn't have option, future, index option nor
        /// future option contracts
        /// </summary>
        private bool _symbolsHaveBeenSetup;

        /// <summary>
        /// Dictionary for counting all the data points found per symbol
        /// </summary>
        private Dictionary<Symbol, int> _pointsFoundPerSymbol;

        /// <summary>
        /// URL of brokerage settings json file
        /// </summary>
        protected abstract string BrokerageSettingsURL { get; set; }

        /// <summary>
        /// Class containing all the information of the security types, order types, data
        /// types and resolutions supported by the defined Brokerage
        /// </summary>
        protected BrokerageAlgorithmSettings BrokerageAlgorithmSettings;

        /// <summary>
        /// Brokerage to test
        /// </summary>
        public abstract BrokerageName Brokerage { get; set; }


        public override void Initialize()
        {
            SetStartDate(2024, 07, 20);
            SetEndDate(2024, 07, 29);
            SetCash(100000000);
            SetCash("USDT", 10000);

            /// Create an instance of class BrokerageAlgorithmSettings using the brokerage settings url.
            /// For example: https://raw.githubusercontent.com/QuantConnect/Lean.Brokerages.InteractiveBrokers/master/interactivebrokers.json
            /// This object will contain the security types, order types, resolutions and data types
            /// allowed by the brokerage
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

        /// <summary>
        /// This method will add one symbol for each of the securities allowed by this brokerage
        /// </summary>
        protected virtual void AddSymbols()
        {
            foreach(var symbol in BrokerageAlgorithmSettings.Symbols)
            {
                if (symbol.Underlying?.SecurityType == SecurityType.Future)
                {
                    AddFutureOption(symbol.Underlying, BrokerageAlgorithmSettings.FutureOptionFilter);
                    continue;
                }

                var security = AddSecurity(symbol, BrokerageAlgorithmSettings.Resolution);
                switch (symbol.SecurityType)
                {
                    case SecurityType.Option:
                        if (BrokerageAlgorithmSettings.OptionFilter != null)
                        {
                            (security as Option).SetFilter(BrokerageAlgorithmSettings.OptionFilter);
                        }
                        break;
                    case SecurityType.Future:
                        if (BrokerageAlgorithmSettings.FutureFilter != null)
                        {
                            (security as Future).SetFilter(BrokerageAlgorithmSettings.FutureFilter);
                        }
                        break;
                    case SecurityType.IndexOption:
                        if (BrokerageAlgorithmSettings.IndexOptionFilter != null)
                        {
                            (security as IndexOption).SetFilter(BrokerageAlgorithmSettings.IndexOptionFilter);
                        }
                        break;
                    case SecurityType.FutureOption:
                        if (BrokerageAlgorithmSettings.FutureOptionFilter != null)
                        {
                            (security as FutureOption).SetFilter(BrokerageAlgorithmSettings.FutureOptionFilter);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Once the future, option, index option and future option contracts have been set,
        /// this method will call `BrokerageAlgorithmSettings.InitializeSymbols()` method.
        /// That method will define the securities allowed per order type according to the 
        /// brokerage
        /// </summary>
        protected virtual void SetupSymbols()
        {
            BrokerageAlgorithmSettings.InitializeSymbols();
            _orderTypes = _orderTypeMethods.Keys.ToList();
            _pointsFoundPerSymbol = BrokerageAlgorithmSettings.Symbols.ToDictionary(x => x, x => 0);
            _symbolsHaveBeenSetup = true;
        }

        public override void OnData(Slice slice)
        {
            /// Set contracts for futures, options, index options and future options
            if (!SetContracts(slice)) return;

            /// Define the securities to test per order type
            if (!_symbolsHaveBeenSetup)
            {
                SetupSymbols();
            }

            /// Check all the securities have a price
            if (!AllSecuritiesHavePrice()) return;

            /// Collect the data points we are getting for each symbol
            foreach(var symbol in slice.Keys)
            {
                if (BrokerageAlgorithmSettings.Symbols.Contains(symbol))
                {
                    _pointsFoundPerSymbol[symbol]++;
                }
            }

            /// In order to prevent running out of funds, liquidate the current holdings
            if (Portfolio.Invested || Transactions.GetOpenOrders().Count > 0)
            {
                Debug($"{Time}: Liquidating so we start from scratch");
                Liquidate();

                Debug($"{Time}: Cancelling open orders so we start from scratch");
                Transactions.CancelOpenOrders();
                return;
            }

            /// Select the order type to test and execute the test associated with it
            var testCase = _orderTypes[_testCaseIndex];
            var result = _orderTypeMethods[testCase](slice);

            /// Check all the order tickets from the test result are valid, except the case
            /// when the order type was not allowed in the brokerage
            if (result != null && result.Any(x => x.Status == OrderStatus.Invalid) && (BrokerageAlgorithmSettings.SymbolToTestPerOrderType.ContainsKey(testCase)))
            {
                throw new RegressionTestException($"Brokerage was supposed to accept orders of type {testCase} but one order was invalid: " +
                    $"{string.Join(", ", result.Where(x => x.Status == OrderStatus.Invalid))}");
            }

            /// Update the index to test the next order type on the next OnData() call
            _testCaseIndex++;

            /// Once finished, assert we can get history data points for each security
            /// at the allowed resolutions and with the defined data types by the brokerage
            if (_testCaseIndex == _orderTypes.Count)
            {
                AssertHistory();
                Quit();
            }
        }

        /// <summary>
        /// At the end of the algorithm, asserts we obtained at least one data point for each
        /// of the tested securities
        /// </summary>
        public override void OnEndOfAlgorithm()
        {
            foreach(var symbol in _pointsFoundPerSymbol.Where(x => x.Value == 0).Select(x => x.Key))
            {
                throw new RegressionTestException($"No data was found for {symbol} symbol");
            }
        }

        /// <summary>
        /// Asserts we can get history data points for each of the tested securities using the allowed
        /// resolutions and data types by the brokerage
        /// </summary>
        protected virtual void AssertHistory()
        {
            IEnumerable<IBaseData> history = default;
            foreach (var symbol in BrokerageAlgorithmSettings.Symbols)
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

        /// <summary>
        /// Gets a price for an order based on the symbol. This price can be above
        /// the market or not depending on the parameter `aboveTheMartet`
        /// </summary>
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

        /// <summary>
        /// This method will get one option contract for the added canonical option (If options 
        /// are allowed by the brokerage). Once one option contract is found it will save it in
        /// the contract symbol provided by reference
        /// </summary>
        protected virtual bool SetOptionContract(Symbol canonical, ICollection<OptionChain> chains, SecurityType securityType, ref Symbol contract)
        {
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

        /// <summary>
        /// This method will get one future contract for the added canonical future (If options 
        /// are allowed by the brokerage). Once one future contract is found it will save it in
        /// the `BrokerageAlgorithmSettings.FutureContract` property
        /// </summary>
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

        /// <summary>
        /// Executes one of the following orders: Market order, Limit order, Stop Market order,
        /// Stop Limit order, LimitIfTouched order or TrailingStop order
        /// </summary>
        protected virtual List<OrderTicket> ExecuteOrder(OrderType orderType)
        {
            if (!BrokerageAlgorithmSettings.SymbolToTestPerOrderType.TryGetValue(orderType, out var symbols))
            {
                symbols = BrokerageAlgorithmSettings.Symbols;
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

        /// <summary>
        /// Executes a Market On Open order for the allowed security types
        /// </summary>
        protected virtual List<OrderTicket> ExecuteMarketOnOpenOrders()
        {
            var result = new List<OrderTicket>();
            if (Time.Date != _last.Date) // each morning submit a market on open order
            {
                Debug($"{Time}: Sending MarketOnOpen orders");
                
                if (!BrokerageAlgorithmSettings.SymbolToTestPerOrderType.TryGetValue(OrderType.MarketOnOpen, out var symbols))
                {
                    symbols = BrokerageAlgorithmSettings.Symbols;
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

        /// <summary>
        /// Executes a Market On Close order for the allowed security types
        /// </summary>
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
                    symbols = BrokerageAlgorithmSettings.Symbols;
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

        /// <summary>
        /// Executes an order to exercise an option if one has been set
        /// </summary>
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

        /// <summary>
        /// Executes one of the following orders: Combo Market order, Combo Limit order or
        /// Combo Leg Limit order
        /// </summary>
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

        /// <summary>
        /// Gets the minimum order quantity allowed by the brokerage for the given symbol
        /// </summary>
        protected virtual decimal GetOrderQuantity(Symbol symbol)
        {
            return Securities[symbol].SymbolProperties.MinimumOrderSize ?? 1;
        }

        private bool SetContracts(Slice slice)
        {
            /// Check if the brokerage supports futures, if it does find a contract and add it to BrokerageAlgorithmSettings.FutureContract
            if (BrokerageAlgorithmSettings.CanonicalFutureSymbol != null && !SetFutureContract(slice))
            {
                Debug($"{Time}: Waiting for future contract to be set...");
                return false;
            }

            /// Check if the brokerage supports options, if it does find a contract and add it to BrokerageAlgorithmSettings.OptionContract
            if (BrokerageAlgorithmSettings.CanonicalOptionSymbol != null && !SetOptionContract(BrokerageAlgorithmSettings.CanonicalOptionSymbol, slice.OptionChains.Values, SecurityType.Option, ref BrokerageAlgorithmSettings.OptionContract))
            {
                Debug($"{Time}: Waiting for option contract to be set...");
                return false;
            }

            /// Check if the brokerage supports index option, if it does find a contract and add it to BrokerageAlgorithmSettings.IndexOptionContract
            if (BrokerageAlgorithmSettings.CanonicalIndexOptionSymbol != null && !SetOptionContract(BrokerageAlgorithmSettings.CanonicalIndexOptionSymbol, slice.OptionChains.Values, SecurityType.IndexOption, ref BrokerageAlgorithmSettings.IndexOptionContract))
            {
                Debug($"{Time}: Waiting for index option contract to be set...");
                return false;
            }

            /// Check if the brokerage supports future options, if it does find a contract and add it to BrokerageAlgorithmSettings.FutureOptionContract
            if (BrokerageAlgorithmSettings.CanonicalFutureOptionSymbol != null && !SetOptionContract(BrokerageAlgorithmSettings.CanonicalFutureOptionSymbol, slice.OptionChains.Values, SecurityType.FutureOption, ref BrokerageAlgorithmSettings.FutureOptionContract))
            {
                Debug($"{Time}: Waiting for future option contract to be set...");
                return false;
            }

            return true;
        }

        private bool AllSecuritiesHavePrice()
        {
            /// Check each symbol allowed by the brokerage has already a price
            foreach (var symbol in BrokerageAlgorithmSettings.Symbols)
            {
                if (!symbol.IsCanonical() && Securities[symbol].Price == 0)
                {
                    Debug($"{Time}: Waiting for {symbol} to have price...");
                    return false;
                }
            }

            return true;
        }
    }
}
