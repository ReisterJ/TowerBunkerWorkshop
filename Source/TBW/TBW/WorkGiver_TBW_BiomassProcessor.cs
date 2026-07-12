using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TBW
{
    public class WorkGiver_TBW_HaulBiomassToProcessor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(TBW_ThingDefOf.TBW_Biomass_Processor);

        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!CanUseProcessor(pawn, t, forced, out Comp_TBW_BiomassProcessor processor))
            {
                return false;
            }

            if (FindIngredient(pawn, processor, forced) == null)
            {
                NotifyIngredientSearchFailed(pawn, processor, forced);
                JobFailReason.Is("No acceptable biomass.");
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!CanUseProcessor(pawn, t, forced, out Comp_TBW_BiomassProcessor processor))
            {
                return null;
            }

            Thing ingredient = FindIngredient(pawn, processor, forced);
            if (ingredient == null)
            {
                NotifyIngredientSearchFailed(pawn, processor, forced);
                return null;
            }

            int acceptCount = processor.GetAcceptStackCount(ingredient);
            if (acceptCount <= 0)
            {
                NotifyIngredientSearchFailed(pawn, processor, forced);
                return null;
            }

            Job job = HaulAIUtility.HaulToContainerJob(pawn, ingredient, t);
            if (job == null)
            {
                return null;
            }

            job.count = Mathf.Min(job.count, acceptCount);
            return job;
        }

        private static bool CanUseProcessor(Pawn pawn, Thing t, bool forced, out Comp_TBW_BiomassProcessor processor)
        {
            processor = t?.TryGetComp<Comp_TBW_BiomassProcessor>();
            if (pawn == null
                || processor == null
                || !processor.CanRequestIngredientHaul
                || (!forced && processor.ShouldWaitToSearchForIngredients)
                || !pawn.CanReserve(t, 1, -1, null, forced))
            {
                return false;
            }

            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }

            return !t.IsForbidden(pawn) && !t.IsBurning();
        }

        private Thing FindIngredient(Pawn pawn, Comp_TBW_BiomassProcessor processor, bool forced)
        {
            if (pawn?.Map == null)
            {
                return null;
            }

            Thing food = null;
            if (processor.AllowsFoodIngredientSearch)
            {
                food = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.FoodSourceNotPlantOrTree),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn, MaxPathDanger(pawn)),
                    90f,
                    Validator);
            }

            Thing corpse = null;
            if (processor.AllowsCorpseIngredientSearch)
            {
                corpse = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.Corpse),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn, MaxPathDanger(pawn)),
                    90f,
                    Validator);
            }

            if (food == null)
            {
                return corpse;
            }

            if (corpse == null)
            {
                return food;
            }

            return (pawn.Position - food.Position).LengthHorizontalSquared <= (pawn.Position - corpse.Position).LengthHorizontalSquared
                ? food
                : corpse;

            bool Validator(Thing thing)
            {
                if (thing.IsForbidden(pawn)
                    || !thing.IsInValidStorage()
                    || !processor.CanAcceptIngredientForHauling(thing)
                    || !pawn.CanReserve(thing, 1, -1, null, forced))
                {
                    return false;
                }

                SlotGroup slotGroup = thing.GetSlotGroup();
                return slotGroup?.parent != null && slotGroup.parent.HaulDestinationEnabled;
            }
        }

        private static void NotifyIngredientSearchFailed(Pawn pawn, Comp_TBW_BiomassProcessor processor, bool forced)
        {
            if (!forced && FloatMenuMakerMap.makingFor != pawn)
            {
                processor.NotifyIngredientSearchFailed();
            }
        }
    }

    public class WorkGiver_TBW_HaulCorpseBiomassToProcessor : WorkGiver_TBW_HaulBiomassToProcessor
    {
    }

    public class WorkGiver_TBW_HaulFoodBiomassToProcessor : WorkGiver_TBW_HaulBiomassToProcessor
    {
    }

    public class WorkGiver_TBW_TakeChaffFromBiomassProcessor : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(TBW_ThingDefOf.TBW_Biomass_Processor);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return CanUseProcessor(pawn, t, forced, out Comp_TBW_BiomassProcessor _);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!CanUseProcessor(pawn, t, forced, out Comp_TBW_BiomassProcessor _))
            {
                return null;
            }

            return JobMaker.MakeJob(TBW_JobDefOf.TakeChaffFromBiomassProcessor, t);
        }

        private bool CanUseProcessor(Pawn pawn, Thing t, bool forced, out Comp_TBW_BiomassProcessor processor)
        {
            processor = t?.TryGetComp<Comp_TBW_BiomassProcessor>();
            return processor != null
                && processor.ReadyForHaulingChaff()
                && !t.IsForbidden(pawn)
                && pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, forced);
        }

    }
}
