using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TBW
{
    public class Comp_TBW_GarrisonFire : ThingComp,IAttackTargetSearcher
    {
        protected LocalTargetInfo forcedTarget = LocalTargetInfo.Invalid;

        private static readonly CachedTexture ToggleTurretIcon = new CachedTexture("UI/Gizmos/ToggleTurret");

        protected float SightRadiusTurret = 30.1f;

        public Thing Weapon;

        protected List<Pawn> pawnsGarrisoned = new List<Pawn>();

        protected int burstCooldownTicksLeft;

        protected int burstWarmupTicksLeft;

        protected LocalTargetInfo currentTarget = LocalTargetInfo.Invalid;

        protected bool fireAtWill = true;
        
        private LocalTargetInfo lastAttackedTarget = LocalTargetInfo.Invalid;

        private int lastAttackTargetTick;

        private Comp_TBW_MultiPawnsHolder CachedComp_MultiPawnsHolder = null;

        private int CachedTempGarrisonTroopNum = 0;

        protected int CurrentAmmo = 0;

        protected int MaxAmmo = 0;



        public virtual void LoadAmmo(int ammo)
        {
            this.CurrentAmmo = ( (this.CurrentAmmo + ammo) > MaxAmmo ? MaxAmmo : this.CurrentAmmo += ammo );
        }
        protected void Get_Parent_MultiPawnsHolderComp() 
        {
            Comp_TBW_MultiPawnsHolder mph = this.parent.TryGetComp<Comp_TBW_MultiPawnsHolder>();
            if (mph != null) {
                CachedComp_MultiPawnsHolder = mph;
            }
        }

        public bool isGarrisoned 
        {
            get {
                return GarrisonedPawnCount > 0;
            }  
        }

        protected int GarrisonedPawnCount
        {
            get
            {
                if (CachedComp_MultiPawnsHolder == null)
                {
                    Get_Parent_MultiPawnsHolderComp();
                }
                return CachedComp_MultiPawnsHolder?.currentPawnNum ?? pawnsGarrisoned.Count;
            }
        }

        protected bool PowerOn
        {
            get
            {
                return parent.GetComp<CompPowerTrader>()?.PowerOn ?? false;
            }
        }

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
        public virtual Verb CurrentEffectiveVerb
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
        public virtual Verb AttackVerb
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
                    if (!isGarrisoned)
                    {
                        if (Props.requireGarrisonedPawnToShoot)
                        {
                            return false;
                        }
                        if (Props.requiresPowerWhenUngarrisoned && !PowerOn)
                        {
                            return false;
                        }
                        if (!this.fireAtWill)
                        {
                            return false;
                        }
                    }
                }
                CompCanBeDormant compCanBeDormant = this.parent.TryGetComp<CompCanBeDormant>();
                return compCanBeDormant == null || compCanBeDormant.Awake;
            }
        }

        

        public bool autoAttack
        {
            get
            {
                return this.Props.autoAttack;
            }
        }

        public void GarrisonFireCoolDownTick()
        {
            this.burstCooldownTicksLeft = (CachedTempGarrisonTroopNum > 0 ?
                            this.burstCooldownTicksLeft - this.Props.CDTicksReducePerPawn * CachedTempGarrisonTroopNum :
                            this.burstCooldownTicksLeft - 1);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!this.CanShoot)
            {
                return;
            }
            if (CachedComp_MultiPawnsHolder == null) 
            {
                utility.ifDebugLog($"{this.parent.def.defName.ToString() }'s CachedComp_MultiPawnsHolder is null");
                this.Get_Parent_MultiPawnsHolderComp();
                if (CachedComp_MultiPawnsHolder != null) {
                    utility.ifDebugLog($"{this.parent.def.defName.ToString()}'s CachedComp_MultiPawnsHolder cached");
                    CachedTempGarrisonTroopNum = CachedComp_MultiPawnsHolder.currentPawnNum;
                }
            }
            else
            {
                CachedTempGarrisonTroopNum = CachedComp_MultiPawnsHolder.currentPawnNum;
                //utility.ifDebugLog("CachedTempGarrisonTroopNum is " + CachedTempGarrisonTroopNum.ToString());
            }
            
            this.AttackVerb.VerbTick();
            //utility.ifDebugLog($"Garrison Weapon Verb ");
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
                        GarrisonFireCoolDownTick();
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
            this.Weapon = ThingMaker.MakeThing(this.Props.mainWeaponDef, null);
            this.UpdateWeaponVerbs();
        }
        public virtual void UpdateWeaponVerbs()
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
         
        public virtual void ResetCurrentTarget()
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
            Scribe_Deep.Look(ref this.Weapon, "Weapon");
            Scribe_Values.Look(ref fireAtWill, "fireAtWill", defaultValue: true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (Weapon == null)
                {
                    Log.Error("null weapon after loading. Recreating.");
                    MakeWeapon();
                }
                else
                {
                    UpdateWeaponVerbs();
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (this.Weapon != null)
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.defaultLabel = "CommandToggleTurret".Translate();
                command_Toggle.defaultDesc = "CommandToggleTurretDesc".Translate();
                command_Toggle.isActive = () => fireAtWill;
                command_Toggle.icon = ToggleTurretIcon.Texture;
                command_Toggle.toggleAction = delegate
                {
                    fireAtWill = !fireAtWill;
                };
                yield return command_Toggle;
            }
        }
        public string GetUniqueLoadID()
        {
            return this.parent.GetUniqueLoadID();
        }


    }

    public class CompProperties_TBW_GarrisonWeapon : CompProperties
    {
        public CompProperties_TBW_GarrisonWeapon(){
            this.compClass = typeof(Comp_TBW_GarrisonFire);
        }

        public ThingDef mainWeaponDef;
        public bool autoAttack = true;
        public bool requireGarrisonedPawnToShoot;
        public bool requiresPowerWhenUngarrisoned;
        public int CDTicksReducePerPawn = 1;


        public List<PawnRenderNodeProperties> renderNodeProperties;
        public List<ThingDef> Weapons;
    }
}
