namespace TransactionManagement.CQRS
{
    using System.Threading.Tasks;
    using LightInject;

    /// <summary>
    /// A <see cref="ICommandExecutor"/> that is capable
    /// of executing commands.
    /// </summary>
    public class CommandExecutor : ICommandExecutor
    {
        private readonly IServiceFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecutor"/> class.
        /// </summary>
        /// <param name="factory">The <see cref="IServiceFactory"/> that is used to 
        /// resolve the <see cref="ICommandHandler{TCommand}"/> to be executed.</param>
        public CommandExecutor(IServiceFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Executes the given <paramref name="command"/>.
        /// </summary>
        /// <typeparam name="TCommand">The type of command to be executed.</typeparam>
        /// <param name="command">The command to be executed.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ExecuteAsync<TCommand>(TCommand command)
        {
            await factory.GetInstance<ICommandHandler<TCommand>>().HandleAsync(command);
        }
    } 
}