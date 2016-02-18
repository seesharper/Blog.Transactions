namespace TransactionManagement.Customers
{
    using CQRS;

    public class CustomersQuery : IQuery<Customer[]>
    {
        public string Country { get; set; }
    }
}