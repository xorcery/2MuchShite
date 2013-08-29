using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2MuchShite.Models
{
    public class DriveDetail
    {
        public string DriveLetter { get; set; }
        public string Name { get; set; }
        public string DriveType { get; set; }
        public long TotalSize { get; set; }
        public long TotalFreeSpace { get; set; }
        public long AvailableFreeSpace { get; set; }
        public float PercentageFreeSpace { get; set; }

        public DriveDetail()
        {
            DriveLetter = null;
            Name = null;
            DriveType = null;
            TotalSize = -1;
            TotalFreeSpace = -1;
            AvailableFreeSpace = -1;
            PercentageFreeSpace = 0.0F;
        }

        public DriveDetail(string driveLetter, string name, string driveType, long totalSize,
            long totalFreeSpace, long availableFreeSpace, float percentageFreeSpace)
        {
            DriveLetter = driveLetter;
            Name = name;
            DriveType = driveType;
            TotalSize = totalSize;
            TotalFreeSpace = totalFreeSpace;
            AvailableFreeSpace = availableFreeSpace;
            PercentageFreeSpace = percentageFreeSpace;
        }
    }
}
