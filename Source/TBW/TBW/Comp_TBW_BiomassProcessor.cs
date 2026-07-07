using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TBW
{
    public class Comp_TBW_BiomassProcessor : ThingComp
    {
        private ThingFilter allowedFilter;
        private ThingFilter fixedFilter;
        private float storedNutrition;
        private int storedChaff;
        private int filterVersion;
        private int nextTickToSearchForIngredients;

        private static readonly CachedTexture ConfigureIcon = new CachedTexture("UI/Commands/LaunchShip");
        private static readonly CachedTexture EjectContentsIcon = new CachedTexture("UI/Commands/PodEject");
        private static readonly IntRange ReCheckFailedIngredientTicksRange = new IntRange(500, 600);
        private const int CurrentFilterVersion = 3;
        private const int FailedHaulRetryDelayTicks = 180;

        public CompProperties_TBW_BiomassProcessor Props => (CompProperties_TBW_BiomassProcessor)props;

        private CompPowerTrader PowerTrader => parent.GetComp<CompPowerTrader>();

        private bool PowerOn => PowerTrader?.PowerOn ?? true;

        public float StoredNutrition => storedNutrition;

        public int StoredChaff => storedChaff;

        public float MaxNutrition => Props.maxNutrition;

        public bool CanAcceptMoreNutrition => storedNutrition < MaxNutrition - 0.001f;

        public bool ShouldWaitToSearchForIngredients => Find.TickManager != null
            && Find.TickManager.TicksGame <= nextTickToSearchForIngredients;

        public ThingFilter AllowedFilter
        {
            get
            {
                EnsureFilters();
                return allowedFilter;
            }
        }

        public ThingFilter FixedFilter
        {
            get
            {
                EnsureFilters();
                return fixedFilter;
            }
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            EnsureFilters();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            EnsureFilters();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref allowedFilter, "allowedFilter");
            Scribe_Values.Look(ref filterVersion, "filterVersion", 0);
            Scribe_Values.Look(ref storedNutrition, "storedNutrition", 0f);
            Scribe_Values.Look(ref storedChaff, "storedChaff", 0);
            Scribe_Values.Look(ref nextTickToSearchForIngredients, "nextTickToSearchForIngredients", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureFilters();
                storedNutrition = Mathf.Clamp(storedNutrition, 0f, MaxNutrition);
                storedChaff = Mathf.Max(0, storedChaff);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!PowerOn || storedNutrition <= 0f)
            {
                return;
            }

            float consumed = Mathf.Min(Props.nutritionConsumedPerRareTick, storedNutrition);
            if (consumed <= 0f)
            {
                return;
            }

            storedNutrition = Mathf.Max(0f, storedNutrition - consumed);
            storedChaff += Props.chaffProducedPerRareTick;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Biomass input filter",
                    defaultDesc = "Configure which foods and corpses can be processed into biomass.",
                    icon = ConfigureIcon.Texture,
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_TBW_BiomassProcessorFilter(this));
                    }
                };

                if (storedChaff > 0)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Eject fermentation chaff",
                        defaultDesc = "Eject all stored fermentation chaff from this biomass processor.",
                        icon = EjectContentsIcon.Texture,
                        activateSound = SoundDefOf.Tick_Tiny,
                        action = EjectContents
                    };
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            return "Biomass nutrition: " + storedNutrition.ToString("0.#") + " / " + MaxNutrition.ToString("0.#")
                + "\nStored fermentation chaff: " + storedChaff;
        }

        public void NotifyIngredientSearchFailed()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            nextTickToSearchForIngredients = Find.TickManager.TicksGame + ReCheckFailedIngredientTicksRange.RandomInRange;
        }

        public void NotifyIngredientHaulFailed()
        {
            if (Find.TickManager == null)
            {
                return;
            }

            nextTickToSearchForIngredients = Mathf.Max(nextTickToSearchForIngredients, Find.TickManager.TicksGame + FailedHaulRetryDelayTicks);
        }

        public void NotifyIngredientHaulSucceeded()
        {
            nextTickToSearchForIngredients = 0;
        }

        public bool CanAcceptIngredient(Thing thing)
        {
            return CanAcceptMoreNutrition
                && thing != null
                && !thing.Destroyed
                && IsBiomassCandidate(thing)
                && IsAllowedByFilter(thing)
                && GetNutritionPerUnit(thing) > 0f;
        }

        public int GetAcceptStackCount(Thing thing)
        {
            if (!CanAcceptIngredient(thing))
            {
                return 0;
            }

            float nutritionPerUnit = GetNutritionPerUnit(thing);
            int countByCapacity = Mathf.CeilToInt((MaxNutrition - storedNutrition) / nutritionPerUnit);
            return Mathf.Clamp(countByCapacity, 1, thing.stackCount);
        }

        public bool TryAcceptIngredient(Thing thing)
        {
            if (!CanAcceptIngredient(thing))
            {
                return false;
            }

            storedNutrition = Mathf.Min(MaxNutrition, storedNutrition + GetNutrition(thing));
            thing.Destroy(DestroyMode.Vanish);
            NotifyIngredientHaulSucceeded();
            return true;
        }

        public bool ReadyForHaulingChaff()
        {
            return storedChaff >= Props.minChaffToHaul;
        }

        public Thing MakeChaffThingForHauling()
        {
            if (storedChaff <= 0 || TBW_ThingDefOf.TBW_Fermentation_Chaff == null)
            {
                return null;
            }

            Thing thing = ThingMaker.MakeThing(TBW_ThingDefOf.TBW_Fermentation_Chaff);
            thing.stackCount = Mathf.Min(storedChaff, thing.def.stackLimit);
            return thing;
        }

        public Thing TakeOutChaff()
        {
            Thing thing = MakeChaffThingForHauling();
            if (thing == null)
            {
                return null;
            }

            storedChaff -= thing.stackCount;
            return thing;
        }

        private void EjectContents()
        {
            while (storedChaff > 0)
            {
                Thing chaff = TakeOutChaff();
                if (chaff == null)
                {
                    break;
                }

                GenPlace.TryPlaceThing(chaff, parent.Position, parent.Map, ThingPlaceMode.Near);
            }
        }

        private void EnsureFilters()
        {
            if (fixedFilter == null)
            {
                fixedFilter = new ThingFilter();
                ConfigureFixedFilter(fixedFilter);
            }
            else
            {
                RepairFilterDisplay(fixedFilter);
            }

            if (allowedFilter == null)
            {
                allowedFilter = new ThingFilter();
                ConfigureDefaultAllowedFilter(allowedFilter);
                filterVersion = CurrentFilterVersion;
            }
            else if (filterVersion < CurrentFilterVersion)
            {
                ConfigureDefaultAllowedFilter(allowedFilter);
                filterVersion = CurrentFilterVersion;
            }
            else
            {
                RepairFilterDisplay(allowedFilter);
            }
        }

        private void ConfigureFixedFilter(ThingFilter filter)
        {
            if (Props.fixedIngredientFilter != null)
            {
                filter.CopyAllowancesFrom(Props.fixedIngredientFilter);
                RepairFilterDisplay(filter);
                return;
            }

            ConfigureFallbackFixedFilter(filter);
        }

        private static void ConfigureFallbackFixedFilter(ThingFilter filter)
        {
            filter.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                filter.SetAllow(thingDef, IsBiomassThingDef(thingDef));
            }
            filter.SetAllow(ThingCategoryDefOf.Corpses, true, DefDatabase<ThingDef>.AllDefsListForReading, DefDatabase<SpecialThingFilterDef>.AllDefsListForReading);
            filter.SetAllow(ThingCategoryDefOf.Foods, true, DefDatabase<ThingDef>.AllDefsListForReading, DefDatabase<SpecialThingFilterDef>.AllDefsListForReading);
            foreach (SpecialThingFilterDef specialThingFilterDef in DefDatabase<SpecialThingFilterDef>.AllDefsListForReading)
            {
                filter.SetAllow(specialThingFilterDef, true);
            }
            RepairFilterDisplay(filter);
        }

        private void ConfigureDefaultAllowedFilter(ThingFilter filter)
        {
            if (Props.defaultIngredientFilter != null)
            {
                filter.CopyAllowancesFrom(Props.defaultIngredientFilter);
                LimitFilterToFixedFilter(filter);
                RepairFilterDisplay(filter);
                return;
            }

            ConfigureFallbackDefaultAllowedFilter(filter);
            LimitFilterToFixedFilter(filter);
            RepairFilterDisplay(filter);
        }

        private static void ConfigureFallbackDefaultAllowedFilter(ThingFilter filter)
        {
            filter.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                filter.SetAllow(thingDef, IsCorpseThingDef(thingDef));
            }
            filter.SetAllow(ThingCategoryDefOf.Corpses, true, DefDatabase<ThingDef>.AllDefsListForReading, DefDatabase<SpecialThingFilterDef>.AllDefsListForReading);
            filter.SetAllow(ThingCategoryDefOf.Foods, false, DefDatabase<ThingDef>.AllDefsListForReading, DefDatabase<SpecialThingFilterDef>.AllDefsListForReading);
            filter.SetAllow(SpecialThingFilterDefOf.AllowFresh, false);
            SpecialThingFilterDef allowRotten = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail("AllowRotten");
            if (allowRotten != null)
            {
                filter.SetAllow(allowRotten, true);
            }
            RepairFilterDisplay(filter);
        }

        private void LimitFilterToFixedFilter(ThingFilter filter)
        {
            if (fixedFilter == null)
            {
                return;
            }

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!fixedFilter.Allows(thingDef))
                {
                    filter.SetAllow(thingDef, false);
                }
            }

            foreach (SpecialThingFilterDef specialThingFilterDef in DefDatabase<SpecialThingFilterDef>.AllDefsListForReading)
            {
                if (!fixedFilter.Allows(specialThingFilterDef))
                {
                    filter.SetAllow(specialThingFilterDef, false);
                }
            }
        }

        private static void RepairFilterDisplay(ThingFilter filter)
        {
            if (filter == null)
            {
                return;
            }

            filter.RecalculateDisplayRootCategory();
            filter.DisplayRootCategory = ThingCategoryNodeDatabase.RootNode;
        }

        public static bool IsBiomassThingDef(ThingDef thingDef)
        {
            return IsCorpseThingDef(thingDef) || IsFoodThingDef(thingDef);
        }

        public static bool IsCorpseThingDef(ThingDef thingDef)
        {
            return thingDef != null
                && (thingDef.IsWithinCategory(ThingCategoryDefOf.Corpses)
                    || (thingDef.thingClass != null && typeof(Corpse).IsAssignableFrom(thingDef.thingClass)));
        }

        public static bool IsFoodThingDef(ThingDef thingDef)
        {
            return thingDef != null
                && (thingDef.IsWithinCategory(ThingCategoryDefOf.Foods)
                    || thingDef.IsNutritionGivingIngestible);
        }

        public static bool IsBiomassCandidate(Thing thing)
        {
            if (thing is Corpse corpse)
            {
                return corpse.InnerPawn?.RaceProps?.IsFlesh ?? false;
            }

            return thing?.def?.IsNutritionGivingIngestible ?? false;
        }

        private bool IsAllowedByFilter(Thing thing)
        {
            return FixedFilter.Allows(thing) && AllowedFilter.Allows(thing);
        }

        private float GetNutrition(Thing thing)
        {
            return GetNutritionPerUnit(thing) * thing.stackCount;
        }

        private float GetNutritionPerUnit(Thing thing)
        {
            float nutrition = 0f;
            try
            {
                nutrition = thing.GetStatValue(StatDefOf.Nutrition);
            }
            catch
            {
                nutrition = 0f;
            }

            if (nutrition <= 0f && thing.def != null)
            {
                nutrition = thing.def.GetStatValueAbstract(StatDefOf.Nutrition);
            }

            if (nutrition <= 0f && thing is Corpse corpse && corpse.InnerPawn != null)
            {
                nutrition = corpse.InnerPawn.BodySize * Props.corpseNutritionPerBodySize;
            }

            return Mathf.Max(0f, nutrition);
        }
    }

    public class CompProperties_TBW_BiomassProcessor : CompProperties
    {
        public float maxNutrition = 100f;
        public float nutritionConsumedPerRareTick = 0.1f;
        public int chaffProducedPerRareTick = 2;
        public int minChaffToHaul = 2;
        public float corpseNutritionPerBodySize = 4f;
        public float ingredientSearchRadius = 999f;
        public ThingFilter fixedIngredientFilter;
        public ThingFilter defaultIngredientFilter;
        public List<SpecialThingFilterDef> forceHiddenSpecialFilters;

        public CompProperties_TBW_BiomassProcessor()
        {
            compClass = typeof(Comp_TBW_BiomassProcessor);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            fixedIngredientFilter?.ResolveReferences();
            defaultIngredientFilter?.ResolveReferences();
        }
    }

    public class Dialog_TBW_BiomassProcessorFilter : Window
    {
        private readonly Comp_TBW_BiomassProcessor processor;
        private readonly ThingFilterUI.UIState filterState = new ThingFilterUI.UIState();

        public Dialog_TBW_BiomassProcessorFilter(Comp_TBW_BiomassProcessor processor)
        {
            this.processor = processor;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(560f, 640f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "Biomass input filter");
            Text.Font = GameFont.Small;

            Rect filterRect = new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
            ThingFilterUI.DoThingFilterConfigWindow(filterRect, filterState, processor.AllowedFilter, processor.FixedFilter, 8, null, processor.Props.forceHiddenSpecialFilters, false, false, false, null, processor.parent.Map);
        }
    }
}
