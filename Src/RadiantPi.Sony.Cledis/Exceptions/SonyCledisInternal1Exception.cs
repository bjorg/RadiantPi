namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisInternal1Exception : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisInternal1Exception() : base("err_internal1: A communication error occurred in the controller") { }
    }
}
