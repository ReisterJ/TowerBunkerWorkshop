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
    //DEPRECATED 
    public abstract class Building_MutliplePawns : Building,IThingHolder, ISuspendableThingHolder
    {
        protected ThingOwner innerContainer;

        protected int startTick = -1;

        protected List<Pawn> selectedPawns;

        protected int currentPawnNum
        {

            get
            {
                return this.selectedPawns.Count();
            } 
          
        }

        protected int maxPawnNum;

        public Building_MutliplePawns()
        {
            this.innerContainer = new ThingOwner<Thing>(this);
        }
        public bool Working
        {
            get
            {
                return this.startTick >= 0;
            }
        }
        public virtual bool IsContentsSuspended
        {
            get
            {
                return true;
            }
        }
        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public bool AnyAcceptablePawns
        {
            get
            {
                return base.Spawned && base.Map.mapPawns.AllPawnsSpawned.Any((Pawn x) => this.CanAcceptPawn(x));
            }
        }
        public abstract AcceptanceReport CanAcceptPawn(Pawn p);
        public abstract void TryAcceptPawn(Pawn p);
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            this.innerContainer.TryDropAll(base.Position, base.Map, ThingPlaceMode.Near, null, null, true);
            base.DeSpawn(mode);
        }
        protected virtual void SelectPawn(Pawn pawn)
        {
            if (pawn.Downed)
            {
                return;
            }
            
            if ( ! pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.EnterBuilding, this), new JobTag?(JobTag.Misc), false))
            {
#if DEBUG
                Log.Message("Failed to get in building");   
            }
            Log.Message(pawn.Name.ToStringFull +  " get in building");
#endif
        }
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            //if (!this.Working && this.selectedPawn != null && this.selectedPawn.Map == base.Map)
            //{
            //   GenDraw.DrawLineBetween(this.TrueCenter(), this.selectedPawn.TrueCenter());
            //}
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            /*
            
            foreach (Thing item in ((IEnumerable<Thing>)this.innerContainer))
            {
                Gizmo gizmo2;
                if ((gizmo2 = Building.SelectContainedItemGizmo(this, item)) != null)
                {
                    yield return gizmo2;
                }
            }
            IEnumerator<Thing> enumerator2 = null;
            */
            yield break;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[]
            {
                this
            });
            Scribe_Values.Look<int>(ref this.startTick, "startTick", -1, false);
            Scribe_Collections.Look(ref this.selectedPawns, "selectedPawns", false);
        }
    }
}
