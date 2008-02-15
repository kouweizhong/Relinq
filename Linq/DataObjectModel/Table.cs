using System;
using Rubicon.Utilities;

namespace Rubicon.Data.Linq.DataObjectModel
{
  public class Table : IFieldSourcePath
  {
    public Table ()
    {
    }

    public Table(string name, string alias)
    {
      ArgumentUtility.CheckNotNull ("name", name);
      Name = name;
      Alias = alias;
    }

    public string Name { get; private set; }
    public string Alias { get; private set; }

    public override string ToString ()
    {
      return Name + " (" + Alias + ")";
    }

    public Table GetStartingTable ()
    {
      return this;
    }

    public override bool Equals (object obj)
    {
      Table other = obj as Table;
      return other != null && other.Name == Name && other.Alias == Alias;
    }

    public override int GetHashCode ()
    {
      return EqualityUtility.GetRotatedHashCode (Name, Alias);
    }

    public void SetAlias (string newAlias)
    {
      ArgumentUtility.CheckNotNull ("newAlias", newAlias);

      if (Alias != null)
      {
        string message = string.Format ("Table '{0}' already has alias '{1}', new alias '{2}' cannot be set.", Name, Alias, newAlias);
        throw new InvalidOperationException (message);
      }
      Alias = newAlias;
    }
  }
}