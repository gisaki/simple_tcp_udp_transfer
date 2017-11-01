using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace tcp_udp_transfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // 
        // フォームの表示関連
        // 

        private void Form1_Load(object sender, EventArgs e)
        {
            // 三角形
            {
                //描画先とするImageオブジェクトを作成する
                Bitmap canvas = new Bitmap(pictureBox1.Width, pictureBox1.Height);
                //ImageオブジェクトのGraphicsオブジェクトを作成する
                Graphics g = Graphics.FromImage(canvas);

                //直線で接続する点の配列を作成
                int padding = 2;
                Point[] ps = {new Point(0 + padding, 0 + padding),
                 new Point(pictureBox1.Width - padding, 0 + padding),
                 new Point(pictureBox1.Width / 2, pictureBox1.Height - padding),
                 new Point(0 + padding, 0 + padding)};
                //多角形を描画する
                g.FillPolygon(Brushes.Pink, ps, FillMode.Winding);

                //リソースを解放する
                g.Dispose();

                //PictureBox1に表示する
                pictureBox1.Image = canvas;
            }
        }

        private System.Net.Sockets.UdpClient udpClient_ = null;
//        private IPAddress sourceIPAddress_;
        private int sourcePort_;

        private IPAddress sendIPAddress_;
        private int sendPort_;

        private byte[] lastRcvBytes_;
        private System.Net.IPEndPoint lastRemoteEP_ = null;

        // 
        // UDP送受信関連
        // 

        // UDP受信
        private void buttonStart_Click(object sender, EventArgs e)
        {
            // 
            // 実施中→終了
            // 
            if (udpClient_ != null)
            {
                udpClient_.Close();
                udpClient_ = null;
                // ボタン等
                change_ui(false);
                return;
            }

            // 
            // 未実施→実施
            // 

            // 送信元、送信先
            try
            {
                sourcePort_ = Int32.Parse(textBoxBindPort.Text);
                sendPort_ = Int32.Parse(textBoxSendPort.Text);
                sendIPAddress_ = IPAddress.Parse(textBoxSendIPAddress.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // 開始せず中断
            }

            try
            {
                //UdpClientを作成し、指定したポート番号にバインドする
                System.Net.IPEndPoint localEP =
                    new System.Net.IPEndPoint(
                        System.Net.IPAddress.Any, //sourceIPAddress_, 
                        Int32.Parse(textBoxBindPort.Text)
                    );
                udpClient_ = new System.Net.Sockets.UdpClient(localEP);
                //非同期的なデータ受信を開始する
                udpClient_.BeginReceive(ReceiveCallback, udpClient_);
                // ボタン等
                change_ui(true);
            }
            catch (Exception ex)
            {
                if (udpClient_ != null)
                {
                    udpClient_.Close();
                }
                udpClient_ = null;

                MessageBox.Show(ex.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        //データを受信した時
        private void ReceiveCallback(IAsyncResult ar)
        {
            System.Net.Sockets.UdpClient udp =
                (System.Net.Sockets.UdpClient)ar.AsyncState;

            //非同期受信を終了する
            System.Net.IPEndPoint remoteEP = null;
            byte[] rcvBytes;
            try
            {
                rcvBytes = udp.EndReceive(ar, ref remoteEP);
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                Console.WriteLine("受信エラー({0}/{1})",
                    ex.Message, ex.ErrorCode);
                return;
            }
            catch (ObjectDisposedException ex)
            {
                //すでに閉じている時は終了
                Console.WriteLine("Socketは閉じられています。");
                return;
            }

            // 受信情報を控えておく（送信データに載せるため）
            // 受信データ
            lastRcvBytes_ = new byte[rcvBytes.Length];
            Array.Copy(rcvBytes, 0, lastRcvBytes_, 0, rcvBytes.Length);
            // 受信元
            lastRemoteEP_ = remoteEP;

            // 転送する
            SendUDP(sendIPAddress_, sendPort_); // 送信先に転送する場合

            //再びデータ受信を開始する
            udp.BeginReceive(ReceiveCallback, udp);
        }

        // UDP送信
        private void SendUDP(IPAddress remoteIPAddress, int remotePort)
        {
            //UdpClientオブジェクトを作成する
            System.Net.Sockets.UdpClient udp =
                new System.Net.Sockets.UdpClient();

            // 送信データ
            byte[] msg = null;
            // 送信データは受信データと同一
            msg = new byte[lastRcvBytes_.Length];
            Array.Copy(lastRcvBytes_, 0, msg, 0, lastRcvBytes_.Length);

            // 送信元は規定しない
            // 送信先
            //リモートホストを指定してデータを送信する
            udp.Send(msg, msg.Length, remoteIPAddress.ToString(), remotePort);

            //UdpClientを閉じる
            udp.Close();
        }

        public void change_ui(Boolean start)
        {
            if (start)
            {
                buttonStart.Text = "Abort UDP Listen";
                textBoxBindPort.Enabled = false;
                textBoxSendIPAddress.Enabled = false;
                textBoxSendPort.Enabled = false;
            }
            else
            {
                buttonStart.Text = "Start UDP Listen";
                textBoxBindPort.Enabled = true;
                textBoxSendIPAddress.Enabled = true;
                textBoxSendPort.Enabled = true;
            }
        }

    }
}
