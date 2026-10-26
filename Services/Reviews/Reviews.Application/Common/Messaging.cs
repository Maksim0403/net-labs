using MediatR;

namespace Reviews.Application.Common;

/// <summary>Базовий маркерний інтерфейс команди без значення, що повертається.</summary>
public interface ICommand : IRequest { }

/// <summary>Базовий інтерфейс команди зі значенням, що повертається.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>Базовий інтерфейс запиту зі значенням, що повертається.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

