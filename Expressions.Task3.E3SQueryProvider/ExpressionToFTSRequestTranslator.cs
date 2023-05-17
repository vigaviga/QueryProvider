using Microsoft.Extensions.Primitives;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;
        private const string _prefix = "Workstation:(";

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == "Where")
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            } 
            else if (node.Method.Name == "StartsWith")
            {
                VisitMember(node.Object as MemberExpression);
                _resultStringBuilder.Append('(');
                Visit(node.Arguments[0]);
                _resultStringBuilder.Append("*)");

                return node;
            }
            else if (node.Method.Name == "EndsWith")
            {
                VisitMember(node.Object as MemberExpression);
                _resultStringBuilder.Append("(*");
                Visit(node.Arguments[0]);
                _resultStringBuilder.Append(')');
                
                return node;
            }
            else if (node.Method.Name == "Contains")
            {
                VisitMember(node.Object as MemberExpression);
                _resultStringBuilder.Append("(*");
                Visit(node.Arguments[0]);
                _resultStringBuilder.Append("*)");

                return node;
            }
            else if (node.Method.Name == "Equals")
            {
                if (node.Object.NodeType == ExpressionType.MemberAccess) 
                {
                    VisitMember(node.Object as MemberExpression);
                    _resultStringBuilder.Append('(');
                    Visit(node.Arguments[0]);
                    _resultStringBuilder.Append(')');

                    return node;
                }
               
            }
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:
                    if (node.Left.NodeType == ExpressionType.MemberAccess && node.Right.NodeType == ExpressionType.Constant)
                    {
                        Visit(node.Left);
                        _resultStringBuilder.Append("(");
                        Visit(node.Right);
                        _resultStringBuilder.Append(")");
                        break;
                    }
                    else
                    {
                        Visit(node.Right);
                        _resultStringBuilder.Append("(");
                        Visit(node.Left);
                        _resultStringBuilder.Append(")");
                        break;
                    }
                case ExpressionType.AndAlso:
                    {
                        _resultStringBuilder.Append("{\"statements\":[{\"query\":\"");
                        Visit(node.Left);
                        _resultStringBuilder.Append("\"},{\"query\":\"");

                        Visit(node.Right);
                        _resultStringBuilder.Append("\"}]}\"");
                        break;
                    }

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name + ":");
            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
