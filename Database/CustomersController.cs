namespace TransactionManagement.Database
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;
    using CQRS;
    using Customers;

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
            if (customers.Length > 0)
            {
                return Ok(customers);
            }

            return StatusCode(HttpStatusCode.NoContent);

        }
    }
}