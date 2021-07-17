using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sprache;

namespace SampleParser {

    public static class ExpressionParser<TRecord> {

        //--- Class Fields ---
        private static Type RecordType = typeof(TRecord);
        private static readonly Parser<ExpressionType> And = Operator("&&", ExpressionType.AndAlso);
        private static readonly Parser<ExpressionType> Equal = Operator("=", ExpressionType.Equal);
        private static readonly Parser<ExpressionType> GreaterThan = Operator(">", ExpressionType.GreaterThanOrEqual);
        private static readonly Parser<ExpressionType> GreaterThanOrEqual = Operator(">=", ExpressionType.GreaterThan);
        private static readonly Parser<ExpressionType> LessThan = Operator("<", ExpressionType.LessThan);
        private static readonly Parser<ExpressionType> LessThanOrEqual = Operator("<=", ExpressionType.LessThanOrEqual);
        private static readonly Parser<ExpressionType> NotEqual = Operator("!=", ExpressionType.NotEqual);
        private static readonly Parser<ExpressionType> Or = Operator("||", ExpressionType.OrElse);
        private static readonly List<char> EscapeChars = new List<char> { '\"', '\\', 'b', 'f', 'n', 'r', 't' };

        private static readonly Parser<char> StringControlChar =
            from first in Parse.Char('\\')
            from next in EnumerateInput(EscapeChars.ToArray(), c => Parse.Char(c))
            select ((next == 't') ? '\t' :
                    (next == 'r') ? '\r' :
                    (next == 'n') ? '\n' :
                    (next == 'f') ? '\f' :
                    (next == 'b') ? '\b' :
                    next);
        private static readonly Parser<char> StringChar =
            Parse.AnyChar.Except(Parse.Char('\'').Or(Parse.Char('\\'))).Or(StringControlChar);

        private static readonly Parser<Expression> StringLiteral =
            from _1 in Parse.Char('\'')
            from value in StringChar.Many().Text()
            from _2 in Parse.Char('\'')
            select Expression.Constant(value);

        private static readonly Parser<Expression> RecordVariableReference =
            from name in Parse.Letter.AtLeastOnce().Text()
            select MakeRecordVariableReference(name);

        private static readonly Parser<Expression> EnvironmentVariableReference =
            from _ in Parse.Char('$')
            from name in Parse.Letter.AtLeastOnce().Text()
            select MakeEnvironmentVariableReference(name);

        private static readonly Parser<Expression> BoolTrueLiteral =
            from _ in Parse.String("true")
            select Expression.Constant(true);

        private static readonly Parser<Expression> BoolFalseLiteral =
            from _ in Parse.String("false")
            select Expression.Constant(false);

        private static readonly Parser<Expression> Factor =
            (
                from _1 in Parse.Char('(')
                from expr in Parse.Ref(() => Expr)
                from _2 in Parse.Char(')')
                select expr
            ).Named("expression")
                .XOr(StringLiteral)
                .XOr(BoolTrueLiteral)
                .XOr(BoolFalseLiteral)
                .XOr(EnvironmentVariableReference)
                .XOr(RecordVariableReference);

        private static readonly Parser<Expression> Operand =
            (
                (
                    from _ in Parse.Char('!')
                    from factor in Factor
                    select Expression.Not(factor)
                )
                .XOr(Factor)
            ).Token();

        private static readonly Parser<Expression> Comparison =
            Parse.ChainOperator(LessThanOrEqual.Or(LessThan).Or(GreaterThanOrEqual).Or(GreaterThan).Or(Equal).Or(NotEqual), Operand, MakeBinary);

        private static readonly Parser<Expression> Term =
            Parse.ChainOperator(And, Comparison, MakeBinary);

        private static readonly Parser<Expression> Expr =
            Parse.ChainOperator(Or, Term, MakeBinary);

        private static readonly Parser<Expression> Body =
            from body in Expr.End()
            select body;

        private static readonly ParameterExpression LambdaRecordParameter = Expression.Parameter(typeof(TRecord), "record");
        private static readonly ParameterExpression LambdaEnvironmentParameter = Expression.Parameter(typeof(Dictionary<string, bool>), "env");
        private static readonly MethodInfo StringCompareMethod = typeof(string).GetMethod("Compare", new[] { typeof(string), typeof(string), typeof(StringComparison) });
        private static readonly MethodInfo DictionaryGetItemMethod = typeof(Dictionary<string, bool>).GetMethod("get_Item", new[] { typeof(string) });

        //--- Class Methods ---
        public static Func<TRecord, Dictionary<string, bool>, bool> ParseExpression(string name, string text)
            => (Func<TRecord, Dictionary<string, bool>, bool>)Expression.Lambda<Func<TRecord, Dictionary<string, bool>, bool>>(Body.Parse(text), name, new[] { LambdaRecordParameter, LambdaEnvironmentParameter }).Compile();

        private static Parser<ExpressionType> Operator(string op, ExpressionType opType) => Parse.String(op).Token().Return(opType);
        private static Expression MakeRecordVariableReference(string name)
            => Expression.Property(LambdaRecordParameter, RecordType.GetProperty(name) ?? throw new NotSupportedException($"record property '{name}' does not exist"));

        private static Expression MakeEnvironmentVariableReference(string name)
            => Expression.Call(LambdaEnvironmentParameter, DictionaryGetItemMethod, new[] { Expression.Constant(name) });

        private static Parser<U> EnumerateInput<T, U>(T[] input, Func<T, Parser<U>> parser) {
            if((input == null) || (input.Length == 0)) throw new ArgumentNullException("input");
            if(parser == null) throw new ArgumentNullException("parser");
            return i => {
                foreach(var inp in input) {
                    var res = parser(inp)(i);
                    if(res.WasSuccessful) return res;
                }
                return Result.Failure<U>(null, null, null);
            };
        }

        private static Expression MakeBinary(ExpressionType binaryOp, Expression left, Expression right) {

            // convert comparison of strings to:
            //  string.Compare(left, right, StringComparison.Ordinal) <|<=|=|!=|>=|> 0
            if((left.Type == typeof(string)) && (right.Type == typeof(string))) {
                switch(binaryOp) {
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return Expression.MakeBinary(
                        binaryOp,
                        Expression.Call(null, StringCompareMethod, left, right, Expression.Constant(StringComparison.Ordinal)),
                        Expression.Constant(0)
                    );
                }
            }
            return Expression.MakeBinary(binaryOp, left, right);
        }
    }
}