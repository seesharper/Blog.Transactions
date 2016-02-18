namespace TransactionManagement.Database
{
    using System.Data;
    using System.Threading.Tasks;
    using CQRS;
    public class TransactionalCommandHandler<TCommand> : ICommandHandler<TCommand>
    {
        private readonly IDbConnection dbConnection;
        private readonly ICommandHandler<TCommand> commandHandler;

        public TransactionalCommandHandler(IDbConnection dbConnection, ICommandHandler<TCommand> commandHandler)
        {
            this.dbConnection = dbConnection;
            this.commandHandler = commandHandler;
        }

        public async Task HandleAsync(TCommand command)
        {
            using (var transaction = dbConnection.BeginTransaction())
            {
                await commandHandler.HandleAsync(command);
                transaction.Commit();
            }                
        }
    }
}