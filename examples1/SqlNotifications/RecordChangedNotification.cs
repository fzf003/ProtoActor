using TableDependency.SqlClient.Base.Enums;

public class RecordChangedNotification<TEntity>
{
    public TEntity Entity { get; set; }

    public TEntity EntityOldValues { get; set; }

    public ChangeType ChangeType { get; set; }
}