using MyApp.Domain.Entities;

namespace MyApp.Domain.Validation;

/// <summary>
/// Domain validation using the generated constants. The lengths are configured once, in the EF
/// configuration, and no number is repeated here.
/// </summary>
public static class CustomerValidator
{
    public static IReadOnlyList<string> Validate(Customer customer)
    {
        var errors = new List<string>();

        if (customer.Name.Length is 0 or > EntityLengths.Customer.NameLength)
        {
            errors.Add($"Name must be 1 to {EntityLengths.Customer.NameLength} characters.");
        }

        if (customer.Email.Length > EntityLengths.Customer.EmailLength)
        {
            errors.Add($"Email must be at most {EntityLengths.Customer.EmailLength} characters.");
        }

        if (customer.Notes?.Length > EntityLengths.Customer.NotesLength)
        {
            errors.Add($"Notes must be at most {EntityLengths.Customer.NotesLength} characters.");
        }

        return errors;
    }
}
