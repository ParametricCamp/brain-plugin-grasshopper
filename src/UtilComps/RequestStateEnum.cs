using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brain.UtilComps
{
    enum RequestState
    {
        Off, 
        Idle, 
        Requesting, 
        Done,
        Error
    };
}
