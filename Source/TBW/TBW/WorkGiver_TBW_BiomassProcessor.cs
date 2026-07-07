using RimWorld;
using System.Collections.Generic;
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

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            Map map = pawn?.Map;
            if (map == null)
            {
                return true;
            }

            return !HasUsableProcessor(map, pawn);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn?.Map?.listerThings.ThingsOfDef(TBW_ThingDefOf.TBW_Biomass_Processor);
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

            return MakeCarryJob(ingredient, t, processor);
        }

        private static bool HasUsableProcessor(Map map, Pawn pawn)
        {
            List<Thing> processors = map.listerThings.ThingsOfDef(TBW_ThingDefOf.TBW_Biomass_Processor);
            for (int i = 0; i < processors.Count; i++)
            {
                Thing processorThing = processors[i];
                Comp_TBW_BiomassProcessor processor = processorThing.TryGetComp<Comp_TBW_BiomassProcessor>();
                if (processor != null
                    && processor.CanAcceptMoreNutrition
                    && !processor.ShouldWaitToSearchForIngredients
                    && !processorThing.IsForbidden(pawn)
                    && !processorThing.IsBurning()
                    && map.designationManager.DesignationOn(processorThing, DesignationDefOf.Deconstruct) == null)
                {
                    return true;
                }
            }

            return false;
        }

        private Job MakeCarryJob(Thing ingredient, Thing processorThing, Comp_TBW_BiomassProcessor processor)
        {
            Job job = JobMaker.MakeJob(TBW_JobDefOf.CarryBiomassToProcessor, ingredient, processorThing);
            job.count = processor.GetAcceptStackCount(ingredient);
            job.haulOpportunisticDuplicates = false;
            return job;
        }

        private bool CanUseProcessor(Pawn pawn, Thing t, bool forced, out Comp_TBW_BiomassProcessor processor)
        {
            processor = t?.TryGetComp<Comp_TBW_BiomassProcessor>();
            if (processor == null || !processor.CanAcceptMoreNutrition)
            {
                return false;
            }

            if (!forced && processor.ShouldWaitToSearchForIngredients)
            {
                return false;
            }

            if (t.Faction != pawn.Faction)
            {
                return false;
            }

            if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
            {
                return false;
            }

            if (t.IsForbidden(pawn) || t.IsBurning())
            {
                return false;
            }

            return pawn.CanReserve(t, 1, -1, null, forced);
        }

        private Thing FindIngredient(Pawn pawn, Comp_TBW_BiomassProcessor processor, bool forced)
        {
            if (pawn?.Map == null || processor == null)
            {
                return null;
            }

            float searchRadius = processor.Props.ingredientSearchRadius;
            if (searchRadius < 0f)
            {
                searchRadius = 0f;
            }

            IntVec3 searchRoot = processor.parent.def.hasInteractionCell ? processor.parent.InteractionCell : processor.parent.Position;
            foreach (ThingDef thingDef in processor.AllowedFilter.AllowedThingDefs)
            {
                if (thingDef == null
                    || !Comp_TBW_BiomassProcessor.IsBiomassThingDef(thingDef)
                    || !processor.FixedFilter.Allows(thingDef)
                    || pawn.Map.listerThings.ThingsOfDef(thingDef).Count == 0)
                {
                    continue;
                }

                Thing found = GenClosest.ClosestThingReachable(
                    searchRoot,
                    pawn.Map,
                    ThingRequest.ForDef(thingDef),
                    PathEndMode.ClosestTouch,
                    TraverseParms.For(pawn),
                    searchRadius,
                    x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && IsValidIngredient(pawn, processor, x, forced),
                    searchRegionsMax: 99999
                );
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private bool IsValidIngredient(Pawn pawn, Comp_TBW_BiomassProcessor processor, Thing thing, bool forced)
        {
            return processor.CanAcceptIngredient(thing)
                && pawn.carryTracker.AvailableStackSpace(thing.def) > 0
                && HaulAIUtility.PawnCanAutomaticallyHaulFast_NewTemp(pawn, thing, forced, checkReachability: false);
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
