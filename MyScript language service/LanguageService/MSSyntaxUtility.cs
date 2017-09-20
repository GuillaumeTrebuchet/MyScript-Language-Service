using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCompany.LanguageServices.MyScript
{
	class MSSyntaxUtility
	{
		public static FunctionNode GetFunctionAtPos(int index, IEnumerable<SyntaxNode> syntaxTree)
		{
			foreach (SyntaxNode node in syntaxTree)
			{
				if (node is FunctionNode)
				{
					FunctionNode functionNode = node as FunctionNode;

					//	Outside of function boundaries
					if (functionNode.separatorTokens.Count == 0 || index < functionNode.separatorTokens.First().Span.End)
						continue;

					if (functionNode.endToken != null && index > functionNode.endToken.Span.Start)
						continue;

					return functionNode;
				}
			}

			return null;
		}
	}
}
