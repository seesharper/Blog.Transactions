namespace TransactionManagement.Customers
{
    using System;
    using System.Threading.Tasks;
    using CQRS;

    public class CustomerService : ICustomerService
    {
        private readonly IQueryExecutor queryExecutor;
        private readonly ICommandExecutor commandExecutor;

        public CustomerService(IQueryExecutor queryExecutor, ICommandExecutor commandExecutor)
        {
            this.queryExecutor = queryExecutor;
            this.commandExecutor = commandExecutor;
        }

        public async Task<Customer[]> GetCustomerAsync(string country)
        {
            if (string.IsNullOrEmpty(country))
            {
                throw new ArgumentNullException(country);
            }
            return await queryExecutor.ExecuteAsync(new CustomersQuery() {Country = country});
        }

        public Task SaveAsync(Customer customer)
        {
            throw new System.NotImplementedException();
        }
    }
}