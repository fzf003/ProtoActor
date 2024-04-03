
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServerNative;

SqlConnectionStringBuilder connStrBldr = new SqlConnectionStringBuilder
{
    UserID = "sa",
    Password = "sczs_dev2020",
    ApplicationName = "s_applicationName",
    DataSource = "10.32.3.13",
    InitialCatalog = "HD",
    IntegratedSecurity = false,
    TrustServerCertificate = true,
    Pooling = true
};


using SqlConnection sqlConnection = new SqlConnection(connStrBldr.ConnectionString);
await sqlConnection.OpenAsync().ConfigureAwait(false);
var manager = new QueueManager("endpointTable", sqlConnection);
/*
var subscriptionManagermanager = new SubscriptionManager("SubscriptionRouting", sqlConnection);
await subscriptionManagermanager.Create();
*/
//NativeQueue.ReadMessage(connStrBldr.ConnectionString);

/*var result = await manager.Consume(
                  size: 5,
                  //  startRowVersion: 1,
                  func: async (message, cancel) =>
                  {
                      if (message.Body == null)
                      {
                          return;
                      }

                      using var reader = new StreamReader(message.Body);
                      var bodyText = await reader.ReadToEndAsync(cancel);
                      Console.WriteLine(message.Id + "---" + bodyText + "--" + message.RowVersion);

                  });*/
/*
Task.Run(async () =>
{
 
    static async Task Callback(SqlTransaction transaction, IncomingMessage message, CancellationToken cancel)
    {
        if (message.Body == null)
        {
            return;
        }

        using var reader = new StreamReader(message.Body);
        var bodyText = await reader.ReadToEndAsync(cancel);
        Console.WriteLine(message.Id + "---" + bodyText + "--" + message.RowVersion);
    }

    static void ErrorCallback(Exception exception)
    {
        Console.WriteLine(exception.Message);
    }

    Task<SqlTransaction> BuildTransaction(CancellationToken cancel)
    {
        SqlConnection sqlConnection = new SqlConnection(connStrBldr.ConnectionString);
        sqlConnection.Open();
        return Task.FromResult(sqlConnection.BeginTransaction());
    };


    Task PersistRowVersion(SqlTransaction transaction, long rowVersion, CancellationToken cancel)
    {
        return Task.CompletedTask;
    };


    var consumingLoop = new MessageConsumingLoop(table: "endpointTable",delay: TimeSpan.FromSeconds(1),transactionBuilder: BuildTransaction,callback: Callback,errorCallback: ErrorCallback);
   // consumingLoop.Start();


    /*
        var processingLoop = new MessageProcessingLoop(
            table: "endpointTable",
            delay: TimeSpan.FromSeconds(1),
            transactionBuilder: BuildTransaction,
            callback: Callback,
            errorCallback: ErrorCallback,
            startingRow: 1,
            persistRowVersion: PersistRowVersion);
        processingLoop.Start();

        Console.ReadKey();

        await processingLoop.Stop();
         


});*/



UserPubSubService userPubSubService = new UserPubSubService();

var OnError = (Exception ex, InPutMessage message) =>
{
    Console.WriteLine(ex.Message);
};

var Process = (InPutMessage message) =>
{
    Console.WriteLine(message);

    return Task.CompletedTask;
};

userPubSubService.Subscribe(Process, OnError);



JsonSerializerOptions jsonSerializer = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
};

for (; ; )
{
    var jsonString = JsonSerializer.Serialize(UserInfo.CreateUser(Guid.NewGuid().ToString("N"), "男", DateTime.Now), jsonSerializer);
    var headers = new Dictionary<string, string>
    {
        {"Title", "发送信息"}
    };
    var serialized = JsonSerializer.Serialize(headers, jsonSerializer);
    var dic= JsonSerializer.Deserialize<IDictionary<string, string>>(serialized, jsonSerializer);
    foreach(var item in dic)
    {
        Console.WriteLine(item.Key+"--"+item.Value);
    }

    var message = InPutMessage.Create(Id: Guid.NewGuid(), Expires: DateTime.Now.AddHours(-1), Headers: serialized, jsonString: jsonString);
    Console.WriteLine(message.IsExpires());
    await userPubSubService.SendAsync(message).ConfigureAwait(false);

    /* var result = await manager.Consume(
                size: 5,
              //  startRowVersion: 1,
                func: async (message, cancel) =>
                {
                    //Console.WriteLine(message.Headers);
                    if (message.Body == null)
                    {
                        return;
                    }

                    using var reader = new StreamReader(message.Body);
                    var bodyText = await reader.ReadToEndAsync(cancel);
                    Console.WriteLine(message.Id+"---"+bodyText+"--"+message.RowVersion);

                });

       Console.WriteLine(result.LastRowVersion);
       */
    //await NativeQueue.SendMessage(manager);


    Console.WriteLine("==========================================================");
    Console.ReadKey();
}