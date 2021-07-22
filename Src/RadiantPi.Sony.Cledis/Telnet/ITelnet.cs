using System;
using System.IO;
using System.Threading.Tasks;

namespace RadiantPi.Sony.Telnet {

    public sealed class TelnetMessageReceivedEventArgs : EventArgs {

        //--- Properties ---
        public string Message { get; init; }
    }

    public delegate Task TelnetConnectionHandshakeAsync(ITelnet client, TextReader reader, TextWriter writer);

    public interface ITelnet : IDisposable {

        //--- Event Handlers ---
        event EventHandler<TelnetMessageReceivedEventArgs> MessageReceived;

        //--- Properties ---
        TelnetConnectionHandshakeAsync ConfirmConnectionAsync { get; set; }

        //--- Methods ---
        Task SendAsync(string message);

        // TODO: add method to close connection rather than waiting for it to timeout
    }
}