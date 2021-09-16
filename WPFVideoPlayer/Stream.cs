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
    public class Stream
    {
        /// <summary>Index</summary>
        [XmlAttribute(AttributeName = "index")]
        public string Index { get; set; }

        /// <summary>CodecName</summary>
        [XmlAttribute(AttributeName = "codec_name")]
        public string CodecName { get; set; }

        /// <summary>CodecLongName</summary>
        [XmlAttribute(AttributeName = "codec_long_name")]
        public string CodecLongName { get; set; }

        /// <summary>Profile</summary>
        [XmlAttribute(AttributeName = "profile")]
        public string Profile { get; set; }

        /// <summary>CodecType</summary>
        [XmlAttribute(AttributeName = "codec_type")]
        public string CodecType { get; set; }

        /// <summary>CodecTimeBase</summary>
        [XmlAttribute(AttributeName = "codec_time_base")]
        public string CodecTimeBase { get; set; }

        /// <summary>CodecTagString</summary>
        [XmlAttribute(AttributeName = "codec_tag_string")]
        public string CodecTagString { get; set; }

        /// <summary>CodecTag</summary>
        [XmlAttribute(AttributeName = "codec_tag")]
        public string CodecTag { get; set; }

        /// <summary>Width</summary>
        [XmlAttribute(AttributeName = "width")]
        public string Width { get; set; }

        /// <summary>Height</summary>
        [XmlAttribute(AttributeName = "height")]
        public string Height { get; set; }

        /// <summary>CodedWidth</summary>
        [XmlAttribute(AttributeName = "coded_width")]
        public string CodedWidth { get; set; }

        /// <summary>CodedHeight</summary>
        [XmlAttribute(AttributeName = "coded_height")]
        public string CodedHeight { get; set; }

        /// <summary>HasBFrames</summary>
        [XmlAttribute(AttributeName = "has_b_frames")]
        public string HasBFrames { get; set; }

        /// <summary>SampleAspectRatio</summary>
        [XmlAttribute(AttributeName = "sample_aspect_ratio")]
        public string SampleAspectRatio { get; set; }

        /// <summary>DisplayAspectRatio</summary>
        [XmlAttribute(AttributeName = "display_aspect_ratio")]
        public string DisplayAspectRatio { get; set; }

        /// <summary>PixFmt</summary>
        [XmlAttribute(AttributeName = "pix_fmt")]
        public string PixFmt { get; set; }

        /// <summary>Level</summary>
        [XmlAttribute(AttributeName = "level")]
        public string Level { get; set; }

        /// <summary>ColorRange</summary>
        [XmlAttribute(AttributeName = "color_range")]
        public string ColorRange { get; set; }

        /// <summary>ColorSpace</summary>
        [XmlAttribute(AttributeName = "color_space")]
        public string ColorSpace { get; set; }

        /// <summary>ColorTransfer</summary>
        [XmlAttribute(AttributeName = "color_transfer")]
        public string ColorTransfer { get; set; }

        /// <summary>ColorPrimaries</summary>
        [XmlAttribute(AttributeName = "color_primaries")]
        public string ColorPrimaries { get; set; }

        /// <summary>ChromaLocation</summary>
        [XmlAttribute(AttributeName = "chroma_location")]
        public string ChromaLocation { get; set; }

        /// <summary>Refs</summary>
        [XmlAttribute(AttributeName = "refs")]
        public string Refs { get; set; }

        /// <summary>IsAvc</summary>
        [XmlAttribute(AttributeName = "is_avc")]
        public string IsAvc { get; set; }

        /// <summary>NalLengthSize</summary>
        [XmlAttribute(AttributeName = "nal_length_size")]
        public string NalLengthSize { get; set; }

        /// <summary>RFrameRate</summary>
        [XmlAttribute(AttributeName = "r_frame_rate")]
        public string RFrameRate { get; set; }

        /// <summary>AvgFrameRate</summary>
        [XmlAttribute(AttributeName = "avg_frame_rate")]
        public string AvgFrameRate { get; set; }

        /// <summary>TimeBase</summary>
        [XmlAttribute(AttributeName = "time_base")]
        public string TimeBase { get; set; }

        /// <summary>StartPts</summary>
        [XmlAttribute(AttributeName = "start_pts")]
        public string StartPts { get; set; }

        /// <summary>StartTime</summary>
        [XmlAttribute(AttributeName = "start_time")]
        public string StartTime { get; set; }

        /// <summary>DurationTs</summary>
        [XmlAttribute(AttributeName = "duration_ts")]
        public string DurationTs { get; set; }

        /// <summary>Duration</summary>
        [XmlAttribute(AttributeName = "duration")]
        public string Duration { get; set; }

        /// <summary>BitRate</summary>
        [XmlAttribute(AttributeName = "bit_rate")]
        public string BitRate { get; set; }

        /// <summary>BitsPerRawSample</summary>
        [XmlAttribute(AttributeName = "bits_per_raw_sample")]
        public string BitsPerRawSample { get; set; }

        /// <summary>NbFrames</summary>
        [XmlAttribute(AttributeName = "nb_frames")]
        public string NbFrames { get; set; }

        [XmlElement("disposition")]
        public Disposition Disposition { get; set; }

        [XmlElement("tag")]
        public Tag[] Tags { get; set; }
    }
}
