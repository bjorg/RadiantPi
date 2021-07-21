using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RadiantPi.Core.Telnet {

    public sealed class TelnetClient : ITelnet {

        //--- Class Methods ---
        private static async Task WaitForMessages(
            TcpClient tcpClient,
            StreamReader streamReader,
            Action<string> messageReceived,
            CancellationTokenSource cancellationToken
        ) {
            try {
                while(true) {

                    // check if cancelation token is set
                    if(cancellationToken.IsCancellationRequested) {

                        // operation was canceled
                        break;
                    }

                    // attempt to read from socket
                    try {
                        if(!tcpClient.Connected) {

                            // client is no longer connected
                            break;
                        }
                        var message = await streamReader.ReadLineAsync().ConfigureAwait(false);
                        if(message == null) {

                            // found end of stream
                            break;
                        }

                        // ignore empty messages
                        if(!string.IsNullOrWhiteSpace(message)) {
                            messageReceived(message);
                        }
                    } catch(ObjectDisposedException) {

                        // nothing to do: underlying stream was closed and disposed
                        break;
                    } catch(IOException) {

                        // nothing to do: underlying stream was disconnected
                        break;
                    } catch {

                        // TODO: add mechanism for reporting asynchronous exceptions
                        break;
                    }
                }
            } finally {
                streamReader.Close();
            }
        }

        //--- Fields ---
        private readonly int _port;
        private readonly string _host;
        private CancellationTokenSource _internalCancellation;
        private TcpClient _tcpClient;
        private StreamWriter _streamWriter;
        private bool _disposed = false;

        //--- Constructors ---
        public TelnetClient(string host, int port) {
            _host = host;
            _port = port;
        }

        //--- Event Handlers ---
        public event EventHandler<TelnetMessageReceivedEventArgs> MessageReceived;

        //--- Properties ---
        public TelnetConnectionHandshakeAsync ConfirmConnectionAsync { get; set; }

        //--- Methods ---
        public async Task SendAsync(string message) {
            if(_disposed) {
                throw new ObjectDisposedException("TelnetClient");
            }

            // open connection
            await ConnectAsync().ConfigureAwait(false);

            // Send command + params
            await _streamWriter.WriteLineAsync(message).ConfigureAwait(false);
        }

        public void Disconnect() {
            if(_tcpClient == null) {

                // nothing to do
                return;
            }

            // cancel socket read operations
            try {

                _internalCancellation?.Cancel();
                _internalCancellation = null;
            } catch {

                // nothing to do
            }

            // close write stream
            try {
                _streamWriter?.Close();
                _streamWriter = null;
            } catch {

                // nothing to do
            }

            // close socket
            try {
                _tcpClient?.Close();
                _tcpClient = null;
            } catch {

                // nothing to do
            }
        }

        public void Dispose() => Dispose(true);

        private async Task ConnectAsync() {

            // check if socket is already connected
            if(_tcpClient?.Connected ?? false) {
                return;
            }

            // cancel any previous listener
            _internalCancellation?.Cancel();
            _internalCancellation = new();

            // initialize a new client
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(_host, _port).ConfigureAwait(false);

            // initialize reader/writer streams
            _streamWriter = new(_tcpClient.GetStream()) {
                AutoFlush = true
            };

            // notify that a connection is opening
            StreamReader streamReader = new(_tcpClient.GetStream());
            if(ConfirmConnectionAsync != null) {
                await ConfirmConnectionAsync(this, streamReader, _streamWriter).ConfigureAwait(false);
            }

            // wait for messages to arrive
            _ = WaitForMessages(
                _tcpClient,
                streamReader,
                OnMessageReceived,
                _internalCancellation
            );
        }

        private void OnMessageReceived(string message) => MessageReceived?.Invoke(this, new TelnetMessageReceivedEventArgs {
            Message = message
        });

        private void Dispose(bool disposing) {
            if(_disposed) {
                return;
            }
            if(disposing) {
                Disconnect();
            }
            _disposed = true;
        }
    }
}