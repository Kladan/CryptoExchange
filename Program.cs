using GetBestPossibleOrders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/cryptoexchange", (string orderType, string amount) =>
    {
        string[] userArgs = new[] { orderType, amount };
        string[] files = Directory.GetFiles("data", "*.json");
        string[] args = userArgs.Concat(files).ToArray();
        string result = OrderService.GetBestPossibleOrders(args);
        return result;
    })
    .WithName("GetCryptoExchange")
    .WithOpenApi();

app.Run();