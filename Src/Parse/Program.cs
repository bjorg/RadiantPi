using System;
using System.Collections.Generic;
using SampleParser;



SampleRecord record = new() {
    BoolValue = false,
    StringValue = "abc"
};

var env = new Dictionary<string, bool> {
    ["IsGameInput"] = false
};

Print("F1", "BoolValue");
Print("F2", "StringValue < 'xyz' && true");
Print("F2", "StringValue < 'xyz' && !$IsGameInput");


void Print(string name, string expression) {
    var lambda = ExpressionParser<SampleRecord>.ParseExpression(name, expression);
    Console.WriteLine($"Result ({name}): {expression} => {lambda(record, env)}");
}

