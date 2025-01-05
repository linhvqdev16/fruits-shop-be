using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Sale.Service.Constant;
using Sale.Service.Dtos;
using Sale.Service.Services;
using Sales.Model;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sales.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VnPayController : ControllerBase
    {
        private IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        public VnPayController(IVnPayService vnPayService, IConfiguration configuration)
        {
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("createPaymentUrl")]
        public IActionResult CreatePaymentUrl(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return StatusCode(StatusCodes.Status200OK, new ResponseWithDataDto<dynamic>
            {
                Data = url,
                Status = StatusConstant.SUCCESS,
                Message = "Thành công."
            });
        }



        [AllowAnonymous]
        [HttpGet("paymentExecute")]
        public IActionResult PaymentExecute()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Success)
            {
                /// Call Service Update Order Status 
                /// 

                return Redirect(_configuration["Vnpay:RedirectWhenPaymentSuccess"]); 
            }
            return Redirect(_configuration["Vnpay:RedirectWhenPaymentFail"]);

            //return StatusCode(StatusCodes.Status200OK, new ResponseWithDataDto<dynamic>
            //{
            //    Data = response,
            //    Status = StatusConstant.SUCCESS,
            //    Message = "Thành công."
            //});
        }
    }
}
