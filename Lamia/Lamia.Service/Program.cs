using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace Lamia.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (NetMQContext context = NetMQContext.Create())
            {
                if (args.Length > 0 && args[0] == "-console")
                {
                    RouterSocket requestSocket = context.CreateRouterSocket();
                    requestSocket.Bind("tcp://*:7500");
                    requestSocket.ReceiveReady += RequestSocket_ReceiveReady;
                    Poller poller = new Poller(new[] { requestSocket });

                    NetMQTimer timer = new NetMQTimer(new TimeSpan(0, 0, 5));
                    timer.Elapsed += Timer_Elapsed;
                    poller.AddTimer(timer);

                    poller.PollTillCancelledNonBlocking();

                    //so, how do we get full duplex flow between these two, so we can send or receive?
                    // we will generate a random port pair per service for send/receive,bind both,
                    // and the client will connect to both

                    Console.WriteLine("Press enter to exit");
                    DealerSocket dealerSocket = context.CreateDealerSocket();
                    dealerSocket.Connect("tcp://localhost:7500");
                    
                    for(int i = 0; i < 10; i ++)
                    {
                        dealerSocket.SendFrameEmpty(true);
                        dealerSocket.SendFrame("Foo " + i);
                    }
                    Console.ReadLine();
                    poller.CancelAndJoin();

                    poller.Dispose();

                    requestSocket.Dispose();
                }
                else
                {
                    throw new NotImplementedException();
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                new Service1()
                    };
                    ServiceBase.Run(ServicesToRun);
                }

                
            }
        }

        internal class ServiceDescriptor
        {
            private int _serviceNumber;
        }

        internal class NetworkDescriptor
        {
            private string _networkDescription;
        }

        internal class ServiceBinding
        {
            private ServiceDescriptor _service;
            private NetworkDescriptor _network;
            private PublisherSocket _pub;
            private SubscriberSocket _sub;
        }

        private static void Timer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            Console.WriteLine("In Timer_Elapsed");
            
        }

        private static void RequestSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Console.WriteLine("In RequestSocket_ReceiveReady");
            string id = e.Socket.ReceiveFrameString();
            Console.WriteLine(id);
            string del = e.Socket.ReceiveFrameString();//delimeter
            if(!string.IsNullOrEmpty(del))
            {
                //bad message;
                byte[] bytes;
                while(e.Socket.TryReceiveFrameBytes(out bytes))
                {
                    //do nothing
                }
            }
            Console.WriteLine(del);
            string body;
            if (e.Socket.TryReceiveFrameString(out body))
            {

            }
            else
            {
                //bad message
            }
        }
    }
}
