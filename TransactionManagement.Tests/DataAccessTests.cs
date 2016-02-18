namespace TransactionManagement.Tests
{
    using System.Threading.Tasks;
    using CQRS;
    using Customers;
    using LightInject;
    using LightInject.xUnit2;
    using Xunit;
    using Xunit.Sdk;

    public class DataAccessTests
    {
        [Theory, Scoped, InjectData]
        public async Task ShouldGetCustomersFromGermany(IQueryExecutor queryExecutor)
        {
            var query = new CustomersQuery {Country = "Germany"};
            var result = await queryExecutor.ExecuteAsync(query);
            Assert.Equal(11, result.Length);
        }

        [Theory, Scoped]
        [InjectData("France", 11)]
        [InjectData("Germany", 11)]
        [InjectData("Norway", 1)]
        public async Task ShouldGetCustomers(IQueryExecutor queryExecutor, string country, int expectedCount)
        {
            var result = await queryExecutor.ExecuteAsync(new CustomersQuery() { Country = country });
            Assert.Equal(expectedCount, result.Length);
        }

        [Theory, Scoped, InjectData]
        public async Task ShouldAddCustomer(ICommandExecutor commandExecutor, IQueryExecutor queryExecutor)
        {
            await commandExecutor.ExecuteAsync(new AddCustomerCommand {CustomerId = "AAPL", CompanyName = "Apple Inc"});
            var newCustomer = await queryExecutor.ExecuteAsync(new CustomerQuery {CustomerId = "AAPL"});
            Assert.Equal("Apple Inc", newCustomer.CompanyName);
        }


        public static void Configure(IServiceContainer container)
        {
            container.RegisterFrom<CompositionRoot>();
            container.Decorate(typeof(ICommandHandler<>), typeof(RollbackCommandHandler<>));
        }
    }
}