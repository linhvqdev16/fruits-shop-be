using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sale.Service.OrdersService;
using System.Security.Cryptography;
using System.Text;

namespace Sales.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly string vnpayMerchantId = "7MBM5C0S"; // Thay thế bằng MerchantID của bạn
        private readonly string vnpayHashSecret = "AP5Z61HP5V3C14IVMAKFTWQHDJWYGHXS"; // Thay thế bằng HashSecret của bạn
        private readonly string vnpayApiUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"; // Địa chỉ sandbox của VNPay, thay đổi khi đi vào production

        private readonly IOrdersService _orderService; // Dịch vụ truy vấn đơn hàng

        public PaymentController(IOrdersService orderService)
        {
            _orderService = orderService;
        }

        private string GenerateSecureHash(Dictionary<string, string> parameters, string secretKey)
        {
            var sortedParams = parameters.OrderBy(kv => kv.Key)
                                          .Where(kv => kv.Key != "vnp_SecureHash" && !string.IsNullOrEmpty(kv.Value))
                                          .ToList();

            var stringToHash = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={kv.Value}")) + secretKey;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(stringToHash));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower(); // Return hash as lowercase hex string
            }
        }

        private string CreateQueryString(Dictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        }


        [HttpPost("create-payment")]
        [Authorize]
        public  IActionResult CreatePayment([FromBody] Model.Payment.PaymentRequest request)
        {

            // Lấy thông tin đơn hàng từ OrderId
            var order =  _orderService.GetById(request.OrderId);
            if (order == null)
            {
                return BadRequest("Đơn hàng không tồn tại");
            }

            // Lấy danh sách các mặt hàng trong đơn hàng
            var orderItems = _orderService.getByCartId(request.OrderId);

            // Tính toán tổng số tiền từ các mặt hàng trong đơn hàng
            var totalAmount = order.totalPrice;

            StringBuilder orderInfo = new StringBuilder();

            // Thêm thông tin các mặt hàng vào orderInfo
            foreach (var item in orderItems)
            {
                orderInfo.Append($"{item.CartId} (x{item.count}), ");
            }
            // Tạo các tham số cần thiết để gửi đến VNPay
            var vnpParams = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", vnpayMerchantId },
                { "vnp_Amount", (totalAmount * 100).ToString() }, // Chuyển số tiền sang đơn vị nhỏ nhất (VND = 100)
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", Guid.NewGuid().ToString() },
                { "vnp_OrderInfo", Uri.EscapeDataString("Thanh toan don hang :5") },
                { "vnp_ReturnUrl", Uri.EscapeDataString(request.ReturnUrl) },
                { "vnp_Locale", "vn" },
                { "vnp_IpAddr", HttpContext.Connection.RemoteIpAddress.ToString() == "::1" ? "127.0.0.1" : HttpContext.Connection.RemoteIpAddress.ToString()  }
            };
            var secureHash = GenerateSecureHash(vnpParams, vnpayHashSecret);
            vnpParams.Add("vnp_SecureHash", secureHash);

            var queryString = CreateQueryString(vnpParams);

            var paymentUrl = vnpayApiUrl + "?" + queryString;

            // Trả về URL thanh toán để frontend chuyển hướng
            return Ok(new { url = paymentUrl });
        }

      


    }
}
