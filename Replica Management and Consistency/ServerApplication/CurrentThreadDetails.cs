//Khushboo Babariya
//1001668208
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerWinsdowsFormsApplication
{
    /// <summary>
    /// This class is used to store information of connected client
    /// </summary>
    public class CurrentThreadDetails
    {
        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        /// <value>
        /// The name of the client.
        /// </value>
        public string ClientName { get; set; }
        /// <summary>
        /// Gets or sets the wait request handle.
        /// </summary>
        /// <value>
        /// The wait request handle.
        /// </value>
        public AutoResetEvent WaitRequestHandle { get; set; }


    }
}
