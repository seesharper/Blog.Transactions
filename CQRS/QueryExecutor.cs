namespace TransactionManagement.CQRS
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Tasks;
    using LightInject;

    /// <summary>
    /// An <see cref="IQueryExecutor"/> that is capable of executing a query.
    /// </summary>
    public class QueryExecutor : IQueryExecutor
    {
        private static readonly MethodInfo GetInstanceMethod;
        private static readonly MethodInfo GetTypeFromHandleMethod;
        private readonly IServiceFactory factory;

        static QueryExecutor()
        {
            GetInstanceMethod = typeof(IServiceFactory).GetMethod("GetInstance", new[] { typeof(Type) });
            GetTypeFromHandleMethod = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExecutor"/> class.
        /// </summary>
        /// <param name="factory">The <see cref="IServiceFactory"/> used to resolve the 
        /// <see cref="IQueryHandler{TQuery,TResult}"/> to be executed.</param>
        public QueryExecutor(IServiceFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Executes the given <paramref name="query"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
        /// <param name="query">The query to be executed.</param>
        /// <returns>The result from the query.</returns>
        public async Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query)
        {
            return await Cache<TResult>.GetOrAdd(query.GetType(), CreateDelegate<TResult>)(query);
        }

        private Func<IQuery<TResult>, Task<TResult>> CreateDelegate<TResult>(Type queryType)
        {
            // Define the signature of the dynamic method.
            var dynamicMethod = new DynamicMethod("Query", typeof(Task<TResult>), new[] { typeof(IServiceFactory), typeof(IQuery<TResult>) });
            ILGenerator generator = dynamicMethod.GetILGenerator();

            // Create the closed generic query handler type.
            Type queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));

            // Get the MethodInfo that represents the HandleAsync method.
            MethodInfo method = queryHandlerType.GetMethod("HandleAsync");

            // Push the service factory onto the evaluation stack. 
            generator.Emit(OpCodes.Ldarg_0);

            // Push the query handler type onto the evaluation stack.                      
            generator.Emit(OpCodes.Ldtoken, queryHandlerType);
            generator.Emit(OpCodes.Call, GetTypeFromHandleMethod);

            // Call the GetInstance method and push the query handler 
            // instance onto the evaluation stack.
            generator.Emit(OpCodes.Callvirt, GetInstanceMethod);

            // Since the GetInstance method returns an object, 
            // we need to cast it to the actual query handler type.
            generator.Emit(OpCodes.Castclass, queryHandlerType);

            // Push the query onto the evaluation stack.
            generator.Emit(OpCodes.Ldarg_1);

            // The query is passed in as an IQuery<TResult> instance 
            // and we need to cast it to the actual query type.
            generator.Emit(OpCodes.Castclass, queryType);

            // Call the Query method and push the Task<TResult>
            // onto the evaluation stack.
            generator.Emit(OpCodes.Callvirt, method);

            // Mark the end of the dynamic method.
            generator.Emit(OpCodes.Ret);

            var getQueryHandlerDelegate =
                (Func<IServiceFactory, IQuery<TResult>, Task<TResult>>)
                dynamicMethod.CreateDelegate(typeof(Func<IServiceFactory, IQuery<TResult>, Task<TResult>>));

            // Since the service factory will always be the same instance,
            // we can close around the service factory to provide a simpler delegate.
            return query => getQueryHandlerDelegate(factory, query);
        }

        private static class Cache<TResult>
        {
            private static ImmutableHashTree<Type, Func<IQuery<TResult>, Task<TResult>>> HashTree =
                ImmutableHashTree<Type, Func<IQuery<TResult>, Task<TResult>>>.Empty;

            public static Func<IQuery<TResult>, Task<TResult>> GetOrAdd(Type queryType, Func<Type, Func<IQuery<TResult>, Task<TResult>>> delegateFactory)
            {
                var func = HashTree.Search(queryType);
                if (func == null)
                {
                    func = delegateFactory(queryType);
                    Interlocked.Exchange(ref HashTree, HashTree.Add(queryType, func));
                }

                return func;
            }
        }
    }
}