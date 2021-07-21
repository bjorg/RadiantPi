namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisInternal2Exception : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisInternal2Exception() : base("err_internal2: A communication error occurred in the controller") { }
    }
}
