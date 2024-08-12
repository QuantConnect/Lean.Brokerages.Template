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
    public class BrokerageAlgorithmSettings
    {
        private string _url;
        public Symbol OptionContract;
        public Symbol FutureContract;
        public Symbol FutureOptionContract;
        public Symbol IndexOptionContract;
        public Dictionary<SecurityType, Symbol> SymbolsPerSecurityType;
        public BrokerageName BrokerageName { get; set; } = BrokerageName.Default;
        public Resolution Resolution => Resolution.Minute;
        public int OpenOrdersTimeout => 5;
        public BaseMarketSymbols MarketSymbols { get; protected set; }
        public Symbol EquitySymbol => MarketSymbols.EquitySymbol;
        public Symbol ForexSymbol => MarketSymbols.ForexSymbol;
        public Symbol CryptoSymbol => MarketSymbols.CryptoSymbol;
        public Symbol CfdSymbol => MarketSymbols.CfdSymbol;
        public Symbol CryptoFutureSymbol => MarketSymbols.CryptoFutureSymbol;
        public Symbol CanonicalOptionSymbol => MarketSymbols.CanonicalOptionSymbol;
        public Symbol CanonicalIndexOptionSymbol => MarketSymbols.CanonicalIndexOptionSymbol;
        public Symbol CanonicalFutureSymbol => MarketSymbols.CanonicalFutureSymbol;
        public Symbol CanonicalFutureOptionSymbol => MarketSymbols.CanonicalFutureOptionSymbol;
        public Func<OptionFilterUniverse, OptionFilterUniverse> IndexOptionFilter => null;
        public Func<FutureFilterUniverse, FutureFilterUniverse> FutureFilter => u => u.Expiration(0, 182);
        public Func<OptionFilterUniverse, OptionFilterUniverse> FutureOptionFilter => u => u.Strikes(-2, +2).Expiration(0, 60);
        public Func<OptionFilterUniverse, OptionFilterUniverse> OptionFilter => u => u.Strikes(-2, +2).Expiration(0, 60);
        public Dictionary<OrderType, List<Symbol>> SymbolToTestPerOrderType { get; protected set; }
        public List<SecurityType> Securities { get; protected set; }
        public List<OrderType> OrderTypes { get; protected set; }
        public List<Resolution> Resolutions { get; protected set; }
        public List<Type> DataTypes { get; protected set; }
        public List<string> Markets { get; protected set; }
        public List<Symbol> SecurityTypes { get; protected set; }
        public List<Symbol> SecurityTypesToAdd { get; protected set; }
        public Dictionary<SecurityType, List<Resolution>> ResolutionsPerSecurity { get ; protected set; }
        public Dictionary<SecurityType, List<Type>> DataTypesPerSecurity { get; protected set; }

        public BrokerageAlgorithmSettings(string brokerageSettingsURL)
        {
            _url = brokerageSettingsURL;
            LoadConfigs();
            SymbolsToAdd();
        }

        public async void LoadConfigs()
        {
            var json = _url.DownloadData();
            var jObject = JObject.Parse(json);
            var jsonSecurities = jObject["module-specification"]["download"]["security-types"].ToString();
            Securities = JsonConvert.DeserializeObject<List<SecurityType>>(jsonSecurities);

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

        public void InitializeSymbols()
        {
            SecurityTypes = SecurityTypesToAdd.Select(x =>
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

            SymbolToTestPerOrderType = OrderTypes.ToDictionary(x => x, x => SecurityTypes);
            ResolutionsPerSecurity = Securities.ToDictionary(x => x, x => Resolutions);
            DataTypesPerSecurity = Securities.ToDictionary(x => x, x => DataTypes);
        }

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

        private void SymbolsToAdd()
        {
            SecurityTypesToAdd = new List<Symbol>();
            foreach(var securityType in Securities)
            {
                switch (securityType)
                {
                    case SecurityType.Equity:
                        SecurityTypesToAdd.Add(EquitySymbol);
                        break;
                    case SecurityType.Option:
                        SecurityTypesToAdd.Add(CanonicalOptionSymbol);
                        break;
                    case SecurityType.Forex:
                        SecurityTypesToAdd.Add(ForexSymbol);
                        break;
                    case SecurityType.Future:
                        SecurityTypesToAdd.Add(CanonicalFutureSymbol);
                        break;
                    case SecurityType.Cfd:
                        SecurityTypesToAdd.Add(CfdSymbol);
                        break;
                    case SecurityType.Crypto:
                        SecurityTypesToAdd.Add(CryptoSymbol);
                        break;
                    case SecurityType.FutureOption:
                        SecurityTypesToAdd.Add(CanonicalFutureOptionSymbol);
                        break;
                    case SecurityType.IndexOption:
                        SecurityTypesToAdd.Add(CanonicalIndexOptionSymbol);
                        break;
                    case SecurityType.CryptoFuture:
                        SecurityTypesToAdd.Add(CryptoFutureSymbol);
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
