namespace GetBestPossibleOrders
{
    public class Order
    {
        public Guid Id { get; set; }
        public DateTime Time { get; set; }
        public string Type { get; set; }
        public string Kind { get; set; }
        public double Amount { get; set; }
        public double Price { get; set; }
    }
}
