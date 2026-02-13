using System.Collections;
using System.Security.AccessControl;

public sealed class FastName : Singleton, IEquatable<FastName>, IEnumerable<FastName>
{
    public static FastName None = new();
    System.UInt32 NameID;
    private FastName ()
    {
        NameID = 0;
    }
    public FastName (string InName)
    {
        NameID = NameManager.CreateOrRetrieveID(InName);
    }
    public override string ToString()
    {
        if  (this == None)
        {
            return "None";
        }
            return NameManager.RetrieveNameFromID(NameID);
        
    }
    public override bool Equals(object obj)
    {
        return Equals(obj as FastName);
    }
    public bool Equals(FastName? InOther)
    {
        return InOther is not null &&
        NameID == InOther.NameID;
    }
    public override int GetHashCode()
    {
        return NameID.GetHashCode();  
    }
    public int CompareTo(FastName InOther)
    {
       return NameID.CompareTo(InOther.NameID);
    }
    public static bool operator ==(FastName InLHS, FastName InRHS)
    {
        return EqualityComparer<FastName>.Default.Equals(InLHS, InRHS);
    }
     public static bool operator !=(FastName InLHS, FastName InRHS)
    {
    return !(InLHS == InRHS);
    }
    public static implicit operator bool(FastName InLHS)
    {
        return InLHS != None;
    }

    public IEnumerator<FastName> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }
}