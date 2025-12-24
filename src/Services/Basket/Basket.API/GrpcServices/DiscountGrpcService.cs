using Discount.Grpc.Protos;
using Grpc.Core;
using Grpc.Net.Client;

namespace Basket.API.GrpcServices;

public class DiscountGrpcService
{
    private readonly DiscountProtoService.DiscountProtoServiceClient _client;
    private readonly ILogger<DiscountGrpcService> _logger;

    public DiscountGrpcService(
        DiscountProtoService.DiscountProtoServiceClient client,
        ILogger<DiscountGrpcService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CouponModel> GetDiscount(string productName)
    {
        try
        {
            _logger.LogInformation("Getting discount for {ProductName}", productName);
            
            var request = new GetDiscountRequest { ProductName = productName };
            var response = await _client.GetDiscountAsync(request);
            
            _logger.LogInformation("Discount retrieved for {ProductName}: {Amount}", 
                productName, response.Amount);
            
            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "Error getting discount for {ProductName}. StatusCode: {StatusCode}", 
                productName, ex.StatusCode);
            
            // İndirim yoksa veya servis çalışmıyorsa 0 döndür (hata fırlatma)
            // NotFound = İndirim yok (normal durum)
            // Internal, Unavailable, Unimplemented = Servis çalışmıyor (graceful degradation)
            if (ex.StatusCode == StatusCode.NotFound || 
                ex.StatusCode == StatusCode.Internal || 
                ex.StatusCode == StatusCode.Unavailable ||
                ex.StatusCode == StatusCode.Unimplemented)
            {
                return new CouponModel { Amount = 0 };
            }
            
            throw;
        }
        catch (Exception ex)
        {
            // Diğer tüm exception'lar için de graceful degradation
            _logger.LogWarning(ex, "Unexpected error getting discount for {ProductName}", productName);
            return new CouponModel { Amount = 0 };
        }
    }
}

