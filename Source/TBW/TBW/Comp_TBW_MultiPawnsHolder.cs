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
    public class Comp_TBW_MultiPawnsHolder : ThingComp, IThingHolder, ISuspendableThingHolder
    {
        protected ThingOwner innerContainer;

        protected List<Pawn> insidePawns = new List<Pawn>();

        protected List<Thing> insideThings = new List<Thing>();

        public IReadOnlyList<Pawn> getPawns => insidePawns;
        protected bool allowSlaveAndPrisoner => Props.allowSlaveAndPrisoner;

        public List<Pair<Pawn, int>> pawnHolderTickRecord = new List<Pair<Pawn, int>>();

        public int maxPawnNumOffset = 0;
        //private int startTick = -1;

        public int currentPawnNum => insidePawns.Count();

        public override void CompTick()
        {
            base.CompTick();
            innerContainer.DoTick();
            if (IsContentsSuspended)
            {
                foreach (Pawn pawn in insidePawns)
                {
                    pawn.ageTracker.AgeTickInterval(1);
                }
            }
        }
        public Comp_TBW_MultiPawnsHolder()
        {
            innerContainer = new ThingOwner<Thing>(this);
        }

        public int maxPawnNum
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
        public bool IsContentsSuspended
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
            if (!allowSlaveAndPrisoner) yield break;
            List<Pawn> pawnstoCarry = SlaveAndPrisonerPawnstoCarry(pawn);
            if (null != pawnstoCarry)
            {
                foreach (Pawn takee in pawnstoCarry)
                {
                    if (null != takee)
                    {
                        // utility.ifDebugLog(takee.Name, true);
                        string carryText = "Carry" + takee.Name.ToString() + " to " + this.parent.def.label.ToString();
                        yield return new FloatMenuOption(carryText.Translate(), delegate
                            {
                                // utility.ifDebugLog("make job TBW_JobDefOf.CarrytoMultiPawnsHolder", true);
                                Job job2 = JobMaker.MakeJob(TBW_JobDefOf.CarrytoMultiPawnsHolder, parent, takee);
                                job2.count = 1;
                                pawn.jobs.TryTakeOrderedJob(job2, JobTag.Misc);
                            }
                        );
                    }
                }
            }
        }

        protected List<Pawn> SlaveAndPrisonerPawnstoCarry(Pawn worker)
        {
            List<Pawn> pawnlist = worker.Map.mapPawns.SlavesAndPrisonersOfColonySpawned.Where((Pawn x) => x != worker).ToList();
            return pawnlist;
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
            IntVec3 dropPoint = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
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

        protected void ClearAllPawn()
        {
            List<Pawn> insidePawnstemp = new List<Pawn>();
            int tempCount = insidePawns.Count;
            for (int i = 0; i < tempCount; i++) { 
                insidePawnstemp.Add(insidePawns[i]);
            }
            for (int i = 0; i < tempCount; i++)
            {
                DropPawn(insidePawnstemp[i]);
            }
            if(0 != insidePawns.Count)
            {
                Log.Warning("Container cleared but still got pawns inside");
            }
            if(0 != pawnHolderTickRecord.Count)
            {
                Log.Warning("Container tick record cleared but still got record remaining");
            }
        }
        public virtual void DropPawn(Pawn pawn)
        {
            if(null != pawn)
            {
                if (insidePawns.Contains(pawn))
                {
                    insidePawns.Remove(pawn);
                }
                else {
                    Log.Error( pawn.Name + " is not in this container");
                }

                for (int i = pawnHolderTickRecord.Count - 1; i >= 0; i--)
                {
                    if (pawnHolderTickRecord[i].First == pawn)
                    {
                        OnLeavingBuilding(pawn);
                        pawnHolderTickRecord.RemoveAt(i);

                    }
                }
            }
        }
        protected void OnLeavingBuilding(Pawn pawn)
        {
            //todo
            return;
        }
        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            EjectContents(map);
            base.PostDeSpawn(map, mode);
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
            if (this.currentPawnNum < this.maxPawnNum)
            {
                utility.ifDebugLog("Can accept pawn " + pawn.Name.ToStringFull);
                return true;
            }
            utility.ifDebugLog("Can't accept pawn " + pawn.Name.ToStringFull);
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (base.parent.Faction == Faction.OfPlayer && innerContainer.Count() > 0)
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

            if (this.GetDirectlyHeldThings().TryAddOrTransfer(pawn))
            {
                this.insidePawns.Add(pawn);
                Pair<Pawn, int> tempRecord = new Pair<Pawn, int>(pawn, Find.TickManager.TicksThisFrame);
                this.pawnHolderTickRecord.Add(tempRecord);
                if (num)
                {
                    Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
                    
                }
                return true;
            }

            return false;
        }
        public override string CompInspectStringExtra()
        {
            string holder = "Holder";
            string text = holder.Translate() + " : " + currentPawnNum.ToString() + " / " + maxPawnNum.ToString();
            return text;
        }
    }

    public class CompProperties_TBW_MultiPawnsHolder : CompProperties
    {
        public CompProperties_TBW_MultiPawnsHolder() {
            this.compClass = typeof(Comp_TBW_MultiPawnsHolder);
        }
        public int baseMaxPawnNum = 4;//for list size optimization
        public bool isSuspended = true;
        public bool allowSlaveAndPrisoner = true;
    }

    
}
