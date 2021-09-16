using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WPFVideoPlayer
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Tag
    {
        /// <summary>Key</summary>
        [XmlAttribute(AttributeName = "key")]
        public string Key { get; set; }

        /// <summary>Value</summary>
        [XmlAttribute(AttributeName = "value")]
        public string Value { get; set; }
    }
}
