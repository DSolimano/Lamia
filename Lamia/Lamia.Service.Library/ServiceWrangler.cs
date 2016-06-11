using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lamia.Service.Library
{
    public class ServiceWrangler : IDisposable
    {
        /*
            encapsulates a send and receive pair for a service
            Also a proxy and a poller to drive it

            Send proxy
            send frontend is a bound tcp socket, backend is a pgm socket, control socket is bound as well
            we sniff messages off of the control socket and paste them into the receive proxy

            Recieve proxy
            Receive fronend is bound to pgm and also a random tcp socket (for replaying local messages)
            backend is a tcp socket
            no control socket
            
            */
            
        private Proxy _sendProxy;
        

        private Proxy _receiveProxy;
        private NetMQPoller _poller;

        private readonly int _frontendPortForSend;
        private readonly int _backendPortForReceive;
        private readonly ServiceDescription _description;

        private readonly SubscriberSocket _forwardingSubscriber;
        private readonly PublisherSocket _forwardingPublisher;


        public ServiceWrangler(ServiceDescription sd_)
        {
            _description = sd_;
            _poller = new NetMQPoller();

            
            //Establish send proxy
            XPublisherSocket sendBackend = new XPublisherSocket();
            int capturePort = sendBackend.BindRandomPort("tcp://localhost");
            XSubscriberSocket sendFrontend = new XSubscriberSocket();
            _frontendPortForSend = sendFrontend.BindRandomPort("tcp://localhost");

            _poller.Add(sendBackend);
            _poller.Add(sendFrontend);
            _sendProxy = new Proxy(sendFrontend, sendBackend, null, null, _poller);
            _sendProxy.Start();

            XSubscriberSocket receiveFrontend = new XSubscriberSocket();
            int republishPort = receiveFrontend.BindRandomPort("tcp://localhost");
            XPublisherSocket receiveBackend = new XPublisherSocket();
            _backendPortForReceive = receiveBackend.BindRandomPort("tcp://localhost");

            _poller.Add(receiveBackend);
            _poller.Add(receiveFrontend);
            _receiveProxy = new Proxy(receiveFrontend, receiveBackend, null, null, _poller);
            _receiveProxy.Start();

            _forwardingPublisher = new PublisherSocket();
            _forwardingPublisher.Connect("tcp://localhost:" + republishPort);
            _forwardingSubscriber = new SubscriberSocket();
            _forwardingSubscriber.Connect("tcp://localhost:" + capturePort);
            _forwardingSubscriber.ReceiveReady += _forwardingSubscriber_ReceiveReady;
            _forwardingSubscriber.SubscribeToAnyTopic();

            _poller.Add(_forwardingSubscriber);
            _poller.Add(_forwardingPublisher);

            _poller.RunAsync();
        }

        private void _forwardingSubscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            Console.WriteLine("_forwardingSubscriber_ReceiveReady - Hi");
            _forwardingPublisher.SendMultipartMessage(e.Socket.ReceiveMultipartMessage());
        }

        public ServiceSocketPair GetSocketPair()
        {
            PublisherSocket pub = new PublisherSocket();
            pub.Connect("tcp://localhost:" + _frontendPortForSend);

            SubscriberSocket sub = new SubscriberSocket();
            sub.Connect("tcp://localhost:" + _backendPortForReceive);
            return new ServiceSocketPair(_description, sub, pub);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _sendProxy.Stop();
                    _receiveProxy.Stop();
                    _poller.Stop();
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ServiceWrangler() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
