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
    public class Comp_TBW_GarrisonWeapon : ThingComp,IAttackTargetSearcher
    {
        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        private const float SightRadiusTurret = 30.1f;

        public Thing Weapon;

        protected int burstCooldownTicksLeft;

        protected int burstWarmupTicksLeft;

        protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

        private bool fireAtWill = true;
        
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

        private int lastAttackTargetTick;

        private bool isGarrisoned = false;

        public Thing Thing
        {
            get
            {
                return this.parent;
            }
        }
        public CompProperties_TBW_GarrisonWeapon Props
        {
            get
            {
                return (CompProperties_TBW_GarrisonWeapon)this.props;
            }
        }
        protected void OnAttackedTarget(LocalTargetInfo target)
        {
            this.lastAttackTargetTick = Find.TickManager.TicksGame;
            this.lastAttackedTarget = target;
        }
        public Verb CurrentEffectiveVerb
        {
            get
            {
                return this.AttackVerb;
            }
        }
        public LocalTargetInfo LastAttackedTarget
        {
            get
            {
                return this.lastAttackedTarget;
            }
        }
        public int LastAttackTargetTick
        {
            get
            {
                return this.lastAttackTargetTick;
            }
        }
        public CompEquippable WeaponCompEq
        {
            get
            {
                return this.Weapon.TryGetComp<CompEquippable>();
            }
        }
        public Verb AttackVerb
        {
            get
            {
                return this.WeaponCompEq.PrimaryVerb;
            }
        }
        protected bool WarmingUp
        {
            get
            {
                return this.burstWarmupTicksLeft > 0;
            }
        }
        public virtual bool CanShoot
        {
            get
            {
                Building building ;
                if ((building = (this.parent as Building)) != null)
                {
                    if (building.DestroyedOrNull()) 
                    { 
                        return false;
                    }
                    if (!isGarrisoned && !this.fireAtWill)
                    {
                        return false;
                    }
                }
                CompCanBeDormant compCanBeDormant = this.parent.TryGetComp<CompCanBeDormant>();
                return compCanBeDormant == null || compCanBeDormant.Awake;
            }
        }
        public bool AutoAttack
        {
            get
            {
                return this.Props.AutoAttack;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!this.CanShoot)
            {
                return;
            }
            this.AttackVerb.VerbTick();
            if (this.AttackVerb.state != VerbState.Bursting)
            {
                if (this.WarmingUp)
                {
                    this.burstWarmupTicksLeft--;
                    if (this.burstWarmupTicksLeft == 0)
                    {
                        this.AttackVerb.TryStartCastOn(this.currentTarget, false, true, false, true);
                        this.lastAttackTargetTick = Find.TickManager.TicksGame;
                        this.lastAttackedTarget = this.currentTarget;
                        return;
                    }
                }
                else
                {
                    if (this.burstCooldownTicksLeft > 0)
                    {
                        this.burstCooldownTicksLeft--;
                    }
                    if (this.burstCooldownTicksLeft <= 0 && this.parent.IsHashIntervalTick(10))
                    {
                        this.currentTarget = (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(this, TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable, null, 0f, 9999f);
                        if (this.currentTarget.IsValid)
                        {
                            this.burstWarmupTicksLeft = 1;
                            return;
                        }
                        this.ResetCurrentTarget();
                    }
                }
            }

        }
        public override void PostPostMake()
        {
            base.PostPostMake();
            this.MakeWeapon();
        }
        public virtual void MakeWeapon()
        {
            this.Weapon = ThingMaker.MakeThing(this.Props.MainWeaponDef, null);
            this.UpdateWeaponVerbs();
        }
        protected virtual void UpdateWeaponVerbs()
        {
            List<Verb> allVerbs = this.Weapon.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this.parent;
                verb.castCompleteCallback = delegate ()
                {
                    this.burstCooldownTicksLeft = this.AttackVerb.verbProps.defaultCooldownTime.SecondsToTicks();
                };
            }
        }

        private void ResetCurrentTarget()
        {
            this.currentTarget = LocalTargetInfo.Invalid;
            this.burstWarmupTicksLeft = 0;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<int>(ref this.burstCooldownTicksLeft, "burstCooldownTicksLeft", 0, false);
            Scribe_Values.Look<int>(ref this.burstWarmupTicksLeft, "burstWarmupTicksLeft", 0, false);
            Scribe_TargetInfo.Look(ref this.currentTarget, "currentTarget");
        }
    }

    public class CompProperties_TBW_GarrisonWeapon : CompProperties
    {
        public CompProperties_TBW_GarrisonWeapon(){
            this.compClass = typeof(Comp_TBW_GarrisonWeapon);
        }

        public ThingDef MainWeaponDef;
        public bool AutoAttack = true;

        public List<PawnRenderNodeProperties> renderNodeProperties;
        public List<ThingDef> Weapons;
    }
}
