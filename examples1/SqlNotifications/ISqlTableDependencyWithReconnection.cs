using System;
using TableDependency.SqlClient.Base.Abstracts;

 
  public interface ISqlTableDependencyWithReconnection<TEntity> : ITableDependency<TEntity> 
    where TEntity : class, new()
  {

  }
