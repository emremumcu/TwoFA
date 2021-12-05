using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwoFA.ViewModels
{
    public class AuthCodeViewModel
    {
        public string AuthCode { get; set; }
        public string RemainingTime { get; set; }
        public string Message { get; set; }
    }
}
