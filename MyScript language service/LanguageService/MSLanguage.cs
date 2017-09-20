using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.VisualStudio.Package;

/*
 * Regroups some constants of the language, like keywords, built in types and such.
 * Other components may refer to it to get consistent data across all parts of the software.
 * */
namespace MyCompany.LanguageServices.MyScript
{
    public class MSLanguage
    {
        static string[] m_builtinTypes =
        {
            "bool",
            "float",
            "int",
            "string",
        };

        static public string[] BuiltinTypes { get { return m_builtinTypes; } }
        
        static string[] m_keywords =
        {
			"and",
			"break",
            "bool",
            //"class",
            "do",
            "else",
            "end",
            "false",
            "float",
            //"for",
            "function",
            "if",
            //"in",
            "int",
			"import",
            //"local",
            //"namespace",
            "null",
            "not",
            "or",
            "return",
            "string",
            "then",
            "true",
			//"typedef",
			//"using",
			"void",
            "while",
        };
        static public string[] Keywords { get { return m_keywords; } }


        public enum OperatorType
        {
            Addition,
            Subtraction,
            Multiply,
            Divide,
            Modulo,
            LeftShift,
            RightShift,
            Lesser,
            Greater,
            LesserOrEqual,
            GreaterOrEqual,
            Equal,
            NotEqual,
            LogicalNot,
            LogicalAnd,
            LogicalOr,
            BitwiseNot,
            BitwiseAnd,
            BitwiseOr,
            Assignment,
            AddAssignment,
            SubAssignment,
            MulAssignment,
            DivAssignment,
            ModAssignment,
            LeftShiftAssignment,
            RightShiftAssignment,
            BitwiseNotAssignment,
            BitwiseAndAssignment,
            BitwiseOrAssignment,
        }
        public class BinaryOperator
        {
            public string Text { get; set; }
            public int Precedence { get; set; }
            public bool LeftAssociative { get; set; }
            public OperatorType Type { get; set; }
        }
        static BinaryOperator[] m_operators =
        {
			//	arithmetic
            new BinaryOperator() { Text = "+", LeftAssociative = true, Precedence = 0, Type = OperatorType.Addition },
            new BinaryOperator() { Text = "-", LeftAssociative = true, Precedence = 0, Type = OperatorType.Subtraction },
            new BinaryOperator() { Text = "*", LeftAssociative = true, Precedence = 0, Type = OperatorType.Multiply },
            new BinaryOperator() { Text = "/", LeftAssociative = true, Precedence = 0, Type = OperatorType.Divide },
            new BinaryOperator() { Text = "%", LeftAssociative = true, Precedence = 0, Type = OperatorType.Modulo },
            new BinaryOperator() { Text = "<<", LeftAssociative = true, Precedence = 0, Type = OperatorType.LeftShift },
            new BinaryOperator() { Text = ">>", LeftAssociative = true, Precedence = 0, Type = OperatorType.RightShift },

			//	relational
			new BinaryOperator() { Text = "<", LeftAssociative = true, Precedence = 0, Type = OperatorType.Lesser },
            new BinaryOperator() { Text = ">", LeftAssociative = true, Precedence = 0, Type = OperatorType.Greater },
            new BinaryOperator() { Text = "<=", LeftAssociative = true, Precedence = 0, Type = OperatorType.LesserOrEqual },
            new BinaryOperator() { Text = ">=", LeftAssociative = true, Precedence = 0, Type = OperatorType.GreaterOrEqual },
            new BinaryOperator() { Text = "==", LeftAssociative = true, Precedence = 0, Type = OperatorType.Equal },
            new BinaryOperator() { Text = "!=", LeftAssociative = true, Precedence = 0, Type = OperatorType.NotEqual },

			//	logical
			new BinaryOperator() { Text = "not", LeftAssociative = true, Precedence = 0, Type = OperatorType.LogicalNot },
            new BinaryOperator() { Text = "and", LeftAssociative = true, Precedence = 0, Type = OperatorType.LogicalAnd },
            new BinaryOperator() { Text = "or", LeftAssociative = true, Precedence = 0, Type = OperatorType.LogicalOr },

			//	bitwise
			/*new BinaryOperator() { Text = "~", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseNot },
            new BinaryOperator() { Text = "&", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseAnd },
            new BinaryOperator() { Text = "|", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseOr },
            //new BinaryOperator() { Text = "^", LeftAssociative = true, Precedence = 0 },

			//	assignment
			new BinaryOperator() { Text = "=", LeftAssociative = true, Precedence = 0, Type = OperatorType.Assignment },
            new BinaryOperator() { Text = "+=", LeftAssociative = true, Precedence = 0, Type = OperatorType.AddAssignment },
            new BinaryOperator() { Text = "-=", LeftAssociative = true, Precedence = 0, Type = OperatorType.SubAssignment },
            new BinaryOperator() { Text = "*=", LeftAssociative = true, Precedence = 0, Type = OperatorType.MulAssignment },
            new BinaryOperator() { Text = "/=", LeftAssociative = true, Precedence = 0, Type = OperatorType.DivAssignment },
            new BinaryOperator() { Text = "%=", LeftAssociative = true, Precedence = 0, Type = OperatorType.ModAssignment },
            new BinaryOperator() { Text = "<<=", LeftAssociative = true, Precedence = 0, Type = OperatorType.LeftShiftAssignment },
            new BinaryOperator() { Text = ">>=", LeftAssociative = true, Precedence = 0, Type = OperatorType.RightShiftAssignment },
            new BinaryOperator() { Text = "~=", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseNotAssignment },
            new BinaryOperator() { Text = "&=", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseAndAssignment },
            new BinaryOperator() { Text = "|=", LeftAssociative = true, Precedence = 0, Type = OperatorType.BitwiseOrAssignment },*/
            //new BinaryOperator() { Text = "^=", LeftAssociative = true, Precedence = 0 },
        };
        static public BinaryOperator[] BinaryOperators { get { return m_operators; } }


        /*public struct Delimiter
        {
            public TokenTriggers Trigger { get; set; }
            public string Text { get; set; }
        }*/
    }
}
