

using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Data.SqlClient;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Abstracts;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Base.EventArgs;
using ErrorEventArgs = TableDependency.SqlClient.Base.EventArgs.ErrorEventArgs;




public class SqlTableDependencyProvider<TEntity> : DisposableObject, ISqlTableDependencyProvider<TEntity>, ISqlTableDependencyProviderInit<TEntity>
      where TEntity : class, new()
  {
    #region Fields

    private readonly string connectionString;
    private readonly IScheduler scheduler;
    private readonly LifetimeScope lifetimeScope;

    #endregion

    #region Constructors
    
 
    public SqlTableDependencyProvider(
      ConnectionStringSettings connectionStringSettings, 
      IScheduler scheduler, 
      LifetimeScope lifetimeScope)
      : this(connectionStringSettings.ConnectionString, scheduler, lifetimeScope)
    {
    }

 
    public SqlTableDependencyProvider(
      string connectionString,
      IScheduler scheduler,
      LifetimeScope lifetimeScope)
    {
      if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

      this.connectionString = connectionString;
      this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
      this.lifetimeScope = lifetimeScope;
    }

    #endregion

    #region Properties

    #region SqlConnectionProvider

    protected virtual ISqlConnectionProvider SqlConnectionProvider { get; } = new SqlConnectionProvider();

    #endregion

    #region WhenEntityRecordChanges

    private readonly Subject<RecordChangedNotification<TEntity>> whenEntityRecordChangesSubject = new Subject<RecordChangedNotification<TEntity>>();
 
    public IObservable<RecordChangedNotification<TEntity>> WhenEntityRecordChanges => whenEntityRecordChangesSubject.AsObservable();

    #endregion

    #region WhenStatusChanges

    private readonly ISubject<TableDependencyStatus> whenStatusChanges = new ReplaySubject<TableDependencyStatus>(1);
 
    public IObservable<TableDependencyStatus> WhenStatusChanges => whenStatusChanges.AsObservable();

    #endregion

    #region Settings

    private SqlTableDependencyOptions<TEntity> settings;

    private SqlTableDependencyOptions<TEntity> Settings => settings ?? (settings = OnCreateSettings() ?? new SqlTableDependencyOptions<TEntity>());

    #endregion

    #region TableName

    protected virtual string TableName
    {
      get
      {
        var tableAttribute = TypeExtensions.GetTableAttribute<TEntity>();
        
        if (tableAttribute != null)
          return tableAttribute.Name;

        if (!string.IsNullOrWhiteSpace(Settings.TableName))
          return Settings.TableName;

        return typeof(TEntity).Name;
      }
    }

    #endregion

    #region SchemaName

    internal string SchemaName
    {
      get
      {
        var tableAttribute = TypeExtensions.GetTableAttribute<TEntity>();

        if (tableAttribute != null)
          return tableAttribute.Schema;
        
        return Settings?.SchemaName;
      }
    }

    #endregion

    #region IsDatabaseAvailable

    private readonly TimeSpan testConnectionTimeout = TimeSpan.FromSeconds(2);

    protected virtual bool IsDatabaseAvailable
    {
      get
      {
        var connected = SqlConnectionProvider.TestConnection(connectionString, testConnectionTimeout);

        return connected;
      }
    }

    #endregion

    #region ReconnectionTimeSpan

    public virtual TimeSpan ReconnectionTimeSpan => TimeSpan.FromSeconds(5);

    #endregion

    #endregion

    #region Methods

    #region SubscribeToEntityChanges

    private int subscriptionsCounter;


    public ISqlTableDependencyProvider<TEntity> SubscribeToEntityChanges()
    {
      if (Interlocked.Increment(ref subscriptionsCounter) != 1)
        throw new NotSupportedException("不支持多订阅");
      
      if(IsDisposed)
        throw new ObjectDisposedException(GetType().Name);

      TrySubscribeToTableChanges();

      return this;
    }

    #endregion

    #region TryReconnect

    private readonly SerialDisposable reconnectSubscription = new SerialDisposable();

    private void TryReconnect()
    {
      reconnectSubscription.Disposable = Observable.Timer(ReconnectionTimeSpan, ReconnectionTimeSpan, scheduler)
        .Where(c => IsDatabaseAvailable)
        .Take(1)
        .Subscribe(_ =>
        {
          OnBeforeServiceBrokerSubscription();

          TrySubscribeToTableChanges();
        }, error => TryReconnect());
    }

    #endregion

    #region OnBeforeServiceBrokerSubscription

    protected virtual void OnBeforeServiceBrokerSubscription()
    {
    }

    #endregion
    
    #region OnCreateSettings

 
    protected virtual SqlTableDependencyOptions<TEntity> OnCreateSettings()
    {
      return new SqlTableDependencyOptions<TEntity>();
    }

    #endregion

    #region CreateSqlTableDependency

    protected virtual ITableDependency<TEntity> CreateSqlTableDependency(IModelToTableMapper<TEntity> modelToTableMapper)
    {
      var SqlTableDependencyOptions = Settings;

      switch (lifetimeScope)
      {
        case LifetimeScope.ConnectionScope:
          return new SqlTableDependencyWithReconnection<TEntity>(connectionString, TableName,
            schemaName: SchemaName, mapper: modelToTableMapper, updateOf: SqlTableDependencyOptions.UpdateOf,
            filter: SqlTableDependencyOptions.Filter, notifyOn: SqlTableDependencyOptions.NotifyOn,
            executeUserPermissionCheck: SqlTableDependencyOptions.ExecuteUserPermissionCheck,
            includeOldValues: SqlTableDependencyOptions.IncludeOldValues);

        case LifetimeScope.ApplicationScope:
          return new SqlTableDependencyWitApplicationScope<TEntity>(connectionString, TableName,
            schemaName: SchemaName, mapper: modelToTableMapper, updateOf: SqlTableDependencyOptions.UpdateOf,
            filter: SqlTableDependencyOptions.Filter, notifyOn: SqlTableDependencyOptions.NotifyOn,
            executeUserPermissionCheck: SqlTableDependencyOptions.ExecuteUserPermissionCheck,
            includeOldValues: SqlTableDependencyOptions.IncludeOldValues);

        case LifetimeScope.UniqueScope:
          return new SqlTableDependencyWithUniqueScope<TEntity>(connectionString, TableName,
            schemaName: SchemaName, mapper: modelToTableMapper, updateOf: SqlTableDependencyOptions.UpdateOf,
            filter: SqlTableDependencyOptions.Filter, notifyOn: SqlTableDependencyOptions.NotifyOn,
            executeUserPermissionCheck: SqlTableDependencyOptions.ExecuteUserPermissionCheck,
            includeOldValues: SqlTableDependencyOptions.IncludeOldValues);

        default:
          return null;
      }
    }

    #endregion

    #region TrySubscribeToTableChanges

    private ITableDependency<TEntity> sqlTableDependency;

    private void TrySubscribeToTableChanges()
    {
      if (lifetimeScope != LifetimeScope.ConnectionScope && sqlTableDependency != null)
      {
        StartSqlTableDependency();

        return;
      }

      TryStopLastConnection();

      var modelToTableMapper = InitializeMapper();

      try
      {
        sqlTableDependency = CreateSqlTableDependency(modelToTableMapper);

        sqlTableDependency.OnChanged += SqlTableDependencyOnChanged;
        sqlTableDependency.OnError += SqlTableDependencyOnError;
        sqlTableDependency.OnStatusChanged += OnSqlTableDependencyStatusChanged;
        
        StartSqlTableDependency();
      }
      catch (Exception error)
      {
        TryHandlerErrors(error);

        TryReconnect();
      }
    }

    #endregion

    #region StartSqlTableDependency

    private void StartSqlTableDependency()
    {
      Console.WriteLine("启动.......");
      sqlTableDependency.Start(Settings.TimeOut, Settings.WatchDogTimeOut);
    }

    #endregion

    #region OnConnected

    protected virtual void OnConnected()
    {
    }

    #endregion

    #region OnError

    protected virtual void OnError(Exception error)
    {
    }

    #endregion

    #region TryHandlerErrors

    private readonly int TheConversationHandleIsNotFound = 8426;

    private void TryHandlerErrors(Exception error)
    {
      sqlTableDependency?.Stop();

      if (error is SqlException sqlException && sqlException.Number == TheConversationHandleIsNotFound)
        TryStopLastConnection();

      whenStatusChanges.OnNext(TableDependencyStatus.StopDueToError);
      
      OnError(error);
    }

    #endregion

    #region SqlTableDependencyOnChanged

    private void SqlTableDependencyOnChanged(object sender, RecordChangedEventArgs<TEntity> eventArgs)
    {
      var entity = eventArgs.Entity;

      switch (eventArgs.ChangeType)
      {
        case ChangeType.Insert:
          OnInserted(entity);
          break;
        case ChangeType.Update:
          OnUpdated(entity, eventArgs.EntityOldValues);
          break;
        case ChangeType.Delete:
          OnDeleted(entity);
          break;
      }

      var recordChangedNotification = new RecordChangedNotification<TEntity>()
                                      {
                                        Entity = entity,
                                        EntityOldValues = eventArgs.EntityOldValues,
                                        ChangeType = eventArgs.ChangeType
                                      };

      whenEntityRecordChangesSubject.OnNext(recordChangedNotification);
    }

    #endregion

    #region OnSqlTableDependencyStatusChanged

    private void OnSqlTableDependencyStatusChanged(object sender, StatusChangedEventArgs e)
    {
      if(e.Status == TableDependencyStatus.Started)
        OnConnected();

      whenStatusChanges.OnNext(e.Status);

      SqlTableDependencyOnStatusChanged(sender, e);
    }

    #endregion

    #region SqlTableDependencyOnStatusChanged

    protected virtual void SqlTableDependencyOnStatusChanged(object sender, StatusChangedEventArgs e)
    {
    }

    #endregion

    #region SqlTableDependencyOnError

    private void SqlTableDependencyOnError(object sender, ErrorEventArgs e)
    {
      TryHandlerErrors(e.Error);

      TryReconnect();
    }

    #endregion

    #region InitializeMapper

    private ModelToTableMapper<TEntity> InitializeMapper()
    {
      var modelToTableMapper = OnInitializeMapper(new ModelToTableMapper<TEntity>());

      if (modelToTableMapper == null || modelToTableMapper.Count() == 0)
        return null;

      return modelToTableMapper;
    }

    #endregion

    #region OnInitializeMapper

    protected virtual ModelToTableMapper<TEntity> OnInitializeMapper(ModelToTableMapper<TEntity> modelToTableMapper)
    {
      return modelToTableMapper;
    }

    #endregion

    #region OnInserted

    protected virtual void OnInserted(TEntity entity)
    {
    }

    #endregion

    #region OnUpdated

    protected virtual void OnUpdated(TEntity entity, TEntity entityOldValues)
    {
    }

    #endregion

    #region OnDeleted

    protected virtual void OnDeleted(TEntity entity)
    {
    }

    #endregion
    
    #region CreateBulkRecordChangesNotifier

  
    public IObservable<RecordsChangedNotification<TEntity>> CreateBulkRecordChangesNotifier(TimeSpan timeSpan, int count = 50, IScheduler bulkScheduler = null)
    {
      if (timeSpan < TimeSpan.Zero)
        throw new ArgumentOutOfRangeException(nameof(timeSpan));

      if (count <= 0)
        throw new ArgumentOutOfRangeException(nameof(count));

      if (bulkScheduler == null)
        bulkScheduler = scheduler;

      return whenEntityRecordChangesSubject
        .Buffer(timeSpan, count, bulkScheduler)
        .Where(recordChangedNotifications => recordChangedNotifications.Count > 0)
        .Select(c => new RecordsChangedNotification<TEntity>(c))
        .AsObservable();
    }

    #endregion

    #region TryStopLastConnection

    protected void TryStopLastConnection()
    {
      if (sqlTableDependency == null)
        return;

      sqlTableDependency.OnError -= SqlTableDependencyOnError;
      sqlTableDependency.OnStatusChanged -= SqlTableDependencyOnStatusChanged;
      sqlTableDependency.OnChanged -= SqlTableDependencyOnChanged;

      try
      {
        sqlTableDependency.Dispose();
        sqlTableDependency = null;
      }
      catch (Exception e)
      {
        TryHandlerErrors(e);
      }
    }

    #endregion

    #region OnDispose

    protected override void OnDispose()
    {
      base.OnDispose();

      using (reconnectSubscription)
      {
      }

      TryStopLastConnection();
    }

    #endregion

    #endregion

    private static class TypeExtensions
    {
      private static Attribute GetAttribute<TType, TTableAttribute>()
      {
        var attribute = typeof(TType).GetTypeInfo().GetCustomAttribute(typeof(TTableAttribute));
	  
        return attribute;
      }

      internal static TableAttribute GetTableAttribute<TType>()
      {
        var tableAttribute = GetAttribute<TType, TableAttribute>();
	  
        return (TableAttribute)tableAttribute;
      }
    }
  }
