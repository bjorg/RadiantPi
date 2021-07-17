using System;
using SampleParser;

SampleRecord record = new() {
    BoolValue = false,
    StringValue = "abc"
};

Print("F1", "BoolValue");
Print("F2", "StringValue < 'xyz' && true");

void Print(string name, string expression) {
    var lambda = ExpressionParser.ParseExpression(name, expression);
    var compiled = (Func<SampleRecord, bool>)lambda.Compile();
    Console.WriteLine($"Result ({name}): {expression} => {compiled(record)}");
}

