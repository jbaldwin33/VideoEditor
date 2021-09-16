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
    public class Disposition
    {
        /// <summary>Default</summary>
        [XmlAttribute(AttributeName = "default")]
        public string Default { get; set; }

        /// <summary>Dub</summary>
        [XmlAttribute(AttributeName = "dub")]
        public string Dub { get; set; }

        /// <summary>Original</summary>
        [XmlAttribute(AttributeName = "original")]
        public string Original { get; set; }

        /// <summary>Comment</summary>
        [XmlAttribute(AttributeName = "comment")]
        public string Comment { get; set; }

        /// <summary>Lyrics</summary>
        [XmlAttribute(AttributeName = "lyrics")]
        public string Lyrics { get; set; }

        /// <summary>Karaoke</summary>
        [XmlAttribute(AttributeName = "karaoke")]
        public string Karaoke { get; set; }

        /// <summary>Forced</summary>
        [XmlAttribute(AttributeName = "forced")]
        public string Forced { get; set; }

        /// <summary>HearingImpaired</summary>
        [XmlAttribute(AttributeName = "hearing_impaired")]
        public string HearingImpaired { get; set; }

        /// <summary>VisualImpaired</summary>
        [XmlAttribute(AttributeName = "visual_impaired")]
        public string VisualImpaired { get; set; }

        /// <summary>CleanEffects</summary>
        [XmlAttribute(AttributeName = "clean_effects")]
        public string CleanEffects { get; set; }

        /// <summary>AttachedPic</summary>
        [XmlAttribute(AttributeName = "attached_pic")]
        public string AttachedPic { get; set; }

        /// <summary>TimedThumbnails</summary>
        [XmlAttribute(AttributeName = "timed_thumbnails")]
        public string TimedThumbnails { get; set; }
    }
}
