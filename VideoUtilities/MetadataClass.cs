using System;
using System.Collections.Generic;

namespace VideoUtilities
{
    public class MetadataClass
    {
        public List<Program> programs { get; set; }
        public List<Stream> streams { get; set; }
        public Format format { get; set; }
    }

    public class Program
    {
        public Dictionary<object, object> dict { get; set; }
    }

    public class Stream
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Format
    {
        public TimeSpan duration { get; set; }
    }
    
}
