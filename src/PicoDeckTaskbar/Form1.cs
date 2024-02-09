using System.IO.Pipes;
using System.Text;

namespace PicoDeckTaskbar
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
            Resize += Form1_Resize;
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void Form1_Resize(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void SendMessageToService(string message)
        {
            try
            {
                using var client = new NamedPipeClientStream("PicoDeckServicePipe");
                client.Connect(500);
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                client.Write(messageBytes, 0, messageBytes.Length);
                client.Flush();
            }
            catch (Exception)
            {
            }
        }

        private void startServerBtn_Click(object sender, EventArgs e)
        {
            SendMessageToService("start");
        }

        private void stopServerBtn_Click(object sender, EventArgs e)
        {
            SendMessageToService("stop");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
