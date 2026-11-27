namespace biddingServer.services.xendit
{
    public interface IXenditService
    {
        Task<XenditInvoiceResponse> CreateInvoiceAsync(decimal amount, string externalId, string description, string? successRedirectUrl = null, string? failureRedirectUrl = null);
    }

    public class XenditInvoiceResponse
    {
        public string Id { get; set; } = "";
        public string ExternalId { get; set; } = "";
        public string InvoiceUrl { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Amount { get; set; }
    }
}