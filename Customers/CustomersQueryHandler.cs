namespace TransactionManagement.Customers
{
    using System.Data;
    using System.Linq;
    using System.Threading.Tasks;
    using CQRS;
    using Dapper;
    using Database;

    public class CustomersQueryHandler : IQueryHandler<CustomersQuery, Customer[]>
    {
        private readonly IDbConnection dbConnection;

        public CustomersQueryHandler(IDbConnection dbConnection)
        {
            this.dbConnection = dbConnection;
        }

        public async Task<Customer[]> HandleAsync(CustomersQuery query)
        {
            var result = await dbConnection.QueryAsync<CustomerRow>(SQL.CustomersByCountry, query);
            return result.Select(MapToCustomer).ToArray();        
        }

        private static Customer MapToCustomer(CustomerRow customerRow)
        {
            return new Customer {CompanyName = customerRow.CompanyName, CustomerId = customerRow.CustomerId};
        }
    }
}