# Transaction Management

In this example we will be looking into how to deal with connections and transactions in a Web application. We will also discuss how to implement automatic rollback for integration tests. Our testing framework will be **xUnit** and together with **LightInject** this will hopefully turn into a very smooth testing experience.

## Commands and Queries

There are many ways to manage the interaction with the database and in this application we are going to implement something that has come to be known as the Command-Query pattern. The basic idea here is that we have one interface (**IQueryHandler**) for everything that comes out of the database and another interface (**ICommandHandler**)for everything that goes into the database. The fact that we are dealing with the same set of interfaces for all interaction with the database means that we can very easily add features through the use of decorators. 

I am not going to cover everything with regards to command and queries here, but we will look into the interfaces we need to implement in order to shuffle data back and forth to the database.

### Queries

The following interface represents a class that can handle a query and return some kind of result.
```
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{    
    Task<TResult> HandleAsync(TQuery query);
}
```

You might wonder about the **IQuery&lt;TResult&gt;** generic contraint. This is just an interface to give type inference a helping hand as we move on to the next interface. 

The IQueryExecutor represents a class that can execute any query.
```
public interface IQueryExecutor
{     
    Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query);
}

```

This is where the **IQuery&lt;TResult&gt;** generic contraint comes into play. Without the contraint we would have to specify the type of result when calling the **ExecuteAsync** method.

```
var result = queryExecutor.ExecuteAsync<SomeResultType>(somequery);
```

With the query class "implementing" the IQuery&lt;TResult&gt; interface we can instead do this

```
var result = queryExecutor.ExecuteAsync(somequery);
```

### Commands

The following interface represents a class that can handle a command where the command can be any class.
```   
    public interface ICommandHandler<in TCommand>
    {   
        Task HandleAsync(TCommand command);
    }
```

Will be seeing examples of both queries and commands throughout this example.


## Why not an ORM?

The best answer I can give you is that I have already been there and felt the pain that eventually comes sneaking upon you. A little at first and then more and more as things gets more complicated. In fact, I have actually written an inhouse ORM back in the days and writing a Linq provider for instance, certainly represents the Mount Everest of programming. Not because Linq is so hard, but because you have to deal with all the mismatches between the relational model and the object model. ORM's tries to free you from understanding SQL and it is such a failed abstraction. Eventually at some point, you find yourself in a situation where you are trying to come up with a Linq expression that generates the SQL you've already written. Bottom line, bite the bullet and learn SQL.

## Customers

The first task is to create a query handler that can retrieve customers from the database based on their origin (country).

The SQL for ths look like this:

```SQL
SELECT 
	CustomerId,
	CompanyName
FROM 
	Customers
WHERE 
	Country = @Country;
```
 
With the SQL in place, we are ready to implement the query handler.

```
public class CustomersQueryHandler : IQueryHandler<CustomersQuery, Customer[]>
{
    private readonly IDbConnection dbConnection;

    public CustomersQueryHandler(IDbConnection dbConnection)
    {
        this.dbConnection = dbConnection;
    }

    public async Task<Customer[]> HandleAsync(CustomersQuery query)
    {
        var result = await dbConnection.QueryAsync<CustomerRow>(SQL.CustomersByCountry, query);
        return result.ToArray();
    }
}
```

> Note: Query and command handlers does not dictate that the data is stored in a relational database. It might just as well be stored in a file or another type of database such as a document database.

This query handler has just one dependency, the **IDbConnection** off which Dapper provides the **QueryAsync** method used to execute the query.
The query (**CustomersQuery**) looks like this

```
public class CustomersQuery : IQuery<Customer[]>
{
    public string Country { get; set; }
}
```

## Passing data through layers
 
Okay, so we have our application that consists of different layers. 

* Data Access Layer (query and command handlers)
* Business Layer (services using the data access layer)
* Public API (typically a REST based API using services from the business layer)

### Data Access Layer

This is where we actually interact with the underlying data store which for most applications even today means a relational database. These databases store relational data and is not very well suited for storing objects and we should treat the database accordingly. Relational databases does not store objects, they store rows of data. 
This is why we represent the result from **Dapper** as a set of **CustomerRow** instances. 

### Business Layer

This layer consists of the services that in turn will be using the data access layer. This is also the place to put any kind of business logic related to our services.  By mocking the data access layer we can test this functionality without hitting the database.


### API Layer

This is our public API which in this sample application is exposed as a RESTful API using Web Api.  Try to keep this layer as thin as possible. It should not deal with any kind of business logic, but it should for instance deal with making sure that we return the correct status codes according to REST best practices. 

So should each layer have its own representation of the same data? 

My answer is this: Be pragmatic about this. If the representation looks identical throughout the various layers, you might as well reuse the same class for different layers. Once you need to add JsonIgnore attributes to ensure that database-only properties does not get exposed in the Web API, you might consider another representation.  It is also likely that the representations for reading and writing will differ so there is a lot to consider her. There is also a performance penalty involved here since we need to constantly map the "same" data between layers. 

There might also be situations where the service layer just acts as a pass-t through layer and it that case it might be just fine to use the query/command handlers directly in our controllers. 

## Composition Root

The composition root is where we register services into the container.
```
serviceRegistry.Register<IQueryExecutor, QueryExecutor>(new PerContainerLifetime());
            serviceRegistry.RegisterQueryHandlers();
            
            // We register the connection that it is disposed when the scope ends.
            // The scope here is per web request.
            serviceRegistry.Register(factory => CreateConnection(), new PerScopeLifetime());
``` 

We register the **IDbConnection** with the **PerScopeLifetime** which means that we get the same connection within a scope. The scope is usually per web request, but it can also be per test method as we will see in a minute.  
This is actually a nice aspect of the scoping mechanism in LightInject. We tell a service to be per scope without providing any details about how the scope is started or ended.


## Testing query handlers

Before we dive into how to handle transactions, we are going to look at how to test our new query handler. **LightInject** provides an extension(**LightInject.xUnit**) that makes it possible to inject services into test methods.

```
[Theory, Scoped, InjectData]
public async Task ShouldGetCustomersFromGermany(IQueryExecutor queryExecutor)
{
    var query = new CustomersQuery {Country = "Germany"};
    var result = await queryExecutor.ExecuteAsync(query);
    Assert.Equal(11, result.Length);
}
```

The **Scoped** attribute tells **LightInject** to wrap a scope around this test method. When the test method ends, the scope will end as well and that will in turn cause the **IDbConnection** to be disposed since that service is registered with the **PerScopeLifetime**. 

The **InjectData** attribute simply tells **LightInject** to inject method arguments and can be thought off as a more sophisticated version of the **InlineData** attribute usually seen in **xUnit** theory based tests.

**LightInject.xUnit** creates a container instance behind the scenes and uses that instance to inject services into the test methods. The default behavior in **LightInject** is to look for composition roots in the same assembly as the requested service. This is part of the fallback mechanism and means that we don't really need to explicitly configure the container in the test class although it is possible to do so by declaring the following static method.

```
public static void Configure(IServiceContainer container)
{
    container.RegisterFrom<CompositionRoot>();
}
```

We can also use the **InjectData** attribute so specify inline data in addition to the service being injected.

```
[Theory, Scoped]
[InjectData("France", 11)]
[InjectData("Germany", 11)]
[InjectData("Norway", 1)]
public async Task ShouldGetCustomers(IQueryExecutor queryExecutor, string country, int expectedCount)
{
    var result = await queryExecutor.ExecuteAsync(new CustomersQuery() { Country = country });
    Assert.Equal(expectedCount, result.Length);
}
```

## Transactions

We are going to build upon the standard **IDbConnection** and **IDbTransaction** interfaces and provide a way to transparently apply transactions to command handlers. 

**Requirements:** 

* Transactions should as short lived as possible.
* Only one transaction per scope (web request)
* Support sequential and nested execution of command handlers within the same transaction. 
* Allow integration tests to roll back changes when the test ends.

This might seem like a tall order and the solution might look a bit controversial, but bare with me on this one and you will see that the implementation is actually quite simple. 

### Inserting data

Before we start to think about transactions, we need something that writes to the database. A simple insert should do the trick.

```
public class AddCustomerCommandHandler : ICommandHandler<AddCustomerCommand>
{
    private readonly IDbConnection dbConnection;

    public AddCustomerCommandHandler(IDbConnection dbConnection)
    {
        this.dbConnection = dbConnection;
    }

    public async Task HandleAsync(AddCustomerCommand command)
    {
        await dbConnection.ExecuteAsync(SQL.InsertCustomer, command);
    }
}
```

The command handler takes care of executing a simple insert into the **Customers** table using the **ExecuteAsync** extension method provided by **Dapper**. The  **AddCustomerCommand**  is just a simple POCO class that contains the data to be inserted.

```
public class AddCustomerCommand
{
    public string CustomerId { get; set; }

    public string CompanyName { get; set; }
}
```

The SQL looks like this:

```SQL
INSERT INTO Customers (CustomerId, CompanyName)
VALUES (@CustomerId, @CompanyName)
```

Wrapping command handlers inside a transaction is just a matter of applying a simple decorator.

```
public class TransactionalCommandHandler<TCommand> : ICommandHandler<TCommand>
{
    private readonly IDbConnection dbConnection;
    private readonly ICommandHandler<TCommand> commandHandler;

    public TransactionalCommandHandler(IDbConnection dbConnection, ICommandHandler<TCommand> commandHandler)
    {
        this.dbConnection = dbConnection;
        this.commandHandler = commandHandler;
    }

    public async Task HandleAsync(TCommand command)
    {
        using (var transaction = dbConnection.BeginTransaction())
        {
            await commandHandler.HandleAsync(command);
            transaction.Commit();
        }                
    }
}
```

With a single line of code in the composition root , we can now apply this decorator to all command handlers.

```
serviceRegistry.Decorate(typeof(ICommandHandler<>), typeof(TransactionalCommandHandler<>));
```

### Multiple command handlers

Within a single scope (web request), we might have to execute more than one command handler, either sequentially or nested within each other. Since we now have an all-purpose decorator (**TransactionalCommandHandler**) that starts a new transaction before each underlying command handler, we need to make sure that only one transaction exists within the scope (web request).

We do this by implementing yet another decorator and this time a decorator for the **IDbConnection** interface.

```
public class ConnectionDecorator : IDbConnection
{
    private readonly IDbConnection dbConnection;
    private readonly Lazy<TransactionDecorator> dbTransaction;        

    public ConnectionDecorator(IDbConnection dbConnection)
    {
        this.dbConnection = dbConnection;
        dbTransaction =
            new Lazy<TransactionDecorator>(() => new TransactionDecorator(this, dbConnection.BeginTransaction()));
    }

    public void Dispose()
    {
        if (dbTransaction.IsValueCreated)
        {
            dbTransaction.Value.EndTransaction();
        }
        dbConnection.Dispose();
    }

    public IDbTransaction BeginTransaction()
    {
        dbTransaction.Value.IncrementTransactionCount();
        return dbTransaction.Value;           
    }

    public IDbTransaction BeginTransaction(IsolationLevel il)
    {
        return BeginTransaction();
    }

    public void Close()
    {
        dbConnection.Close();
    }

    public void ChangeDatabase(string databaseName)
    {
        dbConnection.ChangeDatabase(databaseName);
    }

    public IDbCommand CreateCommand()
    {
        return dbConnection.CreateCommand();
    }

    public void Open()
    {
        dbConnection.Open();
    }

    public string ConnectionString
    {
        get { return dbConnection.ConnectionString; }
        set { dbConnection.ConnectionString = value; }
    }

    public int ConnectionTimeout
    {
        get { return dbConnection.ConnectionTimeout; }
    }

    public string Database
    {
        get { return dbConnection.Database; }
    }

    public ConnectionState State
    {
        get { return dbConnection.State; }
    }
}
```

Most of the methods and properties here just call into the underlying **IDbConnection**, except for the **Dispose** and **BeginTransaction** methods that we will explain in a minute. 

But first the code for the **TransactionDecorator**

```
public class TransactionDecorator : IDbTransaction
{
    private readonly IDbTransaction dbTransaction;
    private int transactionCount;
    private int commitCount;
    public TransactionDecorator(IDbConnection dbConnection, IDbTransaction dbTransaction)
    {
        Connection = dbConnection;
        this.dbTransaction = dbTransaction;
    }

    public void IncrementTransactionCount()
    {
        transactionCount++;
    }

    public void EndTransaction()
    {
        if (commitCount == transactionCount)
        {
            dbTransaction.Commit();
        }
        else
        {
            dbTransaction.Rollback();
        }
        dbTransaction.Dispose();
    }

    public void Dispose() { }
    
    public virtual void Commit()
    {
        commitCount++;
    }

    public void Rollback() { }
    
    public IDbConnection Connection { get; }

    public IsolationLevel IsolationLevel => dbTransaction.IsolationLevel;
}
```

Okay, it is time to what is going on here. Hang on!

When the **BeginTransaction** method is executed we create a new **IDbTransaction**  and wraps that transaction inside a **TransactionDecorator**. This transaction is provided through a **Lazy&lt;T&gt;** that makes sure that we only create a single transaction no matter how many times the **BeginTransaction** method is called. 
We also increment the "**transactionCount**"  which basically reflects the number of calls to the **BeginTransaction** method.

The "**transactionCount**" is then used inside the **EndTransaction** method to decide if we should perform a commit or a rollback.  The rule here is simple. In order for the transaction to be committed, we need the **commitCount** to be equal to the **transactionCount**. If they are not equal it means that a BeginTransaction was executed without a commit. In that case we do a rollback.

The **EndTransaction** method is called from the **Dispose** method inside the the **ConnectionDecorator** that first checks if we actually have a transaction at all. If so, we execute the **EndTransaction** method and finally disposes the underlying connection.

The connection is as mentioned before disposed when the scope (web request) ends because it is registered with the **PerScopeLifetime**.

Plugging all this goodness into our code is a simple as 

```
serviceRegistry.Decorate<IDbConnection, ConnectionDecorator>();
```

We can now execute nested command handlers as well as command handlers sequentially and still have them operate within the same transaction that either gets committed or rolled back when the connection is disposed.


### Automatic rollback

Integration tests that writes to the database should perform a rollback when the test ends. This is now just a matter of adding another decorator that simply executes a rollback rather than a commit.

```
public class RollbackCommandHandler<TCommand> : ICommandHandler<TCommand>
{
    private readonly IDbConnection dbConnection;
    private readonly ICommandHandler<TCommand> commandHandler;

    public RollbackCommandHandler(IDbConnection dbConnection, ICommandHandler<TCommand> commandHandler)
    {
        this.dbConnection = dbConnection;
        this.commandHandler = commandHandler;
    }

    public async Task HandleAsync(TCommand command)
    {
        using (var transaction = dbConnection.BeginTransaction())
        {
            await commandHandler.HandleAsync(command);
            transaction.Rollback();
        }
    }
}
```

This decorator only lives in the test project and we can apply the decorator by implementing a static **Configure** method in the test class.

```
public static void Configure(IServiceContainer container)
{
    container.RegisterFrom<CompositionRoot>();
    container.Decorate(typeof(ICommandHandler<>), typeof(RollbackCommandHandler<>));
}
```

We can now finally write a test that verifies that a new customer has been written to the database.

```
[Theory, Scoped, InjectData]
public async Task ShouldAddCustomer(ICommandExecutor commandExecutor, IQueryExecutor queryExecutor)
{
    await commandExecutor.ExecuteAsync(new AddCustomerCommand {CustomerId = "AAPL", CompanyName = "Apple Inc"});
    var newCustomer = await queryExecutor.ExecuteAsync(new CustomerQuery {CustomerId = "AAPL"});
    Assert.Equal("Apple Inc", newCustomer.CompanyName);
}
```

Since the transaction is not ended until the test ends, we can still query the database for the newly inserted customer and verify that is was inserted.

## Testing Controllers 

Testing the public API in a Web API application means testing the controllers and by using the **Microsoft.Owin.Testing** package we can create an in-memory server that lets us test our Owin based web application end to end. 

Lets just quickly take a look at the controller we are going to test.

```
public class CustomersController : ApiController
{
    private readonly IQueryExecutor queryExecutor;        

    public CustomersController(IQueryExecutor queryExecutor)
    {
        this.queryExecutor = queryExecutor;            
    }

    public async Task<IHttpActionResult> Get(string country)
    {            
        var customers = await queryExecutor.ExecuteAsync(new CustomersQuery {Country = country});            
        return Ok(customers);
    }
}
```

A typical test for this controller would look like this.

``` 
[Fact]
public async Task ShouldGetCustomersUsingTestServer()
{
    using (var server = TestServer.Create<Startup>())
    {
        HttpClient client = server.HttpClient;
        HttpResponseMessage response = await client.GetAsync("api/customers?country=Germany");
        string content = await response.Content.ReadAsStringAsync();
        Customer[] customers = JsonConvert.DeserializeObject<Customer[]>(content);
        Assert.Equal(11, customers.Length);
    }
}
```

Let's create an extension method to help us shorten this code a bit.

```
public static class HttpClientExtensions
{
    public static async Task<Response<TResult>> GetAsync<TResult>(this HttpClient client, string requestUri)
    {
        
        var responseMessage = await client.GetAsync(requestUri).ConfigureAwait(false);
        Response<TResult> response = new Response<TResult>() {Message = responseMessage};
        if (responseMessage.IsSuccessStatusCode)
        {
            var content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Value = JsonConvert.DeserializeObject<TResult>(content);
        }
        return response;
    }
}
```

This extension method returns the response as a Response&lt;T&gt; that contains the **HttpResponseMessage** and the typed result object. This means that we can do asserts on the actual result in addition to data related to the request such as the **HttpStatusCode**.

```
[Fact]
public async Task ShouldGetCustomersUsingExtensionMethod()
{
    using (var server = TestServer.Create<Startup>())
    {
        HttpClient client = server.HttpClient;
        var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");
        Assert.Equal(11, response.Value.Length);
    }
}
```

The nice thing about the **TestServer** is that it allows us to pass the **Startup** class to be used for the test. This means that we can pass a startup class that might be specific to the test.  The startup class for this application looks like this.

```
public class Startup
{
    public Startup()
    {
        Container = new ServiceContainer();            
    }

    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        Configure(Container);
        ConfigureMediaFormatter(config);
        ConfigureHttpRoutes(config);
        Container.RegisterApiControllers();
        Container.EnableWebApi(config);
        app.UseWebApi(config);
    }

    private static void ConfigureMediaFormatter(HttpConfiguration configuration)
    {
        configuration.Formatters.Clear();
        configuration.Formatters.Add(new JsonMediaTypeFormatter());
    }

    private static void ConfigureHttpRoutes(HttpConfiguration config)
    {
        config.Routes.MapHttpRoute(
            name: "API Default",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional });
    }

    public virtual void Configure(IServiceContainer serviceContainer)
    {
        serviceContainer.RegisterFrom<CompositionRoot>();        
    }

    public IServiceContainer Container { get; }         
}
```

The thing to notice here is that we have a virtual **Configure** method that makes it possible to inherit from this class in a test project and override the way we configure the container.  We also expose the container used by Web Api so that we can get access to it in an inherited startup class.

The next class is a base class for testing controllers that makes it possible to specify the startup class type as a generic argument to the class itself.

```
public abstract class ControllerTestBase<TStartup> where TStartup : Startup, new()
{
    public static void Configure(IServiceContainer container)
    {
        var startup = new TStartup();
        container.Register(factory => TestServer.Create(builder => startup.Configuration(builder)), new PerScopeLifetime());
        container.Register(CreateHttpClient);
    }

    private static HttpClient CreateHttpClient(IServiceFactory container)
    {
        var testServer = container.GetInstance<TestServer>();
        var httpClient = new HttpClient(testServer.Handler);
        httpClient.BaseAddress = testServer.BaseAddress;
        return httpClient;
    }
}
```

This base class now makes it possible to specify the startup class and it also allows us to inject the **HttpClient** instance. 

```
public class ControllerTests : ControllerTestBase<Startup>
{
    [Theory, Scoped, InjectData]
    public async Task ShouldGetCustomersUsingInjectedClient(HttpClient client)
    {
        var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");
        Assert.Equal(11, response.Value.Length);
    }
}
```

Being good REST citizens, we should also make sure that we return the correct status code along with the content. Say now that we want to test that the service returns 204-NoContent if no customers are found for the given country. 
We could do this by making sure that we have the appropriate  test data in the database or we could mock the **IQueryExecutor** and have it return an empty list without even touching the database.

By extending the **HttpClient** we can really simplyfy the way to mock services used in the test.

```
public class TestClient : HttpClient
{
    private readonly IServiceRegistry serviceRegistry;

    public TestClient(IServiceRegistry serviceRegistry, HttpMessageHandler handler) : base(handler)
    {
        this.serviceRegistry = serviceRegistry;
    }

    public Mock<TService> Mock<TService>() where TService:class 
    {
        var mock = new Mock<TService>();
        
        serviceRegistry.Override(registration => registration.ServiceType == typeof(TService),
            (factory, registration) => CreateMockRegistration(mock));

        return mock;
    }

    private static ServiceRegistration CreateMockRegistration<TService>(Mock<TService> mock) where TService:class
    {
        return new ServiceRegistration() {ServiceType = typeof(TService), Value = mock.Object };
    }
}
```

This class basically replaces the existing **IQueryExecutor** registration with a mock instance and makes it possible to mock services very easily.

```
[Theory, Scoped, InjectData]
public async Task ShouldReturnNoContent(TestClient client)
{
    var mock = client.Mock<IQueryExecutor>();
    mock.Setup(m => m.ExecuteAsync(It.IsAny<IQuery<Customer[]>>())).ReturnsAsync(new Customer[] {});
                
    var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");
    
    Assert.Equal(HttpStatusCode.NoContent, response.Message.StatusCode);
}
```