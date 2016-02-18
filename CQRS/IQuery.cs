namespace TransactionManagement.CQRS
{
    /// <summary>
    /// A marker interface to be implemented by all queries.
    /// </summary>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    public interface IQuery<TResult>
    {
    }
}