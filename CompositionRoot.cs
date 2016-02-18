namespace TransactionManagement
{
    using System.Configuration;
    using System.Data;
    using System.Data.SQLite;
    using CQRS;
    using Customers;
    using Dapper;
    using Database;
    using LightInject;
    public class CompositionRoot : ICompositionRoot
    {
        static CompositionRoot()
        {
            // This code is just to ensure that the sample application 
            // has a database to work with. Not production code!!
            EnsureDatabaseIsInitialized();
        }

        public void Compose(IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterQueryHandlers();
            serviceRegistry.Register<IQueryExecutor>(factory => new QueryExecutor(factory),new PerContainerLifetime());
            serviceRegistry.Register<ICommandExecutor>(factory => new CommandExecutor(factory), new PerContainerLifetime());
            serviceRegistry.Register<ICommandHandler<AddCustomerCommand>, AddCustomerCommandHandler>();
            

            serviceRegistry.Decorate(typeof(ICommandHandler<>), typeof(TransactionalCommandHandler<>));
            // We register the connection that it is disposed when the scope ends.
            // The scope here is per web request.
            serviceRegistry.Register(factory => CreateConnection(), new PerScopeLifetime());
            serviceRegistry.Decorate<IDbConnection, ConnectionDecorator>();
        }

        private static IDbConnection CreateConnection()
        {
            var connection = new SQLiteConnection(ConfigurationManager.AppSettings["ConnectionString"]);
            connection.Open();
            return connection;
        }

        private static void EnsureDatabaseIsInitialized()
        {
            using (var connection = new SQLiteConnection(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                var result = connection.ExecuteScalar<int?>(SQL.DatabaseInitializedQuery);
                if (result == null)
                {
                    connection.Execute(SQL.CreateDatabase);
                }
            }
        }
    }
}