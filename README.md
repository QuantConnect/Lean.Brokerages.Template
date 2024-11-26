![lean-brokerage-template](https://user-images.githubusercontent.com/18473240/131904120-f67dab9c-cc6f-4c08-83e9-5d3ffafdb85d.png)


# Lean.Brokerages.Template

[![Build Status](https://github.com/QuantConnect/Lean.Brokerages.Template/workflows/Build%20%26%20Test/badge.svg)](https://github.com/QuantConnect/Lean.Brokerages.Template/actions?query=workflow%3A%22Build%20%26%20Test%22)
 
Template Brokerage Plugin for [Lean](https://github.com/QuantConnect/Lean)

See the [brokerage development guide](https://www.quantconnect.com/tutorials/open-source/brokerage-development-guide)

# Develop new Brokerage plugin
## Initial Setup

1. Review documentation for the new brokerage integration.
2. Approve supported **security types** (e.g., Equity, Index, Option).
3. Approve support order types: Market, Limit, Stop, StopLimit, TrailingStop, MarketOnClose, TrailingStopLimit, etc.
4. Create a new development account.
5. Generate API keys.
6. 🍴📦Fork QuantConnect brokerage repository.
7. Rename all template to new brokerage names by a skeleton.
8. Remove: not used parts (for instance: downloader lean has generic now, but some brokerages support downloading trading pairs process like some crypto exchanges)
9. Implement API connection (simple request) infrastructure (generic method that send GET/POST requests) 
10. If brokerage support several steps of authentication like **OAuth 2**
11. Implement URL method to generate **Authorization URL**
12. 🧪 Unit test: to get **Authorization URL**
13. Create a method to automatically generate a new refresh token using the Authorization URL. Allow passing either the authorization code manually or the full URL in the configuration. Use a breakpoint to retrieve the refresh token and update the config file, streamlining the development process.
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
17. Implement `CanSubscribe()`
18. Implement `GetHistory()` 
    1. For each support **Security Type**.
    2. For each **Resolution** (which brokerage supports)
        1. Tick, Second, Minute, Hour, Daily
    3. For each **TickType**
        1. Trade, Quote, OpenInterest
    4. Remarks:
        1. If the brokerage does not support a specific input parameter, it should log a warning immediately and return **`null`** from the **`GetHistory()`** method.
19. 🧪 Unit test: `GetHistory()` 
    1. Create valid test cases for each combination of **`Resolution`**, **`TickType`**, **`SecurityType`**, and other relevant parameters.
    2. Create invalid test cases to ensure the method returns **`null`** and no internal exceptions are thrown.
20. Implement `GetBrokerageSymbol()`  in `class SymbolMapper` .
21. 🧪 Unit test: `ReturnsCorrectBrokerageSymbol()`
    1. we pass Lean Symbol and get string of brokerage symbol
22. Implement `GetCashBalance()`
23. 🧪 Unit test: `GetCashBalance()`
    1. we have generic method in `class BrokerageTests` 
24. Implement `GetAccountHoldings()`
25. 🧪 Unit test: `GetAccountHoldings()` 
    1. Note: skip doesn't support Security Types.
26. Implement `GetOpenOrders()` 
    1. All support OrderType
    2. Convert brokerage order duration to Lean TimeInForce for each order.
    3. Note: skip doesn't support Security Types.
    4. Note: can create simple order with using brokerage Web UI or desktop app.
27. Implement `GetLeanSymbol()` (Brokerage Ticker → Lean Symbol)
28. 🧪 Unit test: `GetLeanSymbol()`
29. Implement `PlaceOrder()`
    1. Convert the Lean order type to the corresponding brokerage order type using specific parameters, then pass the request to the brokerage API.
    2. **Add new BrokerageId to Lean.Order.BrokerId**.
    3. Send the event that the order was submitted successfully.
30. 🧪 Unit test: `PlaceOrder()`
    1. different Security Types
    2. different MarketType (Market, Limit, Stop)
31. Implement `CancelOrder()`
    1. Validate that order has not cancelled or filled already to prevent from extra steps.
32. Set up a continuous WebSocket connection.
33. Implement `Connect()`
    1. `public override bool IsConnected`
34. Implement `Disconnect()`
35. Implement User/Order update event.
    1. `PlaceOrder()` and receive updates about it via WebSocket, sending the relevant **`OrderEvent`** to **Lean** in real-time.
    2. Note: We should synchronize the process to avoid race conditions. First, complete the **`PlaceOrder`** operation, then return the corresponding event. This means we need to pause new updates from the WebSocket until the order is fully processed.
36. 🧪 Unit test: `PlaceOrder()` → WS Update about filled.
37. Implement ReSubscribe to user update.
    1. When the internet connection is lost, we should initiate a new WebSocket connection.
38. Implement `UpdateOrder()`
39. 🧪 Unit test: `UpdateOrder()`
    1. different Security Types.
    2. different quantities.
    3. different price.
    4. wrong price.
    5. wrong quantity.
40. Implement the Subscription/Unsubscription process. (`interface IDataQueueHandler` )
    1. subscribe on level one update (quotes, trades, openInterest)
    2. Remarks:
        1. use `IDataAggregator`
        2. use different `ExchangeTimeZone` to different symbols.
41. 🧪 Unit test: `DataQueueHandler` 
    1. different Security Types
    2. different `SubscriptionDataConfig` (Resolution, TickType)
42. Implement `ReSubscriptionProcess` 
    1. When the internet connection is lost, we should establish a new stable connection and resubscribe to all the symbols that were previously subscribed to.
43. Manually test `ReSubscriptionProcess` (switch on/off your internet connection)
44. Implement `IDataQueueUniverseProvider`
    1. option chain provider
45. 🧪 Unit test: `IDataQueueUniverseProvider`
    1. different symbol of the same security type (Option, IndexOption)
46. Implement `Initialize()` to initialize brokerage only through one method.
47. **NOT FORGET**: `ValidateSubscription()`
    1. place in `Initialize()`
48. Implement `SetJob()`
49. Implement `class BrokerageFactory` 
50. 🧪 Unit test: `BrokerageFactory`
51. Refactor: `brokerage.json` file in root of `.sln` 
52. Implement `gh-actions.yml` to great CI/CD.

## Lean integration

53. Fork [Lean](https://github.com/quantConnect/lean)
54. Implement `class BrokerageModel`
    1. Support securities.
    2. Place and update orders.
55. 🧪 Unit test: `BrokerageModel`
56. Added brokerage name in `BrokerageName.cs`
57. Added brokerage in `IBrokerageModel.cs` 
58. Implement class `class BrokreageOrderProperties.cs` 
    1. different additional property for brokerage (e.g. support extend hours when place order)
59. Implement `class BrokerageFeeModel` 
60. Added config in `Launcher/config.json` 

## Lean-CLI integration

61. Fork [lean-cli](https://github.com/QuantConnect/lean-cli)
62. Upgrade `modules.json`
63. Update lean-cli Readmi file 
    1. `python scripts/readme.py`