using System;

namespace RadiantPi.Sony.Cledis.Exceptions {

    public abstract class ASonyCledisException : Exception {

        //--- Constructors ---
        protected ASonyCledisException() { }
        protected ASonyCledisException(string message) : base(message) { }
        protected ASonyCledisException(string message, Exception innerException) : base(message, innerException) { }
    }
}
