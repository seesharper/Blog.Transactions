namespace TransactionManagement.CQRS
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Security.Policy;
    using LightInject;

    public static class ContainerExtensions
    {
        public static void RegisterQueryHandlers(this IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterAssembly(typeof(IQueryHandler<,>),t => !t.Namespace.Contains("Database"));           
        }

        public static void RegisterCommandHandlers(this IServiceRegistry serviceRegistry)
        {
            serviceRegistry.RegisterAssembly(typeof(ICommandHandler<>), t => !t.Namespace.Contains("Database"));
        }

        public static void RegisterAssembly(this IServiceRegistry serviceRegistry, Type serviceType, Func<Type, bool> predicate,Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = serviceType.Assembly;
            }

            var types = assembly.GetTypes().Where(t => !t.IsAbstract && t.Implements(serviceType) && predicate(t) );
            foreach (var type in types)
            {
                if (serviceType.IsGenericType)
                {
                    var closedgenericServiceType =
                        type.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == serviceType);
                    serviceRegistry.Register(closedgenericServiceType, type);
                }
                else
                {
                    serviceRegistry.Register(serviceType, type);
                }
                
            }
        }

        public static bool Implements(this Type type, Type interfaceType)
        {
            if (interfaceType.IsGenericType)
            {
                return
                    type.GetInterfaces()
                        .Any(
                            i =>
                                i.IsGenericType &&
                                i.GetGenericTypeDefinition() == interfaceType.GetGenericTypeDefinition());
            }
            return type.GetInterfaces().Any(i => i == interfaceType);
        }


        public static void RegisterCommandHandler(this IServiceRegistry serviceRegistry)
        {
            var assembly = typeof(ContainerExtensions).Assembly;

            var queryHandlerTypes = assembly.GetTypes()
                .Where(
                    t =>
                        t.GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));
            foreach (var queryHandlerType in queryHandlerTypes)
            {
                var interfaceType =
                    queryHandlerType.GetInterfaces()
                        .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
                serviceRegistry.Register(interfaceType, queryHandlerType);
            }
        }


        public static void EnableCQRS(this IServiceRegistry serviceRegistry)
        {
            serviceRegistry.Register<IQueryExecutor>(factory => new QueryExecutor(factory));
        }

    }
}