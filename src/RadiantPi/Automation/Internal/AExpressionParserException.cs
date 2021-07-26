using System;

namespace RadiantPi.Automation.Internal {

    public abstract class AExpressionParserException : Exception {

        //--- Constructors ---
        protected AExpressionParserException(string message) : base(message) { }
    }

    public class ExpressionParserPropertyNotFoundException : AExpressionParserException {

        //--- Constructors ---
        public ExpressionParserPropertyNotFoundException(string name) : base($"property '{name}' does not exist") { }
    }

    public class ExpressionParserConditionNotFoundException : AExpressionParserException {

        //--- Constructors ---
        public ExpressionParserConditionNotFoundException(string name) : base($"condition '{name}' does not exist") { }
    }
}