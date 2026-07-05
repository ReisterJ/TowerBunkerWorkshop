using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TBW
{
    public class ITab_HolderContent : ITab_ContentsBase
    {
        public ITab_HolderContent()
        {
            labelKey = "TabMultiPawnsHolderContents";
            containedItemsKey = "ContainedItems";
            size = new Vector2(460f, 450f);
        }

        protected Comp_TBW_MultiPawnsHolder HolderComp => base.SelThing?.TryGetComp<Comp_TBW_MultiPawnsHolder>();

        public override IList<Thing> container
        {
            get
            {
                return (IList<Thing>)(HolderComp?.getPawns?.Cast<Thing>().ToList() ?? new List<Thing>());
            }
        }

        public override bool IsVisible
        {
            get
            {
                return base.SelThing != null
                    && HolderComp != null
                    && (base.SelThing.Faction == null || base.SelThing.Faction == Faction.OfPlayer);
            }
        }

        public override IntVec3 DropOffset => IntVec3.Zero;

        protected override void OnDropThing(Thing t, int count)
        {
            if (t == null || base.SelThing == null || base.SelThing.Map == null)
            {
                return;
            }

            Comp_TBW_MultiPawnsHolder holderComp = HolderComp;
            ThingOwner owner = holderComp?.GetDirectlyHeldThings();
            if (holderComp == null || owner == null)
            {
                return;
            }

            if (owner.TryDrop(t, base.SelThing.Position + DropOffset, base.SelThing.Map, ThingPlaceMode.Near, count, out Thing _, null, null) && t is Pawn pawn)
            {
                holderComp.DropPawn(pawn);
            }
        }
    }
}
