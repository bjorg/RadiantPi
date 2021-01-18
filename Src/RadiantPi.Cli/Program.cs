/*
 * RadiantPi.Cli - Command line tool for Lumagen RadiancePro
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

Console.WriteLine("RadiantPi CLI");
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
port.DataReceived += (sender, args) => {
    var received = ((SerialPort)sender).ReadExisting();
    Console.WriteLine($"received: '{string.Join("", received.Select(EscapeChar))}'");
};
Console.WriteLine($"Opening port {args[0]} (Press ESC to stop)");
port.Open();
try {

    // send data to initiate communication
    Write("ZQI23");

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
string EscapeChar(char c) => c switch {
    >= (char)32 and < (char)128 => c.ToString(),
    '\r' => "\\r",
    '\n' => "\\n",
    _ => $"\\u{(int)c:X4}"
};

void Write(string command) {
    var bytes = Encoding.UTF8.GetBytes(command);
    port.Write(bytes, 0, bytes.Length);

}