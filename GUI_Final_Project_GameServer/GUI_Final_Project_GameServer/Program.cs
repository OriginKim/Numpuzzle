using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GUI_Final_Project_GameServer
{
    internal class Program
    {
        private static List<Socket> clientSockets = new List<Socket>();
        private static TcpListener listener;
        private static readonly IPAddress ip = IPAddress.Parse("192.168.0.208");
        private static readonly int port = 5000;
        private static List<NetworkStream> streams = new List<NetworkStream>();
        private static bool[] clientsReady = new bool[3]; // 세 클라이언트의 준비 상태를 추적

        static void Main(string[] args)
        {
            listener = new TcpListener(ip, port);
            listener.Start();

            Console.WriteLine("플레이어를 기다리는 중...");

            while (clientSockets.Count < 3)
            {
                Socket clientSocket = listener.AcceptSocket();
                clientSockets.Add(clientSocket);
                Console.WriteLine($"플레이어 {clientSockets.Count} 연결됨");
                streams.Add(new NetworkStream(clientSocket));
            }

            // 클라이언트 핸들링 시작
            for (int i = 0; i < clientSockets.Count; i++)
            {
                int index = i;
                Thread clientThread = new Thread(() => HandleClient(clientSockets[index], streams[index], index));
                clientThread.Start();
            }
        }

        static void HandleClient(Socket clientSocket, NetworkStream stream, int clientIndex)
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"(클라이언트에서 보냄)\n플레이어{clientIndex + 1}: {message}"); // 디버깅 메시지
                    if (message == "start")
                    {
                        clientsReady[clientIndex] = true;
                        Console.WriteLine($"플레이어 {clientIndex + 1} 준비 완료!");

                        if (clientsReady[0] && clientsReady[1] && clientsReady[2])
                        {
                            foreach (var s in streams)
                            {
                                SendMessage(s, "start");
                            }
                            Console.WriteLine("모든 플레이어 준비 완료, 게임 시작!");
                        }
                    }
                    else if (message == "GameOver")
                    {
                        Console.WriteLine($"플레이어 {clientIndex + 1} 승리!");
                        for (int i = 0; i < streams.Count; i++)
                        {
                            if (i == clientIndex)
                            {
                                SendMessage(streams[i], "win");
                            }
                            else
                            {
                                SendMessage(streams[i], "lose");
                            }
                        }
                        // Regame 질문을 모든 클라이언트에게 보냄
                        foreach (var s in streams)
                        {
                            SendMessage(s, "regame");
                        }
                    }
                    else if (message.StartsWith("regame:"))
                    {
                        if (message == "regame:yes")
                        {
                            clientsReady[clientIndex] = false;
                            Console.WriteLine($"플레이어 {clientIndex + 1} 재시작 준비 완료!");
                            // 모든 클라이언트가 재시작에 동의했는지 확인
                            bool allReady = true;
                            foreach (bool ready in clientsReady)
                            {
                                if (ready)
                                {
                                    allReady = false;
                                    break;
                                }
                            }
                            if (allReady)
                            {
                                foreach (var s in streams)
                                {
                                    SendMessage(s, "start");
                                }
                            }
                        }
                        else if (message == "regame:no")
                        {
                            Console.WriteLine($"플레이어 {clientIndex + 1} 게임 종료!");
                            Environment.Exit(0); // 서버 종료
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                clientSocket.Close();
            }
        }

        static void SendMessage(NetworkStream stream, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }

    }
}
