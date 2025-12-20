using Basket.API.Data;
using MediatR;

namespace Basket.API.Features.Basket.Commands.DeleteBasket;

/// <summary>
/// DELETEBASKET HANDLER - COMMAND (Yazma İşlemi)
/// 
/// NE ZAMAN ÇALIŞIR:
/// - Kullanıcı sepeti manuel olarak silmek istediğinde
/// - Admin panelinden sepet silindiğinde
/// - Frontend: DELETE /api/baskets/{userName} endpoint'i çağrıldığında
/// 
/// NE YAPAR:
/// - Sepeti PostgreSQL'den siler (kalıcı depolamadan)
/// - Sepeti Redis'ten siler (cache'den)
/// - Silme işleminin başarılı olup olmadığını döner
/// 
/// ÖNEMLİ: Bu handler VERİ DEĞİŞTİRİR (sepet silinir)
/// NOT: CheckoutBasketHandler içinde de sepet silinir, ama bu handler manuel silme için
/// </summary>
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
        // ADIM 1: Sepeti sil
        // Repository içinde:
        // - PostgreSQL'den silinir (kalıcı depolamadan)
        // - Redis'ten silinir (cache'den)
        var deleted = await _repository.DeleteBasket(request.UserName);
        
        if (deleted)
            _logger.LogInformation("Basket deleted for {UserName}", request.UserName);
        else
            _logger.LogWarning("Basket not found for {UserName}", request.UserName);

        return deleted; // Başarılı/başarısız durumunu döner
    }
}

