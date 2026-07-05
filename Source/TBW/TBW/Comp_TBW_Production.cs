using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TBW
{
    public class Comp_TBW_Production : ThingComp
    {
        protected int ticksUntilSpawn = -1;
        protected bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;

        protected Comp_TBW_MultiPawnsHolder cachedMultiPawnsHolder;

        protected ThingDef thingToSpawn;

        protected TBW_Production_Set currentSet;

        protected int tickstoSpawn ;

        protected int output;

        protected int outputAddPerPawn;

        protected float outputMultiplier;

        protected int spawnCount;

        protected Designator designator;

        public List<TBW_Production_Set> spawnListDefault = new List<TBW_Production_Set>();
        public CompProperties_TBW_Production Props 
        { 
            get
            {
                return (CompProperties_TBW_Production)props;
            }        
        }

        public override void CompTick()
        {
            TickInterval(1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            cachedMultiPawnsHolder = parent.TryGetComp<Comp_TBW_MultiPawnsHolder>();
            if(ticksUntilSpawn < 0)
            {
                ResetCountdown();
            }

            if (spawnListDefault.Count() == 0)
            {
                foreach (TBW_Production_Set tps in Props.spawnList)
                {
                    this.spawnListDefault.Add(tps.Copy());
                }
            }
            RefreshSelectedProductionPreview();
        }
        /*
        public override void comptickrare()
        {
            tickinterval(250);
        }
        */

        protected virtual void TickInterval(int interval)
        {
            if (!parent.Spawned)
            {
                return;
            }
            CompCanBeDormant comp = parent.GetComp<CompCanBeDormant>();
            if (comp != null)
            {
                if (!comp.Awake)
                {
                    return;
                }
            }
            else if (parent.Position.Fogged(parent.Map))
            {
                return;
            }
            if(Props.requirePawn && ( ( null == cachedMultiPawnsHolder ) || ( 0 == cachedMultiPawnsHolder.currentPawnNum )))
            {
                return;
            }
            if (!Props.requiresPower || PowerOn)
            {
                SpawnTick(interval);
                CheckShouldSpawn();
            }
        }

        public virtual void SpawnTick(int interval)
        {
            int tickReduction = interval;
            int pawnNum = cachedMultiPawnsHolder?.currentPawnNum ?? 0;
            if (pawnNum >= 5)
            {
                tickReduction = interval * 2;
            }
            ticksUntilSpawn -= tickReduction;
            
        }
        public void ResetCountdown()
        {
            ticksUntilSpawn = Props.tickstoSpawnDefault;
        }
        protected virtual void CheckShouldSpawn()
        {
            if (ticksUntilSpawn <= 0)
            {
                ResetCountdown();
                TryDoSpawn();
            }
        }
        protected TBW_Production_Set GetSelectedProductionSet()
        {
            return spawnListDefault.FirstOrDefault((TBW_Production_Set x) => x.spawnDef == thingToSpawn) ?? spawnListDefault.FirstOrDefault();
        }
        protected void RefreshSelectedProductionPreview()
        {
            currentSet = GetSelectedProductionSet();
            if (currentSet == null)
            {
                thingToSpawn = null;
                spawnCount = 0;
                return;
            }
            thingToSpawn = currentSet.spawnDef;
            spawnCount = GetOutputForSet(currentSet);
        }
        protected bool UsesSkillBasedOutput()
        {
            return Props.skillOutputBonuses != null && Props.skillOutputBonuses.Count > 0;
        }
        protected int GetOutputForSet(TBW_Production_Set productionSet)
        {
            if (productionSet == null || productionSet.spawnDef == null)
            {
                return 0;
            }

            int outputLocal = productionSet.output;
            if (UsesSkillBasedOutput())
            {
                if (cachedMultiPawnsHolder != null)
                {
                    foreach (Pawn pawn in cachedMultiPawnsHolder.getPawns)
                    {
                        if (pawn?.skills == null)
                        {
                            continue;
                        }
                        foreach (TBW_ProductionSkillBonus skillOutputBonus in Props.skillOutputBonuses)
                        {
                            if (skillOutputBonus?.skillDef == null)
                            {
                                continue;
                            }
                            SkillRecord skillRecord = pawn.skills.GetSkill(skillOutputBonus.skillDef);
                            if (skillRecord != null)
                            {
                                outputLocal += skillRecord.Level * skillOutputBonus.outputAddPerLevel;
                            }
                        }
                    }
                }
            }
            else
            {
                outputLocal += (cachedMultiPawnsHolder?.currentPawnNum ?? 0) * productionSet.outputAddPerPawn;
            }

            return Mathf.Max(0, outputLocal);
        }
        public virtual bool TryDoSpawn()
        {
            if (!parent.Spawned)
            {
                return false;
            }
            RefreshSelectedProductionPreview();
            TBW_Production_Set productionSet = currentSet;
            int outputLocal = GetOutputForSet(productionSet);
            if (productionSet?.spawnDef == null || outputLocal <= 0)
            {
                return false;
            }
            if (Props.spawnMaxAdjacent >= 0)
            {
                int num = 0;
                for (int i = 0; i < 9; i++)
                {
                    IntVec3 c = parent.Position + GenAdj.AdjacentCellsAndInside[i];
                    if (!c.InBounds(parent.Map))
                    {
                        continue;
                    }
                    List<Thing> thingList = c.GetThingList(parent.Map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j].def == thingToSpawn)
                        {
                            num += thingList[j].stackCount;
                            if (num >= Props.spawnMaxAdjacent)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            if(outputLocal <= productionSet.spawnDef.stackLimit)
            {
                SpawnThing(productionSet.spawnDef, outputLocal);
                return true;
            }
            int mod = outputLocal % productionSet.spawnDef.stackLimit;
            int stacknum = outputLocal / productionSet.spawnDef.stackLimit;
            for (int i = 0; i < stacknum; i++) 
            {
                SpawnThing(productionSet.spawnDef , productionSet.spawnDef.stackLimit);
            }
            if( 0 != mod )
            {
                SpawnThing(productionSet.spawnDef, mod);
            }
            
            return true;
        }

        public void SpawnThing(ThingDef thingdef, int spawnNum)
        {
            if (TryFindSpawnCell(parent, thingdef, spawnNum, out var result))
            {
                Thing thing = ThingMaker.MakeThing(thingdef);

                thing.stackCount = spawnNum;

                if (thing == null)
                {
                    Log.Error("Could not spawn anything for " + parent);
                }
                if (Props.inheritFaction && thing.Faction != parent.Faction)
                {
                    thing.SetFaction(parent.Faction);
                }
                GenPlace.TryPlaceThing(thing, result, parent.Map, ThingPlaceMode.Direct, out var lastResultingThing);
                if (Props.spawnForbidden)
                {
                    lastResultingThing.SetForbidden(value: true);
                }
                if (Props.showMessageIfOwned && parent.Faction == Faction.OfPlayer)
                {
                    Messages.Message("MessageCompSpawnerSpawnedItem".Translate(thingdef.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                Log.Error("Could not find spawn cell for " + parent);
            }
        }

        public virtual bool TryFindSpawnCell(Thing parent, ThingDef thingToSpawn, int spawnCount, out IntVec3 result)
        {
            foreach (IntVec3 item in GenAdj.CellsAdjacent8Way(parent).InRandomOrder())
            {
                if (!item.Walkable(parent.Map))
                {
                    continue;
                }
                Building edifice = item.GetEdifice(parent.Map);
                if ((edifice != null && thingToSpawn.IsEdifice()) || (parent.def.passability != Traversability.Impassable && !GenSight.LineOfSight(parent.Position, item, parent.Map)))
                {
                    continue;
                }
                bool flag = false;
                List<Thing> thingList = item.GetThingList(parent.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing = thingList[i];
                    if (thing.def.category == ThingCategory.Item && (thing.def != thingToSpawn || thing.stackCount > thingToSpawn.stackLimit - spawnCount))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    result = item;
                    return true;
                }
            }
            result = IntVec3.Invalid;
            return false;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            string text = (Props.saveKeysPrefix.NullOrEmpty() ? null : (Props.saveKeysPrefix + "_"));
            Scribe_Values.Look(ref ticksUntilSpawn, text + "ticksUntilSpawn", 0);
            Scribe_Defs.Look(ref thingToSpawn, "thingToSpawn");
            Scribe_Values.Look(ref spawnCount, "spawnCount");
        }


        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            /*
            Command_Action command = new Command_Action();
            command.defaultLabel = "DEV: Spawn " + thingToSpawn.label;
            command.icon = TexCommand.DesirePower;
            command.action = delegate
            {
            
            };
            yield return command;
            */
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if ( this.parent.Faction == Faction.OfPlayer && spawnListDefault.Count > 0)
            {
                Command_Action chooseCommand = new Command_Action();
                RefreshSelectedProductionPreview();
                chooseCommand.defaultLabel = thingToSpawn?.label ?? "Select production";
                chooseCommand.icon = thingToSpawn?.uiIcon;
                chooseCommand.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (TBW_Production_Set tps in spawnListDefault)
                    {
                        FloatMenuOption floatMenuOption = new FloatMenuOption(
                                tps.spawnDef.label,
                                delegate
                                {
                                    this.thingToSpawn = tps.spawnDef;
                                    RefreshSelectedProductionPreview();
                                    chooseCommand.defaultLabel = this.thingToSpawn?.label ?? chooseCommand.defaultLabel;
                                    chooseCommand.icon = this.thingToSpawn?.uiIcon;

                                }
                            );
                        list.Add( floatMenuOption );
                    }
                    Find.WindowStack.Add(new FloatMenu(list));

                };
                yield return chooseCommand;
            }
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                RefreshSelectedProductionPreview();
                command_Action.defaultLabel = "DEV: Spawn " + (thingToSpawn?.label ?? "none");
                command_Action.icon = TexCommand.DesirePower;
                command_Action.action = delegate
                {
                    ResetCountdown();
                    TryDoSpawn();
                };
                yield return command_Action;
            }
        }
        public override string CompInspectStringExtra()
        {
            RefreshSelectedProductionPreview();
            if (thingToSpawn != null && Props.writeTimeLeftToSpawn && (!Props.requiresPower || PowerOn))
            {
                return "NextSpawnedItemIn".Translate(GenLabel.ThingLabel(thingToSpawn, null, spawnCount)).Resolve() +"x" + spawnCount.ToString() + ": " + ticksUntilSpawn.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
            }
            return null;
        }
    }

    public class CompProperties_TBW_Production:CompProperties
    {
        public CompProperties_TBW_Production()
        {
            compClass = typeof(Comp_TBW_Production);
        }
        
        public List<Upgrade_Utility> UpgradeResearchProjs;

        //public ThingDef thingToSpawn;

        //public int spawnCount = 1;

        //public IntRange spawnIntervalRange = new IntRange(100, 100);

        public int tickstoSpawnDefault;

        public int spawnMaxAdjacent = -1;

        public bool spawnForbidden;

        public bool requiresPower;

        public bool writeTimeLeftToSpawn = true;

        public bool showMessageIfOwned;

        public string saveKeysPrefix;

        public bool inheritFaction;

        public bool useDefaultSpawnTick = true;

        public bool requirePawn;

        public List<TBW_Production_Set> spawnList;

        public List<TBW_ProductionSkillBonus> skillOutputBonuses;

    }

    public class TBW_Production_Set
    {
        public ThingDef spawnDef;

        public int tickstoSpawn;

        public int output;

        public int outputAddPerPawn;

        public float outputMultiplier = 1f;

        public bool shouldSpawn = true;

        public TBW_Production_Set Copy()
        {
            TBW_Production_Set copy = new TBW_Production_Set();
            copy.spawnDef = spawnDef; 
            copy.tickstoSpawn = tickstoSpawn; 
            copy.output = output;
            copy.outputAddPerPawn = outputAddPerPawn; 
            copy.shouldSpawn = shouldSpawn;
            copy.outputMultiplier = outputMultiplier;
            return copy;
        }
    }

    public class TBW_ProductionSkillBonus
    {
        public SkillDef skillDef;

        public int outputAddPerLevel;
    }

    

}
