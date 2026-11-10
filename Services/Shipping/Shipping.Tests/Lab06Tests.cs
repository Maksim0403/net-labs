using System.Diagnostics.Metrics;
using System.Text.Json;
using Fleet.Api.Grpc;
using Fleet.Bll.Caching;
using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using Fleet.Domain.Exceptions;
using Fleet.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shipping.Api.Grpc;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Grpc;
using Xunit;

namespace Shipping.Tests;

public class Lab06Tests
{
    private class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options.Name);
        public void Dispose() { }
    }

    [Fact]
    public async Task TwoLevelCache_L1Hit_ReturnsFromMemoryWithoutCallingRedisOrFactory()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var mockRedis = new Mock<IDistributedCache>();
        var metrics = new CacheMetrics(new DummyMeterFactory());
        var cache = new TwoLevelCache(memoryCache, mockRedis.Object, metrics, NullLogger<TwoLevelCache>.Instance);

        var key = "test:key:1";
        memoryCache.Set(key, "cached_value", new MemoryCacheEntryOptions { Size = 1 });

        var factoryCalled = false;

        // Act
        var result = await cache.GetOrCreateAsync(
            key,
            () =>
            {
                factoryCalled = true;
                return Task.FromResult<string?>("db_value");
            });

        // Assert
        Assert.Equal("cached_value", result);
        Assert.False(factoryCalled);
        mockRedis.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TwoLevelCache_L1Miss_L2Hit_PopulatesL1AndReturns()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var mockRedis = new Mock<IDistributedCache>();
        var metrics = new CacheMetrics(new DummyMeterFactory());
        var cache = new TwoLevelCache(memoryCache, mockRedis.Object, metrics, NullLogger<TwoLevelCache>.Instance);

        var key = "test:key:2";
        var expectedDto = new CourierDto { Id = 2, FullName = "John Doe", Phone = "+380501112233" };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(expectedDto);

        mockRedis.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonBytes);

        var factoryCalled = false;

        // Act
        var result = await cache.GetOrCreateAsync(
            key,
            () =>
            {
                factoryCalled = true;
                return Task.FromResult<CourierDto?>(null);
            });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal(expectedDto.FullName, result.FullName);
        Assert.False(factoryCalled);

        // Перевіряємо що L1 заповнено
        Assert.True(memoryCache.TryGetValue(key, out CourierDto? inMemory));
        Assert.Equal(expectedDto.Id, inMemory!.Id);
    }

    [Fact]
    public async Task TwoLevelCache_L1Miss_L2Miss_CallsFactoryAndPopulatesBothLevels()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var mockRedis = new Mock<IDistributedCache>();
        var metrics = new CacheMetrics(new DummyMeterFactory());
        var cache = new TwoLevelCache(memoryCache, mockRedis.Object, metrics, NullLogger<TwoLevelCache>.Instance);

        var key = "test:key:3";
        var expectedDto = new CourierDto { Id = 3, FullName = "Alice", Phone = "+380502223344" };

        mockRedis.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await cache.GetOrCreateAsync(
            key,
            () => Task.FromResult<CourierDto?>(expectedDto));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);

        // Перевіряємо що L1 і L2 заповнено
        Assert.True(memoryCache.TryGetValue(key, out CourierDto? inMemory));
        Assert.Equal(expectedDto.Id, inMemory!.Id);

        mockRedis.Verify(r => r.SetAsync(
            key,
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TwoLevelCache_RemoveAsync_RemovesFromMemoryAndRedis()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var mockRedis = new Mock<IDistributedCache>();
        var metrics = new CacheMetrics(new DummyMeterFactory());
        var cache = new TwoLevelCache(memoryCache, mockRedis.Object, metrics, NullLogger<TwoLevelCache>.Instance);

        var key = "test:key:remove";
        memoryCache.Set(key, "data", new MemoryCacheEntryOptions { Size = 1 });

        // Act
        await cache.RemoveAsync(key, "TestInvalidation");

        // Assert
        Assert.False(memoryCache.TryGetValue(key, out _));
        mockRedis.Verify(r => r.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TwoLevelCache_WhenRedisFails_FallsBackToDatabase()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10_000 });
        var mockRedis = new Mock<IDistributedCache>();
        var metrics = new CacheMetrics(new DummyMeterFactory());
        var cache = new TwoLevelCache(memoryCache, mockRedis.Object, metrics, NullLogger<TwoLevelCache>.Instance);

        var key = "test:key:fallback";
        mockRedis.Setup(r => r.GetAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Redis connection refused"));

        // Act
        var result = await cache.GetOrCreateAsync(
            key,
            () => Task.FromResult<string?>("fallback_db_value"));

        // Assert
        Assert.Equal("fallback_db_value", result);
        Assert.True(memoryCache.TryGetValue(key, out string? inMemory));
        Assert.Equal("fallback_db_value", inMemory);
    }

    [Fact]
    public async Task ShippingGrpcService_InvalidId_ThrowsInvalidArgument()
    {
        var mockShipmentService = new Mock<IShipmentService>();
        var service = new ShippingGrpcService(mockShipmentService.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetShipment(new GetShipmentRequest { Id = -1 }, null!));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task ShippingGrpcService_NotFound_ThrowsNotFound()
    {
        var mockShipmentService = new Mock<IShipmentService>();
        mockShipmentService.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShipmentDto?)null);

        var service = new ShippingGrpcService(mockShipmentService.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetShipment(new GetShipmentRequest { Id = 999 }, null!));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task FleetGrpcService_InvalidId_ThrowsInvalidArgument()
    {
        var mockCourierService = new Mock<ICourierService>();
        var service = new FleetGrpcService(mockCourierService.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetCourier(new GetCourierRequest { Id = 0 }, null!));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task FleetGrpcService_NotFound_ThrowsNotFound()
    {
        var mockCourierService = new Mock<ICourierService>();
        mockCourierService.Setup(s => s.GetByIdAsync(888, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityNotFoundException("Courier 888 not found"));

        var service = new FleetGrpcService(mockCourierService.Object);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetCourier(new GetCourierRequest { Id = 888 }, null!));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
