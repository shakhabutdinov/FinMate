namespace aspnet.DTOs;

public record CreatePaymentDto(decimal Amount, string Currency, string Description);
public record PaymentResponseDto(string TransactionId, string Status, decimal Amount, string Currency);
