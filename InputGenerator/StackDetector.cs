// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion
    private void RunScript(
        List<Brep> sticks,
        List<Brep> cubes,
        ref object A)
    {
        // 1. Combine everything into one list with names
        List<Brep> allBreps = new List<Brep>();
        List<string> allNames = new List<string>();

        if (sticks != null)
        {
            for (int i = 0; i < sticks.Count; i++)
            {
                allBreps.Add(sticks[i]);
                allNames.Add("stick" + (i + 1));
            }
        }

        if (cubes != null)
        {
            for (int i = 0; i < cubes.Count; i++)
            {
                allBreps.Add(cubes[i]);
                allNames.Add("cube" + (i + 1));
            }
        }

        if (allBreps.Count < 2) return;

        // ±1 mm contact tolerance
        double contactTol = 0.001;

        List<string> onTopPredicates = new List<string>();

        for (int i = 0; i < allBreps.Count; i++)
        {
            for (int j = i + 1; j < allBreps.Count; j++)
            {
                Brep bA = allBreps[i];
                Brep bB = allBreps[j];
                if (bA == null || bB == null) continue;

                BoundingBox boxA = bA.GetBoundingBox(true);
                BoundingBox boxB = bB.GetBoundingBox(true);

                // Check XY overlap: the two pieces must overlap in plan view
                bool xOverlap = boxA.Min.X <= boxB.Max.X + contactTol &&
                                boxB.Min.X <= boxA.Max.X + contactTol;
                bool yOverlap = boxA.Min.Y <= boxB.Max.Y + contactTol &&
                                boxB.Min.Y <= boxA.Max.Y + contactTol;
                if (!xOverlap || !yOverlap) continue;

                // Check Z contact: the top of one must meet the bottom of the other within ±1mm
                // A is on top of B if A's bottom ≈ B's top
                // B is on top of A if B's bottom ≈ A's top
                double aBottom = boxA.Min.Z;
                double aTop = boxA.Max.Z;
                double bBottom = boxB.Min.Z;
                double bTop = boxB.Max.Z;

                string nameHigh = null;
                string nameLow = null;

                if (Math.Abs(aBottom - bTop) <= contactTol)
                {
                    // A sits on top of B
                    nameHigh = allNames[i];
                    nameLow = allNames[j];
                }
                else if (Math.Abs(bBottom - aTop) <= contactTol)
                {
                    // B sits on top of A
                    nameHigh = allNames[j];
                    nameLow = allNames[i];
                }
                else
                {
                    continue;
                }

                string capHigh = char.ToUpper(nameHigh[0]) + nameHigh.Substring(1);
                string capLow = char.ToUpper(nameLow[0]) + nameLow.Substring(1);

                onTopPredicates.Add(string.Format("OnTop({0} {1})", capHigh, capLow));
            }
        }

        A = onTopPredicates;
    }
}
