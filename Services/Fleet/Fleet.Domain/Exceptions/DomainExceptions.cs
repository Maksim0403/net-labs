namespace Fleet.Domain.Exceptions;

public class DomainException(string message) : Exception(message);
public class EntityNotFoundException(string message) : Exception(message);
public class ValidationException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
