using TwoFA.AppLib;
using TwoFA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OtpNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwoFA.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            string randomKey = TFAManager.GetAuthenticatorKey();

            TFAViewModel vm = new TFAViewModel();

            vm.UserId = Guid.NewGuid().ToString();
            vm.UserName = "Demo User";
            vm.RandomKey = randomKey;
            vm.RandomKeyFormatted = TFAManager.FormatKey(randomKey);
            vm.QRCodeData = TFAManager.QrCodeUri("YourCompanyName", vm.UserId, randomKey);

            return View(model: vm);
        }

        // Install-Package Otp.NET
        private JsonResult CreateCode(string authenticatorKey)
        {
            try
            {
                if (string.IsNullOrEmpty(authenticatorKey)) throw new ArgumentNullException(nameof(authenticatorKey));

                AuthCodeViewModel vm = new AuthCodeViewModel();

                // Code & Remaining Time From TFAManager
                // var code = TwoFactorAuth.GetAuthenticatorCode(authenticatorKey);
                // var remainingTime = TwoFactorAuth.GetCurrentCounter();

                // Code & Remaining Time From Otp.NET
                // https://github.com/kspearrin/Otp.NET
                // var totp = new Totp(Base32Encoding.ToBytes(authenticatorKey));
                var totp = new Totp(Base32Encoding.ToBytes(authenticatorKey), timeCorrection: new TimeCorrection(DateTime.UtcNow.AddSeconds(+1)));
                var code = totp.ComputeTotp();
                var remainingTime = totp.RemainingSeconds();

                vm.AuthCode = $"{code.PadLeft(6, '0')}";
                vm.RemainingTime = $"{remainingTime,2:00}";

                return Json(vm);
            }
            catch (Exception ex)
            {
                AuthCodeViewModel vm = new AuthCodeViewModel() { AuthCode = "0", RemainingTime = "0", Message = ex.Message };
                return Json(vm);
            }
        }

        private string OtpVerify(string authenticatorKey, string totpCode)
        {
            var totp = new Totp(Base32Encoding.ToBytes(authenticatorKey));

            long timeWindowUsed;

            //var window = new VerificationWindow(previous: 1, future: 1);
            //bool result = totp.VerifyTotp(totpCode, out timeWindowUsed, window);
            bool result = totp.VerifyTotp(totpCode, out timeWindowUsed, VerificationWindow.RfcSpecifiedNetworkDelay);

            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime timeWindowDate = epoch.AddSeconds(timeWindowUsed);

            return $"OTP: {result} Window: {timeWindowUsed} Date: {timeWindowDate.ToLongDateString()} {timeWindowDate.ToLongTimeString()}";
        }


        [HttpPost]
        public IActionResult Index(string RandomKey)
        {


            return View(viewName: "Validate", model: RandomKey);
        }

        [HttpPost]
        public IActionResult ValidateResult(string authkey, string authcode)
        {
            string result = OtpVerify(authkey, authcode);

            return View(model: result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("get-auth-code")]
        public IActionResult GetAuthCode(string authenticatorKey)
        {
            return CreateCode(authenticatorKey);
        }

        [Route("/about")]
        public IActionResult About()
        {
            return View();
        }
    }
}
