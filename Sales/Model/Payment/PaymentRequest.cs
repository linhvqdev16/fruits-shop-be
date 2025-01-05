namespace Sales.Model.Payment
{
    public class PaymentRequest
    {
        public Guid OrderId { get; set; }
        public string ReturnUrl { get; set; }
    }
}
