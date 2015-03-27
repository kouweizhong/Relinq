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
  /// Represents a <see cref="MethodCallExpression"/> for the different <see cref="Queryable.GroupBy{TSource, TKey}(IQueryable{TSource}, Expression{Func{TSource, TKey}})"/> 
  /// overloads that do not take a result selector. The overloads with a result selector are represented by 
  /// <see cref="GroupByWithResultSelectorExpressionNode"/>.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="Expression"/> tree is parsed.
  /// </summary>
  public class GroupByExpressionNode : ResultOperatorExpressionNodeBase, IQuerySourceExpressionNode
  {
    public static IEnumerable<MethodInfo> GetSupportedMethods()
    {
      return ReflectionUtility.EnumerableAndQueryableMethods.WhereNameMatches ("GroupBy").WithoutResultSelector().WithoutEqualityComparer();
    }

    private readonly ResolvedExpressionCache<Expression> _cachedKeySelector;
    private readonly ResolvedExpressionCache<Expression> _cachedElementSelector;
    private readonly LambdaExpression _keySelector;
    private readonly LambdaExpression _optionalElementSelector;

    public GroupByExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector, LambdaExpression optionalElementSelector)
        : base (parseInfo, null, null)
    {
      ArgumentUtility.CheckNotNull ("keySelector", keySelector);

      if (keySelector.Parameters.Count != 1)
        throw new ArgumentException ("KeySelector must have exactly one parameter.", "keySelector");

      if (optionalElementSelector != null && optionalElementSelector.Parameters.Count != 1)
        throw new ArgumentException ("ElementSelector must have exactly one parameter.", "optionalElementSelector");

      _keySelector = keySelector;
      _optionalElementSelector = optionalElementSelector;

      _cachedKeySelector = new ResolvedExpressionCache<Expression> (this);

      if (optionalElementSelector != null)
        _cachedElementSelector = new ResolvedExpressionCache<Expression> (this);
    }

    public LambdaExpression KeySelector
    {
      get { return _keySelector; }
    }

    public LambdaExpression OptionalElementSelector
    {
      get { return _optionalElementSelector; }
    }

    public Expression GetResolvedKeySelector (ClauseGenerationContext clauseGenerationContext)
    {
      return _cachedKeySelector.GetOrCreate (r => r.GetResolvedExpression (_keySelector.Body, _keySelector.Parameters[0], clauseGenerationContext));
    }

    public Expression GetResolvedOptionalElementSelector (ClauseGenerationContext clauseGenerationContext)
    {
      if (_optionalElementSelector == null)
        return null;

      return _cachedElementSelector.GetOrCreate (
          r => r.GetResolvedExpression (_optionalElementSelector.Body, _optionalElementSelector.Parameters[0], clauseGenerationContext));
    }

    public override Expression Resolve (
        ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      return QuerySourceExpressionNodeUtility.ReplaceParameterWithReference (
          this, 
          inputParameter, 
          expressionToBeResolved, 
          clauseGenerationContext);
    }

    protected override ResultOperatorBase CreateResultOperator (ClauseGenerationContext clauseGenerationContext)
    {
      var resolvedKeySelector = GetResolvedKeySelector (clauseGenerationContext);

      var resolvedElementSelector = GetResolvedOptionalElementSelector (clauseGenerationContext);
      if (resolvedElementSelector == null)
      {
        // supply a default element selector if none is given
        // just resolve KeySelector.Parameters[0], that's the input data flowing in from the source node
        resolvedElementSelector = Source.Resolve (_keySelector.Parameters[0], _keySelector.Parameters[0], clauseGenerationContext);
      }
      
      var resultOperator = new GroupResultOperator (AssociatedIdentifier, resolvedKeySelector, resolvedElementSelector);
      clauseGenerationContext.AddContextInfo (this, resultOperator);
      return resultOperator;
    }
  }
}
