/*
 * RadiantPi.Tool - Command line tool for Lumagen RadiancePro
 * Copyright (C) 2020 - Steve G. Bjorg
 *
 * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Affero General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with this program. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

Console.WriteLine("RadiantPi Tool");
Console.WriteLine();

// list available serial ports
if(args.Length == 0) {
    Console.WriteLine("Available ports");
    foreach(var portName in SerialPort.GetPortNames()) {
        Console.WriteLine($"* {portName}");
    }
    return;
}

// open port
var port = new SerialPort {
    PortName = args[0],
    BaudRate = 9600,
    DataBits = 8,
    Parity = Parity.None,
    StopBits = StopBits.One,
    Handshake = Handshake.None,
    ReadTimeout = 1_000,
    WriteTimeout = 1_000
};

// open port and wait for user to exit or port to be closed
Console.WriteLine($"Opening port {args[0]} (Press ESC to stop)");
port.Open();

// add handler for receiving bytes
ReceiveData(port, buffer => {
    var received = BytesToString(buffer);
    Console.WriteLine($"received: '{received}'");
});
try {

    // send data to initiate communication
    await WriteAsync(port, "ZQI22");

    // listen on port until closed or user exits
    while(port.IsOpen) {

        // check if user pressed the ESC key
        if(Console.KeyAvailable && (Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)) {
            break;
        }
        Thread.Sleep(500);
    }
    if(port.IsOpen) {
        Console.WriteLine("Closing port");
        port.Close();
    } else {
        Console.WriteLine("Port was closed remotely");
    }
} finally {
    port.Dispose();
}

// local functions
static string BytesToString(IEnumerable<byte> bytes) => string.Join("", bytes.Select(b => b switch {
    >= 32 and < 128 => ((char)b).ToString(),
    (byte)'\r' => "\\r",
    (byte)'\n' => "\\n",
    _ => $"\\u{b:X4}"
}));

static async Task WriteAsync(SerialPort port, string command) {
    var bytes = Encoding.ASCII.GetBytes(command);
    Console.WriteLine($"sending: '{BytesToString(bytes)}'");
    await port.BaseStream.WriteAsync(bytes, 0, bytes.Length);
}

static async void ReceiveData(SerialPort port, Action<byte[]> callback) {
    var blockLimit = 64;
    byte[] buffer = new byte[blockLimit];
    try {
    again:

        // initiate an asynchronous read operation
        var actualLength = await port.BaseStream.ReadAsync(buffer, 0, buffer.Length);
        if(actualLength == 0) {

            // we're done
            return;
        }

        // copy received data to a new buffere and invoke callback
        byte[] received = new byte[actualLength];
        Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
        callback(received);

        // continue receiving more data
        goto again;
    } catch(OperationCanceledException) {

        // nothing to do
    } catch(Exception e) {
        Console.WriteLine($"ERROR ReadAsync(): {e}");
    }
}