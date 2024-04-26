using System.Reflection.Metadata.Ecma335;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(c=>c.AddConsole());
builder.Services.AddTransient<IDBFactory,DatabaseFactory>();
builder.Services.AddWorker<string>();
builder.Services.AddWorker<int>();
builder.Services.AddWorker<Message>();




var app = builder.Build();
int message = 0;


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (IMessageQueue<string> messageQueue) =>
{

    var message = Guid.NewGuid().ToString("N");
    await messageQueue.PublishAsync(message).ConfigureAwait(false);
    return message;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapGet("/Add", async (IMessageQueue<Message> messageQueue,ICommandHandle<string> commandHandle) =>
{
    var message = new Message(Guid.NewGuid().ToString("N"));
    var headers = new Dictionary<string, string>()
    {
        {"Route","Add"}
    };

    await commandHandle.HandleAsync(DateTime.Now.ToString()).ConfigureAwait(false);
    
    await messageQueue.PublishAsync(message, headers).ConfigureAwait(false);

    return message;
})
.WithName("Add")
.WithOpenApi();




app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}


public record Message(string message );
 