namespace RadiantPi.Sony.Cledis.Exceptions {

    public class SonyCledisAuthenticationException : ASonyCledisException {

        //--- Constructors ---
        public SonyCledisAuthenticationException() : base("err_auth: The authentication during start of network communication failed") { }
    }
}
