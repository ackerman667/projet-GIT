using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IrToyLibrary;
using System.Threading;

namespace IrToyExample {

    public class IrToyExample {

        private static readonly string kenwood_leiser = "01 a0 00 d5 00 17 00 1c 00 18 00 1c 00 18 00 1b 00 18 00 50 00 19 00 4f 00 19 00 51 00 17 00 1b 00 18 00 50 00 18 00 1c 00 18 00 1c 00 18 00 1c 00 17 00 1c 00 18 00 1b 00 19 00 1c 00 17 00 1c 00 18 00 1c 00 18 00 1c 00 17 00 50 00 19 00 1b 00 18 00 51 00 18 00 4f 00 19 00 1c 00 18 00 1c 00 17 00 50 00 19 00 4f 00 18 00 1c 00 18 00 4f 00 19 00 1c 00 18 00 1c 00 17 00 50 00 19 00 4f 00 18 00 1b 00 18 08 18 01 9f 00 6b 00 18 ff ff";
    	private static readonly string kenwood_lauter = "01 a0 00 d4 00 18 00 1b 00 17 00 1c 00 18 00 1c 00 18 00 4e 00 19 00 50 00 17 00 50 00 19 00 1b 00 17 00 50 00 19 00 1c 00 17 00 1c 00 18 00 1c 00 18 00 1c 00 17 00 1c 00 18 00 1b 00 18 00 1b 00 17 00 1c 00 18 00 50 00 17 00 50 00 18 00 1c 00 17 00 50 00 18 00 4e 00 19 00 1b 00 18 00 1b 00 17 00 4f 00 19 00 1b 00 17 00 1c 00 18 00 50 00 17 00 1c 00 18 00 1c 00 18 00 4f 00 18 00 4f 00 18 00 1b 00 18 08 18 01 a1 00 6b 00 17 ff ff";


        public static void Main(string[] args) {

            Console.WriteLine("START");

            IrToySettings settings = new IrToySettings { ComPort = 3, UseHandshake = true, UseNotifyOnComplete = true, UseTransmitCount = true };
            IrToyLib lib = IrToyLib.Connect(settings);

            lib.Send(kenwood_leiser);
            Thread.Sleep(500);

            lib.Send(kenwood_leiser);
            Thread.Sleep(500);

            lib.Send(kenwood_lauter);
            Thread.Sleep(500);

            lib.Close();

            Console.WriteLine("ENDE");
            Console.Read();
        }

    }
}
