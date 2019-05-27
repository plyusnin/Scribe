using System.Threading;
using System.Threading.Tasks;

namespace Scribe.EventsLayer
{
    public interface ILogFileOpener
    {
        string Description { get; }
        string Extension { get; }
        Task OpenFileAsync(string FileName, CancellationToken Cancellation);
    }
}
