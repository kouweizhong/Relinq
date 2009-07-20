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
using Remotion.Data.Linq.Clauses.ExecutionStrategies;

namespace Remotion.Data.Linq.Clauses.ResultOperators
{
  public class ExceptResultOperator : ResultOperatorBase
  {
    public ExceptResultOperator (IEnumerable<object> source2)
      : base (CollectionExecutionStrategy.Instance)
    {
    }

    public override object ExecuteInMemory (object input)
    {
      throw new NotImplementedException();
    }

    public override Type GetResultType (Type inputResultType)
    {
      throw new NotImplementedException();
    }

    public override ResultOperatorBase Clone (CloneContext cloneContext)
    {
      throw new NotImplementedException();
    }
  }
}