using System;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using Microsoft.Extensions.Logging;
using TableDependency.SqlClient.Base;
using TableDependency.SqlClient.Base.Enums;
using TableDependency.SqlClient.Where;

public class CarSqlTableDependencyProvider : SqlTableDependencyProvider<Car1>
    {
        public CarSqlTableDependencyProvider(string connectionStringSettings, IScheduler scheduler, ILogger logger, LifetimeScope lifetimeScope) 
            : base(connectionString:connectionStringSettings,scheduler: scheduler,lifetimeScope: lifetimeScope)
        {
            
        }
     

        protected override ModelToTableMapper<Car1> OnInitializeMapper(ModelToTableMapper<Car1> modelToTableMapper)
        {
            modelToTableMapper.AddMapping(c => c.Id, "Id");
            modelToTableMapper.AddMapping(c => c.Name, "Name");
            modelToTableMapper.AddMapping(c => c.Home, "Home");
            modelToTableMapper.AddMapping(c => c.Tel, "Tel");
                            //  .AddMapping(c => c.State, "State");

                            Console.WriteLine("初始化.......");


            return modelToTableMapper;
        }
        //SqlTableDependencyOptions
        protected override SqlTableDependencyOptions<Car1> OnCreateSettings()
        {
            var settings = base.OnCreateSettings();

            Expression<Func<Car1, bool>> filterExpression = p => true;

            var filter = new SqlTableDependencyFilter<Car1>(filterExpression);

            settings.IncludeOldValues = true;

            settings.NotifyOn = DmlTriggerType.All;

            //settings.ExecuteUserPermissionCheck = false;

            settings.Filter = filter;


            var update = new UpdateOfModel<Car1>();

            update.Add(p => p.Name);
            update.Add(p => p.Home);
            

            settings.UpdateOf = update;
 
            return settings;
        }
         
        protected override string TableName => "Car";

        public override TimeSpan ReconnectionTimeSpan => TimeSpan.FromSeconds(3);

    }

public abstract record BaseEntity
    {
  
        public long Id { get; set; }

       // public DateTime Timestamp { get; set; } 
    }

 

 
    public record Car1: BaseEntity
    {
         
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 家庭地址
        /// </summary>

        public string Home { get; set; }
        /// <summary>
        /// 电话
        /// </summary>
        public string Tel { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int State { get; set; }

    }