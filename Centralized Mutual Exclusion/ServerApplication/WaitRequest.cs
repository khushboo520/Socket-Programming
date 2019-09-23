//Khushboo Babariya
//1001668208
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerWinsdowsFormsApplication
{
    /// <summary>
    /// This class represt each client's wait request in queue
    /// </summary>
    public class WaitRequest
    {
        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        /// <value>
        /// The name of the client.
        /// </value>
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets the wait second.
        /// </summary>
        /// <value>
        /// The wait second.
        /// </value>
        public int WaitSecond { get; set; }

        /// <summary>
        /// Gets or sets the wait request handle.
        /// </summary>
        /// <value>
        /// The wait request handle for trigerring and notifying events
        /// </value>
        public EventWaitHandle WaitRequestHandle { get; set; }
    }
}
