using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2MuchShite.Models
{
    public class Alert
    {
        public DriveDetail DriveDetail { get; set; }
        public string Message { get; set; }

        public Alert()
        {
            DriveDetail = null;
            Message = null;
        }
    }
}
