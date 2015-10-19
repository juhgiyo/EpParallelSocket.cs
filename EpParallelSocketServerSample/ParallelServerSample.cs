using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EpParallelSocket.cs;
using EpServerEngine.cs;
using System.Diagnostics;

namespace EpParallelSocketServerSample
{
    public partial class ParallelServerSample : Form,IParallelServerCallback, IParallelSocketCallback
    {
        ParallelServer m_server = new ParallelServer();
        public ParallelServerSample()
        {
            InitializeComponent();
            tbPort.Text = "8088";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (btnStart.Text.CompareTo("Start")==0)
            {
                ParallelServerOps ops = new ParallelServerOps(this, tbPort.Text, ReceiveType.SEQUENTIAL);
                m_server.StartServer(ops);
            }
            else
            {
                m_server.StopServer();
            }
            
        }

        delegate void ChangeTitle_Invoke(bool isConnected);
        private void ChangeTitle(bool isConnected)
        {
            if (!btnStart.InvokeRequired)
            {
                if (isConnected)
                {
                    btnStart.Text = "Stop";
                    tbPort.Enabled = false;
                }
                else
                {
                    btnStart.Text = "Start";
                    tbPort.Enabled = true;
                }
            }
            else
            {
                ChangeTitle_Invoke CI = new ChangeTitle_Invoke(ChangeTitle);
                btnStart.Invoke(CI,isConnected);
            }
        }

        /// <summary>
        /// Server started callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="status">start status</param>
        public void OnServerStarted(IParallelServer server, StartStatus status)
        {
            if (status == StartStatus.SUCCESS || status == StartStatus.FAIL_ALREADY_STARTED)
            {
                Debug.Print("Server started");
                ChangeTitle(true);
            }
            
        }
        /// <summary>
        /// Accept callback
        /// </summary>
        /// <param name="server">server</param>
        /// <param name="ipInfo">connection info</param>
        /// <returns>the socket callback interface</returns>
        public IParallelSocketCallback OnAccept(IParallelServer server, IPInfo ipInfo, int streamCount)
        {
            return this;
        }
        /// <summary>
        /// Server stopped callback
        /// </summary>
        /// <param name="server">server</param>
        public void OnServerStopped(IParallelServer server)
        {
            Debug.Print("Server stopped");
            ChangeTitle(false);
        }

        /// <summary>
        /// NewConnection callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnNewConnection(IParallelSocket socket)
        {
            Debug.Print("socket connected");
        }

        /// <summary>
        /// Receive callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="receivedPacket">received packet</param>
        public void OnReceived(IParallelSocket socket, ParallelPacket receivedPacket)
        {
             string recvString=ASCIIEncoding.ASCII.GetString(receivedPacket.GetData().ToArray());
             Debug.Print("Received [" + receivedPacket.GetPacketID() + "] " + recvString);
            socket.Send(receivedPacket.GetData().ToArray());
        }

        /// <summary>
        /// Send callback
        /// </summary>
        /// <param name="socket">client socket</param>
        /// <param name="status">stend status</param>
        /// <param name="sentPacket">sent packet</param>
        public void OnSent(IParallelSocket socket, SendStatus status, ParallelPacket sentPacket)
        {
            string sentString = ASCIIEncoding.ASCII.GetString(sentPacket.GetData().ToArray());
            Debug.Print("Sent [" + sentPacket.GetPacketID() + "] " + sentString);
        }

        /// <summary>
        /// Disconnect callback
        /// </summary>
        /// <param name="socket">client socket</param>
        public void OnDisconnect(IParallelSocket socket)
        {
            Debug.Print("socket disconnected");
        }
    }
}
