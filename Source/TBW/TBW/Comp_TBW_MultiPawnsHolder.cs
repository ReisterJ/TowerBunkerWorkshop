using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace TBW
{
    public class Comp_TBW_MultiPawnsHolder : ThingComp, IThingHolder , ISuspendableThingHolder
    {
        protected ThingOwner innerContainer;
        
        protected List<Pawn> insidePawns = new List<Pawn>();

        protected List<Thing> insideThings = new List<Thing>();

        public List<Pair<Pawn,int>> pawnHolderTickRecord = new List<Pair<Pawn,int>>();

        public int maxPawnNumOffset = 0;
        //private int startTick = -1;

        public int currentPawnNum => insidePawns.Count();

        public override void CompTick()
        {
            base.CompTick();
            innerContainer.ThingOwnerTick();
            if (IsContentsSuspended)
            {
                foreach (Pawn pawn in insidePawns)
                {
                    pawn.ageTracker.AgeTick();
                }
            }
        }
        public Comp_TBW_MultiPawnsHolder() 
        {
            innerContainer = new ThingOwner<Thing>(this);
        }
        
        public virtual int maxPawnNum
        {
            get
            {
                return Props.baseMaxPawnNum + maxPawnNumOffset;
            }
        }
        public CompProperties_TBW_MultiPawnsHolder Props
        {
            get
            {
                return (CompProperties_TBW_MultiPawnsHolder)this.props;
            }
        }
        public virtual bool IsContentsSuspended
        {
            get
            {
                return this.Props.isSuspended;
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn pawn)
        {
            if (pawn.IsQuestLodger())
            {
                yield return new FloatMenuOption("CannotEnter".Translate() + ": " + "GuestsNotAllowed".Translate().CapitalizeFirst(), null);
                yield break;
            }
            string text = "Enter".Translate();
            yield return new FloatMenuOption(text, delegate
            {
                Job job = JobMaker.MakeJob(TBW_JobDefOf.EnterMultiPawnsHolder, parent);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public virtual void EjectContents(Map destMap = null)
        {
            if (destMap == null)
            {
                destMap = parent.Map;
            }
            utility.ifDebugLog($"Pawn num of {this.parent.def.defName.ToString()} is {this.currentPawnNum}.Ejecting all pawns.");
            IntVec3 dropPoint = parent.InteractionCell != null ? parent.InteractionCell : parent.Position;
            innerContainer.TryDropAll(dropPoint, destMap, ThingPlaceMode.Near);
            this.insidePawns.Clear();
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (mode == DestroyMode.Deconstruct || mode == DestroyMode.KillFinalize)
            {
                utility.ifDebugLog($"{this.parent.def.defName.ToString()} was destroyed.");
                EjectContents(previousMap);
            }
            innerContainer.ClearAndDestroyContents();
            base.PostDestroy(mode, previousMap);
        }
        public override void PostDeSpawn(Map map)
        {
            EjectContents(map);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
            //Scribe_Values.Look<int>(ref this.startTick, "startTick", -1, false);

            Scribe_Collections.Look(ref this.insidePawns, "insidePawns", false, LookMode.Reference);
        }
        public virtual bool CanAcceptPawn(Pawn pawn)
        {
#if DEBUG
            string pawns = "pawn";
            if (currentPawnNum > 1) pawns = "pawns";
            Log.Message("this building has " + currentPawnNum.ToString() + " " + pawns + ". Max capacity is " + maxPawnNum.ToString());
#endif
            if(this.currentPawnNum < this.maxPawnNum)
            {
                utility.ifDebugLog("Can accept pawn "+ pawn.Name.ToStringFull );
                return true;
            }
            utility.ifDebugLog("Can't accept pawn " + pawn.Name.ToStringFull);
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (base.parent.Faction == Faction.OfPlayer && innerContainer.Count() > 0 )
            {
                Command_Action command_Action = new Command_Action();
                command_Action.action = delegate
                {
                    EjectContents();

                };    
                command_Action.defaultLabel = "CommandEject";
                command_Action.defaultDesc = "CommandEjectDesc";
                if (innerContainer.Count == 0)
                {
                    command_Action.Disable("CommandEjectFailEmpty");
                }
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/PodEject");
                yield return command_Action;
            }   
            if (DebugSettings.ShowDevGizmos) {  
                yield break; 
            }
        }

        public virtual bool TryAcceptPawn(Pawn pawn)
        {
            if (!CanAcceptPawn(pawn)) return false;
            bool num = pawn.DeSpawnOrDeselect();
#if DEBUG
            Log.Message(pawn.Name);
#endif
            if (this.GetDirectlyHeldThings().TryAdd(pawn))
            {
                this.insidePawns.Add(pawn);
#if DEBUG
                Log.Message("add " + pawn.Name.ToString() + " to " + this.parent.def.defName.ToString());
#endif
                Pair<Pawn,int> tempRecord = new Pair<Pawn, int>(pawn,Find.TickManager.TicksThisFrame);
                
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                }
                return true;
            }

            return false; 
        }
    }

    public class CompProperties_TBW_MultiPawnsHolder : CompProperties 
    {
        public CompProperties_TBW_MultiPawnsHolder() {
            this.compClass = typeof(Comp_TBW_MultiPawnsHolder);
        }
        public int baseMaxPawnNum;
        public bool isSuspended = true;

    }
}
