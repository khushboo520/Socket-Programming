//Khushboo Babariya
//1001668208
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ServerWinsdowsFormsApplication
{
    delegate void SetTextCallback(string text);
    public partial class Form1 : Form
    {
        TcpClient tcpClientSocket = new TcpClient();

        /// <summary>
        /// Thread safe Queue of connected client's thread
        /// </summary>
        ConcurrentQueue<CurrentThreadDetails> runningThread = new ConcurrentQueue<CurrentThreadDetails>();

        /// <summary>
        /// Thread safe Queue of threads of clients from which server has received expression while polling
        /// </summary>
        ConcurrentQueue<CurrentThreadDetails> expressionThread = new ConcurrentQueue<CurrentThreadDetails>();

        /// <summary>
        /// Thread safe Queue for backing up conncted cleint thread for further poll
        /// </summary>
        ConcurrentQueue<CurrentThreadDetails> backupThread = new ConcurrentQueue<CurrentThreadDetails>();

        /// <summary>
        /// Server will save final answer after poll.
        /// </summary>
        object finalAnswer;

        /// <summary>
        /// Thread safe expression list for storing polled expression
        /// </summary>
        ConcurrentBag<string> expressionList = new ConcurrentBag<string>();
        string mainCopy = "1";

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

            while (true)
            {
                //Once client socket establishes connetion, server instantiate new object for each client.
                clientSocket = serverSocket.AcceptTcpClient();
                Thread chatThread = new Thread(() => ServeRequest(clientSocket));
                chatThread.Start();
            }
        }

        /// <summary>
        /// Register client, send poll request
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
                                currentClient.Close();
                                //Remove client from connected client queue
                                CurrentThreadDetails currentThreadDetails = new CurrentThreadDetails();
                                while (!runningThread.IsEmpty)
                                {
                                    runningThread.TryDequeue(out currentThreadDetails);
                                    if (currentThreadDetails.ClientName != clientName)
                                    {
                                        backupThread.Enqueue(currentThreadDetails);
                                    }
                                }
                                runningThread = backupThread;
                            }
                        }
                        else
                        {
                            JObject json = JObject.Parse(message1.Last());
                            clientName = json["ClientName"].ToString();
                            if (json["Expression"] == null)
                            {
                                if (this.label1.InvokeRequired)
                                {
                                    SetTextCallback d = new SetTextCallback(SetText);
                                    string responseMessage = "Client " + json["ClientName"] + " has registered successfully.";
                                    this.Invoke(d, new object[] { responseMessage });
                                    SendMessage(responseMessage, networkStream);
                                    AutoResetEvent waitRequestHandle = new AutoResetEvent(false);
                                    CurrentThreadDetails currentThreadDetails = new CurrentThreadDetails();
                                    currentThreadDetails.ClientName = json["ClientName"].ToString();
                                    currentThreadDetails.WaitRequestHandle = waitRequestHandle;
                                    runningThread.Enqueue(currentThreadDetails);
                                    ///Send poll request
                                    if (waitRequestHandle.WaitOne())
                                    {
                                        string registerRequest = HttpMessageFormat.HttpMessageBody.Replace("{Response}", "Poll");
                                        string contnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", registerRequest.Length.ToString());
                                        byte[] responseStream = System.Text.Encoding.ASCII.GetBytes(HttpMessageFormat.HttpMessagResponseHeader + HttpMessageFormat.HttpMessageContentType
                                            + contnentLength + HttpMessageFormat.HttpMessageResponseDate.Replace("{Date}", DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT")) + registerRequest);
                                        networkStream.Write(responseStream, 0, responseStream.Length);
                                        continue;
                                    }
                                }
                            }
                            //Get expression
                            else
                            {
                                expressionList.Add(json["Expression"].ToString());
                                if (this.label1.InvokeRequired)
                                {
                                    SetTextCallback d = new SetTextCallback(SetText);
                                    string responseMessage = "Client " + clientName + " :" + json["Expression"].ToString();
                                    this.Invoke(d, new object[] { responseMessage });
                                }
                                AutoResetEvent waitRequestHandle1 = new AutoResetEvent(false);
                                CurrentThreadDetails currentThreadDetails = new CurrentThreadDetails();
                                currentThreadDetails.WaitRequestHandle = waitRequestHandle1;
                                currentThreadDetails.ClientName = clientName;
                                expressionThread.Enqueue(currentThreadDetails);

                                //wait for calculation of merged expression
                                if (waitRequestHandle1.WaitOne())
                                {
                                    //send  answer to client
                                    string registerRequest = HttpMessageFormat.HttpMessageBody.Replace("{Response}", finalAnswer.ToString());
                                    string contnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", registerRequest.Length.ToString());
                                    byte[] responseStream = System.Text.Encoding.ASCII.GetBytes(HttpMessageFormat.HttpMessagResponseHeader + HttpMessageFormat.HttpMessageContentType
                                        + contnentLength + HttpMessageFormat.HttpMessageResponseDate.Replace("{Date}", DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT")) + registerRequest);
                                    networkStream.Write(responseStream, 0, responseStream.Length);
                                }

                            }
                        }
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
        /// Handles the Click event of the Disconnect control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Disconnect_Click(object sender, EventArgs e)
        {
            tcpClientSocket.Close();
            this.Close();
        }

        /// <summary>
        /// Handles the Click event of the btnPoll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void btnPoll_Click(object sender, EventArgs e)
        {
            CurrentThreadDetails currentThreadDetails = new CurrentThreadDetails();
            if (runningThread.Count == 0)
            {
                runningThread = backupThread;
            }
            while (!runningThread.IsEmpty)
            {
                runningThread.TryDequeue(out currentThreadDetails);
                backupThread.Enqueue(currentThreadDetails);
                currentThreadDetails.WaitRequestHandle.Set();
            }

            int i = 0;
            if (i <= 1)
            {
                //Start calculate expression thread
                Thread calculateThread = new Thread(() => CalculateMainExpression());
                calculateThread.Start();
            }
            i++;
        }

        /// <summary>
        /// Calculates the main expression.
        /// </summary>
        private void CalculateMainExpression()
        {
            Thread.Sleep(2000);
            if (expressionList.Count == expressionThread.Count)
            {
                CurrentThreadDetails currentThreadDetails = new CurrentThreadDetails();
                StringBuilder mainExpression = new StringBuilder();
                //create main expression from polled expression
                while (!expressionList.IsEmpty)
                {
                    string expression = string.Empty;
                    expressionList.TryTake(out expression);
                    mainExpression.Append(expression);
                }
                finalAnswer = new DataTable().Compute(mainCopy + mainExpression, null);
                if (this.label1.InvokeRequired)
                {
                    SetTextCallback d = new SetTextCallback(SetText);
                    string responseMessage = "MainExpression " + mainCopy + mainExpression + " =" + finalAnswer.ToString();
                    this.Invoke(d, new object[] { responseMessage });
                }

                if (finalAnswer.ToString() != "∞")
                {
                    //set final answer to all client
                    while (!expressionThread.IsEmpty)
                    {
                        expressionThread.TryDequeue(out currentThreadDetails);
                        currentThreadDetails.WaitRequestHandle.Set();
                    }
                }

            }

        }
    }
}

