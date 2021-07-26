namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisCommandValueException : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisCommandValueException() : base("err_val: The value set using a command is out of the range") { }
    }
}
