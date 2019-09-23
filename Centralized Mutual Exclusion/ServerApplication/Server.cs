//Khushboo Babariya
//1001668208
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerWinsdowsFormsApplication
{
    delegate void SetTextCallback(string text);
    public partial class Form1 : Form
    {
        TcpClient tcpClientSocket = new TcpClient();

        /// <summary>
        /// The wait request queue
        /// </summary>
        /// URL:http://dotnetpattern.com/csharp-concurrentqueue
        ConcurrentQueue<WaitRequest> waitRequestQueue = new ConcurrentQueue<WaitRequest>();

        /// <summary>
        /// The currently served request
        /// </summary>
        WaitRequest servedRequest = new WaitRequest();
        public Form1()
        {
            InitializeComponent();
            this.Focus();
        }


        /// <summary>
        /// Handles the Click event of the StartServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void StartServer_Click(object sender, EventArgs e)
        {
            Thread chatThread = new Thread(StartServer);
            chatThread.Start();
        }

        /// <summary>
        /// Starts the server.
        /// </summary>
        private void StartServer()
        {
            //Create server socket on port 8888
            TcpListener serverSocket = new TcpListener(8888);
            TcpClient clientSocket = default(TcpClient);
            //Start Listnening
            serverSocket.Start();
            if (this.label1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { "Server started." });
            }
            Thread waitRequestProcessor = new Thread(WaitRequestProcessor);
            waitRequestProcessor.Start();
            while (true)
            {
                //Once client socket establishes connetion, server instantiate new object for each client.
                clientSocket = serverSocket.AcceptTcpClient();
                Thread chatThread = new Thread(() => ServeRequest(clientSocket));
                chatThread.Start();
            }
        }

        /// <summary>
        /// Register client, parse http message and wait for requested time peroid
        /// </summary>
        public void ServeRequest(TcpClient currentClient)
        {
            TcpClient tcpClientSocket = currentClient;
            string clientName = string.Empty;
            while (true)
            {
                try
                {
                    if (tcpClientSocket.Connected)
                    {
                        //Get message from socket
                        NetworkStream networkStream = tcpClientSocket.GetStream();
                        byte[] message = new byte[tcpClientSocket.ReceiveBufferSize];
                        networkStream.Read(message, 0, message.Length);
                        string clientData = System.Text.Encoding.ASCII.GetString(message);
                        string[] message1 = clientData.Split('\n');
                        //TcpClient sends empty message while disconnects
                        if (message[0] == 0)
                        {
                            if (this.label1.InvokeRequired)
                            {
                                SetTextCallback d = new SetTextCallback(SetText);
                                string responseMessage = "Client " + clientName + " has disconnected successfully.";
                                this.Invoke(d, new object[] { responseMessage });
                                tcpClientSocket.Close();
                            }
                            break;
                        }
                        else
                        {
                            JObject json = JObject.Parse(message1.Last());
                            clientName = json["ClientName"].ToString();
                            if (json["WaitRequest"] == null)
                            {
                                if (this.label1.InvokeRequired)
                                {
                                    SetTextCallback d = new SetTextCallback(SetText);
                                    string responseMessage = "Client " + json["ClientName"] + " has registered successfully.";
                                    this.Invoke(d, new object[] { responseMessage });
                                    SendMessage(responseMessage, networkStream);
                                }
                            }
                            else
                            {
                                int waitSeconds = Convert.ToInt32(json["WaitRequest"]);
                                SetTextCallback d = new SetTextCallback(SetText);
                                if (this.label1.InvokeRequired)
                                {
                                    this.Invoke(d, new object[] { "Client " + json["ClientName"] + " has  sent request wait of " + json["WaitRequest"] + "." });

                                }
                                //Enque wait request. 
                                EventWaitHandle waitRequestHandle = new AutoResetEvent(false);
                                WaitRequest waitRequest = new WaitRequest { ClientName = json["ClientName"].ToString(), WaitSecond = waitSeconds, WaitRequestHandle = waitRequestHandle };
                                waitRequestQueue.Enqueue(waitRequest);
                                //Once request processor serves request from queue it will notify respected thread by triggering event.
                                //URL:http://www.yoda.arachsys.com/csharp/threads/waithandles.shtml
                                if (waitRequestHandle.WaitOne())
                                {
                                    string waitSuccess = "Server waited for client " + clientName + " for " + servedRequest.WaitSecond + " successfully.";
                                    if (this.label1.InvokeRequired)
                                    {
                                        this.Invoke(d, new object[] { waitSuccess });
                                    }
                                    SendMessage(waitSuccess, networkStream);
                                }

                            }
                        }
                    }
                    else
                    {
                        currentClient.Close();
                        if (this.label1.InvokeRequired)
                        {
                            SetTextCallback d = new SetTextCallback(SetText);
                            string responseMessage = "Client " + clientName + " has disconnected successfully.";
                            this.Invoke(d, new object[] { responseMessage });
                        }
                        break;
                    }

                }
                catch (Exception ex)
                {
                    if (this.label1.InvokeRequired)
                    {
                        SetTextCallback d = new SetTextCallback(SetText);
                        this.Invoke(d, new object[] { ex.Message });
                    }

                }
            }

        }

        /// <summary>
        /// Send message in Http format
        /// </summary>
        /// <param name="message"></param>
        /// <param name="networkStream"></param>
        void SendMessage(string message, NetworkStream networkStream)
        {
            string registerRequest = HttpMessageFormat.HttpMessageBody.Replace("{Response}", message);
            string contnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", registerRequest.Length.ToString());
            byte[] responseStream = System.Text.Encoding.ASCII.GetBytes(HttpMessageFormat.HttpMessagResponseHeader + HttpMessageFormat.HttpMessageContentType
                + contnentLength + HttpMessageFormat.HttpMessageResponseDate.Replace("{Date}", DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT")) + registerRequest);
            networkStream.Write(responseStream, 0, responseStream.Length);
            networkStream.Flush();
        }

        /// <summary>
        /// Sets the text to the label for displaying current status of server
        /// </summary>
        /// <param name="text">The text.</param>
        private void SetText(string text)
        {
            this.label1.Text = this.label1.Text + text + Environment.NewLine;
        }

        /// <summary>
        /// Request processor dequeues wait request and notify respected thread after waiting for requested time
        /// </summary>
        private void WaitRequestProcessor()
        {
            Thread.Sleep(2000);
            while (true)
            {
                if (waitRequestQueue.Count > 0)
                {
                    WaitRequest currentRequest;
                    waitRequestQueue.TryDequeue(out currentRequest);
                    if (this.label1.InvokeRequired)
                    {
                        SetTextCallback d = new SetTextCallback(SetText);
                        this.Invoke(d, new object[] { "Server is waiting for "+currentRequest.WaitSecond+" of client "+ currentRequest.ClientName+"." });
                    }
                    Thread.Sleep(currentRequest.WaitSecond * 1000);
                    servedRequest = currentRequest;
                    //Notify thread
                    currentRequest.WaitRequestHandle.Set();
                }

            }
        }

        /// <summary>
        /// Handles the Click event of the Disconnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Disconnect_Click(object sender, EventArgs e)
        {
            tcpClientSocket.Close();
            this.Close();
        }

       
    }
}

