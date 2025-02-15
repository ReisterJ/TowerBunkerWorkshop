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

    public abstract class Building_TBW: Building
    {
        protected float InComingDamageMultiplier = 1f;

        protected float InComingDamageStatic = -1f;

        protected float ProductionMultiplier = 1f;

        protected int ProductionStatic = 1;

        protected float OutputDamageMultiplier = 1f;
        
        protected float OutputDamageStatic = -1f;

        protected float ExpMultiplier = 1f;
        public virtual float InComingDamage(float baseDamage)
        {
            if (InComingDamageStatic >= 0)
            {
                return InComingDamageStatic;
            }
            return (InComingDamageMultiplier < 0f ? 0 : InComingDamageMultiplier) * baseDamage;
        }
        
        public virtual void RegenerateHitPoints(int hpRegenerated)
        {
            this.HitPoints+= hpRegenerated;
        }

        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            dinfo.SetAmount(InComingDamage(dinfo.Amount));
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
    }
    public class Building_TBW_Security: Building_TBW, IAttackTargetSearcher,IAttackTarget,ILoadReferenceable
    {
        
        Thing IAttackTarget.Thing => this;
        public float TargetPriorityFactor => 1f;

        public virtual bool isThreat
        {
            get
            {
                return true;
            }

        }

        Thing IAttackTargetSearcher.Thing => this;

        Verb IAttackTargetSearcher.CurrentEffectiveVerb => null;

        LocalTargetInfo IAttackTargetSearcher.LastAttackedTarget => null;

        int IAttackTargetSearcher.LastAttackTargetTick => -1;

        public LocalTargetInfo TargetCurrentlyAimingAt => null;

        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            if (!isThreat)
            {
                return true;
            }

            return false;
        }

        public override void Tick()
        {
            base.Tick();
        }

    }
}
