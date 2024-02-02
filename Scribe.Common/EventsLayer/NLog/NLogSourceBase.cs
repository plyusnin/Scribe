using System;
using System.Net;
using Newtonsoft.Json;

namespace Scribe.EventsLayer.NLog
{
    public abstract class NLogSourceBase : ILogSource<NLogEvent>
    {
        public abstract IObservable<NLogEvent> Source { get; }

        protected NLogEvent Deserialize(string EventText, string Sender)
        {
            try
            {
                var ev = JsonConvert.DeserializeObject<NLogEvent>(EventText);
                ev.Sender = Sender;
                return ev;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
