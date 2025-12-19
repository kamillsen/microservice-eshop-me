using Discount.Grpc.Data;
using Discount.Grpc.Entities;
using Discount.Grpc.Protos;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace Discount.Grpc.Services;

public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly DiscountDbContext _context;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(
        DiscountDbContext context, 
        ILogger<DiscountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GetDiscount implementasyonu
    public override async Task<CouponModel> GetDiscount(
        GetDiscountRequest request, 
        ServerCallContext context)
    {
        // 1. ProductName'e göre kuponu veritabanından sorgula
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
        
        // 2. Kupon bulunamazsa RpcException fırlat (StatusCode.NotFound)
        if (coupon == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound, 
                $"Discount for {request.ProductName} not found"));
        }

        // 3. Loglama (başarılı sorgu)
        _logger.LogInformation("Discount retrieved for {ProductName}: {Amount}", 
            coupon.ProductName, coupon.Amount);

        // 4. CouponModel oluştur ve döndür
        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description ?? string.Empty,
            Amount = coupon.Amount
        };
    }

    // CreateDiscount implementasyonu
    public override async Task<CouponModel> CreateDiscount(
        CreateDiscountRequest request, 
        ServerCallContext context)
    {
        // 1. Aynı ProductName'e sahip kupon var mı kontrol et (unique constraint)
        var existingCoupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == request.Coupon.ProductName);
        
        if (existingCoupon != null)
        {
            throw new RpcException(new Status(
                StatusCode.AlreadyExists, 
                $"Discount for {request.Coupon.ProductName} already exists"));
        }

        // 2. Yeni Coupon entity oluştur
        var coupon = new Coupon
        {
            ProductName = request.Coupon.ProductName,
            Description = request.Coupon.Description,
            Amount = request.Coupon.Amount
        };

        // 3. Veritabanına ekle
        await _context.Coupons.AddAsync(coupon);
        await _context.SaveChangesAsync();

        // 4. Loglama (başarılı oluşturma)
        _logger.LogInformation("Discount created for {ProductName}: {Amount}", 
            coupon.ProductName, coupon.Amount);

        // 5. CouponModel oluştur ve döndür (ID artık veritabanından geliyor)
        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description ?? string.Empty,
            Amount = coupon.Amount
        };
    }

    // UpdateDiscount implementasyonu
    public override async Task<CouponModel> UpdateDiscount(
        UpdateDiscountRequest request, 
        ServerCallContext context)
    {
        // 1. ID'ye göre kuponu veritabanından bul
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Id == request.Coupon.Id);
        
        // 2. Kupon bulunamazsa RpcException fırlat (StatusCode.NotFound)
        if (coupon == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound, 
                $"Discount with ID {request.Coupon.Id} not found"));
        }

        // 3. Kupon bilgilerini güncelle
        coupon.ProductName = request.Coupon.ProductName;
        coupon.Description = request.Coupon.Description;
        coupon.Amount = request.Coupon.Amount;

        // 4. Veritabanına kaydet
        await _context.SaveChangesAsync();

        // 5. Loglama (başarılı güncelleme)
        _logger.LogInformation("Discount updated for {ProductName}: {Amount}", 
            coupon.ProductName, coupon.Amount);

        // 6. CouponModel oluştur ve döndür
        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description ?? string.Empty,
            Amount = coupon.Amount
        };
    }

    // DeleteDiscount implementasyonu
    public override async Task<DeleteDiscountResponse> DeleteDiscount(
        DeleteDiscountRequest request, 
        ServerCallContext context)
    {
        // 1. ProductName'e göre kuponu veritabanından bul
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.ProductName == request.ProductName);
        
        // 2. Kupon bulunamazsa RpcException fırlat (StatusCode.NotFound)
        if (coupon == null)
        {
            throw new RpcException(new Status(
                StatusCode.NotFound, 
                $"Discount for {request.ProductName} not found"));
        }

        // 3. Kuponu sil
        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync();

        // 4. Loglama (başarılı silme)
        _logger.LogInformation("Discount deleted for {ProductName}", 
            request.ProductName);

        // 5. DeleteDiscountResponse döndür
        return new DeleteDiscountResponse
        {
            Success = true
        };
    }
}

