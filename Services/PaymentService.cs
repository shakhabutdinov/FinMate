using aspnet.DTOs;

namespace aspnet.Services;

public class PaymentService(IConfiguration configuration)
{
    public Task<PaymentResponseDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto)
    {
        var merchantId = configuration["GooglePay:MerchantId"];
        var merchantName = configuration["GooglePay:MerchantName"];

        _ = merchantId;
        _ = merchantName;

        var response = new PaymentResponseDto(
            TransactionId: Guid.NewGuid().ToString(),
            Status: "pending",
            Amount: dto.Amount,
            Currency: dto.Currency
        );

        return Task.FromResult(response);
    }

    public PaymentConfigDto GetPaymentConfig()
    {
        return new PaymentConfigDto(
            MerchantId: configuration["GooglePay:MerchantId"] ?? "",
            MerchantName: configuration["GooglePay:MerchantName"] ?? "",
            Environment: configuration["GooglePay:Environment"] ?? "TEST"
        );
    }
}

public record PaymentConfigDto(string MerchantId, string MerchantName, string Environment);
