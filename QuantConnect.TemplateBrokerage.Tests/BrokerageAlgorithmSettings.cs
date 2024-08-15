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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Brokerages.Template.Tests
{
    /// <summary>
    /// Class containing the security types, order types, data types and resolutions supported by
    /// certain brokerage
    /// </summary>
    public class BrokerageAlgorithmSettings
    {
        private string _url;
        public Symbol OptionContract;
        public Symbol FutureContract;
        public Symbol FutureOptionContract;
        public Symbol IndexOptionContract;

        /// <summary>
        /// Name of the brokerage to use in the algorithm. If not specified it uses the default
        /// </summary>
        public BrokerageName BrokerageName { get; set; } = BrokerageName.Default;

        /// <summary>
        /// Resolution to use in the algorithm
        /// </summary>
        public Resolution Resolution => Resolution.Minute;

        /// <summary>
        /// Class containing symbols to use in the algorithm
        /// </summary>
        public BaseMarketSymbols MarketSymbols { get; protected set; }

        /// <summary>
        /// Equity symbol to use in the algorithm
        /// </summary>
        public Symbol EquitySymbol => MarketSymbols.EquitySymbol;

        /// <summary>
        /// Forex symbol to use in the algorithm
        /// </summary>
        public Symbol ForexSymbol => MarketSymbols.ForexSymbol;

        /// <summary>
        /// Crypto symbol to use in the algorithm
        /// </summary>
        public Symbol CryptoSymbol => MarketSymbols.CryptoSymbol;

        /// <summary>
        /// Cfd symbol to use in the algorithm
        /// </summary>
        public Symbol CfdSymbol => MarketSymbols.CfdSymbol;

        /// <summary>
        /// Crypto future symbol to use in the algorithm
        /// </summary>
        public Symbol CryptoFutureSymbol => MarketSymbols.CryptoFutureSymbol;

        /// <summary>
        /// Canonical option symbol to use in the algorithm
        /// </summary>
        public Symbol CanonicalOptionSymbol => MarketSymbols.CanonicalOptionSymbol;

        /// <summary>
        /// Canonical index option symbol to use in the algorithm
        /// </summary>
        public Symbol CanonicalIndexOptionSymbol => MarketSymbols.CanonicalIndexOptionSymbol;

        /// <summary>
        /// Canonical future symbol to use in the algorithm
        /// </summary>
        public Symbol CanonicalFutureSymbol => MarketSymbols.CanonicalFutureSymbol;

        /// <summary>
        /// Canonical future option symbol to use in the algorithm
        /// </summary>
        public Symbol CanonicalFutureOptionSymbol => MarketSymbols.CanonicalFutureOptionSymbol;

        /// <summary>
        /// Filter for the index option contracts
        /// </summary>
        public Func<OptionFilterUniverse, OptionFilterUniverse> IndexOptionFilter => null;

        /// <summary>
        /// Filter for the future contracts
        /// </summary>
        public Func<FutureFilterUniverse, FutureFilterUniverse> FutureFilter => u => u.Expiration(0, 182);

        /// <summary>
        /// Filter for the future option contracts
        /// </summary>
        public Func<OptionFilterUniverse, OptionFilterUniverse> FutureOptionFilter => u => u.Strikes(-2, +2).Expiration(0, 60);

        /// <summary>
        /// Filter for the option contracts
        /// </summary>
        public Func<OptionFilterUniverse, OptionFilterUniverse> OptionFilter => u => u.Strikes(-2, +2).Expiration(0, 60);

        /// <summary>
        /// Dictionary where the key is the order type to test in the algorithm and the value associated
        /// with is the list of symbols for which the orders will be done. For example, if the key is
        /// MarketOrder and the values are Equity, Forex and Crypto, the algorithm will make market orders
        /// for those Equity, Forex and Crypto symbols.
        /// </summary>
        public Dictionary<OrderType, List<Symbol>> SymbolToTestPerOrderType { get; protected set; }

        /// <summary>
        /// List of security types allowed by the brokerage
        /// </summary>
        public List<SecurityType> SecurityTypes { get; protected set; }

        /// <summary>
        /// List of order types allowed by the brokerage
        /// </summary>
        public List<OrderType> OrderTypes { get; protected set; }

        /// <summary>
        /// List of resolutions allowed by the brokerage
        /// </summary>
        public List<Resolution> Resolutions { get; protected set; }

        /// <summary>
        /// List of data types allowed by the brokerage
        /// </summary>
        public List<Type> DataTypes { get; protected set; }

        /// <summary>
        /// List of markets allowed by the brokerage
        /// </summary>
        public List<string> Markets { get; protected set; }

        /// <summary>
        /// List of symbols to use in the algorithm
        /// </summary>
        public List<Symbol> Symbols { get; protected set; }

        /// <summary>
        /// Dictionary where each key is a security type and the value associated with it is a list
        /// of resolutions for which a history request will be made at the mentioned security type.
        /// For example, if the key is Forex and the value is a list with Minute, Second and Hour
        /// resolutions, a history request for that forex symbol will be made at the mentioned
        /// resolutions
        /// </summary>
        public Dictionary<SecurityType, List<Resolution>> ResolutionsPerSecurity { get ; protected set; }

        /// <summary>
        /// Dictionary where each key is a security type and the value associated with it is a list
        /// of data types for which a history request will be made with the mentioned security type.
        /// For example, if the key is Forex and the value is a list with TradeBar and Quotebar,
        /// a history request for that Forex symbol will be made with the mentioned security
        /// types
        /// </summary>
        public Dictionary<SecurityType, List<Type>> DataTypesPerSecurity { get; protected set; }

        /// <summary>
        /// Initializes an instance of BrokerageAlgorithmSettingsURL class
        /// </summary>
        /// <param name="brokerageSettingsURL">URL of the brokerage settings json file</param>
        public BrokerageAlgorithmSettings(string brokerageSettingsURL)
        {
            _url = brokerageSettingsURL;
            LoadConfigs();
            SelectSymbolsToAdd();
        }

        /// <summary>
        /// Loads the allowed order types, security types, data types, resolutions and markets
        /// from the provided url of the settings .json file
        /// </summary>
        public async void LoadConfigs()
        {
            var json = _url.DownloadData();
            var jObject = JObject.Parse(json);
            var jsonSecurities = jObject["module-specification"]["download"]["security-types"].ToString();
            SecurityTypes = JsonConvert.DeserializeObject<List<SecurityType>>(jsonSecurities);

            var jsonOrderTypes = jObject["order-types"].ToString();
            var orderTypes = JsonConvert.DeserializeObject<List<string>>(jsonOrderTypes);
            ConvertOrderTypes(orderTypes);

            var jsonResolutions = jObject["module-specification"]["download"]["resolutions"].ToString();
            Resolutions = JsonConvert.DeserializeObject<List<Resolution>>(jsonResolutions);
            Resolutions = Resolutions.Where(x => x != Resolution.Tick).ToList();

            var jsonDataTypes = jObject["module-specification"]["download"]["data-types"].ToString();
            var dataTypes = JsonConvert.DeserializeObject<List<string>>(jsonDataTypes);
            ConvertDataTypes(dataTypes);

            var jsonMarkets = jObject["module-specification"]["download"]["markets"].ToString();
            Markets = JsonConvert.DeserializeObject<List<string>>(jsonMarkets);
            ConfigSymbols();
        }

        /// <summary>
        /// Once we have a symbol for each security type, initializes the
        /// list of symbols to use in the algorithm so that it can also
        /// initialize the dictionaries of order types, resolutions and
        /// security types to test in the algorithm
        /// </summary>
        public void InitializeSymbols()
        {
            Symbols = Symbols.Select(x =>
            {
                switch (x.SecurityType)
                {
                    case SecurityType.Future:
                        return FutureContract;
                    case SecurityType.Option:
                        return OptionContract;
                    case SecurityType.IndexOption:
                        return IndexOptionContract;
                    case SecurityType.FutureOption:
                        return FutureOptionContract;
                    default:
                        return x;
                }
            }).ToList();

            /// Since the value for each key in the following dictionaries is always
            /// the same, one could think we can just use a list of order types,
            /// resolutions and data types. However, the purpose of these dictionaries
            /// is that we can specify the security types that will be tested
            // for each resolution, order type and data type.
            // See InteractiveBrokersBrokerageRegressionAlgorithm
            SymbolToTestPerOrderType = OrderTypes.ToDictionary(x => x, x => Symbols);
            ResolutionsPerSecurity = SecurityTypes.ToDictionary(x => x, x => Resolutions);
            DataTypesPerSecurity = SecurityTypes.ToDictionary(x => x, x => DataTypes);
        }

        /// <summary>
        /// Parses a string list of order types into an actual list of order types
        /// </summary>
        private void ConvertOrderTypes(List<string> orderTypes)
        {
            var orderTypesDict = new Dictionary<string, dynamic>();
            foreach(var orderType in Enum.GetValues(typeof(OrderType)))
            {
                orderTypesDict[orderType.ToString()] = orderType;
            }

            OrderTypes = new List<OrderType>();
            foreach(var orderType in orderTypes)
            {
                if (orderType == "Trailing")
                {
                    OrderTypes.Add(OrderType.TrailingStop);
                }
                else if (orderType == "Exercise")
                {
                    OrderTypes.Add(OrderType.OptionExercise);
                }
                else
                {
                    OrderTypes.Add(orderTypesDict[orderType.Replace(" ", "").Replace("-", "")]);
                }
            }
        }

        /// <summary>
        /// Parses a string list of data types into an actual list of data types
        /// </summary>
        private void ConvertDataTypes(List<string> types)
        {
            DataTypes = new List<Type>();
            foreach(var type in types)
            {
                if (type == "Trade")
                {
                    DataTypes.Add(typeof(TradeBar));
                }
                else if (type == "Quote")
                {
                    DataTypes.Add(typeof(QuoteBar));
                }
            }
        }

        /// <summary>
        /// Defines the symbols that will be used in the algorithm
        /// </summary>
        private void ConfigSymbols()
        {
            MarketSymbols = new BaseMarketSymbols();
            if (Markets.Contains("USA"))
            {
                MarketSymbols = new DefaultMarketSymbols();
            }
            else if (Markets.Contains("Kraken"))
            {
                MarketSymbols.CryptoSymbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Kraken);
            }
            else if (Markets.Contains("Binance"))
            {
                MarketSymbols.CryptoSymbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Binance);
                MarketSymbols.CryptoFutureSymbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Binance);
            }
            else if (Markets.Contains("Bybit"))
            {
                MarketSymbols.CryptoSymbol = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
                MarketSymbols.CryptoFutureSymbol = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Bybit);
            }
            else if (Markets.Contains("Oanda"))
            {
                MarketSymbols.ForexSymbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
            }
            else if (Markets.Contains("Bitfinex"))
            {
                MarketSymbols.CryptoSymbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitfinex);
            }
            else if (Markets.Contains("Coinbase"))
            {
                MarketSymbols.CryptoSymbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Coinbase);
            }
        }

        /// <summary>
        /// From the list of security types to use in the algorithm, it
        /// selects for each type a symbol of that type and adds it into
        /// SymbolsToAdd list
        /// </summary>
        private void SelectSymbolsToAdd()
        {
            Symbols = new List<Symbol>();
            foreach(var securityType in SecurityTypes)
            {
                switch (securityType)
                {
                    case SecurityType.Equity:
                        Symbols.Add(EquitySymbol);
                        break;
                    case SecurityType.Option:
                        Symbols.Add(CanonicalOptionSymbol);
                        break;
                    case SecurityType.Forex:
                        Symbols.Add(ForexSymbol);
                        break;
                    case SecurityType.Future:
                        Symbols.Add(CanonicalFutureSymbol);
                        break;
                    case SecurityType.Cfd:
                        Symbols.Add(CfdSymbol);
                        break;
                    case SecurityType.Crypto:
                        Symbols.Add(CryptoSymbol);
                        break;
                    case SecurityType.FutureOption:
                        Symbols.Add(CanonicalFutureOptionSymbol);
                        break;
                    case SecurityType.IndexOption:
                        Symbols.Add(CanonicalIndexOptionSymbol);
                        break;
                    case SecurityType.CryptoFuture:
                        Symbols.Add(CryptoFutureSymbol);
                        break;
                }
            }
        }
    }

    public class BaseMarketSymbols
    {
        public virtual Symbol EquitySymbol { get; set; }
        public virtual Symbol ForexSymbol { get; set; }
        public virtual Symbol CryptoSymbol { get; set; }
        public virtual Symbol CfdSymbol { get; set; }
        public virtual Symbol CryptoFutureSymbol { get; set; }
        public virtual Symbol CanonicalOptionSymbol { get; set; }
        public virtual Symbol CanonicalIndexOptionSymbol { get; set; }
        public virtual Symbol CanonicalFutureSymbol { get; set; }
        public virtual Symbol CanonicalFutureOptionSymbol { get; }
    }

    public class DefaultMarketSymbols : BaseMarketSymbols
    {
        public override Symbol EquitySymbol { get; set; } = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
        public override Symbol ForexSymbol { get; set; } = Symbol.Create("EURUSD", SecurityType.Forex, Market.Oanda);
        public override Symbol CryptoSymbol { get; set; } = Symbol.Create("BTCUSDT", SecurityType.Crypto, Market.Bybit);
        public override Symbol CfdSymbol { get; set; } = Symbol.Create("XAUUSD", SecurityType.Cfd, Market.Oanda);
        public override Symbol CryptoFutureSymbol { get; set; } = Symbol.Create("BTCUSDT", SecurityType.CryptoFuture, Market.Bybit);
        public override Symbol CanonicalOptionSymbol { get; set; } = Symbol.Create("AAPL", SecurityType.Option, Market.USA);
        public override Symbol CanonicalIndexOptionSymbol { get; set; } = Symbol.Create("SPX", SecurityType.IndexOption, Market.USA);
        public override Symbol CanonicalFutureSymbol { get; set; } = Symbol.Create("ES", SecurityType.Future, Market.CME);
        public override Symbol CanonicalFutureOptionSymbol => Symbol.CreateOption(CanonicalFutureSymbol, Market.CME, OptionStyle.American, OptionRight.Put, 1000, new DateTime(2024, 1, 1));
    }
}
