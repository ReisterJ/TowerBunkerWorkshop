using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace TBW
{
    public class JobDriver_CarryBiomassToProcessor : JobDriver_HaulToContainer
    {
        private const TargetIndex IngredientInd = TargetIndex.A;
        private const TargetIndex ProcessorInd = TargetIndex.B;
        private const int InsertDuration = 60;

        private Thing Ingredient => job.GetTarget(IngredientInd).Thing;

        private Comp_TBW_BiomassProcessor Processor => job.GetTarget(ProcessorInd).Thing?.TryGetComp<Comp_TBW_BiomassProcessor>();

        private void EndHaulJobWithCooldown()
        {
            Processor?.NotifyIngredientHaulFailed();
            EndJobWith(JobCondition.Incompletable);
        }

        // public override bool TryMakePreToilReservations(bool errorOnFailed)
        // {
        //     return pawn.Reserve(job.GetTarget(IngredientInd), job, 1, job.count, null, errorOnFailed)
        //         && pawn.Reserve(job.GetTarget(ProcessorInd), job, 1, -1, null, errorOnFailed);
        // }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(ProcessorInd);
            this.FailOnBurningImmobile(ProcessorInd);
            this.FailOn(() => Processor == null || !Processor.CanAcceptMoreNutrition);

            yield return Toils_General.DoAtomic(delegate
            {
                Comp_TBW_BiomassProcessor processor = Processor;
                Thing ingredient = Ingredient;
                if (processor == null || ingredient == null || !processor.CanAcceptIngredient(ingredient))
                {
                    EndHaulJobWithCooldown();
                    return;
                }

                int carrySpace = pawn.carryTracker.AvailableStackSpace(ingredient.def);
                if (carrySpace <= 0)
                {
                    EndHaulJobWithCooldown();
                    return;
                }

                int acceptCount = processor.GetAcceptStackCount(ingredient);
                job.count = acceptCount < carrySpace ? acceptCount : carrySpace;
                if (job.count <= 0)
                {
                    EndHaulJobWithCooldown();
                }
            });
            yield return Toils_Goto.GotoThing(IngredientInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(IngredientInd)
                .FailOnSomeonePhysicallyInteracting(IngredientInd);
            yield return Toils_Haul.StartCarryThing(IngredientInd, putRemainderInQueue: false, subtractNumTakenFromJobCount: true)
                .FailOnDestroyedNullOrForbidden(IngredientInd);
            yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.Touch);
            yield return Toils_General.Wait(InsertDuration, ProcessorInd)
                .FailOnDestroyedNullOrForbidden(ProcessorInd)
                .FailOnCannotTouch(ProcessorInd, PathEndMode.Touch)
                .WithProgressBarToilDelay(ProcessorInd);
            yield return Toils_General.Do(delegate
            {
                Comp_TBW_BiomassProcessor processor = Processor;
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                if (processor == null || carriedThing == null || !processor.TryAcceptIngredient(carriedThing))
                {
                    EndHaulJobWithCooldown();
                }
            });
        }
    }

    public class JobDriver_TakeChaffFromBiomassProcessor : JobDriver
    {
        private const TargetIndex ProcessorInd = TargetIndex.A;
        private const TargetIndex ChaffInd = TargetIndex.B;
        private const TargetIndex StoreCellInd = TargetIndex.C;
        private const int TakeDuration = 60;

        private Comp_TBW_BiomassProcessor Processor => job.GetTarget(ProcessorInd).Thing?.TryGetComp<Comp_TBW_BiomassProcessor>();

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.GetTarget(ProcessorInd), job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(ProcessorInd);
            this.FailOn(() => Processor == null || !Processor.ReadyForHaulingChaff());

            yield return Toils_Goto.GotoThing(ProcessorInd, PathEndMode.InteractionCell);
            yield return Toils_General.WaitWith(ProcessorInd, TakeDuration, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
                Thing chaff = Processor.TakeOutChaff();
                if (chaff == null || !GenPlace.TryPlaceThing(chaff, pawn.Position, Map, ThingPlaceMode.Near))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(chaff);
                if (!StoreUtility.TryFindBestBetterStoreCellFor(chaff, pawn, Map, currentPriority, pawn.Faction, out IntVec3 storeCell))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                job.SetTarget(ChaffInd, chaff);
                job.SetTarget(StoreCellInd, storeCell);
                job.count = chaff.stackCount;
            });
            yield return Toils_Reserve.Reserve(ChaffInd);
            yield return Toils_Reserve.Reserve(StoreCellInd);
            yield return Toils_Goto.GotoThing(ChaffInd, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(ChaffInd);
            yield return Toils_Haul.CarryHauledThingToCell(StoreCellInd, PathEndMode.Touch);
            yield return Toils_Haul.PlaceHauledThingInCell(StoreCellInd, null, storageMode: true);
        }
    }
}
