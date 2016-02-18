namespace TransactionManagement.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using CQRS;
    using Customers;
    using LightInject;
    using LightInject.xUnit2;
    using Microsoft.Owin.Testing;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class ControllerTests : ControllerTestBase<Startup>
    {
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

        [Theory, Scoped, InjectData]
        public async Task ShouldGetCustomersUsingInjectedClient(HttpClient client)
        {
            var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");
            Assert.Equal(11, response.Value.Length);
        }


        [Theory, Scoped, InjectData]
        public async Task ShouldGetCustomers(TestClient client)
        {
            var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");            

            Assert.Equal(11, response.Value.Length);
        }

        [Theory, Scoped, InjectData]
        public async Task ShouldReturnNoContent(TestClient client)
        {
            // Arrange
            var mock = client.Mock<IQueryExecutor>();
            mock.Setup(m => m.ExecuteAsync(It.IsAny<IQuery<Customer[]>>())).ReturnsAsync(new Customer[] {});
            
            // Act
            var response = await client.GetAsync<Customer[]>("api/customers?country=Germany");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.Message.StatusCode);
        }
                                
    }


    public abstract class ControllerTestBase<TStartup> where TStartup : Startup, new()
    {
        public static void Configure(IServiceContainer container)
        {
            var startup = new TStartup();
            container.Register(factory => TestServer.Create(builder => startup.Configuration(builder)), new PerScopeLifetime());
            container.Register(factory => CreateTestClient(startup, container));
            container.Register(CreateHttpClient);
        }

        private static TestClient CreateTestClient(TStartup startup, IServiceContainer container)
        {
            var testServer = container.GetInstance<TestServer>();
            var testClient = new TestClient(startup.Container, testServer.Handler);
            testClient.BaseAddress = testServer.BaseAddress;
            return testClient;
        }

        private static HttpClient CreateHttpClient(IServiceFactory container)
        {
            var testServer = container.GetInstance<TestServer>();
            var httpClient = new HttpClient(testServer.Handler);
            httpClient.BaseAddress = testServer.BaseAddress;
            return httpClient;
        }
    }

    



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

    public class Response<T>
    {
        public T Value { get; set; }

        public HttpResponseMessage Message { get; set ; }
    }

}