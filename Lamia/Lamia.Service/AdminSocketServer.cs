using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

namespace Lamia.Service
{
    public class AdminSocketServer : IDisposable
    {
        private readonly string _adminPort = "tcp://*:7500";

        private RouterSocket requestSocket;
        NetMQPoller poller;
        NetMQTimer timer;

        private IDictionary<string, ClientInfo> _clientInfo = new Dictionary<string, ClientInfo>();

        public AdminSocketServer()
        {
            requestSocket = new RouterSocket();
            requestSocket.Bind(_adminPort);
            requestSocket.ReceiveReady += RequestSocket_ReceiveReady;

            poller = new NetMQPoller();
            poller.Add(requestSocket);

            timer = new NetMQTimer(new TimeSpan(0, 0, 5));
            timer.Elapsed += Timer_Elapsed;
            poller.Add(timer);

            poller.RunAsync();
        }

        private void Timer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            Console.WriteLine("In Timer_Elapsed");
            foreach (var pair in _clientInfo)
            {
                requestSocket.SendFrame(pair.Key, true);
                pair.Value.PingSent();
                requestSocket.SendFrame("PING", false);
            }

        }

        private class ClientInfo
        {
            private readonly string _id; public string ID { get { return _id; } }
            public ClientInfo(string id_)
            {
                _id = id_;
            }
            private readonly int _pingCutoff = 5;
            private int _pingsSentNoAck = 0;
            public void PingSent()
            {
                _pingsSentNoAck++;
            }

            public bool IsClientNotResponding()
            {
                return _pingsSentNoAck > _pingCutoff;
            }

            public void PongReceived()
            {
                _pingsSentNoAck = 0;
            }
        }

        private ClientInfo AddClient(string id_)
        {
            return _clientInfo[id_] = new ClientInfo(id_);
        }

        private void RemoveClient(ClientInfo client)
        {
            _clientInfo.Remove(client.ID);
        }

        private void RequestSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Console.WriteLine("In RequestSocket_ReceiveReady");
            string id = e.Socket.ReceiveFrameString();
            Console.WriteLine(id);
            string body;
            if (e.Socket.TryReceiveFrameString(out body))
            {
                switch (body)
                {
                    case "HALO":
                        Console.WriteLine("Got HALO");
                        AddClient(id);
                        e.Socket.SendFrame(id, true);
                        //e.Socket.SendFrameEmpty(true);
                        e.Socket.SendFrame("YOYO", false);
                        break;
                    case "PONG":
                        Console.WriteLine("Got PONG");
                        _clientInfo[id].PongReceived();
                        break;
                    default:
                        Console.WriteLine("Got bad msg " + body);
                        break;
                }

                //should be nothing else - check
                byte[] bytes;
                while (e.Socket.TryReceiveFrameBytes(out bytes))
                {
                    Console.WriteLine("Got bad frame");
                    //do nothing
                }
            }
            else
            {
                //bad message
            }
        }

        public void Dispose()
        {
            poller.Stop();
            poller.Dispose();
            requestSocket.Dispose();
        }
    }
}
