using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace TBW
{
    public class Comp_TBW_Production : ThingComp
    {
        protected int ticksUntilSpawn;
        protected bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;
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

        public override void CompTickRare()
        {
            TickInterval(250);
        }

        protected void TickInterval(int interval)
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
            if (!Props.requiresPower || PowerOn)
            {
                ticksUntilSpawn -= interval;
                CheckShouldSpawn();
            }
        }
        public void ResetCountdown()
        {
            ticksUntilSpawn = Props.spawnIntervalRange.RandomInRange;
        }
        protected virtual void CheckShouldSpawn()
        {
            if (ticksUntilSpawn <= 0)
            {
                ResetCountdown();
                TryDoSpawn();
            }
        }
        public virtual bool TryDoSpawn()
        {
            if (!parent.Spawned)
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
                        if (thingList[j].def == Props.thingToSpawn)
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
            if (TryFindSpawnCell(parent, Props.thingToSpawn, Props.spawnCount, out var result))
            {
                Thing thing = ThingMaker.MakeThing(Props.thingToSpawn);
                thing.stackCount = Props.spawnCount;
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
                    Messages.Message("MessageCompSpawnerSpawnedItem".Translate(Props.thingToSpawn.LabelCap), thing, MessageTypeDefOf.PositiveEvent);
                }
                return true;
            }
            return false;
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
            string text = (Props.saveKeysPrefix.NullOrEmpty() ? null : (Props.saveKeysPrefix + "_"));
            Scribe_Values.Look(ref ticksUntilSpawn, text + "ticksUntilSpawn", 0);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Spawn " + Props.thingToSpawn.label;
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
            if (Props.writeTimeLeftToSpawn && (!Props.requiresPower || PowerOn))
            {
                return "NextSpawnedItemIn".Translate(GenLabel.ThingLabel(Props.thingToSpawn, null, Props.spawnCount)).Resolve() + ": " + ticksUntilSpawn.ToStringTicksToPeriod().Colorize(ColoredText.DateTimeColor);
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

        public ThingDef thingToSpawn;

        public int spawnCount = 1;

        public IntRange spawnIntervalRange = new IntRange(100, 100);

        public int spawnMaxAdjacent = -1;

        public bool spawnForbidden;

        public bool requiresPower;

        public bool writeTimeLeftToSpawn;

        public bool showMessageIfOwned;

        public string saveKeysPrefix;

        public bool inheritFaction;



    }
}
