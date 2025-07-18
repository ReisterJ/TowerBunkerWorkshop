using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TBW
{
    public class JobDriver_CarrytoMultiPawnsHolder : JobDriver
    {
        protected int enterDelay = 60;

        protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.B).Thing;
        protected Comp_TBW_MultiPawnsHolder multipawnsholder => job.targetA.Thing.TryGetComp<Comp_TBW_MultiPawnsHolder>();
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //utility.ifDebugLog($"Try entering {multipawnsholder.parent}");
            //utility.ifDebugLog($"Max capacity of {multipawnsholder.parent} is {multipawnsholder.maxPawnNum} ");
            //utility.ifDebugLog($"Current pawn num of {multipawnsholder.parent} is {multipawnsholder.currentPawnNum} ");
            utility.ifDebugLog($"target A : {TargetA}",true);
            utility.ifDebugLog($"target B : {TargetB}",true);

            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => Takee != null && !multipawnsholder.CanAcceptPawn(Takee));
            
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, enterDelay, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
                multipawnsholder.TryAcceptPawn(Takee);
            });
        }
    }
}
