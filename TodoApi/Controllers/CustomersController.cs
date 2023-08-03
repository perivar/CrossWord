using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using TodoApi.Models;

namespace TodoApi.Controllers;

public class CustomersController : ODataController
{
    private static readonly Random random = new();
    private static readonly List<Customer> customers = new(
        Enumerable.Range(1, 3).Select(idx => new Customer
        {
            Id = idx,
            Name = $"Customer {idx}",
            Orders = new List<Order>(
                Enumerable.Range(1, 2).Select(dx => new Order
                {
                    Id = (idx - 1) * 2 + dx,
                    Amount = random.Next(1, 9) * 10
                }))
        }));

    [EnableQuery]
    public ActionResult<IEnumerable<Customer>> Get()
    {
        return Ok(customers);
    }

    [EnableQuery]
    public ActionResult<Customer> Get([FromRoute] int key)
    {
        var item = customers.SingleOrDefault(d => d.Id.Equals(key));

        if (item == null)
        {
            return NotFound();
        }

        return Ok(item);
    }
}
