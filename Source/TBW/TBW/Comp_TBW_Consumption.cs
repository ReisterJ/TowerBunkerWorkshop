using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TBW
{
    public class Comp_TBW_Consumption: ThingComp
    {
        protected float stuffConsumptionPerDayBase;

        public float stuffConsumptionPerDayMultiplier;

        public List<ConsumptionType> consumptionList;

        public CompProperties_TBW_Consumption Props
        {
            get
            {
                return (CompProperties_TBW_Consumption)this.props;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

        }

        public virtual void ConsumeProcess()
        {

        }
    }
    public class CompProperties_TBW_Consumption : CompProperties
    {
        public CompProperties_TBW_Consumption()
        {
            this.compClass = typeof(Comp_TBW_Consumption);
        }
        
        public List<ConsumptionType> consumptionList = new List<ConsumptionType>();

        public bool autoRefuel = true;

        public bool consumeOnlyWhenUsed;

        public bool consumeOnlyWhenPowered;

        public float consumptionMultiplier = 1f;

        public ThingFilter filter;

        
    }
    public class ConsumptionType
    {
        public ThingDef stuffDef;

        public float consumptionMultiplierPerPawn;

        public float stuffConsumptionPerDay;

        public bool critical;

        public ConsumptionType Copy()
        {
            ConsumptionType copy = new ConsumptionType()
            {
                stuffDef = this.stuffDef,
                consumptionMultiplierPerPawn = this.consumptionMultiplierPerPawn,
                stuffConsumptionPerDay = this.stuffConsumptionPerDay,
                critical = this.critical    
            };
            return copy;
        }
    }
}
