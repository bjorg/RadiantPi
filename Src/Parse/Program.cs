using System;
using SampleParser;

SampleRecord record = new() {
    BoolValue = false,
    StringValue = "abc"
};


// TODO: fix string comparison
// https://social.msdn.microsoft.com/Forums/vstudio/en-US/78d80818-b5a1-4bf8-8ace-863f5a1cc55f/we-are-doing-string-comparison-while-creating-dynamic-lambda-expressions-we-receive-an-integer-from

Print("F1", "BoolValue");
Print("F2", "'xyz' < StringValue");

void Print(string name, string expression) {
    var lambda = ExpressionParser.ParseExpression(name, expression);
    var compiled = (Func<SampleRecord, bool>)lambda.Compile();
    Console.WriteLine($"Result ({name}): {expression} => {compiled(record)}");
}

