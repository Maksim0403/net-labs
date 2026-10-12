using AutoMapper;
using Shipping.Bll.Mapping;
using Xunit;

namespace Shipping.Tests;

public sealed class ShippingMappingTests
{
    [Fact]
    public void ShippingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ShippingProfile>());

        configuration.AssertConfigurationIsValid();
    }
}
