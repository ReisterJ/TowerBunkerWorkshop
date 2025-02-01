using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TBW
{
   
    public class JobDriver_EnterMultiPawnsHolder: JobDriver
    {
        protected int enterDelay = 60;
        protected Comp_TBW_MultiPawnsHolder multipawnsholder => job.targetA.Thing.TryGetComp<Comp_TBW_MultiPawnsHolder>();
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                utility.ifDebugLog("Pre toil reservation failed");
                return false;
            }
            utility.ifDebugLog("Pre toil reservation completed");
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            utility.ifDebugLog($"Try entering {multipawnsholder.parent}");
            utility.ifDebugLog($"Max capacity of {multipawnsholder.parent} is {multipawnsholder.maxPawnNum} ");
            utility.ifDebugLog($"Current pawn num of {multipawnsholder.parent} is {multipawnsholder.currentPawnNum} ");
            utility.ifDebugLog($"target A : {TargetA}");
            //utility.ifDebugLog($"target B : {TargetB}");
            this.FailOnDespawnedOrNull(TargetIndex.A);
            
            this.FailOn(() => !multipawnsholder.CanAcceptPawn(pawn));
            utility.ifDebugLog("New toils : GotoThing");
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            yield return Toils_General.WaitWith(TargetIndex.A, enterDelay, useProgressBar: true);
            utility.ifDebugLog($"New toils : WaitWith {enterDelay}");
            yield return new Toil
            {
                initAction = delegate ()
                {
                    utility.ifDebugLog("New toils : Do TryAcceptPawn");
                    multipawnsholder.TryAcceptPawn(pawn);
                }
            };  
        }
    }
    
}
