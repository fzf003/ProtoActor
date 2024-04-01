using System.Data;
using Microsoft.Data.SqlClient;

public static partial class SqlQueueExtensions
{
 
    public static SqlCommand AddParameter(this SqlCommand command, string name, SqlDbType type, object value)
    {
        command.Parameters.Add(name, type).Value = value ?? DBNull.Value;
        return command;
    }

    public static SqlCommand AddParameter(this SqlCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    public static SqlCommand AddParameter(this SqlCommand command, string name, SqlDbType type, object value, int size)
    {
        command.Parameters.Add(name, type, size).Value = value ?? DBNull.Value;
        return command;
    }

    public static async Task<int> GetMessageIdAsync(this SqlDataReader dataReader, int headersIndex, CancellationToken cancellationToken = default)
    {
        if (await dataReader.IsDBNullAsync(headersIndex, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }
        return await dataReader.GetFieldValueAsync<int>(headersIndex, cancellationToken).ConfigureAwait(false);
    }


    public static async Task<string> GetStringAsync(this SqlDataReader dataReader, int headersIndex, CancellationToken cancellationToken = default)
    {
        if (await dataReader.IsDBNullAsync(headersIndex, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }
        return await dataReader.GetFieldValueAsync<string>(headersIndex, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<DateTime> GetDataTimeAsync(this SqlDataReader dataReader, int headersIndex, CancellationToken cancellationToken = default)
    {
        if (await dataReader.IsDBNullAsync(headersIndex, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }

        return await dataReader.GetFieldValueAsync<DateTime>(headersIndex, cancellationToken).ConfigureAwait(false);
    }


    public static async Task<string> GetMessageAsync(this SqlDataReader dataReader, int messageIndex, CancellationToken cancellationToken = default)
    {
        if (await dataReader.IsDBNullAsync(messageIndex, cancellationToken).ConfigureAwait(false))
        {
            return default;
        }

        using (var textReader = dataReader.GetTextReader(messageIndex))
        {
            return await textReader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}



 