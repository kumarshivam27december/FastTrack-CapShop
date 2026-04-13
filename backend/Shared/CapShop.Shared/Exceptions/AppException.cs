namespace CapShop.Shared.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}

public sealed class ValidationException : AppException
{
    public ValidationException(string message)
        : base(message, 400)
    {
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}