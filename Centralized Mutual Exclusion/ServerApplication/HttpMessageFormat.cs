//Khushboo Babariya
//1001668208
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerWinsdowsFormsApplication
{
    /// <summary>
    /// A class for http message format
    /// </summary>
    static class HttpMessageFormat
    {
        public static string HttpMessagResponseHeader = "HTTP/1.1 200 OK\r\n";
        public static string HttpMessageContentType = "Content-Type: text/json\r\n";
        public static string HttpMessageContentLength = "Content-Length: {Length}\r\n";
        public static string HttpMessageResponseDate = "Date: {Date}\r\n";
        public static string HttpMessageBody = @"{'ResponseMessage':'{Response}'}";

    }
}
