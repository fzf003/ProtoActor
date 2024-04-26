using System.Collections.ObjectModel;

public class RecordsChangedNotification<TEntity> : Collection<RecordChangedNotification<TEntity>>
{
    public RecordsChangedNotification(IList<RecordChangedNotification<TEntity>> list)
      : base(list)
    {
    }
}