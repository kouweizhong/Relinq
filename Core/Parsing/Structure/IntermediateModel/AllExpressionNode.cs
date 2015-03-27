// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Utilities;
using Remotion.Utilities;

namespace Remotion.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Represents a <see cref="MethodCallExpression"/> for the 
  /// <see cref="Queryable.All{TSource}(System.Linq.IQueryable{TSource},System.Linq.Expressions.Expression{System.Func{TSource,bool}})"/> and
  /// <see cref="Enumerable.All{TSource}(System.Collections.Generic.IEnumerable{TSource},System.Func{TSource,bool})"/> methods.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="Expression"/> tree is parsed.
  /// When this node is used, it marks the beginning (i.e. the last node) of an <see cref="IExpressionNode"/> chain that represents a query.
  /// </summary>
  public class AllExpressionNode : ResultOperatorExpressionNodeBase
  {
    public static IEnumerable<MethodInfo> GetSupportedMethods()
    {
      return ReflectionUtility.EnumerableAndQueryableMethods.WhereNameMatches ("All");
    }

    private readonly ResolvedExpressionCache<Expression> _cachedPredicate;
    private readonly LambdaExpression _predicate;

    public AllExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression predicate)
        : base (parseInfo, null, null)
    {
      ArgumentUtility.CheckNotNull ("predicate", predicate);

      _predicate = predicate;
      _cachedPredicate = new ResolvedExpressionCache<Expression> (this);
    }

    public LambdaExpression Predicate
    {
      get { return _predicate; }
    }

    public Expression GetResolvedPredicate (ClauseGenerationContext clauseGenerationContext)
    {
      return _cachedPredicate.GetOrCreate (r => r.GetResolvedExpression (_predicate.Body, _predicate.Parameters[0], clauseGenerationContext));
    }

    public override Expression Resolve (
        ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      // no data streams out from this node, so we cannot resolve All expressions
      throw CreateResolveNotSupportedException();
    }

    protected override ResultOperatorBase CreateResultOperator (ClauseGenerationContext clauseGenerationContext)
    {
      return new AllResultOperator (GetResolvedPredicate (clauseGenerationContext));
    }
  }
}
