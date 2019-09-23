//Khushboo Babariya
//1001668208
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWindowsFormsApplication
{
    //Class defines http message format.
    static class HttpMessageFormat
    {
        public static string HttpMessagRequesteLine = "{Method} ServerApplication HTTP/1.1\r\n";
        public static string HttpMessageContentType = "Content-Type: text/json\r\n";
        public static string HttpMessageContentLength = "Content-Length: {Length}\r\n";
        public static string HttpMessageHost = "Host:127.0.0.1\r\n";
        public static string HttpMessageUserAgent = "User-Agent:ClientWIndowsFormsApplication\r\n";
        public static string HttpMessageBody = @"{'ClientName':'{ClientName}'}";

    }
}
