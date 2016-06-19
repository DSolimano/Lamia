using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lamia.Service.Library
{
    public class ServiceDescription
    {
        private readonly int _service; public int Service { get { return _service; } }
        private readonly string _network; public string Network { get { return _network; } }
        private readonly string _interface; public string Interface { get { return _interface; } }

        private readonly string _pgmString; public string PgmString { get { return _pgmString; } }

        //TODO separate send and receive strings
        public ServiceDescription(int service_, string network_, string interface_)
        {
            _service = service_;
            _network = network_;
            _interface = interface_;

            _pgmString = string.Format("pgm://{0};{1}:{2}", _interface, _network, _service);
        }
    }
}
