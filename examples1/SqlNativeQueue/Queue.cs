

using System.Formats.Asn1;
using System.Text.Unicode;
using Microsoft.Data.SqlClient;
using NServiceBus.Transport.SqlServerNative;

public static class NativeQueue
{
    public static async Task CreateQueue(SqlConnection sqlConnection)
    {
        var manager = new QueueManager("endpointTable", sqlConnection);
        await manager.Create();
    }

    public static async Task SendMessage(QueueManager queueManager)
    {
        string headers = "fzf003";
        byte[] body = System.Text.Encoding.UTF8.GetBytes("我是你的小苹果");
        var message = new OutgoingMessage(id: Guid.NewGuid(), headers: headers, bodyBytes: body);
  
        await queueManager.Send(message);
    }


    public static  Task ReadMessage(string ConnectionString)
    {

        return Task.Run(() =>
        {
             var loop = new MessageProcessingLoop(
                table: "endpointTable",
                startingRow: 1,
                connectionBuilder: async (Cancel) =>
                {
                    var sqlConnection = new SqlConnection(ConnectionString);
                    await sqlConnection.OpenAsync().ConfigureAwait(false);
                    return (sqlConnection);
                },
                delay: TimeSpan.FromSeconds(1),
                callback: async (sqltran, message, cancel) =>
                {
                    using var reader = new StreamReader(message.Body);
                    var bodyText = await reader.ReadToEndAsync(cancel);
                    Console.WriteLine(message.Id + "---" + bodyText + "--" + message.RowVersion);
                },
                errorCallback: innerException =>
                {
                    Console.WriteLine(innerException.Message);
                },
                persistRowVersion: (_, v, _) =>
                {
                    Console.WriteLine("Version:" + v);
                    return Task.CompletedTask;
                }
            );

            loop.Start();
        });
    }


}