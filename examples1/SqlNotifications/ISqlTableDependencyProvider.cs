using System;
using TableDependency.SqlClient.Base.Enums;
 
public interface ISqlTableDependencyProviderEvents : IDisposable
{
  IObservable<TableDependencyStatus> WhenStatusChanges { get; }
}

public interface ISqlTableDependencyProvider : ISqlTableDependencyProviderEvents
{
}

public interface ISqlTableDependencyProviderInit<TEntity>
  where TEntity : class, new()
{
  ISqlTableDependencyProvider<TEntity> SubscribeToEntityChanges();
}

public interface ISqlTableDependencyProviderEvents<TEntity> : ISqlTableDependencyProvider
  where TEntity : class, new()
{
  IObservable<RecordChangedNotification<TEntity>> WhenEntityRecordChanges { get; }
}

public interface ISqlTableDependencyProvider<TEntity> : ISqlTableDependencyProviderInit<TEntity>, ISqlTableDependencyProviderEvents<TEntity>
  where TEntity : class, new()
{
}
