using FluentResults;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace HiveHub.API.Hubs;

public abstract class BaseGameHub<TClient> : Hub<TClient> where TClient : class
{
    protected readonly IMediator _mediator;

    protected BaseGameHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected async Task<object> HandleCommand<TResponse>(IRequest<Result<TResponse>> command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailed)
        {
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Unknown error" };
        }

        return new { success = true, data = result.Value };
    }

    protected async Task<object> HandleCommand(IRequest<Result> command)
    {
        var result = await _mediator.Send(command);

        if (result.IsFailed)
        {
            return new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Unknown error" };
        }

        return new { success = true };
    }
}