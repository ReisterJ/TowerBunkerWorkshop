using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.AI;
using Verse;

namespace TBW
{
    internal class WorkGiver_EnterMultiPawnsHolder : WorkGiver_Scanner
    {

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (pawn.IsPrisonerOfColony)
            {
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }
            if (!t.HasComp<Comp_TBW_MultiPawnsHolder>())
            {
                return false;
            }
            Comp_TBW_MultiPawnsHolder comp = t.TryGetComp<Comp_TBW_MultiPawnsHolder>();
            if (!comp.CanAcceptPawn(pawn))
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.HasComp<Comp_TBW_MultiPawnsHolder>())
            {
                return JobMaker.MakeJob(TBW_JobDefOf.EnterMultiPawnsHolder, t);
            }
            return null;
        }
    }
}
