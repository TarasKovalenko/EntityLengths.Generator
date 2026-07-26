using MyApp.Domain;
using MyApp.Domain.Entities;
using MyApp.Domain.Validation;

// The constants come from MyApp.Domain, even though the lengths are configured by the EF
// configurations in MyApp.Infrastructure, which Domain does not reference.
Console.WriteLine($"Customer.Name      max {EntityLengths.Customer.NameLength}");
Console.WriteLine($"Customer.Email     max {EntityLengths.Customer.EmailLength}");
Console.WriteLine($"Customer.Notes     max {EntityLengths.Customer.NotesLength}");
Console.WriteLine($"Order.Reference    max {EntityLengths.Order.ReferenceLength}");
Console.WriteLine($"Order.ShippingAddr max {EntityLengths.Order.ShippingAddressLength}");

var customer = new Customer
{
    Id = Guid.NewGuid(),
    Name = new string('x', EntityLengths.Customer.NameLength + 1),
    Email = "customer@example.com",
};

foreach (var error in CustomerValidator.Validate(customer))
{
    Console.WriteLine($"invalid: {error}");
}
