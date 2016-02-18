namespace TransactionManagement.Customers
{
    using CQRS;
    public class CustomerQuery : IQuery<Customer>
    {
         public string CustomerId { get; set; }
    }
}