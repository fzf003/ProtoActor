
using TableDependency.SqlClient.Base.Abstracts;
using TableDependency.SqlClient.Base.Enums;


public class SqlTableDependencyWithUniqueScope<TEntity> : SqlTableDependencyWithReconnection<TEntity>
  where TEntity : class, new()
{


  public SqlTableDependencyWithUniqueScope(string connectionString, string tableName = null, string schemaName = null, IModelToTableMapper<TEntity> mapper = null, IUpdateOfModel<TEntity> updateOf = null, ITableDependencyFilter filter = null, DmlTriggerType notifyOn = DmlTriggerType.All, bool executeUserPermissionCheck = true, bool includeOldValues = false)
    : base(connectionString, tableName, schemaName, mapper, updateOf, filter, notifyOn, executeUserPermissionCheck,
      includeOldValues)
  {
  }
  public override LifetimeScope LifetimeScope { get; } = LifetimeScope.UniqueScope;
}
