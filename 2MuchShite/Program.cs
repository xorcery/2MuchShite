using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Management;
using System.Text;
using _2MuchShite.Models;
using System.Net.Mail;
using System.Net;

namespace _2MuchShite
{
    class Program
    {
        static void Main(string[] args)
        {
            CheckDrives();
        }


        /// <summary>
        /// Check drives from config file
        /// </summary>
        static void CheckDrives()
        {
            List<string> drivesToCheck = ConfigurationManager.AppSettings.Get("DrivesToCheck").Split(',').ToList();
            List<DriveDetail> driveDetailList = new List<DriveDetail>();
            List<Alert> alertList = new List<Alert>();

            // Check drives
            foreach (string drive in drivesToCheck)
            {
                Console.WriteLine("Checking drive: " + drive + "...");
                DriveDetail driveDetail = CheckFreeSpace(drive);

                if (false == string.IsNullOrEmpty(driveDetail.Name))
                {
                    driveDetailList.Add(driveDetail);
                }
            }

            // Generate alerts
            alertList = GenerateAlerts(driveDetailList);

            Console.WriteLine("Checking drive free space...");
            if (alertList.Count > 0)
            {
                SendEmail(alertList);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Everything is great! Bye.");
            }

        }


        /// <summary>
        /// Check free space on a drive
        /// </summary>
        /// <param name="driveLetter"></param>
        /// <returns></returns>
        static DriveDetail CheckFreeSpace(string driveLetter)
        {
            DriveDetail driveDetail = new DriveDetail();

            try
            {
                DriveInfo driveInfo = new DriveInfo(driveLetter);

                driveDetail.DriveLetter = driveLetter;
                driveDetail.Name = driveInfo.Name;
                driveDetail.DriveType = driveInfo.DriveType.ToString();
                driveDetail.TotalSize = driveInfo.TotalSize;
                driveDetail.TotalFreeSpace = driveInfo.TotalFreeSpace;
                driveDetail.AvailableFreeSpace = driveInfo.AvailableFreeSpace;
                driveDetail.PercentageFreeSpace = ((float)driveDetail.TotalFreeSpace / (float)driveDetail.TotalSize) * 100;

                Console.WriteLine(" - Total Size: " + driveDetail.TotalSize);
                Console.WriteLine(" - Total Free Space: " + driveDetail.TotalFreeSpace);
                Console.WriteLine(" - Percentage Free Space: " + Math.Round(driveDetail.PercentageFreeSpace, 1) + "%");
            }
            catch (IOException error)
            {
                Console.WriteLine(error);
            }

            Console.WriteLine();

            return driveDetail;
        }


        /// <summary>
        /// Generate alertgs
        /// </summary>
        /// <param name="driveDetailList"></param>
        /// <returns></returns>
        static List<Alert> GenerateAlerts(List<DriveDetail> driveDetailList)
        {
            List<Alert> alertList = new List<Alert>();
            float checkPercentage = 101F;
            long checkDriveSpace = long.MaxValue;

            if (ConfigurationManager.AppSettings["Alert:FreeSpacePercentage"] != null && 
                ConfigurationManager.AppSettings.Get("Alert:FreeSpacePercentage").Length > 0)
            {
                checkPercentage = Convert.ToSingle(ConfigurationManager.AppSettings.Get("Alert:FreeSpacePercentage"));
            }

            if (ConfigurationManager.AppSettings["Alert:FreeSpaceBytes"] != null && 
                ConfigurationManager.AppSettings.Get("Alert:FreeSpaceBytes").Length > 0)
            {
                checkDriveSpace = Convert.ToInt64(ConfigurationManager.AppSettings.Get("Alert:FreeSpaceBytes"));
            }


            foreach (DriveDetail driveDetail in driveDetailList)
            {
                if (driveDetail.PercentageFreeSpace < checkPercentage)
                {
                    Alert alert = new Alert();

                    alert.DriveDetail = driveDetail;
                    alert.Message = "Drive " + driveDetail.DriveLetter + " has " + Math.Round(driveDetail.PercentageFreeSpace, 1) + "% free.";

                    alertList.Add(alert);

                    Console.WriteLine("ALERT: " + alert.Message);
                }
                else if (driveDetail.TotalFreeSpace < checkDriveSpace)
                {
                    Alert alert = new Alert();

                    alert.DriveDetail = driveDetail;
                    alert.Message = "Drive " + driveDetail.DriveLetter + " has " + NiceFileSizeString(driveDetail.TotalFreeSpace) + " free.";

                    alertList.Add(alert);

                    Console.WriteLine("ALERT: " + alert.Message);
                }
            }

            return alertList;
        }


        /// <summary>
        /// Send Email 
        /// </summary>
        static void SendEmail(List<Alert> alertList)
        {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage message = new MailMessage();

            smtpClient.Host = ConfigurationManager.AppSettings.Get("SMTP:Host");
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = ConfigurationManager.AppSettings.Get("SMTP:EnableSsl") == "1" ? true : false;
            smtpClient.Port = Convert.ToInt32(ConfigurationManager.AppSettings.Get("SMTP:Port"));
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.UseDefaultCredentials = ConfigurationManager.AppSettings.Get("SMTP:UseDefaultCredentials") == "1" ? true : false;

            if (false == string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get("SMTP:Username")))
            {
                smtpClient.Credentials = new NetworkCredential(ConfigurationManager.AppSettings.Get("SMTP:Username"), 
                    ConfigurationManager.AppSettings.Get("SMTP:Password"));
            }

            MailAddress fromAddress = new MailAddress(ConfigurationManager.AppSettings.Get("Email:FromAddress"));

            message.From = fromAddress;
            message.Subject = "SERVER ALERT: (" + ConfigurationManager.AppSettings.Get("ServerFriendlyName") + ") Low Disk Space";
            message.IsBodyHtml = false;

            string body = "";

            body += "= " + message.Subject + " =\r\n";

            foreach (Alert alert in alertList)
            {
                body += alert.Message + "\r\n";
                body += " - Drive " + alert.DriveDetail.DriveLetter + ":\r\n";
                body += " - Total Size: " + NiceFileSizeString(alert.DriveDetail.TotalSize) + "\r\n";
                body += " - Free Space: " + NiceFileSizeString(alert.DriveDetail.TotalFreeSpace) + "\r\n";
                body += " - Percentage Free: " + Math.Round(alert.DriveDetail.PercentageFreeSpace,1) + "%\r\n";
                body += "\r\n";
            }

            message.Body = body;

            List<string> emailToList = new List<string>();

            emailToList = ConfigurationManager.AppSettings.Get("Email:ToAddress").Split(',').ToList();

            foreach (string emailTo in emailToList)
            {
                message.To.Add(emailTo);
            }

            try
            {
                smtpClient.Send(message);
                Console.WriteLine("Emails sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email Error: " + ex.ToString());
            }
        }


        /// <summary>
        /// Size suffix
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static string NiceFileSizeString(long value)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1 << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
