using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
namespace ResponsivePortal.Models
{
    public class FileResult :ActionResult
    {
        public static Dictionary<string, string> ContentType = new Dictionary<string, string>();
        private string m_path = "";
        public FileResult(string path) { m_path = path; }
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public override void ExecuteResult(ControllerContext context)
        {
            if (ContentType.Count == 0) loadFileResultContentTypes();
            string ext = System.IO.Path.GetExtension(m_path);

            if (ContentType.ContainsKey(ext)) context.HttpContext.Response.ContentType = ContentType[ext];
            context.HttpContext.Response.WriteFile(m_path);
        }

        private static void loadFileResultContentTypes()
        {
            ResponsivePortal.Models.FileResult.ContentType = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            ResponsivePortal.Models.FileResult.ContentType.Add(".gif", "image/gif");
            ResponsivePortal.Models.FileResult.ContentType.Add(".jpg", "image/jpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".jpeg", "image/jpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".jpe", "image/jpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".pdf", "application/pdf");
            ResponsivePortal.Models.FileResult.ContentType.Add(".mpg", "video/mpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".mpeg", "video/mpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".mpe", "video/mpeg");
            ResponsivePortal.Models.FileResult.ContentType.Add(".mov", "video/quicktime");
            ResponsivePortal.Models.FileResult.ContentType.Add(".qt", "video/quicktime");
            ResponsivePortal.Models.FileResult.ContentType.Add(".wav", "audio/x-wav");
            ResponsivePortal.Models.FileResult.ContentType.Add(".zip", "application/zip");
            ResponsivePortal.Models.FileResult.ContentType.Add(".doc", "application/msword");
            ResponsivePortal.Models.FileResult.ContentType.Add(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            ResponsivePortal.Models.FileResult.ContentType.Add(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            ResponsivePortal.Models.FileResult.ContentType.Add(".xls", "application/msexcel");
            ResponsivePortal.Models.FileResult.ContentType.Add(".ppt", "application/mspowerpoint");
            ResponsivePortal.Models.FileResult.ContentType.Add(".bmp", "image/bmp");
            ResponsivePortal.Models.FileResult.ContentType.Add(".png", "image/png");
            ResponsivePortal.Models.FileResult.ContentType.Add(".css", "text/css");
            ResponsivePortal.Models.FileResult.ContentType.Add(".js", "text/javascript");
            ResponsivePortal.Models.FileResult.ContentType.Add(".html", "text/html");
            ResponsivePortal.Models.FileResult.ContentType.Add(".htm", "text/html");
            ResponsivePortal.Models.FileResult.ContentType.Add(".stm", "text/html");
            ResponsivePortal.Models.FileResult.ContentType.Add(".txt", "text/plain");
            ResponsivePortal.Models.FileResult.ContentType.Add(".ico", "image/x-icon");
            ResponsivePortal.Models.FileResult.ContentType.Add(".tif", "image/tiff");
            ResponsivePortal.Models.FileResult.ContentType.Add(".tiff", "image/tiff");
            ResponsivePortal.Models.FileResult.ContentType.Add(".swf", "application/x-shockwave-flash");
            ResponsivePortal.Models.FileResult.ContentType.Add(".rtf", "application/rtf");
            ResponsivePortal.Models.FileResult.ContentType.Add(".dll", "application/x-msdownload");
            ResponsivePortal.Models.FileResult.ContentType.Add(".exe", "application/octet-stream");
            ResponsivePortal.Models.FileResult.ContentType.Add(".pps", "application/vnd.ms-powerpoint");

        }
    }
}