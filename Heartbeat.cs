using System;

namespace WakaTime
{
    internal class Heartbeat
    {
        public string entity { get; set; }
        public long timestamp { get; set; }
        public string project { get; set; }
        public bool is_write { get; set; }

        public Heartbeat()
        {
        }

        internal Heartbeat(Heartbeat h)
        {
            entity = h.entity;
            timestamp = h.timestamp;
            project = h.project;
            is_write = h.is_write;
        }
    }
}
