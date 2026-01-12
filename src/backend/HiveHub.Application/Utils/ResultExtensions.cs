using FluentResults;
using HiveHub.Application.Errors;

namespace HiveHub.Application.Utils;

public static class Results
{
    public static Result NotFound(string message)
    {
        return Result.Fail(new NotFound(message));
    }

    public static Result<T> NotFound<T>(string message)
    {
        return Result.Fail<T>(new NotFound(message));
    }

    public static Result ActionFailed(string message)
    {
        return Result.Fail(new ActionFailedError(message));
    }

    public static Result<T> ActionFailed<T>(string message)
    {
        return Result.Fail<T>(new ActionFailedError(message));
    }

    public static Result Forbidden(string message)
    {
        return Result.Fail(new ActionFailedError(message));
    }

    public static Result<T> Forbidden<T>(string message)
    {
        return Result.Fail<T>(new ActionFailedError(message));
    }

    public static Result ValidationFailed(string message)
    {
        return Result.Fail(new ValidationFailedError(message));
    }

    public static Result<T> ValidationFailed<T>(string message)
    {
        return Result.Fail<T>(new ValidationFailedError(message));
    }
}
