using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EpLibrary.cs;
using EpParallelSocket.cs;
using EpServerEngine.cs;
using System.Diagnostics;

namespace EpParallelSocketClientSample
{
    public partial class ParallelClientSample : Form, IParallelClientCallback
    {
        ParallelClient m_client = new ParallelClient();
        int m_count = 0;
        string m_sendText = "";


        public ParallelClientSample()
        {
            InitializeComponent();
            tbHostname.Text = "localhost";
            tbPort.Text = "8088";
            tbSendText.Text = "Hello";
            tbCount.Text = "100";
        }
        
        private void btnSend_Click(object sender, EventArgs e)
        {
            m_count = Convert.ToInt32(tbCount.Text);
            m_sendText = tbSendText.Text;
            if (m_client.IsConnectionAlive)
                m_client.Disconnect();
            ParallelClientOps ops=new ParallelClientOps(this,tbHostname.Text,tbPort.Text,ReceiveType.SEQUENTIAL,20);
            m_client.Connect(ops);

        }

        /// <summary>
        /// Connection callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">connection status</param>
        public void OnConnected(IParallelClient client, ConnectStatus status)
        {
            if (status == ConnectStatus.SUCCESS)
            {
                Debug.Print("Connected: " + client.Guid.ToString());

                Task t = new Task(delegate()
                {
                    for (int i = 0; i < m_count; i++)
                    {
                        byte[] array = Encoding.ASCII.GetBytes("[" + i + "] " + m_sendText);
                        client.Send(array);
                    }
                });
                t.Start();
            }
            else
            {
                Debug.Print(status.ToString());
            }
            
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(IParallelClient client, ParallelPacket receivedPacket)
        {
            string recvString=ASCIIEncoding.ASCII.GetString(receivedPacket.GetData().ToArray());
            Debug.Print("Received [" + receivedPacket.GetPacketID() + "] " + recvString);
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="client">client</param>
        /// <param name="status">send status</param>
        /// <param name="sentPacket">sent packet</param>
        public void OnSent(IParallelClient client, SendStatus status, ParallelPacket sentPacket)
        {
            string sentString = ASCIIEncoding.ASCII.GetString(sentPacket.GetData().ToArray());
            Debug.Print("Sent [" + sentPacket.GetPacketID() + "] " + sentString);
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="client">client</param>
        public void OnDisconnect(IParallelClient client)
        {
            Debug.Print("Disconnected");
        }
    }
}
