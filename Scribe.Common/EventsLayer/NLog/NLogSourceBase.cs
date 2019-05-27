using System;
using Newtonsoft.Json;

namespace Scribe.EventsLayer.NLog
{
    public abstract class NLogSourceBase : ILogSource<NLogEvent>
    {
        public abstract IObservable<NLogEvent> Source { get; }

        protected NLogEvent Deserialize(string msg)
        {
            try
            {
                return JsonConvert.DeserializeObject<NLogEvent>(msg);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
