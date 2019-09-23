//Khushboo Babariya
//1001668208
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ClientWindowsFormsApplication
{


    public partial class Client : Form
    {
        TcpClient clientSocket = new TcpClient();
        NetworkStream serverStream;
        public Client()
        {
            InitializeComponent();
            clientSocket.Connect("127.0.0.1", 8888);
            labelProgress.Text = "Server Connected.";
        }

        /// <summary>
        /// Register client to server and request for random time wait once client registered successfully
        /// Source:http://csharp.net-informations.com/communications/csharp-multi-threaded-client-socket.htm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Register_Click(object sender, EventArgs e)
        {

            if (clientSocket.Connected)
            {
                if (!string.IsNullOrEmpty(txtRegisterClient.Text))
                {
                    errorProvider1.Clear();
                    NetworkStream serverStream = clientSocket.GetStream();
                    if (txtRegisterClient.Enabled)
                    {
                        //Send registeration message in http format
                        string registerRequest = HttpMessageFormat.HttpMessageBody.Replace("{ClientName}", txtRegisterClient.Text);
                        string contnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", registerRequest.Length.ToString());
                        byte[] requestStream = System.Text.Encoding.ASCII.GetBytes(HttpMessageFormat.HttpMessagRequesteLine.Replace("{Method}", "POST")
                            + HttpMessageFormat.HttpMessageHost + HttpMessageFormat.HttpMessageUserAgent + HttpMessageFormat.HttpMessageContentType + contnentLength + registerRequest);
                        serverStream.Write(requestStream, 0, requestStream.Length);
                        serverStream.Flush();
                        //Receive reply from server,parse the same and display
                        byte[] responseStream = new byte[(int)clientSocket.ReceiveBufferSize];
                        int bytesRead = serverStream.Read(responseStream, 0, responseStream.Length);
                        string response = System.Text.Encoding.ASCII.GetString(responseStream);
                        label4.Text = Environment.NewLine + "Response: " + Environment.NewLine + response;
                        txtRegisterClient.Enabled = false;
                    }


                }
                else
                {
                    errorProvider1.SetError(txtRegisterClient, "Please enter Client Name.");
                }
            }

        }

        /// <summary>
        /// Disconnect client socket based on user request
        /// </summary>
        ///  <param name="sender"></param>
        /// <param name="e"></param>
        private void Disconnect_Click(object sender, EventArgs e)
        {
            clientSocket.Close();
            this.Close();
        }

        /// <summary>
        /// Send wait request and tarck waiting second for getting response
        /// </summary>
        ///  <param name="sender"></param>
        /// <param name="e"></param>
        private void SendRequest_Click(object sender, EventArgs e)
        {
            //Generate random number between 3 to 10 seconds and request for wait
            NetworkStream serverStream = clientSocket.GetStream();
            Random random = new Random();
            int requestedWait = random.Next(3, 10);
            string requestJsonMessage = @"{'ClientName':'" + txtRegisterClient.Text + "','WaitRequest':'" + requestedWait.ToString() + "'}";
            //string waitRequest = HttpMessageFormat.HttpMessageBody.Replace("{ClientName}", txtRegisterClient.Text)+"{'Wait'}""|" + requestedWait.ToString());
            string waitContnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", requestJsonMessage.Length.ToString());
            string requestMessage = HttpMessageFormat.HttpMessagRequesteLine.Replace("{Method}", "POST")
                + HttpMessageFormat.HttpMessageHost + HttpMessageFormat.HttpMessageUserAgent + HttpMessageFormat.HttpMessageContentType + waitContnentLength + requestJsonMessage.ToString();
            label4.Text = label4.Text + Environment.NewLine + "Request for wait." + Environment.NewLine + requestMessage;
            byte[] waitStream = System.Text.Encoding.ASCII.GetBytes(requestMessage);
            serverStream.Write(waitStream, 0, waitStream.Length);
            DateTime requestTime = DateTime.Now;
            byte[] waitResponseStream = new byte[(int)clientSocket.ReceiveBufferSize];
            int bytesRead1 = serverStream.Read(waitResponseStream, 0, waitResponseStream.Length);
            DateTime responseTime = DateTime.Now;
            string waitResponse = System.Text.Encoding.ASCII.GetString(waitResponseStream);
            //Calculate wait time for getting response from server
            TimeSpan waitedTime = (responseTime - requestTime);
            label4.Text = label4.Text + Environment.NewLine + Environment.NewLine + "Client spent " + waitedTime.Seconds.ToString() + " for request of " + requestedWait.ToString() + Environment.NewLine + waitResponse;
            serverStream.Flush();
        }
    }
}
