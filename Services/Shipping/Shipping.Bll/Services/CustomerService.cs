using AutoMapper;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Dal.Abstractions;
using Shipping.Domain.Exceptions;
using Shipping.Domain.Models;

namespace Shipping.Bll.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(id, ct);
        return customer is null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _uow.Customers.GetAllAsync(ct);
        return customers.Select(_mapper.Map<CustomerDto>).ToList();
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default)
    {
        var existing = await _uow.Customers.GetByPhoneAsync(input.Phone, ct);
        if (existing is not null)
            throw new BusinessConflictException($"Клієнт із телефоном {input.Phone} вже існує");

        var customer = new Customer { FullName = input.FullName, Phone = input.Phone, Email = input.Email };
        var id = await _uow.Customers.AddAsync(customer, ct);
        customer.Id = id;
        return _mapper.Map<CustomerDto>(customer);
    }
}
