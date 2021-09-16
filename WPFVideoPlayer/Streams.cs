using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WPFVideoPlayer
{
    public class Streams
    {
        [XmlElement("stream")]
        public Stream[] Stream { get; set; }
    }
}
