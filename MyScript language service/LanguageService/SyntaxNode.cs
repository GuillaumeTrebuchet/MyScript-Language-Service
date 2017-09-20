using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * This contains the syntax nodes for all constructs in MyScript language.
 * */

namespace MyCompany.LanguageServices.MyScript
{
    abstract class SyntaxNode
    {
    }
	
	class ImportNode
		: SyntaxNode
	{
		public MSToken importToken;
		public StringNode filenameNode;
		public MSToken semicolonToken;
	}
    class FunctionNode
        : SyntaxNode
    {
        public struct Argument
        {
            public SyntaxNode typeExpression;
            public SyntaxNode nameExpression;
			public string comment;
		}

        public MSToken functionToken;
        public SyntaxNode nameExpression;
		public string comment;

		public List<Argument> arguments = new List<Argument>();
        public List<MSToken> separatorTokens = new List<MSToken>();

        public MSToken colonToken;
        public SyntaxNode returnTypeExpression;

        public List<SyntaxNode> statements = new List<SyntaxNode>();

        public MSToken endToken;
    }


	class NullNode
	   : SyntaxNode
	{
		public MSToken token;
	}
	class IntegerNode
        : SyntaxNode
    {
        public MSToken token;
    }

    class BooleanNode
        : SyntaxNode
    {
        public MSToken token;
    }

    class FloatNode
        : SyntaxNode
    {
        public MSToken token;
    }

    class NameNode
        : SyntaxNode
    {
        public MSToken token;
    }

    class StringNode
        : SyntaxNode
    {
        public MSToken token;
    }

    // Binary operation node
    class BinopNode
        : SyntaxNode
    {
        public SyntaxNode expression1;
        public SyntaxNode expression2;
        public MSToken operatorToken;
    }

    class ReturnNode
        : SyntaxNode
    {
        public MSToken returnToken;
        public SyntaxNode expression;
        public MSToken semicolonToken;
    }

    class BreakNode
        : SyntaxNode
    {
        public MSToken breakToken;
        public MSToken semicolonToken;
    }

    class ContinueNode
       : SyntaxNode
    {
        public MSToken continueToken;
        public MSToken semicolonToken;
    }

    class WhileNode
       : SyntaxNode
    {
        public MSToken whileToken;
        public MSToken doToken;
        public MSToken endToken;
        public SyntaxNode expression;
        public List<SyntaxNode> statements = new List<SyntaxNode>();
        public List<MSToken> separatorTokens = new List<MSToken>();
    }

    //  If [else] node. MyScript only support if statement with 1 optional else clause.
    class IfNode
       : SyntaxNode
    {
        public MSToken ifToken;
        public MSToken thenToken;
        public MSToken elseToken;
        public MSToken endToken;
		public SyntaxNode expression;
        public List<SyntaxNode> statements = new List<SyntaxNode>();
        public List<SyntaxNode> elseStatements = new List<SyntaxNode>();
        public List<MSToken> separatorTokens = new List<MSToken>();
    }

    class CallNode
       : SyntaxNode
    {
        public SyntaxNode functionExpression;
        public List<SyntaxNode> arguments = new List<SyntaxNode>();
        public List<MSToken> separatorTokens = new List<MSToken>();
		public MSToken semicolonToken;
    }

    class AssignmentNode
       : SyntaxNode
    {
        public SyntaxNode typeExpression;
        public SyntaxNode varExpression;
        public SyntaxNode valueExpression;
        public MSToken equalToken;
        public MSToken semicolonToken;
    }

	class TypeNode
	   : SyntaxNode
	{
		public MSToken token;
	}
}
