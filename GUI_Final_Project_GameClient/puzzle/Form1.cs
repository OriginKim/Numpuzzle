using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace GUI_Final_Project_GameClient
{
    public partial class GUI_Final_Project_GameClient : Form
    {
        int inNullSliceIndex, inmoves = 0;
        List<Bitmap> lstOriginalPictureList = new List<Bitmap>();
        System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        TcpClient client;
        NetworkStream stream;
        Thread listenThread;

        public GUI_Final_Project_GameClient()
        {
            InitializeComponent();
            lstOriginalPictureList.AddRange(new Bitmap[] { Properties.Resources._1, Properties.Resources._2, Properties.Resources._3, Properties.Resources._4, Properties.Resources._5, Properties.Resources._6, Properties.Resources._7, Properties.Resources._8, Properties.Resources._9, Properties.Resources._null });
            labelMove.Text += inmoves;
            labelTime.Text = "00:00:00";
            SetInitialVisibility();

            ConnectToServer();
        }

        private void SetInitialVisibility()
        {
            groupPuzzleBox.Visible = false;
            labelMove.Visible = false;
            labelTime.Visible = false;
            labelGameExplain.Visible = true;
           
            groupBoxOriginal.Visible = false;
            buttonGameReady.Visible = true; // 게임 시작 버튼을 표시
        }

        private void ConnectToServer()
        {
            try
            {
                client = new TcpClient("192.168.0.208", 5000);
                stream = client.GetStream();
                listenThread = new Thread(ListenForMessages);
                listenThread.Start();
            }
            catch
            {
                MessageBox.Show("서버에 연결하지 못했습니다.");
            }
        }

        private void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Message received: {message}"); // 디버깅 메시지
                    if (message == "start")
                    {
                        this.Invoke((MethodInvoker)delegate {
                            StartGame();
                        });
                    }
                    else if (message == "win")
                    {
                        this.Invoke((MethodInvoker)delegate {
                            GameOver("축하합니다! 승리!\n계속하시겠습니까?");
                            ResetGame();
                        });
                    }
                    else if (message == "lose")
                    {
                        this.Invoke((MethodInvoker)delegate {
                            GameOver("아쉽습니다! 패배!\n계속하시겠습니까?");
                        });
                    }
                    else if (message == "regame")
                    {
                        this.Invoke((MethodInvoker)delegate {
                            AskForReGame();
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void StartGame()
        {
            buttonGameReady.Visible = false;
            groupPuzzleBox.Visible = true;
            labelMove.Visible = true;
            labelTime.Visible = true;
            labelGameExplain.Visible = false;
            groupBoxOriginal.Visible = true;
            
            timer.Reset();
            timer.Start();
        }


        private void GameOver(string message)
        {
            timer.Stop();
            var result = MessageBox.Show(message, "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SendMessage("regame");
                ResetGame();
            }
            else
            {
                SendMessage("regame:no");
                Application.Exit();
            }
        }

        private void AskForReGame()
        {
            var result = MessageBox.Show("다시 게임에 참여 하시겠습니까?", "Game Over", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                SendMessage("regame:yes");
                ResetGame();
            }
            else
            {
                SendMessage("regame:no");
                Application.Exit();
            }
        }

        private void ResetGame()
        {
            SetInitialVisibility();
            Shuffle();
            inmoves = 0;
            labelMove.Text = "이동 횟수 : 0";
            labelTime.Text = "00:00:00";
            timer.Reset();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Shuffle();
        }

        void Shuffle()
        {
            do
            {
                int j;
                List<int> Index = new List<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 9 });
                Random r = new Random();
                for (int i = 0; i < 9; i++)
                {
                    Index.Remove(j = Index[r.Next(0, Index.Count)]);
                    ((PictureBox)groupPuzzleBox.Controls[i]).Image = lstOriginalPictureList[j];
                    if (j == 9) { inNullSliceIndex = i; }
                }
            }
            while (CheckWin());
        }

        private void btnShuffle_Click(object sender, EventArgs e)
        {
            DialogResult YesOrNo = new DialogResult();
            if (labelTime.Text != "00:00:00")
            {
                YesOrNo = MessageBox.Show("정말 다시 시작하시겠습니까?", "맞춰라 퍼즐", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
            if (YesOrNo == DialogResult.Yes || labelTime.Text == "00:00:00")
            {
                Shuffle();
                timer.Reset();
                labelTime.Text = "00:00:00";
                inmoves = 0;
                labelMove.Text = "이동 횟수 : 0";
            }
        }

        private void AskPermissionBeforeQuite(object sender, FormClosingEventArgs e)
        {
            DialogResult YesOrNO = MessageBox.Show("정말 종료하시겠습니까?", "맞춰라 퍼즐", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            AskPermissionBeforeQuite(sender, e as FormClosingEventArgs);
        }

        private void SwitchPictureBox(object sender, EventArgs e)
        {
            if (labelTime.Text == "00:00:00")
                timer.Start();
            int inPictureBoxIndex = groupPuzzleBox.Controls.IndexOf(sender as Control);
            if (inNullSliceIndex != inPictureBoxIndex)
            {
                List<int> FourPictures = new List<int>(new int[] { ((inPictureBoxIndex % 3 == 0) ? -1 : inPictureBoxIndex - 1), inPictureBoxIndex - 3, (inPictureBoxIndex % 3 == 2) ? -1 : inPictureBoxIndex + 1, inPictureBoxIndex + 3 });
                if (FourPictures.Contains(inNullSliceIndex))
                {
                    ((PictureBox)groupPuzzleBox.Controls[inNullSliceIndex]).Image = ((PictureBox)groupPuzzleBox.Controls[inPictureBoxIndex]).Image;
                    ((PictureBox)groupPuzzleBox.Controls[inPictureBoxIndex]).Image = lstOriginalPictureList[9];
                    inNullSliceIndex = inPictureBoxIndex;
                    labelMove.Text = "이동 횟수 : " + (++inmoves);
                    if (CheckWin())
                    {
                        SendMessage("GameOver"); // 게임 종료 메시지 전송
                        Console.WriteLine("GameOver message sent"); // 디버깅 메시지
                        timer.Stop();
                        (groupPuzzleBox.Controls[8] as PictureBox).Image = lstOriginalPictureList[8];

                        
                        inmoves = 0;
                        labelMove.Text = "Moves Made : 0";
                        labelTime.Text = "00:00:00";
                        timer.Reset();
                    }
                }
            }
        }

        bool CheckWin()
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                if ((groupPuzzleBox.Controls[i] as PictureBox).Image != lstOriginalPictureList[i]) break;
            }
            if (i == 8)
            {
                
                return true;
            }
            else
            {
                
                return false;
            }
        }

        private void UpdateTimeElapsed(object sender, EventArgs e)
        {
            if (timer.Elapsed.ToString() != "00:00:00")
            {
                labelTime.Text = timer.Elapsed.ToString().Remove(8);
            }
            
        }

        private void SendMessage(string message)
        {
            if (stream != null)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                Console.WriteLine($"Message sent: {message}"); // 디버깅 메시지
            }
        }
        private void gbPuzzleBox_Enter(object sender, EventArgs e)
        {

        }

        private void gbOriginal_Enter(object sender, EventArgs e)
        {

        }

        private void buttonGameStart_Click(object sender, EventArgs e)
        {
            SendMessage("start");
        }

        
    }
}
