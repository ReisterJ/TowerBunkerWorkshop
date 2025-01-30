using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.Sound;
using static System.Net.Mime.MediaTypeNames;

namespace TBW
{
    public class Comp_TBW_MultiPawnsHolder : ThingComp, IThingHolder , ISuspendableThingHolder
    {
        protected ThingOwner innerContainer;
        
        protected List<Pawn> insidePawns = new List<Pawn>();

        protected List<Thing> insideThings = new List<Thing>();

        
        //protected int baseMaxPawnNum;

        public int maxPawnNumOffset = 0;
        private int startTick = -1;

        private int currentPawnNum => insidePawns.Count();
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
 
            innerContainer.TryDropAll(parent.InteractionCell, destMap, ThingPlaceMode.Near);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Values.Look<int>(ref this.startTick, "startTick", -1, false);
            Scribe_Collections.Look(ref this.insidePawns, "insidePawns", false);
        }
        public virtual bool CanAcceptPawn(Pawn pawn)
        {
            if(this.currentPawnNum < this.maxPawnNum)
                return true;
            return false;
        }
        public virtual bool TryAcceptPawn(Pawn pawn)
        {
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
