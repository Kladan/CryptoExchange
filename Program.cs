using System.Text.Json;

namespace GetBestPossibleOrders;

internal class Program
{
    enum OrderType
    {
        Buy,
        Sell
    }

    public static void Main(string[] args)
    {
        string orderType = args[0];
        string amountValue = args[1];
        decimal goalAmount = Convert.ToDecimal(amountValue);
        List<string> files = args.ToList().GetRange(2, args.Length - 2);
        if (!ValidateArguments(orderType, goalAmount, files)) return;

        List<Exchange> exchanges = new List<Exchange>();
        if (!ReadExchangesFromFiles(files, exchanges)) return;

        List<Order> result = new List<Order>();
        if (Enum.TryParse(orderType, out OrderType type))
        {
            result = GetBestOrdersFromBestExchange(exchanges, goalAmount, type);
        }

        string json = JsonSerializer.Serialize(result);
        Console.WriteLine(json);
    }

    private static bool ValidateArguments(string type, decimal goal, List<string> fileList)
    {
        if (type != "Buy" && type != "Sell")
        {
            Console.WriteLine("Type in an order type ...");
            return false;
        }

        if (goal == 0)
        {
            Console.WriteLine("Type in an amount not 0 ...");
            return false;
        }

        if (fileList.Count == 0)
        {
            Console.WriteLine("Type in file paths to your order books ...");
            return false;
        }

        return true;
    }

    private static bool ReadExchangesFromFiles(List<string> list, List<Exchange> exchanges)
    {
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        foreach (string file in list)
        {
            using FileStream json = File.OpenRead(file);
            Exchange? exchange = JsonSerializer.Deserialize<Exchange>(json, options);
            if (exchange == null)
            {
                Console.WriteLine("Check if your file contains a crypto exchange:");
                Console.WriteLine(file);
                return false;
            }

            exchanges.Add(exchange);
        }

        return true;
    }

    private static List<Order> GetBestOrdersFromBestExchange(List<Exchange> exchanges,
        decimal goalAmount, OrderType orderType)
    {
        List<Order> result = new List<Order>();

        foreach (Exchange exchange in exchanges)
        {
            decimal fund = 0;
            List<Order> exchangeOrders = new List<Order>();
            if (orderType == OrderType.Buy)
            {
                fund = exchange.AvailableFunds.Crypto;
                exchangeOrders = exchange.OrderBook.Asks.ConvertAll(a => a.Order);
                exchangeOrders.Sort((a, b) => a.Price < b.Price ? -1 : a.Price == b.Price ? 0 : 1);
            }
            else if (orderType == OrderType.Sell)
            {
                fund = exchange.AvailableFunds.Euro;
                exchangeOrders = exchange.OrderBook.Bids.ConvertAll((b => b.Order));
                exchangeOrders.Sort((a, b) => a.Price > b.Price ? -1 : a.Price == b.Price ? 0 : 1);
            }

            List<Order> bestExchangeOrders = GetBestOrdersFromList(exchangeOrders, goalAmount, fund, orderType);
            decimal exchangeSumEur = bestExchangeOrders.Sum(a => a.Price);
            decimal exchangeSumBtc = bestExchangeOrders.Sum(a => a.Amount);
            decimal bestResultSumEur = result.Sum(a => a.Price);
            decimal bestResultSumBtc = result.Sum(a => a.Amount);

            if (result.Count == 0 ||
                (orderType == OrderType.Buy && exchangeSumEur < bestResultSumEur ||
                 orderType == OrderType.Sell && exchangeSumEur > bestResultSumEur) &&
                exchangeSumBtc >= bestResultSumBtc)
            {
                result = bestExchangeOrders;
            }
        }

        return result;
    }

    private static List<Order> GetBestOrdersFromList(List<Order> orders, decimal goalAmount,
        decimal availableFund, OrderType orderType)
    {
        List<Order> bestOrders = new List<Order>();
        decimal reachedBtc = 0;
        decimal reachedEur = 0;

        foreach (Order order in orders)
        {
            bool isLastOrder = PrepareOrder(order, goalAmount, availableFund, orderType, reachedBtc, reachedEur);

            bestOrders.Add(order);
            reachedBtc += order.Amount;
            reachedEur += order.Amount * order.Price;

            bool isRequestedAmountHit = reachedBtc + order.Amount == goalAmount;
            bool isBuyableFundHit = orderType == OrderType.Buy && reachedBtc + order.Amount == availableFund;
            bool isSellableFundHit =
                orderType == OrderType.Sell && reachedEur + order.Amount * order.Price == availableFund;

            if (isRequestedAmountHit || isBuyableFundHit || isSellableFundHit)
            {
                isLastOrder = true;
            }

            if (isLastOrder) break;
        }

        return bestOrders;
    }

    private static bool PrepareOrder(Order order, decimal goalAmount, decimal availableFund, OrderType orderType,
        decimal reachedBtc, decimal reachedEur)
    {
        bool isLastOrder = false;
        
        bool wouldOverrunRequestedAmount = reachedBtc + order.Amount > goalAmount;
        if (wouldOverrunRequestedAmount)
        {
            decimal restAmount = goalAmount - reachedBtc;
            order.Amount = restAmount;
            isLastOrder = true;
        }
        
        bool wouldOverrunBuyableFund = orderType == OrderType.Buy && reachedBtc + order.Amount > availableFund;
        if (wouldOverrunBuyableFund)
        {
            decimal restAmount = availableFund - reachedBtc;
            order.Amount = restAmount;
            isLastOrder = true;
        }

        bool wouldOverrunSellableFund = orderType == OrderType.Sell &&
                                        reachedEur + order.Amount * order.Price > availableFund;
        if (wouldOverrunSellableFund)
        {
            decimal restEur = availableFund - reachedEur;
            decimal d = order.Price / restEur;
            order.Amount = 1 / d;
            isLastOrder = true;
        }

        return isLastOrder;
    }
}