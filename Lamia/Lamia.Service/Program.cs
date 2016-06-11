using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using Lamia.Library.Flow.Admin;
using Lamia.Service.Library;

namespace Lamia.Service
{
    static class Program
    {
        


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Tester t = new Tester();
            Console.ReadLine();
            Console.WriteLine(t);

            if (args.Length > 0 && args[0] == "-console")
            {

                //so, how do we get full duplex flow between these two, so we can send or receive?
                // we will generate a random port pair per service for send/receive,bind both,
                // and the client will connect to both
                using (AdminSocketServer server = new AdminSocketServer())
                {
                    Console.WriteLine("Press enter to exit");
                    using (AdminClientConnection con = new AdminClientConnection())
                    {
                        Console.ReadLine();
                    }
                }
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

        //internal class ServiceDescriptor
        //{
        //    private int _serviceNumber;
        //}

        //internal class NetworkDescriptor
        //{
        //    private string _networkDescription;
        //}

        //internal class ServiceBinding
        //{
        //    private ServiceDescriptor _service;
        //    private NetworkDescriptor _network;
        //    private PublisherSocket _pub;
        //    private SubscriberSocket _sub;
        //}

        private class Tester
        {
            NetMQPoller poller;
            NetMQTimer timer;
            ServiceSocketPair pari;

            PublisherSocket pub;
            SubscriberSocket sub;
            ServiceWrangler wrangler;
            public Tester()
            {
                poller = new NetMQPoller();

                SetUpRealProxy();
                //SetUpDummyPRoxy();

                timer = new NetMQTimer(new TimeSpan(0, 0, 5));
                timer.Elapsed += Timer_Elapsed;
                poller.Add(timer);

                

                poller.RunAsync();
                Console.ReadLine();
                poller.Dispose();
                
                if(null != wrangler)
                {
                    wrangler.Dispose();
                }

            }

            private void SetUpRealProxy()
            {
                wrangler = new ServiceWrangler(new ServiceDescription());
                pari = wrangler.GetSocketPair();

                poller.Add(pari.Pub);
                poller.Add(pari.Sub);

                pari.Sub.ReceiveReady += Sub_ReceiveReady;
                pari.Sub.SubscribeToAnyTopic();
            }

            private void SetUpDummyPRoxy()
            {
                //Establish send proxy
                XPublisherSocket sendBackend = new XPublisherSocket();
                //sendBackend.Bind("tcp://*:1234");
                //sendBackend.Bind("tcp://localhost:1234");
                int sendBackendPort = sendBackend.BindRandomPort("tcp://localhost");
                XSubscriberSocket sendFrontend = new XSubscriberSocket();
                sendFrontend.Bind("tcp://*:5678");
                poller.Add(sendBackend);
                poller.Add(sendFrontend);
                var _sendProxy = new Proxy(sendFrontend, sendBackend, null, null, poller);
                _sendProxy.Start();

                sub = new SubscriberSocket();
                //sub.Connect("tcp://localhost:1234");
                sub.Connect("tcp://localhost:" + sendBackendPort);
                sub.SubscribeToAnyTopic();
                sub.ReceiveReady += Sub_ReceiveReady;
                poller.Add(sub);

                pub = new PublisherSocket();
                pub.Connect("tcp://localhost:5678");
                poller.Add(pub);
            }

            private void Sub_ReceiveReady(object sender, NetMQSocketEventArgs e)
            {
                var strings = e.Socket.ReceiveMultipartStrings();
                Console.WriteLine("IN Sub_ReceiveReady");
                foreach(var s in strings)
                {
                    Console.WriteLine(s);
                }
            }

            private void Timer_Elapsed(object sender, NetMQTimerEventArgs e)
            {
                Console.WriteLine("In Timer_Elapsed");

                if (null != pub)
                {
                    pub.SendFrame("DSOL.Tick.IBM", true);
                    pub.SendFrame("100", false);
                }
                else
                {
                    pari.Pub.SendFrame("DSOL.Tick.IBM", true);
                    pari.Pub.SendFrame("100", false);
                }
            }
        }
    }
}
