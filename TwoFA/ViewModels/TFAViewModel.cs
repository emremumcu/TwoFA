using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TwoFA.ViewModels
{
    public class TFAViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public String RandomKey { get; set; }
        public String RandomKeyFormatted { get; set; }
        public String QRCodeData { get; set; }
    }
}
