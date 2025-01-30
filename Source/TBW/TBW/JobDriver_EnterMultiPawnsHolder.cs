using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TBW
{
   
    public class JobDriver_TBW_EnterMultiPawnsHolder: JobDriver
    {
        protected int enterDelay = 60;
        protected Comp_TBW_MultiPawnsHolder multipawnsholder => job.targetA.Thing.TryGetComp<Comp_TBW_MultiPawnsHolder>();
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            List<LocalTargetInfo> targetQueue = job.GetTargetQueue(TargetIndex.B);
            for (int i = 0; i < targetQueue.Count; i++)
            {
                if (!pawn.Reserve(targetQueue[i], job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            this.FailOn(() => !multipawnsholder.CanAcceptPawn(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, enterDelay, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
               multipawnsholder.TryAcceptPawn(pawn);
            });
        }
    }
    public class JobDriver_EnterTower : JobDriver_TBW_EnterMultiPawnsHolder     
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            List<LocalTargetInfo> targetQueue = job.GetTargetQueue(TargetIndex.B);
            for (int i = 0; i < targetQueue.Count; i++)
            {
                if (!pawn.Reserve(targetQueue[i], job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            this.FailOn(() => !multipawnsholder.CanAcceptPawn(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, enterDelay, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
                multipawnsholder.TryAcceptPawn(pawn);
            });
        }
    }
    public class JobDriver_EnterBunker : JobDriver_TBW_EnterMultiPawnsHolder
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed))
            {
                return false;
            }
            List<LocalTargetInfo> targetQueue = job.GetTargetQueue(TargetIndex.B);
            for (int i = 0; i < targetQueue.Count; i++)
            {
                if (!pawn.Reserve(targetQueue[i], job, 1, -1, null, errorOnFailed))
                {
                    return false;
                }
            }
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            this.FailOn(() => !multipawnsholder.CanAcceptPawn(pawn));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(TargetIndex.A, enterDelay, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
                multipawnsholder.TryAcceptPawn(pawn);
            });
        }
    }
}
