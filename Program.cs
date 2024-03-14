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
        Console.WriteLine(result.Sum(o => o.Price));
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
            List<Order> exchangeOrders = new List<Order>();
            if (orderType == OrderType.Buy)
            {
                exchangeOrders = exchange.OrderBook.Asks.ConvertAll(a => a.Order);
                exchangeOrders.Sort((a, b) => a.Price < b.Price ? -1 : a.Price == b.Price ? 0 : 1);
            }
            else if (orderType == OrderType.Sell)
            {
                exchangeOrders = exchange.OrderBook.Bids.ConvertAll((b => b.Order));
                exchangeOrders.Sort((a, b) => a.Price > b.Price ? -1 : a.Price == b.Price ? 0 : 1);
            }

            List<Order> bestExchangeOrders = GetBestOrdersFromList(exchangeOrders, goalAmount);
            decimal exchangeSum = bestExchangeOrders.Sum(a => a.Price);
            decimal resultSum = result.Sum(a => a.Price);

            if (result.Count == 0 || exchangeSum < resultSum)
            {
                result = bestExchangeOrders;
            }
        }

        return result;
    }

    private static List<Order> GetBestOrdersFromList(List<Order> orders, decimal goalAmount)
    {
        List<Order> bestOrders = new List<Order>();
        decimal reachedAmount = 0;

        foreach (Order order in orders)
        {
            if (reachedAmount + order.Amount < goalAmount)
            {
                bestOrders.Add(order);
                reachedAmount += order.Amount;
            }
            else if (reachedAmount + order.Amount == goalAmount)
            {
                bestOrders.Add(order);
                break;
            }
            else if (reachedAmount + order.Amount > goalAmount)
            {
                decimal splittedAmount = goalAmount - reachedAmount;
                Order splittedOrder = order;
                splittedOrder.Amount = splittedAmount;
                bestOrders.Add(splittedOrder);
                break;
            }
        }

        return bestOrders;
    }
}