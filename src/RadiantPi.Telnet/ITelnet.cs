/*
 * RadiantPi - Web app for controlling a Lumagen RadiancePro from a RaspberryPi device
 * Copyright (C) 2020-2021 - Steve G. Bjorg
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
using System.IO;
using System.Threading.Tasks;

namespace RadiantPi.Telnet {

    public sealed class TelnetMessageReceivedEventArgs : EventArgs {

        //--- Properties ---
        public string Message { get; init; }
    }

    public delegate Task TelnetConnectionHandshakeAsync(ITelnet client, TextReader reader, TextWriter writer);

    public interface ITelnet : IDisposable {

        //--- Events ---
        event EventHandler<TelnetMessageReceivedEventArgs> MessageReceived;

        //--- Properties ---
        TelnetConnectionHandshakeAsync ConfirmConnectionAsync { get; set; }

        //--- Methods ---
        Task<bool> ConnectAsync();
        Task SendAsync(string message);

        // TODO: add method to close connection rather than waiting for it to timeout
    }
}