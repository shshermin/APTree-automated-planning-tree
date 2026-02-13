using System.ComponentModel;

public class NameManager : Singleton<NameManager>
    {
        UInt32 NextNameID = 1;
        Dictionary<UInt32, string> NameIDs = new();
        static object _NameIDsLock = new object();

        internal static uint CreateOrRetrieveID(string InName)
        {
            if (Instance == null)
                return 0;

            return Instance.CreateOrRetrieveIDInternal(InName);
        }

        internal static string RetrieveNameFromID(uint NameID)
        {
            if (Instance == null)
                return "## NO NameManager ##";

            return Instance.RetrieveNameFromIDInternal(NameID);
        }

        internal UInt32 CreateOrRetrieveIDInternal(string InName)
        {
            lock (_NameIDsLock) 
            {
                // does this name already exist?
                UInt32 FoundNameID = 0;
                foreach(var KVP in NameIDs) 
                { 
                    if (KVP.Value == InName)
                    {
                        FoundNameID = KVP.Key;
                        break;
                    }
                }

                // name ID not found - create
                if (FoundNameID == 0)
                {
                    FoundNameID = NextNameID;
                    ++NextNameID;

                    NameIDs.Add(FoundNameID, InName);
                }

                return FoundNameID;
            }
        }

        internal string RetrieveNameFromIDInternal(UInt32 NameID) 
        { 
            lock(_NameIDsLock) 
            {
                string FoundName = null;

                if (NameIDs.TryGetValue(NameID, out FoundName))
                    return FoundName;

                return "## Missing ID ##";
            }
        }
    }