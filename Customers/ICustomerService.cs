namespace TransactionManagement.Customers
{
    using System.Threading.Tasks;

    public interface ICustomerService
    {
        Task<Customer[]> GetCustomerAsync(string country);

        Task SaveAsync(Customer customerRow);
    }
}