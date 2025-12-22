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
