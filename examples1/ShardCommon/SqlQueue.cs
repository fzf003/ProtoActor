

using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Transactions;
 
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
 

public partial class SqlQueue
{

    #region Sql
    public static readonly string CreateQueueText = @"
                            IF EXISTS (
                                SELECT *
                                FROM sys.objects
                                WHERE object_id = OBJECT_ID(N'{0}')
                                    AND type in (N'U'))
                            RETURN

                            EXEC sp_getapplock @Resource = '{0}_lock', @LockMode = 'Exclusive'

                            IF EXISTS (
                                SELECT *
                                FROM {1}.sys.objects
                                WHERE object_id = OBJECT_ID(N'{0}')
                                    AND type in (N'U'))
                            BEGIN
                                EXEC sp_releaseapplock @Resource = '{0}_lock'
                                RETURN
                            END

                            BEGIN TRY
                                CREATE TABLE {0} (
                                    Id uniqueidentifier NOT NULL,
                                    CorrelationId varchar(255),
                                    ReplyToAddress varchar(255),
                                    Recoverable bit NOT NULL,
                                    Expires datetime,
                                    Headers nvarchar(max) NOT NULL,
                                    Body varbinary(max),
                                    RowVersion bigint IDENTITY(1,1) NOT NULL
                                );

                                CREATE NONCLUSTERED INDEX Index_RowVersion ON {0}
                                (
                                    [RowVersion] ASC
                                )

                                CREATE NONCLUSTERED INDEX Index_Expires ON {0}
                                (
                                    Expires
                                )
                                INCLUDE
                                (
                                    Id,
                                    RowVersion
                                )
                                WHERE
                                    Expires IS NOT NULL
                            END TRY
                            BEGIN CATCH
                                EXEC sp_releaseapplock @Resource = '{0}_lock';
                                THROW;
                            END CATCH;

                            EXEC sp_releaseapplock @Resource = '{0}_lock'";
    public static readonly string PeekText = @"
SELECT isnull(cast(max([RowVersion]) - min([RowVersion]) + 1 AS int), 0) Id FROM {0} WITH (nolock)";

    public static readonly string QueueName = "InputQueue";

    public static readonly string SendSqlText = "INSERT INTO {0} (UserName,Sex,DueAfter,State) values(@UserName,@Sex,@DueAfter,@State)";

    public static readonly string PurgeText = "DELETE FROM {0}";

    public static readonly string GetNextQuery =
           @"DECLARE @Ids TABLE (Id int)
        UPDATE TOP({0}) {1} SET
             DueAfter = getdate(),
             State=3
        OUTPUT Inserted.UserId INTO @Ids
        WHERE State=0
        SELECT 
            UserId,
            UserName,
            Sex, 
            DueAfter
        FROM {1} WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE UserId IN (SELECT Id FROM @Ids)";


    #endregion

    static IServiceProvider GetServiceProvider(Action<ServiceCollection> action = null)
    {
        const string connectionString = "";
        var services = new ServiceCollection();

        if (action is not null)
        {
            action(services);
        }

      /*  services.AddDapperPersistence(dic =>
        {
            dic.Add("HD", connectionString);
        });*/

        return services.BuildServiceProvider();
    }

    public static async Task<int> SendAsync(SqlConnection sqlConnection, UserInfo userInfo, CancellationToken cancellationToken = default)
    {
        if(sqlConnection.State != ConnectionState.Open)
        {
            await sqlConnection.OpenAsync().ConfigureAwait(false);
        }
        
        int Affected = 0;
        using (var transaction = sqlConnection.BeginTransaction())
        {
            Affected = await SendAsync(sqlConnection, transaction, userInfo, cancellationToken);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        return Affected;
    }

    public static async Task<int> SendAsync(SqlConnection sqlConnection, SqlTransaction transaction, UserInfo userInfo, CancellationToken cancellationToken = default)
    {
        int Affected = 0;
        var sendCommand = string.Format(SendSqlText, "HD..Users");
        if(sqlConnection.State != ConnectionState.Open)
        {
            await sqlConnection.OpenAsync().ConfigureAwait(false);
        }

        using (var SqlCommand = new SqlCommand(sendCommand, sqlConnection, transaction))
        {
 
            Affected = await SqlCommand.AddParameter(nameof(userInfo.UserName), userInfo.UserName)
                                      .AddParameter(nameof(userInfo.Sex), userInfo.Sex)
                                      .AddParameter(nameof(userInfo.DueAfter), userInfo.DueAfter)
                                      .AddParameter("State", 0)
                                      .ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await SqlCommand.DisposeAsync().ConfigureAwait(false);
        }
        return Affected;
    }



    public static async Task<int> MyNextAsync(SqlConnection sqlConnection)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        int Id = 0;

        var NextQuerySql = "SELECT NEXT VALUE FOR dbo.FzfSequence";

        using (var SqlCommand = new SqlCommand(NextQuerySql, sqlConnection))
        {
            using var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.Default | CommandBehavior.SequentialAccess).ConfigureAwait(false);

            if (await dataReader.ReadAsync().ConfigureAwait(false))
            {
                Id = dataReader.GetInt32(0);

                // var message = await UserInfo.ReadRow(dataReader, cancellationToken: CancellationToken.None).ConfigureAwait(false);

                // await func(message, sqlConnection).ConfigureAwait(false);
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"{Id} 已被确认耗时:{stopwatch.ElapsedMilliseconds}/ms");
        return Id;
    }
//

    public static async Task FetchAsync(Func<SqlConnection> sqlConnectionfunc, int batchsize, Func<UserInfo, Task> func, CancellationToken cancellationToken = default)
    {
 
        var NextQuerySql = string.Format(GetNextQuery, batchsize, "HD..Users");
 
        while (!cancellationToken.IsCancellationRequested)
        {
            using SqlConnection sqlConnection = sqlConnectionfunc();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using (var SqlCommand = new SqlCommand(NextQuerySql, sqlConnection))
            {
                 using var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.Default | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

                if (dataReader.HasRows == false)
                {
                    await sqlConnection.CloseAsync().ConfigureAwait(false);
                    await Task.Delay(1000 * 2).ConfigureAwait(false);
                    Console.WriteLine("............................................");
                    continue;
                }

                while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var message = await UserInfo.ReadRow(dataReader, cancellationToken: cancellationToken).ConfigureAwait(false);

                    await func(message).ConfigureAwait(false);
                }

                await sqlConnection.CloseAsync().ConfigureAwait(false);

            }

        }
    }



     public static async Task FetchAsync(ChannelWriter<UserInfo> channelWriter ,Func<SqlConnection> sqlConnectionfunc, int batchsize,  CancellationToken cancellationToken = default)
    {
        //var NextQuerySql = string.Format("SELECT top ({0}) * FROM HD..Users WHERE State in(0)", batchsize);

        var NextQuerySql = string.Format(GetNextQuery, batchsize, "HD..Users");

        while (!cancellationToken.IsCancellationRequested)
        {
            using SqlConnection sqlConnection = sqlConnectionfunc();
            await sqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
            using (var SqlCommand = new SqlCommand(NextQuerySql, sqlConnection))
            {

                using var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.Default | CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);

                if (dataReader.HasRows == false)
                {
                    await sqlConnection.CloseAsync().ConfigureAwait(false);
                    await Task.Delay(1000 * 2).ConfigureAwait(false);
                    Console.WriteLine("............................................");
                    continue;
                }

                while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var message = await UserInfo.ReadRow(dataReader, cancellationToken: cancellationToken).ConfigureAwait(false);

                    Console.WriteLine($"Send:{message}");
                    await channelWriter.WriteAsync(message).ConfigureAwait(false);
                    await channelWriter.WaitToWriteAsync(cancellationToken).ConfigureAwait(false);
                      
                    //await func(message).ConfigureAwait(false);
                }

                

                await sqlConnection.CloseAsync().ConfigureAwait(false);

            }

        }
    }




    public static async Task ConsumeAsync(ChannelReader<UserInfo> channelReader, Func<UserInfo, Task> func, CancellationToken cancellationToken = default)
    {
        await foreach (var item in channelReader.ReadAllAsync(cancellationToken))
        {
            await func(item).ConfigureAwait(false);
        }
    }




    public static async Task AckAsync(SqlConnection sqlConnection, int[] ids, CancellationToken cancellationToken = default)
    {
        using var transaction = sqlConnection.BeginTransaction();
        try
        {
            foreach (var id in ids)
            {
                await AckAsync(sqlConnection, id, transaction, cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
    }


    public static async Task AckAsync(SqlConnection sqlConnection, int id, CancellationToken cancellationToken = default)
    {
        if(sqlConnection.State != ConnectionState.Open)
        {
            await sqlConnection.OpenAsync().ConfigureAwait(false);
        }

        using var transaction = sqlConnection.BeginTransaction();
        try
        {
            await AckAsync(sqlConnection, id, transaction, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task AckAsync(SqlConnection sqlConnection, int id, SqlTransaction sqlTransaction, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
         Console.WriteLine($"{id} 开始处理...");

        var sendCommand = string.Format("UPDATE {0} SET State=@State where UserId=@Id ", "HD..Users");
        using (var SqlCommand = new SqlCommand(sendCommand, sqlConnection, sqlTransaction))
        {
           await SqlCommand.AddParameter("Id", id)
                           .AddParameter("State", 6)
                           .ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        stopwatch.Stop();

        Console.WriteLine($"{id} 已被确认耗时:{stopwatch.ElapsedMilliseconds}/ms");

    }


    public static async Task<List<UserInfo>> GetNextAsync(SqlConnection sqlConnection, string TableName, int batchCount = 10, CancellationToken cancellationToken = default)
    {
        List<UserInfo> store = new();

        var NextQuerySql = string.Format(GetNextQuery, batchCount, TableName);

        using (var SqlCommand = new SqlCommand(NextQuerySql, sqlConnection))
        {
            using var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var message = await UserInfo.ReadRow(dataReader, cancellationToken: CancellationToken.None).ConfigureAwait(false);

                store.Add(message);
            }

            await dataReader.CloseAsync().ConfigureAwait(false);
            await SqlCommand.DisposeAsync().ConfigureAwait(false);
            await sqlConnection.CloseAsync().ConfigureAwait(false);
        }

        return store;
    }

    public static async Task PurgeAsync(SqlConnection sqlConnection, string TableName, CancellationToken cancellationToken = default)
    {
        using var transaction = sqlConnection.BeginTransaction();
        await PurgeAsync(sqlConnection, transaction, TableName, cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }


    public static async Task PurgeAsync(SqlConnection sqlConnection, SqlTransaction transaction, string TableName, CancellationToken cancellationToken = default)
    {
        var Purgesql = string.Format(PurgeText, TableName);
        using var SqlCommand = new SqlCommand(Purgesql, sqlConnection, transaction);
        await SqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }





    public static async Task<int> TryPeek(SqlConnection sqlConnection, int? timeoutInSeconds = null, CancellationToken cancellationToken = default)
    {
        int numberOfMessages = 0;
        using (var transaction = sqlConnection.BeginTransaction())
        {
            numberOfMessages = await TryPeek(sqlConnection, transaction, timeoutInSeconds, cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false); ;
        }

        return numberOfMessages;
    }

    public static async Task<int> TryPeek(SqlConnection connection, SqlTransaction transaction, int? timeoutInSeconds = null, CancellationToken cancellationToken = default)
    {
        var peekCommand = string.Format(PeekText, QueueName);
        using var command = new SqlCommand(peekCommand, connection, transaction)
        {
            CommandTimeout = timeoutInSeconds ?? 30
        };
        var numberOfMessages = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return (int)numberOfMessages;
    }



    public static async Task CreateQueue(SqlConnection sqlConnection, CancellationToken cancellationToken = default)
    {

        var sql = string.Format(CreateQueueText, QueueName, "HD");
        try
        {
            using (var transaction = sqlConnection.BeginTransaction())
            {

                using (var command = new SqlCommand(sql, sqlConnection, transaction)
                {
                    CommandType = CommandType.Text
                })
                {
                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (SqlException e) when (e.Number is 2714 or 1913)
        {
            e.Data["Sql"] = sql;
            throw e;
        }
    }

    public static async IAsyncEnumerable<UserInfo> ReaderStreamAsync(SqlConnection sqlConnection, string sql = "SELECT * FROM HD..Users where UserId>@UserId", int Id = 0, CancellationToken cancellationToken = default)
    {
        using (var transaction = sqlConnection.BeginTransaction())
        {
            await foreach (var message in ReaderStreamAsync(sqlConnection, transaction, sql, Id, cancellationToken).ConfigureAwait(false))
            {
                yield return message;
            }
        }
    }


    public static async IAsyncEnumerable<UserInfo> ReaderStreamAsync(SqlConnection sqlConnection, SqlTransaction transaction, string sql = "SELECT * FROM HD..Users where UserId>@UserId", int Id = 0, CancellationToken cancellationToken = default)
    {
        using var SqlCommand = new SqlCommand(sql, sqlConnection, transaction);
        SqlCommand.AddParameter("UserId", SqlDbType.Int, Id);

        using var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var message = await UserInfo.ReadRow(dataReader, cancellationToken: CancellationToken.None).ConfigureAwait(false);

            yield return message;
        }
    }


    public static async Task ReaderCreateQueue(SqlConnection sqlConnection, string sql = "SELECT * FROM HD..Users where UserId>@UserId", int Id = 0)
    {
        // var sqlConnection = sqlConnection;
        /*  if (sqlConnection.State == ConnectionState.Closed)
          {
              await sqlConnection.OpenAsync().ConfigureAwait(false);
          }*/
        using (var scope = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
        using (var Transaction = sqlConnection.BeginTransaction())
        using (var SqlCommand = new SqlCommand(sql, sqlConnection, transaction: Transaction))//sqlConnection.CreateCommand()
        {
            //SqlCommand.CommandText = sql;
            //SqlCommand.Transaction = (SqlTransaction)Transaction;
            #region add Parameters
            SqlCommand.AddParameter("UserId", SqlDbType.Int, Id);
            //SqlCommand.Parameters.AddWithValue("UserId", 0);
            #endregion

            using (var dataReader = await SqlCommand.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection).ConfigureAwait(false))
            {
                int headersIndex = 1;
                while (await dataReader.ReadAsync().ConfigureAwait(false))
                {
                    var message = await UserInfo.ReadRow(dataReader, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                    // var message = await dataReader.GetMessageAsync(headersIndex, CancellationToken.None);
                    Console.WriteLine(message);
                }
            }

            scope.Complete();
        }

    }








}



public record UserInfo
{
    public static async Task<UserInfo> ReadRow(SqlDataReader dataReader, CancellationToken cancellationToken = default)
    {
        // return  dataReader.GetRowParser<UserInfo>()(dataReader);
        return new UserInfo
        {
            UserId = await dataReader.GetMessageIdAsync(0, cancellationToken).ConfigureAwait(false),
            UserName = await dataReader.GetStringAsync(1, cancellationToken).ConfigureAwait(false),
            Sex = await dataReader.GetStringAsync(2, cancellationToken).ConfigureAwait(false),
            DueAfter = await dataReader.GetDataTimeAsync(3, cancellationToken).ConfigureAwait(false)
        };
    }

    public static UserInfo CreateUser(string UserName, string Sex, DateTime DueAfter)
    {
        return new UserInfo
        {
            UserName = UserName,
            Sex = Sex,
            DueAfter = DueAfter
        };
    }

    
   

    public int UserId { get; set; }
    public string UserName { get; set; }
    public string Sex { get; set; }

    public DateTime DueAfter { get; set; }
}


