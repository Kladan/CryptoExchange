using System.Text.Json;

namespace GetBestPossibleOrders;

internal class Program
{
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
        if (orderType == "Buy")
        {
            result = GetBestAsksFromBestExchange(exchanges, goalAmount);
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

    private static List<Order> GetBestAsksFromBestExchange(List<Exchange> exchanges, decimal goalAmount)
    {
        List<Order> result = new List<Order>();
        
        foreach (Exchange exchange in exchanges)
        {
            List<Order> exchangeAsks = new List<Order>();
            foreach (OrderWrapper wrapper in exchange.OrderBook.Asks)
            {
                exchangeAsks.Add(wrapper.Order);
            }
            exchangeAsks.Sort((a, b) => a.Price < b.Price ? -1 : a.Price == b.Price ? 0 : 1);

            List<Order> bestExchangeAsks = GetBestAsksOfOrderList(exchangeAsks, goalAmount);
            decimal exchangeSum = bestExchangeAsks.Sum(a => a.Price);
            decimal resultSum = result.Sum(a => a.Price);

            if (result.Count == 0 || exchangeSum < resultSum)
            {
                result = bestExchangeAsks;
            }
        }

        return result;
    }

    private static List<Order> GetBestAsksOfOrderList(List<Order> orders, decimal goalAmount)
    {
        List<Order> bestAsks = new List<Order>();
        decimal reachedAmount = 0;

        foreach (Order ask in orders)
        {
            if (reachedAmount + ask.Amount < goalAmount)
            {
                bestAsks.Add(ask);
                reachedAmount += ask.Amount;
            }
            else if (reachedAmount + ask.Amount == goalAmount)
            {
                bestAsks.Add(ask);
                break;
            }
            else if (reachedAmount + ask.Amount > goalAmount)
            {
                decimal splittedAmount = goalAmount - reachedAmount;
                Order splittedAsk = ask;
                splittedAsk.Amount = splittedAmount;
                bestAsks.Add(splittedAsk);
                break;
            }
        }

        return bestAsks;
    }
}