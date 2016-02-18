namespace TransactionManagement.CQRS
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a class that is capable of executing a query
    /// </summary>
    public interface IQueryExecutor
    {
        /// <summary>
        /// Executes the given <paramref name="query"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <returns>The result from the query.</returns>
        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query);
    }
}