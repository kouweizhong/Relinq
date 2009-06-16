// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Parsing.ExpressionTreeVisitors;
using Remotion.Utilities;

namespace Remotion.Data.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Represents a <see cref="MethodCallExpression"/> for 
  /// <see cref="Queryable.SelectMany{TSource,TCollection,TResult}(System.Linq.IQueryable{TSource},System.Linq.Expressions.Expression{System.Func{TSource,System.Collections.Generic.IEnumerable{TCollection}}},System.Linq.Expressions.Expression{System.Func{TSource,TCollection,TResult}})"/>.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="Expression"/> tree is parsed.
  /// This node represents an additional query source introduced to the query.
  /// </summary>
  public class SelectManyExpressionNode : MethodCallExpressionNodeBase, IQuerySourceExpressionNode
  {
    public static readonly MethodInfo[] SupportedMethods = new[]
                                                           {
                                                               GetSupportedMethod (
                                                                   () => Queryable.SelectMany<object, object[], object> (null, o => null, null))
                                                           };

    private Expression _cachedCollectionSelector;
    private Expression _cachedResultSelector;

    public SelectManyExpressionNode (
        MethodCallExpressionParseInfo parseInfo, LambdaExpression collectionSelector, LambdaExpression resultSelector)
        : base (parseInfo)
    {
      ArgumentUtility.CheckNotNull ("collectionSelector", collectionSelector);
      ArgumentUtility.CheckNotNull ("resultSelector", resultSelector);

      if (collectionSelector.Parameters.Count != 1)
        throw new ArgumentException ("Collection selector must have exactly one parameter.", "collectionSelector");
      if (resultSelector.Parameters.Count != 2)
        throw new ArgumentException ("Result selector must have exactly two parameters.", "resultSelector");

      CollectionSelector = collectionSelector;
      ResultSelector = resultSelector;
    }

    public LambdaExpression CollectionSelector { get; private set; }
    public LambdaExpression ResultSelector { get; private set; }

    public Type QuerySourceElementType
    {
      get { return ResultSelector.Parameters[1].Type; }
    }

    public Expression GetResolvedCollectionSelector (QuerySourceClauseMapping querySourceClauseMapping)
    {
      ArgumentUtility.CheckNotNull ("querySourceClauseMapping", querySourceClauseMapping);

      if (_cachedCollectionSelector == null)
        _cachedCollectionSelector = Source.Resolve (CollectionSelector.Parameters[0], CollectionSelector.Body, querySourceClauseMapping);

      return _cachedCollectionSelector;
    }

    public Expression GetResolvedResultSelector (QuerySourceClauseMapping querySourceClauseMapping)
    {
      ArgumentUtility.CheckNotNull ("querySourceClauseMapping", querySourceClauseMapping);

      if (_cachedResultSelector == null)
      {
        // our result selector usually looks like this: (i, j) => new { i = i, j = j }
        // with the data for i coming from the previous node and j identifying the data from this node

        // we resolve the selector by first asking the previous node to resolve i, then we substitute j by a QuerySourceReferenceExpression pointing 
        // back to us
        var resolvedResultSelector = Source.Resolve (ResultSelector.Parameters[0], ResultSelector.Body, querySourceClauseMapping);

        var clause = GetClauseForResolve(querySourceClauseMapping);
        var referenceExpression = new QuerySourceReferenceExpression (clause);

        _cachedResultSelector = ReplacingVisitor.Replace (ResultSelector.Parameters[1], referenceExpression, resolvedResultSelector);
      }

      return _cachedResultSelector;
    }

    private FromClauseBase GetClauseForResolve (QuerySourceClauseMapping querySourceClauseMapping)
    {
      try
      {
        return querySourceClauseMapping.GetClause (this);
      }
      catch (KeyNotFoundException ex)
      {
        var message = string.Format (
            "Cannot resolve with a {0} for which no clause was created. Be sure to call CreateClause before calling GetResolvedResultSelector, and pass in the same "
            + "QuerySourceClauseMapping to both methods.",
            GetType ().Name);
        throw new InvalidOperationException (message, ex);
      }
    }

    public override Expression Resolve (ParameterExpression inputParameter, Expression expressionToBeResolved, QuerySourceClauseMapping querySourceClauseMapping)
    {
      ArgumentUtility.CheckNotNull ("inputParameter", inputParameter);
      ArgumentUtility.CheckNotNull ("expressionToBeResolved", expressionToBeResolved);
      ArgumentUtility.CheckNotNull ("querySourceClauseMapping", querySourceClauseMapping);

      // we modify the structure of the stream of data coming into this node by our result selector,
      // so we first resolve the result selector, then we substitute the result for the inputParameter in the expressionToBeResolved
      var resolvedResultSelector = GetResolvedResultSelector(querySourceClauseMapping);
      return ReplacingVisitor.Replace (inputParameter, resolvedResultSelector, expressionToBeResolved);
    }

    public override ParameterExpression CreateParameterForOutput ()
    {
      // we modify the structure of the stream of data coming into this node by our result selector,
      // so we create a parameter capable of holding the modified stream elements
      return Expression.Parameter (ResultSelector.Body.Type, AssociatedIdentifier);
    }

    public override IClause CreateClause (IClause previousClause, QuerySourceClauseMapping querySourceClauseMapping)
    {
      ArgumentUtility.CheckNotNull ("previousClause", previousClause);
      ArgumentUtility.CheckNotNull ("querySourceClauseMapping", querySourceClauseMapping);

      var identifier = ResultSelector.Parameters[1];
      FromClauseBase clause;
      if (CollectionSelector.Body is MemberExpression)
        clause = new MemberFromClause (previousClause, identifier, CollectionSelector, ResultSelector);
      else if (CollectionSelector.Body is SubQueryExpression)
        clause = new SubQueryFromClause (previousClause, identifier, ((SubQueryExpression) CollectionSelector.Body).QueryModel, ResultSelector);
      else
        clause = new AdditionalFromClause (previousClause, identifier, CollectionSelector, ResultSelector);

      querySourceClauseMapping.AddMapping (this, clause);
      return clause;
    }
  }
}