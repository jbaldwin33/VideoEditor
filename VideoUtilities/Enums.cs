using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoUtilities.Enums
{
    public class Enums
    {
        public enum FormatEnum
        {
            wmv,
            avi,
            mpg,
            mpeg,
            mp4,
            mov,
            m4a,
            mkv,
            ts
        }

        public enum ScaleRotate
        {
            NoSNoR,
            NoS90R,
            NoS180R,
            NoS270R,
            SNoR,
            S90R,
            S180R,
            S270R
        }
    }
}
