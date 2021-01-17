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
using System.IO.Ports;

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
    Console.WriteLine($"received: '{received}'");
};
Console.WriteLine($"Opening port {args[0]}");
port.Open();