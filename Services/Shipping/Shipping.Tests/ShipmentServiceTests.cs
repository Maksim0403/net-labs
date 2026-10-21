using System.Data;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Bll.Mapping;
using Shipping.Bll.Services;
using Shipping.Dal.Abstractions;
using Shipping.Domain.Models;
using Xunit;
using BllShipmentService = Shipping.Bll.Services.ShipmentService;

namespace Shipping.Tests;

public sealed class ShipmentServiceTests
{
    [Fact]
    public async Task ConfirmAsync_CommitsChangesAcrossShipmentAndCustomerRepositories()
    {
        var (unitOfWork, shipments, customers) = CreateUnitOfWork();
        var shipment = CreateShipment();
        shipments
            .Setup(repository => repository.GetWithItemsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);

        var service = CreateService(unitOfWork);

        await service.ConfirmAsync(42);

        shipments.Verify(
            repository => repository.ConfirmAsync(42, "shipping-api", It.IsAny<CancellationToken>()),
            Times.Once);
        customers.Verify(
            repository => repository.TouchAsync(7, "shipping-api", It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(
            work => work.BeginTransactionAsync(IsolationLevel.ReadCommitted, It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(
            work => work.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        unitOfWork.Verify(
            work => work.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_RollsBackWhenSecondRepositoryFails()
    {
        var (unitOfWork, shipments, customers) = CreateUnitOfWork();
        var shipment = CreateShipment();
        shipments
            .Setup(repository => repository.GetWithItemsAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shipment);
        customers
            .Setup(repository => repository.TouchAsync(7, "shipping-api", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated second repository failure"));

        var service = CreateService(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmAsync(42));

        unitOfWork.Verify(
            work => work.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        unitOfWork.Verify(
            work => work.RollbackAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static BllShipmentService CreateService(Mock<IUnitOfWork> unitOfWork)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ShippingProfile>(), NullLoggerFactory.Instance).CreateMapper();
        return new BllShipmentService(unitOfWork.Object, mapper);
    }

    private static (Mock<IUnitOfWork> UnitOfWork, Mock<IShipmentRepository> Shipments,
        Mock<ICustomerRepository> Customers) CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var shipments = new Mock<IShipmentRepository>();
        var customers = new Mock<ICustomerRepository>();
        unitOfWork.SetupGet(work => work.Shipments).Returns(shipments.Object);
        unitOfWork.SetupGet(work => work.Customers).Returns(customers.Object);
        unitOfWork.SetupGet(work => work.ServiceTypes).Returns(new Mock<IServiceTypeRepository>().Object);
        return (unitOfWork, shipments, customers);
    }

    private static Shipment CreateShipment() => new()
    {
        Id = 42,
        CustomerId = 7,
        Details = new ShipmentDetails { RecipientName = "Test recipient" },
        Items = [new Shipping.Domain.Models.ShipmentService { ServiceTypeId = 1, Quantity = 1 }]
    };
}
