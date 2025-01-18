using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.Sound;

namespace TBW
{
    [StaticConstructorOnStartup]
    public class Building_Tower : Building_MutliplePawns
    {
        public override AcceptanceReport CanAcceptPawn(Pawn pawn)
        {
            if(this.currentPawnNum >= this.maxPawnNum)
            {
                return "NoMoreRoom";
            } 
            return (pawn.IsColonist || pawn.IsSlaveOfColony ) && !pawn.IsQuestLodger();
        }

        public override void TryAcceptPawn(Pawn pawn)
        {
            
            if (this.CanAcceptPawn(pawn))
            {
                this.selectedPawns.Add(pawn);
                bool flag = pawn.DeSpawnOrDeselect(DestroyMode.Vanish);
                if (this.innerContainer.TryAddOrTransfer(pawn, true))
                {
                    this.startTick = Find.TickManager.TicksGame;
                }
                if (flag)
                {
                    Find.Selector.Select(pawn, false, false);
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            
        }
    }
}
