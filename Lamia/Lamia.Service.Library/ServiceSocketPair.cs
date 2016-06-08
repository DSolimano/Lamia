using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lamia.Service.Library
{
    public class ServiceSocketPair
    {
        private ServiceDescription _description; public ServiceDescription Description { get { return _description; } }
        private PublisherSocket _sendSocket; public PublisherSocket Pub { get { return _sendSocket; } }
        private SubscriberSocket _receiveSocket; public SubscriberSocket Sub { get { return _receiveSocket; } }

        public ServiceSocketPair(ServiceDescription description_, SubscriberSocket receiveSocket_, PublisherSocket sendSocket_)
        {
            _description = description_;
            _sendSocket = sendSocket_;
            _receiveSocket = receiveSocket_;
        }
    }
}
