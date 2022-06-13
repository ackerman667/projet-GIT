using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IrToyLibrary {
    
    public class IrToySettings {
        public byte ComPort { get; set; }
        public bool UseHandshake { get; set; }
        public bool UseNotifyOnComplete { get; set; }
        public bool UseTransmitCount { get; set; }
    }

}
