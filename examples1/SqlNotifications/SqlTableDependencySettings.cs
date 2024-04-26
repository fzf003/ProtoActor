using TableDependency.SqlClient.Base.Abstracts;
using TableDependency.SqlClient.Base.Enums;
 
  public class SqlTableDependencyOptions<TEntity> 
    where TEntity : class
  {
     
    public string SchemaName { get; set; }

 
    public string TableName { get; set; }
    
 
    public IUpdateOfModel<TEntity> UpdateOf { get; set; }
    
 
    public ITableDependencyFilter Filter { get; set; }
    
  
    public DmlTriggerType NotifyOn { get; set; } = DmlTriggerType.All;
    
  
    public bool ExecuteUserPermissionCheck { get; set; } = true;
    
  
    public bool IncludeOldValues { get; set; }

 
    public int TimeOut { get; set; } = 120;

 
    public int WatchDogTimeOut { get; set; } = 180;
  }
