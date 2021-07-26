namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisCommandInactiveException : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisCommandInactiveException() : base("err_inactive: A command is temporarily invalidated") { }
    }
}
