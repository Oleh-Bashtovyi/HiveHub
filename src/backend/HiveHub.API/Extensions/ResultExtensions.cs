using FluentResults;
using HiveHub.API.Dtos;
using HiveHub.Application.Errors;

namespace HiveHub.Application.Extensions;

public static class ResultExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Result<T> result) where T : class
    {
        if (result.IsSuccess)
        {
            return ApiResponse<T>.SuccessResponse(result.Value);
        }

        var (status, message) = MapError(result.Errors.FirstOrDefault());
        return ApiResponse<T>.FailureResponse(status, message);
    }

    public static ApiResponse ToApiResponse(this Result result)
    {
        if (result.IsSuccess)
        {
            return ApiResponse.SuccessResponse();
        }

        var (status, message) = MapError(result.Errors.FirstOrDefault());
        return ApiResponse.FailureResponse(status, message);
    }

    private static (ResponseStatus Status, string Message) MapError(IError? error)
    {
        if (error == null)
            return (ResponseStatus.UnknownError, "Unknown error");

        var status = error switch
        {
            NotFound => ResponseStatus.NotFound,
            Forbidden => ResponseStatus.Forbidden,
            ActionFailedError => ResponseStatus.ActionFailed,
            ValidationFailedError => ResponseStatus.ValidationFailed,
            _ => ResponseStatus.UnknownError
        };

        return (status, error.Message);
    }
}