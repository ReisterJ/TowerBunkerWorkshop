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
                utility.ifDebugLog("pawn Is Prisoner Of Colony");
                return false;
            }
            if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                utility.ifDebugLog("pawn can't reserve toil");
                return false;
            }
            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                utility.ifDebugLog("deconstructed");
                return false;
            }
            if (!t.HasComp<Comp_TBW_MultiPawnsHolder>())
            {
                utility.ifDebugLog("this thing doesn't have Comp_TBW_MultiPawnsHolder");
                return false;
            }
            Comp_TBW_MultiPawnsHolder comp = t.TryGetComp<Comp_TBW_MultiPawnsHolder>();
            if (!comp.CanAcceptPawn(pawn))
            {
                utility.ifDebugLog("this thing can't accept pawns any more");
                return false;
            }
            utility.ifDebugLog("has job check successful");
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.HasComp<Comp_TBW_MultiPawnsHolder>())
            {
                utility.ifDebugLog("Make job start");
                Job job = JobMaker.MakeJob(TBW_JobDefOf.EnterMultiPawnsHolder, t);
                
                utility.ifDebugLog(job);
                utility.ifDebugLog("Make job end");
                return job;
            }
            utility.ifDebugLog("Make job NULL");
            return null;
        }
    }
}
