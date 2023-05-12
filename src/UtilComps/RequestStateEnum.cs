using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace brain_ghplugin.UtilComps
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
