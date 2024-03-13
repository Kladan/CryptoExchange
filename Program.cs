using System.Text.Json;
using GetBestPossibleOrders;

string filepath = args[0];
JsonSerializerOptions? options = new() { PropertyNameCaseInsensitive = true };
using FileStream json = File.OpenRead(filepath);
Exchange? exchange = JsonSerializer.Deserialize<Exchange>(json, options);
