using ClearLend.Domain.Common;

namespace ClearLend.Domain.Finance;

public enum PaymentStatus { Pending = 1, Succeeded = 2, Failed = 3, Refunded = 4 }
public readonly record struct PaymentId(Guid Value) { public static PaymentId New() => new(Guid.NewGuid()); }
public sealed record Fee(string Type, decimal Amount, decimal? Percentage, string Currency, string RecipientReference)
{
    public static DomainResult<Fee> Create(string? type, decimal amount, decimal? percentage, string? currency, string? recipientReference)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(currency) || string.IsNullOrWhiteSpace(recipientReference))
            return DomainResults.Failure<Fee>(new("fee.values.required", "Fee type, currency, and recipient are required."));
        if (amount < 0 || percentage is < 0 or > 100)
            return DomainResults.Failure<Fee>(new("fee.values.invalid", "Fee amount and percentage must be valid."));
        return DomainResults.Success(new Fee(type.Trim(), decimal.Round(amount, 2), percentage, currency.Trim().ToUpperInvariant(), recipientReference.Trim()));
    }
}
public sealed record PaymentAllocation(string RecipientReference, decimal Amount, decimal? Percentage);

public sealed class Payment
{
    private Payment(PaymentId id, string payerReference, decimal amount, string currency, DateTimeOffset createdAt)
    { Id = id; PayerReference = payerReference; Amount = amount; Currency = currency; CreatedAt = createdAt; Status = PaymentStatus.Pending; }
    public PaymentId Id { get; }
    public string PayerReference { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionReference { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyList<PaymentAllocation> Allocations => _allocations;
    private readonly List<PaymentAllocation> _allocations = [];

    public static DomainResult<Payment> Create(string? payerReference, decimal amount, string? currency, DateTimeOffset createdAt, PaymentId? id = null)
    {
        if (string.IsNullOrWhiteSpace(payerReference)) return DomainResults.Failure<Payment>(new("payment.payer.required", "A payer is required."));
        if (amount <= 0) return DomainResults.Failure<Payment>(new("payment.amount.invalid", "Payment amount must be positive."));
        if (string.IsNullOrWhiteSpace(currency)) return DomainResults.Failure<Payment>(new("payment.currency.required", "Currency is required."));
        if (createdAt.Offset != TimeSpan.Zero) return DomainResults.Failure<Payment>(new("payment.created_at.not_utc", "Payment time must be expressed in UTC."));
        return DomainResults.Success(new Payment(id ?? PaymentId.New(), payerReference.Trim(), decimal.Round(amount, 2), currency.Trim().ToUpperInvariant(), createdAt));
    }
    public DomainResult AddAllocation(PaymentAllocation? allocation)
    {
        if (Status != PaymentStatus.Pending) return DomainResult.Failure(new("payment.allocation.invalid_status", "Allocations can only be changed while payment is pending."));
        if (allocation is null || string.IsNullOrWhiteSpace(allocation.RecipientReference) || allocation.Amount <= 0 || allocation.Percentage is < 0 or > 100) return DomainResult.Failure(new("payment.allocation.invalid", "A valid recipient, amount, and percentage are required."));
        if (_allocations.Sum(x => x.Amount) + allocation.Amount > Amount) return DomainResult.Failure(new("payment.allocation.exceeds_amount", "Allocations cannot exceed the payment amount."));
        _allocations.Add(allocation with { RecipientReference = allocation.RecipientReference.Trim(), Amount = decimal.Round(allocation.Amount, 2) }); return DomainResult.Success();
    }
    public DomainResult EnsureFullyAllocated() =>
        _allocations.Sum(x => x.Amount) == Amount
            ? DomainResult.Success()
            : DomainResult.Failure(new("payment.allocation.incomplete", "Payment allocations must equal the payment amount."));
    public DomainResult MarkSucceeded(string? transactionReference) { if (Status != PaymentStatus.Pending) return DomainResult.Failure(new("payment.succeed.invalid_status", "Only a pending payment can succeed.")); if (string.IsNullOrWhiteSpace(transactionReference)) return DomainResult.Failure(new("payment.transaction_reference.required", "A transaction reference is required.")); Status = PaymentStatus.Succeeded; TransactionReference = transactionReference.Trim(); return DomainResult.Success(); }
    public DomainResult MarkFailed() { if (Status != PaymentStatus.Pending) return DomainResult.Failure(new("payment.fail.invalid_status", "Only a pending payment can fail.")); Status = PaymentStatus.Failed; return DomainResult.Success(); }
}
