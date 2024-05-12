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

        private readonly object _lock = new object();
        private const int LogMaxLines = 500;

        public void AddToLog(string message)
        {
            this.SafeInvoke(() =>
            {
                lock (_lock)
                {
                    // add this line at the top of the log
                    listBox1.Items.Add(message);

                    // keep only a few lines in the log
                    while (listBox1.Items.Count > LogMaxLines)
                    {
                        listBox1.Items.RemoveAt(0);
                    }

                    // Automatically scroll to the last added item
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                }
            }, false);
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
            AddToLog("Starting ..");
            SendMessageToService("start");
        }

        private void stopServerBtn_Click(object sender, EventArgs e)
        {
            AddToLog("Stopping ..");
            SendMessageToService("stop");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
