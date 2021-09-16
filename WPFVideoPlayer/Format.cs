using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WPFVideoPlayer
{
    public class Format
    {
        /// <summary>Filename</summary>
        [XmlAttribute(AttributeName = "filename")]
        public string Filename { get; set; }

        /// <summary>NbStreams</summary>
        [XmlAttribute(AttributeName = "nb_streams")]
        public string NbStreams { get; set; }

        /// <summary>NbPrograms</summary>
        [XmlAttribute(AttributeName = "nb_programs")]
        public string NbPrograms { get; set; }

        /// <summary>FormatName</summary>
        [XmlAttribute(AttributeName = "format_name")]
        public string FormatName { get; set; }

        /// <summary>FormatLongName</summary>
        [XmlAttribute(AttributeName = "format_long_name")]
        public string FormatLongName { get; set; }

        /// <summary>StartTime</summary>
        [XmlAttribute(AttributeName = "start_time")]
        public string StartTime { get; set; }

        /// <summary>Duration</summary>
        [XmlAttribute(AttributeName = "duration")]
        public string Duration { get; set; }

        /// <summary>Size</summary>
        [XmlAttribute(AttributeName = "size")]
        public string Size { get; set; }

        /// <summary>BitRate</summary>
        [XmlAttribute(AttributeName = "bit_rate")]
        public string BitRate { get; set; }

        /// <summary>ProbeScore</summary>
        [XmlAttribute(AttributeName = "probe_score")]
        public string ProbeScore { get; set; }

        /// <summary>Tags</summary>
        [XmlElement("tag")]
        public Tag[] Tags { get; set; }
    }
}
