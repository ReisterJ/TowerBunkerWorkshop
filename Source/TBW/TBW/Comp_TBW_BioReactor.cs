using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public class Comp_TBW_BioReactor : CompPowerPlant
    {
        public CompProperties_TBW_BioReactor Properties
        { 
            get
            {
                return (CompProperties_TBW_BioReactor)this.props;
            }
        }
        protected Comp_TBW_MultiPawnsHolder cachedCompMultiPawnsHolder;

        protected float powerOutputPerPawnMultiplier = 1f;
        protected float powerOutputPerPawnBase => Properties.powerOutputPerPawn;
        protected float nutritionConsumptionMultiplierBase => Properties.nutritionConsumptionMultiplier;

        protected virtual void CompsInit()
        {
            cachedCompMultiPawnsHolder = this.parent.TryGetComp<Comp_TBW_MultiPawnsHolder>();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            CompsInit();

        }

        public bool CompCheck()
        {
            if(this.cachedCompMultiPawnsHolder != null )
            {
                return true;
            }    
            return false;
        }

        public override void CompTick()
        {
            if (!CompCheck())
            {
                CompsInit();
            }
            if (CompCheck())
            {
                //FuelConsumptionSet();
                base.CompTick();
                this.UpdateDesiredPowerOutput();
            }
        }
        protected void FuelConsumptionSet()
        {
            //string private_ConsumptionRatePerTick = "ConsumptionRatePerTick";
            if(refuelableComp == null)
            {
                refuelableComp = parent.TryGetComp<CompRefuelable>();
            }
        }
        protected virtual float FuelConsumptionMultiplier()
        {
            float multiplier = 1f; 
            int pawnnum = cachedCompMultiPawnsHolder.currentPawnNum;
            for (int i = 0; i < pawnnum; i++) {
                multiplier *= this.Properties.nutritionConsumptionMultiplier;
            }
            return multiplier;
        }
        public override void UpdateDesiredPowerOutput()
        {
            if ((breakdownableComp != null && breakdownableComp.BrokenDown) || (refuelableComp != null && !refuelableComp.HasFuel) || (flickableComp != null && !flickableComp.SwitchIsOn) || (autoPoweredComp != null && !autoPoweredComp.WantsToBeOn) || (toxifier != null && !toxifier.CanPolluteNow) || !base.PowerOn)
            {
                base.PowerOutput = 0f;
            }
            else
            {
                base.PowerOutput = DesiredPowerOutput + (cachedCompMultiPawnsHolder.currentPawnNum * powerOutputPerPawnBase * PowerOutputPerPawnMultiplier);
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
        }
        public override void PostDeSpawn(Map map, DestroyMode mode)
        {
            base.PostDeSpawn(map, mode);
        }

        public virtual float PowerOutputPerPawnMultiplier 
        { 
            get 
            { 
                return powerOutputPerPawnMultiplier; 
            } 
        }
    }

    public class CompProperties_TBW_BioReactor : CompProperties_Power
    {
        public float powerOutputPerPawn;
        public float nutritionConsumptionMultiplier;
        
      
    }

}
