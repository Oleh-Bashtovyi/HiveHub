using FluentResults;

namespace HiveHub.Application.Errors;

public class ActionFailedError : Error
{
    public ActionFailedError(string message) : base(message)
    {
    }
}

public class NotFound : Error
{
    public NotFound(string message) : base(message)
    {
    }
}

public class Forbidden : Error
{
    public Forbidden(string message) : base(message)
    {
    }
}

public class ValidationFailedError : Error
{
    public ValidationFailedError(string message) : base(message)
    {
    }
}

