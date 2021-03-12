using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Quelle.Listener
{
    public class Listener
    {
        TcpListener server = null;
        private Listener(uint port)
        {
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            server = new TcpListener(localAddr, (int)port);
            server.Start();
            StartListener();
        }
        private void StartListener()
        {
            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for a connection...");
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    Thread t = new Thread(new ParameterizedThreadStart(HandleDeivce));
                    t.Start(client);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                server.Stop();
            }
        }
        private void HandleDeivce(Object obj)
        {
            TcpClient client = (TcpClient)obj;
            var stream = client.GetStream();
            Byte[] length = new Byte[4];
            Byte[] bytes = new Byte[0xFFF];
            UInt32 len;
            try
            {
                for (; ; )
                {
                    for (len = 0; len < 4; /**/)
                    {
                        UInt32 cnt = (UInt32)stream.Read(length, 0, 4);
                        if (cnt < 1)
                            return;
                        len += cnt;
                    }
                    UInt32 size = 0;
                    if (len == 4)
                    {
                        if (length[0] == '@')
                        {
                            string cnt = Encoding.ASCII.GetString(bytes, 1, 3);
                            size = UInt32.Parse(cnt);
                        }
                        else if (length[0] == 0) // max size supported is 0x0FFF
                        {
                            for (int i = 1; i < 4; i++)
                            {
                                size *= 0x10;
                                size += length[i];
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    for (len = 0; len < size; /**/)
                    {
                        UInt32 cnt = (UInt32)stream.Read(bytes, (int) len, (int) (size - len));
                        if (cnt < 1)
                            return;
                        len += cnt;
                    }
                    string okay = "ok\n";
                    Byte[] reply = System.Text.Encoding.ASCII.GetBytes(okay);
                    stream.Write(reply, 0, reply.Length);

                    var data = Encoding.ASCII.GetString(bytes, 0, (int)len);

                    Console.WriteLine("{1}: Read: {0}", data, Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
                client.Close();
            }
        }
        public static Thread Start(uint port)
        {
            Thread t = new Thread(delegate ()
            {
            // replace the IP with your system IP Address...
            var listen = new Listener(port);
            });
            t.Start();

            Console.WriteLine("Listener Started...");

            return t;
        }
        public static void Stop(Dictionary<uint, Thread> listeners)
        {
            foreach (uint port in listeners.Keys)
            {
                try
                {
                    var thread = listeners[port];
                    thread.Abort();
                    Console.WriteLine("Listener Aborted on thread " + port.ToString());
                }
                catch
                {
                    Console.WriteLine("Error Aborting Listener on port " + port.ToString() + " !!!");
                }
            }
        }
    }
}