namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisCommandUnrecognizedException : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisCommandUnrecognizedException() : base("err_cmd: No command can be recognized") { }
    }
}
