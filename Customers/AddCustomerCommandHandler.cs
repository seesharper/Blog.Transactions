namespace TransactionManagement.Customers
{
    using System.Data;
    using System.Threading.Tasks;
    using CQRS;
    using Dapper;
    using Database;

    public class AddCustomerCommandHandler : ICommandHandler<AddCustomerCommand>
    {
        private readonly IDbConnection dbConnection;

        public AddCustomerCommandHandler(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task HandleAsync(AddCustomerCommand command)
        {
            await dbConnection.ExecuteAsync(SQL.InsertCustomer, command);
        }
    }
}