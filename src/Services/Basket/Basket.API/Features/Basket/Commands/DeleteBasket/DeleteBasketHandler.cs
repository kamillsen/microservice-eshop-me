using Basket.API.Data;
using MediatR;

namespace Basket.API.Features.Basket.Commands.DeleteBasket;

public class DeleteBasketHandler : IRequestHandler<DeleteBasketCommand, bool>
{
    private readonly IBasketRepository _repository;
    private readonly ILogger<DeleteBasketHandler> _logger;

    public DeleteBasketHandler(
        IBasketRepository repository,
        ILogger<DeleteBasketHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteBasketCommand request, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteBasket(request.UserName);
        
        if (deleted)
            _logger.LogInformation("Basket deleted for {UserName}", request.UserName);
        else
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);

        return deleted;
    }
}

