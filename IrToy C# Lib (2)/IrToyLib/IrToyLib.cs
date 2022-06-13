using System;
using System.Text;
using System.IO.Ports;

namespace IrToyLibrary {

    /**
     * IrToyLib
     * A C# library for the IrToy (v2) from DangerousPrototypes.com
     * Written by: SeeliSoft.net
     * Version 1.0
     **/

    public class IrToyLib {

        /**
         * Variables
         **/

        private IrToySettings settings;
        private SerialPort serialPort;

        private readonly int IRTOY_BUFFER_SIZE = 62;
        private readonly byte[] CMD_RESET = new byte[] { 0, 0, 0, 0, 0 };                                           // 5 x '0x00' 
        private readonly byte[] CMD_SAMPLEMODE = new byte[] { Convert.ToByte( Convert.ToInt32('s') ) };		        // 's'
        private readonly byte[] CMD_VERSION = new byte[] { Convert.ToByte(Convert.ToInt32('v')) };                  // 'v'
        private readonly byte[] CMD_TRANSMIT = new byte[] { Convert.ToByte(Convert.ToInt32("03", 16)) };            // 0x03
        private readonly byte[] CMD_BYTE_COUNT_REPORT = new byte[] { Convert.ToByte(Convert.ToInt32("24", 16)) };   // 0x24           
        private readonly byte[] CMD_NOTIFY_ON_COMPLETE = new byte[] { Convert.ToByte(Convert.ToInt32("25", 16)) };	// 0x25
        private readonly byte[] CMD_HANDSHAKE = new byte[] { Convert.ToByte( Convert.ToInt32("26", 16)) };          // 0x26
        private readonly static String REQUIRED_VERSION = "V222";
	    private readonly static String REQUIRED_SAMPLEMODE = "S01";


        /**
         * Public API
         **/

        /// <summary>
        /// Initializes a new connection with the default settings.
        /// </summary>
        /// <param name="comPort">Specifies the COM-Port to be used.</param>
        public static IrToyLib Connect(byte comPort) {
            IrToySettings settings = new IrToySettings {
                ComPort = comPort,
                UseHandshake = true,
                UseNotifyOnComplete = true,
                UseTransmitCount = true
            };
            return new IrToyLib(settings);
        }


        /// <summary>
        /// Initializes a new connection with specific settings.
        /// </summary>
        /// <param name="settings">Specifies the IrToy settings.</param>
        public static IrToyLib Connect(IrToySettings settings) {
            return new IrToyLib(settings);
        }


        /// <summary>
        /// Clean up the connection and release ressources.
        /// </summary>
        public void Close() {
            if (serialPort != null) {
                serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
        }


        /// <summary>
        /// Sends a synchronous command. The command must terminate with "ff ff".
        /// Example: "00 01 a3 bf ac e8 89 ff ff"
        /// </summary>
        /// <param name="command"></param>
        public void Send(string command) {
            try {
                sendInternal(command);
            } catch (Exception e) {
                Close();
                throw e;
            }
        }


        /**
          * Internal Methods
          **/

        // Private constructor: use static 'Connect' method
        private IrToyLib(IrToySettings settings) {
            this.settings = settings;

            serialPort = new SerialPort();
            serialPort.PortName = "COM" + settings.ComPort;
            serialPort.BaudRate = 115200;
            serialPort.ReadTimeout = 3000;
            serialPort.Open();

            // Send Reset
            sendRawData(CMD_RESET);

            // Check version
            sendRawData(CMD_VERSION);
            string version = readString(4);
            if (version != REQUIRED_VERSION) {
                throw new IrToyException("The returned version '" + version + "' does not match with the required version '" + REQUIRED_VERSION + "'.");
            }

            // Enter sample Mode
            sendRawData(CMD_SAMPLEMODE);
            string sampleMode = readString(3);
            if (sampleMode != REQUIRED_SAMPLEMODE) {
                throw new IrToyException("Sample mode did not respond with '" + REQUIRED_SAMPLEMODE + "', received: " + sampleMode);
            }

            // Set settings
            if (settings.UseTransmitCount) {
                sendRawData(CMD_BYTE_COUNT_REPORT);
            }
            if (settings.UseNotifyOnComplete) {
                sendRawData(CMD_NOTIFY_ON_COMPLETE);
            }
            if (settings.UseHandshake) {
                sendRawData(CMD_HANDSHAKE);
            }
        }

        private void sendInternal(string command) {
            if (serialPort == null) {
                throw new IrToyException("The connection has been closed.");
            }

            if (!command.EndsWith(" ff ff")) {
                throw new IrToyException("The command does not end with 'ff ff'.");
            }

            // Send Transmit command
            sendRawData(CMD_TRANSMIT);
            readHandshake();

            byte[] cmd = getCommandBytes(command);
            for (int i = 0; i < cmd.Length; i = i + IRTOY_BUFFER_SIZE) {
                int len = Math.Min(cmd.Length - i, IRTOY_BUFFER_SIZE);
                byte[] bytesToSend = new byte[len];
                Array.Copy(cmd, i, bytesToSend, 0, len);
                sendRawData(bytesToSend);
                readHandshake();
            }

            readTransmitCount(cmd.Length);

            readNotifyOnComplete();
        }

        private byte[] getCommandBytes(string cmd) {
            // Source: http://msdn.microsoft.com/de-de/library/bb311038.aspx
            int len = (cmd.Length + 1) / 3;
            byte[] output = new byte[len];

            string[] hex = cmd.Split(' ');
            for (int i = 0; i < hex.Length; i++) {
                int intValue = Convert.ToInt32(hex[i], 16);
                output[i] = Convert.ToByte(intValue);
            }
            return output;
        }

        private void sendRawData(byte[] data) {
            serialPort.Write(data, 0, data.Length);
        }

        private byte[] readRawData(int numBytes) {
            byte[] buffer = new byte[numBytes];
            for (int i = 0; i < numBytes; i++) {
                buffer[i] = (byte)serialPort.ReadByte();
            }
            return buffer;
        }

        private string readString(int length) {
            byte[] response = readRawData(length);
            return Encoding.Default.GetString(response);
        }

        private void readHandshake() {
            if (!settings.UseHandshake) return;

            byte[] response = readRawData(1);
            if (response[0] != IRTOY_BUFFER_SIZE) {
                Close();
                throw new IrToyException("The handshake did not respond with the expected value.");
            }
        }

        private void readTransmitCount(int expectedLength) {
            if (!settings.UseTransmitCount) return;

            byte[] response = readRawData(3);

            if (response[0] != 't') {
                throw new IrToyException("Transmit count did not respond with the expected format.");
            }

            int bytesSent = response[2] < 0 ? response[2] + 256 : response[2];
            bytesSent = bytesSent + (response[1] * 256);
            if (bytesSent != expectedLength) {
                throw new IrToyException("Transmit count did not respond with the expected value. Actual: " + bytesSent + ", expected: " + expectedLength);
            }
        }

        private void readNotifyOnComplete() {
            if (!settings.UseNotifyOnComplete) return;

            // Read the response
            byte[] response = readRawData(1);
            if (response[0] != 'C') {
                throw new IrToyException("Notify on complete did not respond as expected.");
            }
        }
    }
}
