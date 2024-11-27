![lean-brokerage-template](https://user-images.githubusercontent.com/18473240/131904120-f67dab9c-cc6f-4c08-83e9-5d3ffafdb85d.png)


# Lean.Brokerages.Template

[![Build Status](https://github.com/QuantConnect/Lean.Brokerages.Template/workflows/Build%20%26%20Test/badge.svg)](https://github.com/QuantConnect/Lean.Brokerages.Template/actions?query=workflow%3A%22Build%20%26%20Test%22)
 
Template Brokerage Plugin for [Lean](https://github.com/QuantConnect/Lean)

See the [brokerage development guide](https://www.quantconnect.com/tutorials/open-source/brokerage-development-guide)

# Develop new Brokerage plugin
## Initial Setup

1. Review documentation for the new brokerage integration.
2. Approve supported **[Security types](https://github.com/QuantConnect/Lean/blob/master/Common/Global.cs#L244)** (e.g., Equity, Index, Option).
3. Approve support **[Order types](https://github.com/QuantConnect/Lean/blob/master/Common/Orders/OrderTypes.cs#L21)**: Market, Limit, Stop, StopLimit, TrailingStop, MarketOnClose, TrailingStopLimit, etc.
4. Create a new brokerage development account.
5. Generate API keys.
6. 🍴📦Fork QuantConnect brokerage repository.
7. Rename all template to new brokerage names by a skeleton.
    - for instance:
        - `TemplateBrokerage.cs` -> `BinanceBrokerage.cs`
        - `public class TemplateBrokerage` -> `public class InteractiveBrokersBrokerage`
8. Remove: not used parts (for instance: downloader lean has generic now, but some brokerages support downloading trading pairs process like some crypto exchanges) - [Lean download data provider source](https://github.com/QuantConnect/Lean/tree/master/DownloaderDataProvider), [Bybit Exchange Info Downloader](https://github.com/QuantConnect/Lean.Brokerages.ByBit/blob/master/QuantConnect.BybitBrokerage.ToolBox/BybitExchangeInfoDownloader.cs)
9. Implement API connection (simple request) infrastructure (generic method that send GET/POST requests) 
10. If brokerage support several steps of authentication like **OAuth 2** - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/Api/TokenRefreshHandler.cs)
11. Implement URL method to generate **Authorization URL** - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/Api/TradeStationApiClient.cs#L614)
12. 🧪 Unit test: to get **Authorization URL** - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageAdditionalTests.cs#L156)
13. Create a method to automatically generate a new refresh token using the Authorization URL. Allow passing either the authorization code manually or the full URL in the configuration. Use a breakpoint to retrieve the refresh token and update the config file, streamlining the development process. - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/Api/TokenRefreshHandler.cs)
14. Create brokerage config file in test project:
    
    ```json
    {
      "brokerage-api-url": "api-url",
      "brokerage-app-key": "app-key",
      "brokerage-secret": "secret",
      "brokerage-authorization-code": "",
      "brokerage-refresh-token": "",
      "brokerage-redirect-url": "https://127.0.0.1"
    }
    ```
    
15. Create an independent class that manages the authentication process, ensuring it is solely responsible for authentication tasks. This class should be designed to function as middleware for our HTTP client in the future.
16. 🧪 Unit test for the authentication process. 

## **Brokerage Features Implementation**

> I recommend starting with the `GetHistory()` method, as it provides a clear understanding of the logic behind the `SymbolMapper` as well.
> 
17. Implement `CanSubscribe()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L735)
    - Symbol must not be universe or canonical.;
    - Brokerage must support the symbol's security type.
18. Implement `GetHistory()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.HistoryProvider.cs#L51)
    1. For each support **Security Type**;
    2. For each **Resolution** (which brokerage supports);
        1. Tick, Second, Minute, Hour, Daily
    3. For each **TickType**;
        1. Trade, Quote, OpenInterest
    4. Remarks:
        1. If the brokerage does not support a specific input parameter, it should log a warning immediately and return **`null`** from the **`GetHistory()`** method.
        2. Use different [Consolidators](https://github.com/QuantConnect/Lean/tree/master/Common/Data/Consolidators) to aggregate bar resolution.
19. 🧪 Unit test: `GetHistory()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageHistoryProviderTests.cs)
    1. Create valid test cases for each combination of **`Resolution`**, **`TickType`**, **`SecurityType`**, and other relevant parameters;
    2. Create invalid test cases to ensure the method returns **`null`** and no internal exceptions are thrown.
20. Implement `GetBrokerageSymbol()`  in `class SymbolMapper` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationSymbolMapper.cs#L47)
21. 🧪 Unit test: `ReturnsCorrectBrokerageSymbol()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageSymbolMapperTests.cs)
    1. We pass Lean Symbol and get string of brokerage symbol
22. Implement `GetCashBalance()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L401)
23. 🧪 Unit test: `GetCashBalance()`
    1. We have generic method in `class BrokerageTests` 
24. Implement `GetAccountHoldings()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L349)
25. 🧪 Unit test: `GetAccountHoldings()` 
    1. Note: skip doesn't support Security Types.
26. Implement `GetOpenOrders()` - [Binance example](https://github.com/QuantConnect/Lean.Brokerages.Binance/blob/master/QuantConnect.BinanceBrokerage/BinanceBrokerage.cs#L214)
    1. All support OrderType;
    2. Convert brokerage order duration to Lean TimeInForce for each order;
    3. Note: skip doesn't support Security Types;
    4. Note: can create simple order with using brokerage Web UI or desktop app.
27. Implement `GetLeanSymbol()` (Brokerage Ticker → Lean Symbol) - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationSymbolMapper.cs#L96)
    1. Reuse existing conversion methods from the [Lean source](https://github.com/QuantConnect/Lean/blob/master/Common/SymbolRepresentation.cs)
28. 🧪 Unit test: `GetLeanSymbol()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageSymbolMapperTests.cs)
    1. `ReturnsCorrectLeanSymbol()`
29. Implement `PlaceOrder()` - [Binance example](https://github.com/QuantConnect/Lean.Brokerages.Binance/blob/master/QuantConnect.BinanceBrokerage/BinanceBrokerage.cs#L266), [TradeStation example with CrossZeroOrder](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L419)
    1. Convert the Lean order type to the corresponding brokerage order type using specific parameters, then pass the request to the brokerage API.
    2. **Add new BrokerageId to Lean.Order.BrokerId**.
    3. Send the `new OrderEvent(...)` that the order was submitted successfully.
30. 🧪 Unit test: `PlaceOrder()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageTests.cs#L126)
    1. different Security Types
    2. different MarketType (Market, Limit, Stop)
31. Implement `CancelOrder()` - [Bybit example](https://github.com/QuantConnect/Lean.Brokerages.ByBit/blob/master/QuantConnect.BybitBrokerage/BybitBrokerage.Brokerage.cs#L1959)
    1. Validate that order has not cancelled or filled already to prevent from extra steps.
32. Set up a continuous WebSocket connection.
33. Implement `Connect()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L682)
    1. `public override bool IsConnected` - Returns true if the WebSocket is connected and the subscription to user/order updates is active.
34. Implement `Disconnect()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L700)
    1. Stop all WS subscription.
    2. Close connection. 
35. Implement User/Order update event. - [Coinbase example](https://github.com/QuantConnect/Lean.Brokerages.Coinbase/blob/master/QuantConnect.CoinbaseBrokerage/CoinbaseBrokerage.Messaging.cs#L178)
    1. `PlaceOrder()` and receive updates about it via WebSocket, sending the relevant **`OrderEvent`** to **Lean** in real-time.
    2. Note: We should synchronize the process to avoid race conditions. First, complete the **`PlaceOrder`** operation, then return the corresponding event. This means we need to pause new updates from the WebSocket until the order is fully processed.
36. 🧪 Unit test: `PlaceOrder()` → WS Update about filled.
37. Implement ReSubscribe to user update.
    1. When the internet connection is lost, we should initiate a new WebSocket connection.
38. Implement `UpdateOrder()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L609)
39. 🧪 Unit test: `UpdateOrder()`
    1. different Security Types.
    2. different quantities.
    3. different price.
    4. wrong price.
    5. wrong quantity.
40. Implement the Subscription/Unsubscription process. (`interface IDataQueueHandler` )
    1. subscribe on level one update (quotes, trades, openInterest)
    2. Remarks:
        1. Use `IDataAggregator`;
        2. Use different `ExchangeTimeZone` to different symbols - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerageMultiStreamSubscriptionManager.cs#L273);
        3. `IDataAggregator.Update()` - different tick data.
        4. use `DataQueueHandlerSubscriptionManager SubscriptionManager` from `abstract class BaseWebsocketsBrokerage` for Subscription/UnSubscription and count symbol, etc.
        5. Use `class BrokerageMultiWebSocketSubscriptionManager` to multiple connection.
41. 🧪 Unit test: `DataQueueHandler` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage.Tests/TradeStationBrokerageDataQueueHandlerTests.cs#L55)
    1. Different Security Types;
    2. Different `SubscriptionDataConfig` (Resolution, TickType).
42. Implement `ReSubscriptionProcess` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/Streaming/StreamingTaskManager.cs#L160)
    1. When the internet connection is lost, we should establish a new stable connection and resubscribe to all the symbols that were previously subscribed to.
43. Implement `IDataQueueUniverseProvider` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.DataQueueUniverseProvider.cs)
    1. option chain provider
44. 🧪 Unit test: `IDataQueueUniverseProvider`
    1. different symbol of the same security type (Option, IndexOption)
45. Implement `Initialize()` to initialize brokerage only through one method. - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L244)
46. **NOT FORGET**: `ValidateSubscription()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L1255)
    1. place in `Initialize()`
    2. The same in all brokerages.
47. Implement `SetJob()` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.DataQueueHandler.cs#L43)
    1. A brokerage can be started `IDataQueueHandler` and not as a brokerage.
48. Implement `class BrokerageFactory` - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerageFactory.cs)
    1. `Dictionary<string, string> BrokerageData` - all brokerage configs.
    2.  `GetBrokerageModel(...)` - use it like reference to model in Lean.
49. 🧪 Unit test: `BrokerageFactory`
50. Refactor: `brokerage.json` file in root of `.sln` 
51. Implement `gh-actions.yml` to great CI/CD.

## Lean integration - [TradeStation Lean PR](https://github.com/QuantConnect/Lean/pull/8031)
> Use any Lean project as a reference within the brokerage integration to enhance debugging and streamline development.
52. Fork [Lean](https://github.com/quantConnect/lean)
53. Implement `class BrokerageModel`
    1. Support securities.
    2. Place and update orders.
54. 🧪 Unit test: `BrokerageModel`
55. Added brokerage name in `BrokerageName.cs`
56. Added brokerage in `IBrokerageModel.cs` 
57. Implement class `class BrokreageOrderProperties.cs` 
    1. different additional property for brokerage (e.g. support extend hours when place order)
58. Implement `class BrokerageFeeModel` 
59. Added config in `Launcher/config.json` 

## Lean-CLI integration - [CharlesSchwab Lean-CLI PR](https://github.com/QuantConnect/lean-cli/pull/517)

60. Fork [lean-cli](https://github.com/QuantConnect/lean-cli)
61. Upgrade `modules.json`
62. Update lean-cli Readmi file 
    1. `python scripts/readme.py`

## Manual integration test
63. Re-Subscription Process (after internet disconnect)
    1. Re-subscribe to User/Order WebSocket Events;
    2. Re-subscribe to Market Data;
        1. Order Book;
        2. Trade Information;
        3. Level 1 Data.
64. Multiple Subscriptions on Symbols
    1. Subscribe to Option Chains;
    2. Subscribe to Symbols with 500+;
    3. Subscribe to Symbols with 1.000+.
65. Long-Running Test for Night and Weekend.
    1. Test Server Stability and Connection Resilience.
        - Perform long-running tests during off-hours, such as nights and weekends, to validate that the remote server closes properly and that our connection remains stable.
        - During this test, monitor the connection for unexpected disruptions or failures after the server shutdown.

# Best Practices for Implementing a New Plugin in Lean
- <span style="background-color: #610e0e;">DON'T PUSH ANY CREDENTIALS IN THE COMMIT HISTORY.</span>
- Each warning message in `GetHistory()` need log at once - [TradeStation example](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.HistoryProvider.cs#L34)
- Use `OnMessage(...)` to display info to user
    - use different [BrokerageMessageType](https://github.com/QuantConnect/Lean/blob/b99da54d5ae7c8737a85140e6442bdc5b5993ce4/Common/Brokerages/BrokerageMessageType.cs#L21)
    - example [BrokerageMessageType.Warning](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L429)
    - example [BrokerageMessageType.Error](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L580) - stop algorithm in Lean.
- Use `OnOrderEvent(...)` to send any info about order.
    - use appropriate [OrderStatus](https://github.com/QuantConnect/Lean/blob/b99da54d5ae7c8737a85140e6442bdc5b5993ce4/Common/Orders/OrderTypes.cs#L138) in it.
    - example [OnOrderEvent:OrderStatus.Submitted](https://github.com/QuantConnect/Lean.Brokerages.ByBit/blob/master/QuantConnect.BybitBrokerage/BybitBrokerage.Brokerage.cs#L146)
    - example [OnOrderEvent:OrderStatus.UpdateSubmitted](https://github.com/QuantConnect/Lean.Brokerages.ByBit/blob/master/QuantConnect.BybitBrokerage/BybitBrokerage.Brokerage.cs#L181)
    - example [OnOrderEvent:OrderStatus.Filled](https://github.com/QuantConnect/Lean.Brokerages.Coinbase/blob/cf6731c2661694ee79a0a7e69726fa55c8983ddb/QuantConnect.CoinbaseBrokerage/CoinbaseBrokerage.Messaging.cs#L213)
- Not forget about [OrderFee](https://github.com/QuantConnect/Lean.Brokerages.InteractiveBrokers/blob/4a6022984d6d63a3abb9a0e119f7454e211a5621/QuantConnect.InteractiveBrokersBrokerage/InteractiveBrokersBrokerage.cs#L2435).
- To implement a **RateGate** that enforces brokerage restrictions.
- Use [class BrokerageConcurrentMessageHandler](https://github.com/QuantConnect/Lean/blob/b99da54d5ae7c8737a85140e6442bdc5b5993ce4/Brokerages/BrokerageConcurrentMessageHandler.cs#L26) to synchronize processes like `PlaceOrder()`/`UpdateOrder()`/`CancelOrder()` between brokerage and Lean.
    - we have lock getting new update with using `Monitor` underhood, example [TradeStation](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L436).
- Use `IOrderProvider` to interact with actual Lean Order state in brokerage.
- Avoid using `string`; prefer using Enums to reduce the number of bugs, example [TradeStation Account Type](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationExtensions.cs#L252).
- Some brokerage need implement [CrossZeroPositionOrder](https://github.com/QuantConnect/Lean/blob/b99da54d5ae7c8737a85140e6442bdc5b5993ce4/Brokerages/Brokerage.cs#L572), reuse this logic.
- Inherit `class BaseWebsocketsBrokerage` instead `Brokerage` to reuse already implemented WebSocket interaction and overload specific method or use events.
- Use `class BrokerageMultiWebSocketSubscriptionManager` to implement multiple WebSocket connect (if brokerage support)
    - example [ByBit](https://github.com/QuantConnect/Lean.Brokerages.ByBit/blob/master/QuantConnect.BybitBrokerage/BybitBrokerage.cs#L261C47-L261C89);
    - write custom example [TradeStation](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/Streaming/StreamingTaskManager.cs)
- Use [ExchangeTimeZone](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationExtensions.cs#L312) to specific Symbol to convert from UTC, example [TradeStation](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerageMultiStreamSubscriptionManager.cs#L225).
- use [DefaultOrderBook](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerageMultiStreamSubscriptionManager.cs#L46) to keep High price in dictionary and emit new bid/ask price and size.
    - example [TradeStation](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerageMultiStreamSubscriptionManager.cs#L175)
- use `lock(...){ }` block when you call `IDataAggregator.Update()`.
- Example of Re-Subscribe on Order updates - [TradeStation](https://github.com/QuantConnect/Lean.Brokerages.TradeStation/blob/master/QuantConnect.TradeStationBrokerage/TradeStationBrokerage.cs#L769)
    - Use `CancellationToken`;
    - Infinity loop;
    - Small delay before requesting based on `CancellationToken` too.
    - If we will close connection, `CancellationToken` will be canceled and disposed well.
