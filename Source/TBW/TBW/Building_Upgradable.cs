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
    public abstract class Building_Upgradable : Building
    {
        protected float InComingDamageMultiplier = 1f;

        protected float InComingDamageStatic = -1f;

        protected float ProductionMultiplier = 1f;

        protected int ProductionStatic = 1;

        protected float OutputDamageMultiplier = 1f;
        
        protected float OutputDamageStatic = 1f;

        protected float ExpMultiplier = 1f;
        public virtual float InComingDamage(float baseDamage)
        {
            if (InComingDamageStatic >= 0)
            {
                return InComingDamageStatic;
            }
            return (InComingDamageMultiplier <= 0f ? 0 : InComingDamageMultiplier) * baseDamage;
        }
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            dinfo.SetAmount(InComingDamage(dinfo.Amount));
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
        public override void Tick()
        {
            base.Tick();
        }
        public abstract void UpgradeModule();

        
    }
}
