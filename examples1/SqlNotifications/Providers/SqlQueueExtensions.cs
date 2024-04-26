using System.Data;
using System.Data.SqlClient;
 
public static partial class SqlExtensions
{
 
    public static SqlCommand AddParameter(this SqlCommand command, string name, object value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
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
}
