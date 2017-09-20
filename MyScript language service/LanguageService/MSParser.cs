using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.MyScript
{
	class SyntaxError
	{
		public SyntaxError(int errorCode, string message, int startIndex, int endIndex)
		{
			ErrorCode = errorCode;
			Message = message;
			StartIndex = startIndex;
			EndIndex = endIndex;
		}
		public int ErrorCode { get; set; }
		public string Message { get; set; }
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
	}

    /*
     * This is a basic recursive decent parser.
     * */
	class MSParser
    {
        MSLexer m_lexer = null;

        bool m_eof = false;
        MSToken m_currentToken = null;

		string m_comment = null;
		bool m_keepComment = false;

		void SaveComment(MSToken token)
		{
			string text = token.Text;
			if (text.StartsWith("//"))
			{

				string[] s = text.Substring(2).Trim().Split(null);
				text = text.Substring(2);
			}
			else if (text.StartsWith("/*"))
				text = text.Substring(2, text.Length - 4);
			else
				throw new Exception();

			text = text.Trim();

			if (string.IsNullOrEmpty(m_comment))
				m_comment = text;
			else
			{
				if (m_keepComment)
					m_comment += Environment.NewLine + text;
				else
					m_comment = text;
			}
		}

		/*
         * Rollback the parser to the given token.
         * This is required because some expression can only be verified past a certain point.
         */
		void Rollback(MSToken token)
		{
			m_currentToken = token;
			m_lexer.SetIndex(m_currentToken.Span.End);
			m_eof = false;
		}

        //  Returns current token and advance to the next one. Returns null if EOF.
		MSToken AcceptToken()
        {
			if (m_eof)
				return null;

            MSToken tmp = m_currentToken;

			do
			{
				m_currentToken = new MSToken();
				m_eof = !m_lexer.GetNextToken(m_currentToken);

				if (!m_eof && m_currentToken.Type == MSTokenType.Comment)
				{
					SaveComment(m_currentToken);
					if (!m_keepComment)
						m_keepComment = true;
				}
				else if (!m_eof && m_currentToken.Type != MSTokenType.Whitespace)
				{
					if(!m_keepComment)
						m_comment = "";
					else
						m_keepComment = false;
				}
			}
			while (!m_eof && (m_currentToken.Type == MSTokenType.Comment || m_currentToken.Type == MSTokenType.Whitespace));

			return tmp;
        }
        //  Advance if token text equals s
        MSToken AcceptToken(string s)
        {
			if (m_eof)
				return null;

			MSToken tmp = m_currentToken;

			if (m_currentToken.Text == s)
			{
				AcceptToken();
				return tmp;
			}
			else
			{
				return null;
			}
        }
        //  Advance if token is of given type
        MSToken AcceptToken(MSTokenType type)
        {
			if (m_eof)
				return null;

			MSToken tmp = m_currentToken;

			if (m_currentToken.Type == type)
			{
				AcceptToken();
				return tmp;
			}
			else
			{
				return null;
			}
        }

        //  Raise a syntax error
		void Error(string msg)
		{
			if (m_eof)
			{
				m_syntaxErrors.Add(new SyntaxError(0, msg, m_lexer.GetSource().Length, m_lexer.GetSource().Length));
			}
			else
			{
				m_syntaxErrors.Add(new SyntaxError(0, msg, m_currentToken.Span.Start, m_currentToken.Span.End));
			}
		}

        public MSParser()
        {
        }

		NullNode ParseNull()
		{
			MSToken token = AcceptToken("null");
			if (token != null)
			{
				NullNode node = new NullNode();
				node.token = token;
				return node;
			}

			return null;
		}
		IntegerNode ParseInteger()
        {
            MSToken token = AcceptToken(MSTokenType.Integer);
            if(token != null)
            {
                IntegerNode node = new IntegerNode();
                node.token = token;
                return node;
            }

            return null;
        }
        BooleanNode ParseBoolean()
        {
            MSToken token = AcceptToken("true");
            if(token == null)
                token = AcceptToken("false");

            if (token != null)
            {
				BooleanNode node = new BooleanNode();
                node.token = token;
                return node;
            }

            return null;
        }
        FloatNode ParseFloat()
        {
            MSToken token = AcceptToken(MSTokenType.Decimal);
            if (token != null)
            {
                FloatNode node = new FloatNode();
                node.token = token;
                return node;
            }

            return null;
        }
        StringNode ParseString()
        {
            MSToken token = AcceptToken(MSTokenType.String);
            if (token != null)
            {
                StringNode node = new StringNode();
                node.token = token;
                return node;
            }

            return null;
        }
        NameNode ParseName()
        {
            MSToken token = AcceptToken(MSTokenType.Identifier);
            if (token != null)
            {
                NameNode node = new NameNode();
                node.token = token;
                return node;
            }

            return null;
        }

        //  Parse expression made of only one token.
		SyntaxNode ParseSimpleExpression()
		{
			NullNode nullNode = ParseNull();
			if (nullNode != null)
				return nullNode;
			IntegerNode integerNode = ParseInteger();
			if (integerNode != null)
				return integerNode;
			BooleanNode booleanNode = ParseBoolean();
			if (booleanNode != null)
				return booleanNode;
			FloatNode floatNode = ParseFloat();
			if (floatNode != null)
				return floatNode;
			StringNode stringNode = ParseString();
			if (stringNode != null)
				return stringNode;
			CallNode callNode = ParseCall();
			if (callNode != null)
				return callNode;
			NameNode nameNode = ParseName();
			if (nameNode != null)
				return nameNode;

			return null;
		}

        //  Parse complex expression. Complex expression are made of operators and simple expr.
		SyntaxNode ParseExpression()
        {
			SyntaxNode lhs = ParseSimpleExpression();
			if (lhs == null)
				return null;

			while (true)
			{
				MSToken token = AcceptToken(MSTokenType.Operator);
				if (token == null)
					token = AcceptToken("and");
				if (token == null)
					token = AcceptToken("or");
				if (token == null)
					return lhs;

				BinopNode node = new BinopNode();
				node.expression1 = lhs;
				node.operatorToken = token;
				
				node.expression2 = ParseExpression();
				if(node.expression2 == null)
				{
					Error("expression expected");
					return node;
				}

				lhs = node;
			}
		}

        ReturnNode ParseReturn()
        {
            MSToken token = AcceptToken("return");
            if (token == null)
            {
                return null;
            }

            ReturnNode node = new ReturnNode();
            node.returnToken = token;
			
			node.expression = ParseExpression();
			if(node.expression == null)
			{
				Error("expression expected");
			}

			node.semicolonToken = AcceptToken(";");
			if (node.semicolonToken == null)
			{
				Error("';' expected");
			}

			return node;
        }
        BreakNode ParseBreak()
        {
            MSToken token = AcceptToken("break");
            if (token == null)
            {
                return null;
            }

            BreakNode node = new BreakNode();
            node.breakToken = token;
			
			node.semicolonToken = AcceptToken(";");
			if (node.semicolonToken == null)
			{
				Error("';' expected");
			}

			return node;
        }
        ContinueNode ParseContinue()
        {
            MSToken token = AcceptToken("continue");
            if (token == null)
            {
                return null;
            }

            ContinueNode node = new ContinueNode();
            node.continueToken = token;
			
			node.semicolonToken = AcceptToken(";");
			if (node.semicolonToken == null)
			{
				Error("';' expected");
			}

			return node;
        }
        WhileNode ParseWhile()
        {
            MSToken token = AcceptToken("while");
            if (token == null)
            {
                return null;
            }

            WhileNode node = new WhileNode();
            node.whileToken = token;

			token = AcceptToken("(");
			if(token == null)
			{
				Error("'(' expected");
				return node;
			}

			node.separatorTokens.Add(token);

			node.expression = ParseExpression();
			if (node.expression == null)
			{
				Error("expression expected");
				return node;
			}

			token = AcceptToken(")");
			if (token == null)
			{
				Error("')' expected");
				return node;
			}

			node.separatorTokens.Add(token);

			node.doToken = AcceptToken("do");
			if (node.doToken == null)
			{
				Error("'do' expected");
				return node;
			}

			while (true)
			{
				SyntaxNode statementNode = ParseStatement();
				if (statementNode != null)
					node.statements.Add(statementNode);
				else
					break;
			}

			node.endToken = AcceptToken("end");
			if (node.endToken == null)
			{
				Error("'end' expected");
				return node;
			}

			return node;
        }
		IfNode ParseIf()
		{
			MSToken token = AcceptToken("if");
			if (token == null)
			{
				return null;
			}

			IfNode node = new IfNode();
			node.ifToken = token;

			token = AcceptToken("(");
			if (token == null)
			{
				Error("'(' expected");
				return node;
			}

			node.separatorTokens.Add(token);

			node.expression = ParseExpression();
			if (node.expression == null)
			{
				Error("expression expected");
				return node;
			}

			token = AcceptToken(")");
			if (token == null)
			{
				Error("')' expected");
				return node;
			}

			node.separatorTokens.Add(token);

			node.thenToken = AcceptToken("then");
			if (node.thenToken == null)
			{
				Error("'then' expected");
				return node;
			}

			while (true)
			{
				SyntaxNode statementNode = ParseStatement();
				if (statementNode != null)
					node.statements.Add(statementNode);
				else
					break;
			}

			node.elseToken = AcceptToken("else");

			if (node.elseToken != null)
			{
				while (true)
				{
					SyntaxNode statementNode = ParseStatement();
					if (statementNode != null)
						node.elseStatements.Add(statementNode);
					else
						break;
				}
			}

			node.endToken = AcceptToken("end");
			if (node.endToken == null)
			{
				Error("'end' expected");
				return node;
			}

			return node;
		}
		CallNode ParseCall(bool semicolon = false)
		{
			if (m_eof)
				return null;

			MSToken rollbackToken = m_currentToken;

			NameNode name = ParseName();
			if (name == null)
				return null;

			MSToken token = AcceptToken("(");
			if (token == null)
			{
				Rollback(rollbackToken);
				return null;
			}

			CallNode node = new CallNode();
			node.functionExpression = name;
			
			node.separatorTokens.Add(token);
			token = AcceptToken(")");
			if (token != null)
				node.separatorTokens.Add(token);
			else
			{
				while (true)
				{
					SyntaxNode expressionNode = ParseExpression();
					if (expressionNode == null)
					{
						Error("expression expected");
					}

					node.arguments.Add(expressionNode);

					token = AcceptToken(")");
					if (token != null)
					{
						node.separatorTokens.Add(token);
						break;
					}

					token = AcceptToken(",");
					if (token == null)
					{
						Error("',' expected");
						return node;
					}

					node.separatorTokens.Add(token);
				}
			}

			if (semicolon)
			{
				node.semicolonToken = AcceptToken(";");
				if (node.semicolonToken == null)
				{
					Error("';' expected");
					return node;
				}
			}

			return node;
		}
		TypeNode ParseType()
		{
			MSToken token = AcceptToken("int");
			if (token != null)
			{
				TypeNode node = new TypeNode();
				node.token = token;
				return node;
			}
			token = AcceptToken("float");
			if (token != null)
			{
				TypeNode node = new TypeNode();
				node.token = token;
				return node;
			}
			token = AcceptToken("bool");
			if (token != null)
			{
				TypeNode node = new TypeNode();
				node.token = token;
				return node;
			}
			token = AcceptToken("string");
			if (token != null)
			{
				TypeNode node = new TypeNode();
				node.token = token;
				return node;
			}
			token = AcceptToken("void");
			if (token != null)
			{
				TypeNode node = new TypeNode();
				node.token = token;
				return node;
			}

			return null;
		}
		AssignmentNode ParseAssignment()
		{
			if (m_eof)
				return null;

			MSToken rollbackToken = m_currentToken;

			TypeNode type = ParseType();
			NameNode name = ParseName();

			if (name == null)
			{
				Rollback(rollbackToken);
				return null;
			}

			AssignmentNode node = new AssignmentNode();
			node.typeExpression = type;
			node.varExpression = name;
			
			node.equalToken = AcceptToken("=");
			if (node.equalToken == null)
			{
				if (type != null)
				{
					node.semicolonToken = AcceptToken(";");
					if (node.semicolonToken == null)
					{
						Error("'=' expected");
						return node;
					}

					return node;
				}
				else
				{
					Error("'=' expected");
					return node;
				}
			}

			node.valueExpression = ParseExpression();
			if (node.valueExpression == null)
			{
				Error("expression expected");
			}

			node.semicolonToken = AcceptToken(";");
			if (node.semicolonToken == null)
			{
				Error("';' expected");
			}

			return node;
		}

        /*
         * Statements are one of the following :
         *  - return
         *  - break
         *  - continue
         *  - while
         *  - if
         *  - call
         *  - assignment
         * */
        SyntaxNode ParseStatement()
        {
            ReturnNode returnNode = ParseReturn();
            if (returnNode != null)
                return returnNode;

            BreakNode breakNode = ParseBreak();
            if (breakNode != null)
                return breakNode;

            ContinueNode continueNode = ParseContinue();
            if (continueNode != null)
                return continueNode;

            WhileNode whileNode = ParseWhile();
            if (whileNode != null)
                return whileNode;

            IfNode ifNode = ParseIf();
            if (ifNode != null)
                return ifNode;

            CallNode callNode = ParseCall(true);
            if (callNode != null)
                return callNode;

            AssignmentNode assignmentNode = ParseAssignment();
            if (assignmentNode != null)
                return assignmentNode;

            return null;
        }

        FunctionNode ParseFunction()
		{
			string comment = m_comment;
			MSToken token = AcceptToken("function");
			if (token == null)
				return null;

			FunctionNode node = new FunctionNode();
			node.functionToken = token;

			node.comment = comment;
			m_comment = null;

			node.nameExpression = ParseName();
			if (node.nameExpression == null)
			{
				Error("name expected");
				return node;
			}

			token = AcceptToken("(");
			if (token == null)
			{
				Error("'(' expected");
				return node;
			}

			node.separatorTokens.Add(token);

			token = AcceptToken(")");
			if (token != null)
				node.separatorTokens.Add(token);
			else
			{
				while (true)
				{
					TypeNode type = ParseType();
					if (type == null)
					{
						Error("type expected");
					}

					NameNode name = ParseName();
					if (name == null && type != null)
					{
						//	No need to add 2 error messages
						Error("name expected");
					}

					node.arguments.Add(new FunctionNode.Argument() { nameExpression = name, typeExpression = type });

					token = AcceptToken(")");
					if (token != null)
					{
						node.separatorTokens.Add(token);
						break;
					}
					token = AcceptToken(",");
					if (token != null)
					{
						node.separatorTokens.Add(token);
						continue;
					}

					Error("',' expected");
					return node;
				}
			}

			node.colonToken = AcceptToken(":");
			if (node.colonToken != null)
			{
				node.returnTypeExpression = ParseType();
				if (node.returnTypeExpression == null)
				{
					Error("type expected");
				}
			}

			while (true)
			{
				SyntaxNode statementNode = ParseStatement();
				if (statementNode != null)
					node.statements.Add(statementNode);
				else
					break;
			}

			node.endToken = AcceptToken("end");
			if (node.endToken == null)
			{
				Error("'end' expected");
			}

			return node;
		}

		ImportNode ParseImport()
		{
			MSToken token = AcceptToken("import");
			if (token == null)
			{
				return null;
			}

			ImportNode node = new ImportNode();
			node.importToken = token;

			node.filenameNode = ParseString();
			if (node.filenameNode == null)
			{
				Error("string expected");
			}

			node.semicolonToken = AcceptToken(";");
			if (node.semicolonToken == null)
			{
				Error("';' expected");
			}

			return node;
		}

		public void SetLexer(MSLexer lexer)
		{
			m_lexer = lexer;
			AcceptToken();
		}

		public MSLexer GetLexer()
		{
			return m_lexer;
		}

		public void ParseAll()
		{
			while(true)
			{
				if (m_eof)
					break;

				FunctionNode functionNode = ParseFunction();
				if (functionNode != null)
				{
					m_syntaxTree.Add(functionNode);
					continue;
				}

				SyntaxNode statementNode = ParseStatement();
				if (statementNode != null)
				{
					m_syntaxTree.Add(statementNode);
					continue;
				}

				ImportNode importNode = ParseImport();
				if (importNode != null)
				{
					m_syntaxTree.Add(importNode);
					continue;
				}

				Error("unexpected token");
				AcceptToken();
			}
		}

		List<SyntaxNode> m_syntaxTree = new List<SyntaxNode>();
		public IList<SyntaxNode> SyntaxTree
		{
			get
			{
				return m_syntaxTree;
			}
		}

		List<SyntaxError> m_syntaxErrors = new List<SyntaxError>();
		public IList<SyntaxError> SyntaxErrors
		{
			get
			{
				return m_syntaxErrors;
			}
		}
	}
}
