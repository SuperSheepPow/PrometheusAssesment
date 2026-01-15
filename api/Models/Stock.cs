using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Stock
    {
        public string Day { get; set; } = string.Empty;           // "2009-01-30"
        public decimal LowAverage { get; set; }   // 40.2958
        public decimal HighAverage { get; set; }  // 49.7534
        public long Volume { get; set; }
    }
}