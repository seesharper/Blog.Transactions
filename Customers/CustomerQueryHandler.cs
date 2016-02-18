namespace TransactionManagement.Customers
{
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using CQRS;
    using Dapper;
    using Database;

    public class CustomerQueryHandler : IQueryHandler<CustomerQuery, Customer>
    {
        private readonly IDbConnection dbConnection;

        public CustomerQueryHandler(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<Customer> HandleAsync(CustomerQuery query)
        {
            return (await dbConnection.QueryAsync<Customer>(SQL.Customer, query)).Single();
        }
    }
}