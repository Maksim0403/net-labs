using System.ComponentModel.DataAnnotations;

namespace Shipping.Bll.Dto;

public record CustomerDto(long Id, string FullName, string Phone, string? Email);

public class CreateCustomerDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(150)]
    public string? Email { get; set; }
}
