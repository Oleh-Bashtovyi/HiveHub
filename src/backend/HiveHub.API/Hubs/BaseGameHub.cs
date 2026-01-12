using FluentResults;
using HiveHub.API.Dtos;
using HiveHub.Application.Extensions;
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

    protected async Task<ApiResponse<TResponse>> HandleCommand<TResponse>(IRequest<Result<TResponse>> command)
            where TResponse : class
    {
        var result = await _mediator.Send(command);
        return result.ToApiResponse();
    }

    protected async Task<ApiResponse> HandleCommand(IRequest<Result> command)
    {
        var result = await _mediator.Send(command);
        return result.ToApiResponse();
    }
}