//Khushboo Babariya
//1001668208
using System;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace ClientWindowsFormsApplication
{

    delegate void SetTextCallback(string text);
    public partial class Client : Form
    {
        TcpClient clientSocket = new TcpClient();
        double localCopy = 1;
        StringBuilder expression = new StringBuilder();
        public Client()
        {
            InitializeComponent();
            //Connect to server
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
            if (!clientSocket.Connected)
            {
                clientSocket = new TcpClient();
                clientSocket.Connect("127.0.0.1", 8888);
            }

            if (!string.IsNullOrEmpty(txtRegisterClient.Text))
            {
                errorProvider1.Clear();
                NetworkStream serverStream = clientSocket.GetStream();
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
                //Start thread continuously check for poll request from server
                Thread waitRequestProcessor = new Thread(PollRequestProcessor);
                waitRequestProcessor.Start();
            }
            else
            {
                errorProvider1.SetError(txtRegisterClient, "Please enter Client Name.");
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
        }


        /// <summary> Calculate expression entered by user on local copy</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalculate_Click(object sender, EventArgs e)
        {
            //calculate expression in airthmatic order and if it results to NAN then clear expression input
            object newValue = new DataTable().Compute(localCopy.ToString() + textBox1.Text, null).ToString();
            if (!newValue.Equals("∞"))
            {
                localCopy = double.Parse(newValue.ToString());
                expression.Append(textBox1.Text);
                label4.Text += Environment.NewLine + expression.ToString();
                answer.Text = newValue.ToString();
            }
            else
            {
                textBox1.Text = string.Empty;
            }
        }

        /// <summary>
        /// Server polls the request.
        /// </summary>
        private void PollRequestProcessor()
        {
            Thread.Sleep(2000);
            try
            {
                while (true)
                {
                    if (clientSocket.Connected)
                    {
                        NetworkStream serverStream = clientSocket.GetStream();
                        byte[] message = new byte[clientSocket.ReceiveBufferSize];
                        serverStream.Read(message, 0, message.Length);
                        string clientData = System.Text.Encoding.ASCII.GetString(message);
                        string[] message1 = clientData.Split('\n');
                        JObject json = JObject.Parse(message1.Last());
                        //Send expression to server when receive poll request
                        if (json["ResponseMessage"].ToString() == "Poll")
                        {
                            string requestJsonMessage = @"{'ClientName':'" + txtRegisterClient.Text + "','Expression':'" + expression.ToString() + "'}";
                            string waitContnentLength = HttpMessageFormat.HttpMessageContentLength.Replace("{Length}", requestJsonMessage.Length.ToString());
                            string requestMessage = HttpMessageFormat.HttpMessagRequesteLine.Replace("{Method}", "POST")
                                + HttpMessageFormat.HttpMessageHost + HttpMessageFormat.HttpMessageUserAgent + HttpMessageFormat.HttpMessageContentType + waitContnentLength + requestJsonMessage.ToString();
                            byte[] waitStream = System.Text.Encoding.ASCII.GetBytes(requestMessage);
                            NetworkStream serverStream1 = clientSocket.GetStream();
                            serverStream1.Write(waitStream, 0, waitStream.Length);

                        }
                        else
                        {
                            //Receive answer from server and replace local copy with the same
                            localCopy = double.Parse(json["ResponseMessage"].ToString());
                            if (this.label4.InvokeRequired)
                            {
                                SetTextCallback d = new SetTextCallback(SetText);
                                this.Invoke(d, new object[] { Environment.NewLine + "Value after poll: " + localCopy.ToString() });
                            }

                        }
                    }

                }
            }
            catch (Exception y)
            {
                clientSocket.Close();
            }
        }

        /// <summary>
        /// Sets the text to the label for displaying current status of server
        /// </summary>
        /// <param name="text">The text.</param>
        private void SetText(string text)
        {
            this.label4.Text = this.label4.Text + text + Environment.NewLine;
        }
    }
}
