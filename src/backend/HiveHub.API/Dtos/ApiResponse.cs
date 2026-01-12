namespace HiveHub.API.Dtos;

public enum ResponseStatus
{
    Success,
    ActionFailed,
    Forbidden,
    ValidationFailed,
    NotFound,
    UnknownError
}

public record ApiResponse
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public ResponseStatus Status { get; init; }

    public static ApiResponse SuccessResponse() => new()
    {
        Success = true,
        Status = ResponseStatus.Success,
        Error = null
    };

    public static ApiResponse FailureResponse(ResponseStatus status, string message) => new()
    {
        Success = false,
        Status = status,
        Error = message
    };
}

public record ApiResponse<T> : ApiResponse where T : class
{
    public T? Data { get; init; }

    public static ApiResponse<T> SuccessResponse(T data) => new()
    {
        Success = true,
        Status = ResponseStatus.Success,
        Data = data,
        Error = null
    };

    public new static ApiResponse<T> FailureResponse(ResponseStatus status, string message) => new()
    {
        Success = false,
        Status = status,
        Data = null,
        Error = message
    };
}