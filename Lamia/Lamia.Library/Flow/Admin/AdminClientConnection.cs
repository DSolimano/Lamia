using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lamia.Library.Flow.Admin
{
    /// <summary>
    /// This class will take requests to subscribe and unsubscribe, and pass them to the daemon as necessary.
    /// 
    /// It also heartbeats with the daemon, so that both are aware of each other's state at all times.
    /// </summary>
    public class AdminClientConnection : IDisposable
    {
        private readonly string _daemonEndpoint = ">tcp://localhost:7500";

        public event EventHandler ConnectionStateChanged;

        NetMQActor _actor;
        NetMQTimer _housekeepingTimer;
        NetMQQueue<string> _queue;
        NetMQPoller poller;
        DealerSocket s;
        public AdminClientConnection()
        {
            s = new DealerSocket(_daemonEndpoint);
            s.ReceiveReady += S_ReceiveReady;
            s.Options.Identity = ASCIIEncoding.ASCII.GetBytes("baby");
            _housekeepingTimer = new NetMQTimer(new TimeSpan(0, 0, 5));
            _housekeepingTimer.Elapsed += _housekeepingTimer_Elapsed;
            _queue = new NetMQQueue<string>();
            _queue.ReceiveReady += _queue_ReceiveReady;
            poller = new NetMQPoller();
            poller.Add(s);
            poller.Add(_housekeepingTimer);
            poller.Add(_queue);
            poller.RunAsync();
        }

        bool haloSent = false;
        private void _housekeepingTimer_Elapsed(object sender, NetMQTimerEventArgs e)
        {
            if(!haloSent)
            {
                haloSent = true;
                Console.WriteLine("Sending HALO");
                s.SendFrame("HALO");
            }
            //throw new NotImplementedException();
        }

        private void S_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            string s = e.Socket.ReceiveFrameString();
            switch(s)
            {
                case "PING":
                    Console.WriteLine("Got PING");
                    e.Socket.SendFrame("PONG");
                    break;
                case "YOYO":
                    Console.WriteLine("Got YOYO");
                    break;
                default:
                    Console.WriteLine("Got ERROR");
                    break;
            }
        }

        private void _queue_ReceiveReady(object sender, NetMQQueueEventArgs<string> e)
        {
            throw new NotImplementedException();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    poller.Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AdminClientConnection() {
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
