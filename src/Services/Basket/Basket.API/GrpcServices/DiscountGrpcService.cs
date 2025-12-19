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
            _logger.LogError(ex, "Error getting discount for {ProductName}", productName);
            
            // İndirim yoksa null döndür (hata fırlatma)
            if (ex.StatusCode == StatusCode.NotFound)
                return new CouponModel { Amount = 0 };
            
            throw;
        }
    }
}

