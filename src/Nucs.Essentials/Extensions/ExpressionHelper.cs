using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Nucs.Extensions {
    public static class ExpressionHelper {
        // Produces an expression identical to 'expression'
        // except with 'source' parameter replaced with 'target' expression.     
        public static Expression<TOutput> ReplaceLambdaParameter<TInput, TOutput>(Expression<TInput> expression,
                                                                                  ParameterExpression source,
                                                                                  Expression target) where TInput : Delegate where TOutput : Delegate {
            return new LambdaParameterReplacerVisitor<TOutput>(source, target)
               .VisitAndConvert(expression);
        } // Produces an expression identical to 'expression'
        // except with 'source' parameter replaced with 'target' expression.     

        /// <summary>
        ///     Will inline the func's return into a new variable. 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="source"></param>
        /// <param name="variable"></param>
        /// <param name="variableName"></param>
        /// <typeparam name="TInput">Func&lt;...&gt;</typeparam>
        /// <typeparam name="TOutput">Action&lt;...&gt;</typeparam>
        /// <returns>The new TOutput LambdaExpression</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression<TOutput> InlineIntoActionAssigningVariable<TInput, TOutput>(Expression<TInput> func,
                                                                                             out Expression variable,
                                                                                             string? variableName = null) where TInput : Delegate where TOutput : Delegate {
            if (!typeof(TOutput).GetGenericTypeDefinition().Name.StartsWith("Func"))
                throw new ArgumentException(nameof(TOutput));
            variable = Expression.Variable(typeof(TOutput).GetGenericArguments()[^1], variableName);
            return InlineIntoActionAssigningVariable<TInput, TOutput>(func, variable);
        }

        /// <summary>
        ///     Will inline the func's return into given variable. 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="source"></param>
        /// <param name="variable"></param>
        /// <param name="variableName"></param>
        /// <typeparam name="TInput">Func&lt;...&gt;</typeparam>
        /// <typeparam name="TOutput">Action&lt;...&gt;</typeparam>
        /// <returns>The new TOutput LambdaExpression</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression<TOutput> InlineIntoActionAssigningVariable<TInput, TOutput>(Expression<TInput> func,
                                                                                             Expression variable) where TInput : Delegate where TOutput : Delegate {
            #if !DEBUG
            if (!typeof(TOutput).GetGenericTypeDefinition().Name.StartsWith("Action"))
                throw new ArgumentException(nameof(TOutput));
            if (!typeof(TInput).GetGenericTypeDefinition().Name.StartsWith("Func"))
                throw new ArgumentException(nameof(TOutput));
            #endif

            return Expression.Lambda<TOutput>(Expression.Assign(variable, func.Body), func.Parameters);
        }

        /// <summary>
        ///     Will append additional logic to the end of current function. return type is the same.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="source"></param>
        /// <param name="variable"></param>
        /// <param name="variableName"></param>
        /// <typeparam name="TFunction">Func&lt;...&gt;</typeparam>
        /// <returns>The new TOutput LambdaExpression</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression<TFunction> ExtendFunc<TFunction>(Expression<TFunction> func,
                                                                  ParameterExpression source,
                                                                  out Expression variable,
                                                                  string? variableName = null) where TFunction : Delegate {
            #if !DEBUG
            if (!typeof(TFunction).GetGenericTypeDefinition().Name.StartsWith("Func"))
                throw new ArgumentException(nameof(TFunction));
            #endif

            variable = Expression.Variable(typeof(TFunction).GetGenericArguments()[^1], variableName);
            return Expression.Lambda<TFunction>(Expression.Assign(variable, func.Body), func.Parameters);
        }

        /// <summary>
        ///     Will append additional logic to the end of current function. return type is different.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="source"></param>
        /// <param name="variable"></param>
        /// <param name="variableName"></param>
        /// <typeparam name="TFunction">Func&lt;...&gt;</typeparam>
        /// <returns>The new TOutput LambdaExpression</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression<TNewFunction> ExtendFunc<TFunction, TNewFunction>(Expression<TFunction> func,
                                                                                   LambdaExpression suffix,
                                                                                   string? variableName = null) where TFunction : Delegate where TNewFunction : Delegate {
            #if !DEBUG
            if (!typeof(TFunction).GetGenericTypeDefinition().Name.StartsWith("Func"))
                throw new ArgumentException(nameof(TFunction));
            #endif

            _reiterate:
            foreach (var parameter in suffix.Parameters) {
                if (parameter.Type == func.ReturnType) {
                    //inline all parameters
                    suffix = ExpressionHelper.ReplaceExpressionInsideLambda(suffix, parameter, func.Body);
                    goto _reiterate;
                }
            }

            if (suffix.Parameters.Count != 0)
                throw new ArgumentException("Unable to inline all parameters of given suffix, see left parameters: " + string.Join(",", suffix.Parameters.Select(p => p.Name)));

            return Expression.Lambda<TNewFunction>(suffix.Body, func.Parameters);
        }

        /// <summary>
        ///     Will append additional logic to the end of current function. return type is different.
        /// </summary>
        /// <param name="func"></param>
        /// <param name="source"></param>
        /// <param name="variable"></param>
        /// <param name="variableName"></param>
        /// <typeparam name="TFunction">Func&lt;...&gt;</typeparam>
        /// <returns>The new TOutput LambdaExpression</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Expression<TNewFunction> ExtendIntoAction<TFunction, TNewFunction>(Expression<TFunction> func,
                                                                                         LambdaExpression suffix,
                                                                                         string? variableName = null) where TFunction : Delegate where TNewFunction : Delegate {
            throw new NotSupportedException();
            //todo
        }

        /// Produces an expression identical to 'expression'
        /// except with 'source' parameter replaced with 'target' expression.     
        public static LambdaExpression ReplaceLambdaParameter(LambdaExpression expression,
                                                              Expression target,
                                                              Expression newExpression) {
            return (LambdaExpression) new ParameterReplacerVisitor(target, newExpression)
               .VisitAndConvertBody(expression);
        }

        /// Produces an expression identical to 'expression'
        /// except with 'source' parameter replaced with 'target' expression.     
        public static LambdaExpression ReplaceExpressionInsideLambda(LambdaExpression expression,
                                                                     Expression target,
                                                                     Expression newExpression) {
            return (LambdaExpression) new ParameterReplacerVisitor(target, newExpression)
               .VisitAndConvertBody(expression);
        }

        /// Produces an expression identical to 'expression'
        /// except with 'source' parameter replaced with 'target' expression.     
        public static TExpr ReplaceExpressionInside<TExpr>(TExpr expression,
                                                           Expression target,
                                                           Expression newExpression) where TExpr : Expression {
            return (TExpr) new RegularParameterReplacerVisitor(target, newExpression)
               .VisitAndConvertBody(expression);
        }

        private class LambdaParameterReplacerVisitor<TOutput> : ExpressionVisitor {
            private ParameterExpression _target;
            private Expression _newExpression;

            public LambdaParameterReplacerVisitor(ParameterExpression target, Expression newExpression) {
                _target = target;
                _newExpression = newExpression;
            }

            internal Expression<TOutput> VisitAndConvert<T>(Expression<T> root) {
                return (Expression<TOutput>) VisitLambda(root);
            }

            protected override Expression VisitLambda<T>(Expression<T> node) {
                // Leave all parameters alone except the one we want to replace.
                var parameters = node.Parameters
                                     .Where(p => p != _target);

                return Expression.Lambda<TOutput>(Visit(node.Body), parameters);
            }

            protected override Expression VisitParameter(ParameterExpression node) {
                // Replace the source with the target, visit other params as usual.
                return node == _target ? _newExpression : base.VisitParameter(node);
            }

            public override Expression? Visit(Expression? node) {
                return node == _target ? _newExpression : base.Visit(node);
            }
        }

        public class ParameterReplacerVisitor : ExpressionVisitor {
            private readonly Expression _lookFor;
            private readonly Expression _newExpression;


            public ParameterReplacerVisitor(Expression lookFor, Expression newExpression) {
                _lookFor = lookFor;
                _newExpression = newExpression;
            }

            internal Expression VisitAndConvertBody(LambdaExpression lambda) {
                return Visit(lambda);
            }

            protected override Expression VisitParameter(ParameterExpression node) {
                // Replace the source with the target, visit other params as usual.
                return node == _lookFor ? _newExpression : base.VisitParameter(node);
            }

            protected override Expression VisitLambda<T>(Expression<T> node) {
                // Leave all parameters alone except the one we want to replace.

                IEnumerable<ParameterExpression> parameters;
                if (typeof(ParameterExpression).IsAssignableFrom(_lookFor.GetType())) {
                    //we are removing expression:

                    parameters = node.Parameters
                                     .Where(p => p != _lookFor);
                } else {
                    parameters = node.Parameters;
                }

                //resolve type

                return Expression.Lambda(Visit(node.Body), parameters);
            }

            public override Expression? Visit(Expression? node) {
                return node == _lookFor ? _newExpression : base.Visit(node);
            }
        }

        public class RegularParameterReplacerVisitor : ExpressionVisitor {
            private readonly Expression _lookFor;
            private readonly Expression _newExpression;


            public RegularParameterReplacerVisitor
                (Expression lookFor, Expression newExpression) {
                _lookFor = lookFor;
                _newExpression = newExpression;
            }

            internal Expression VisitAndConvertBody(Expression expr) {
                return Visit(expr);
            }

            protected override Expression VisitParameter(ParameterExpression node) {
                // Replace the source with the target, visit other params as usual.
                return node == _lookFor ? _newExpression : base.VisitParameter(node);
            }

            public override Expression? Visit(Expression? node) {
                if (node.NodeType != _lookFor.NodeType)
                    return node;
                return node == _lookFor ? _newExpression : base.Visit(node);
            }
        }
    }
}