using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace TBW
{
    public class ITab_HolderContent : ITab
    {
        protected Vector2 scrollPosition;

        protected float lastDrawnHeight;



        protected List<Thing> thingsToSelect = new List<Thing>();

        public bool canRemoveThings = true;

        protected static List<Thing> tmpSingleThing = new List<Thing>();

        protected const float TopPadding = 20f;

        protected const float SpaceBetweenItemsLists = 10f;

        protected const float ThingRowHeight = 28f;

        protected const float ThingIconSize = 28f;

        protected const float ThingLeftX = 36f;

        protected static readonly Color ThingLabelColor = ITab_Pawn_Gear.ThingLabelColor;

        protected static readonly Color ThingHighlightColor = ITab_Pawn_Gear.HighlightColor;

        public string containedItemsKey;

        public virtual IList<Thing> container => GetSelfComp()?.GetDirectlyHeldThings().ToList();

        protected Comp_TBW_MultiPawnsHolder cachedCompMultiPawnsHolder => GetSelfComp();

        public virtual IntVec3 DropOffset => IntVec3.Zero;

        public virtual bool UseDiscardMessage => true;

        public ITab_HolderContent()
        {
            labelKey = "TabMultiPawnsHolderContents";
            containedItemsKey = "ContainedItems";
        }

        public Comp_TBW_MultiPawnsHolder GetSelfComp() {
            return base.SelThing.TryGetComp<Comp_TBW_MultiPawnsHolder>();
        }

        public override bool IsVisible
        {
            get
            {
                if ((base.SelThing.Faction == null || base.SelThing.Faction == Faction.OfPlayer) && cachedCompMultiPawnsHolder != null)
                {
                    
                    return true;
                }
                return false;
            }
        }
        protected override void FillTab()
        {
            thingsToSelect.Clear();
            Rect outRect = new Rect(default(Vector2), size).ContractedBy(10f);
            outRect.yMin += 20f;
            Rect rect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(lastDrawnHeight, outRect.height));
            Text.Font = GameFont.Small;
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect);
            float curY = 0f;
            DoItemsLists(rect, ref curY);
            lastDrawnHeight = curY;
            Widgets.EndScrollView();
            if (thingsToSelect.Any())
            {
                ITab_Pawn_FormingCaravan.SelectNow(thingsToSelect);
                thingsToSelect.Clear();
            }
        }

        protected virtual void DoItemsLists(Rect inRect, ref float curY)
        {
            Widgets.BeginGroup(inRect);
            Widgets.ListSeparator(ref curY, inRect.width, containedItemsKey.Translate());
            bool flag = false;
            if (cachedCompMultiPawnsHolder != null)
            {
                int tempCount = cachedCompMultiPawnsHolder.currentPawnNum;
                for (int i = 0; i < tempCount; i++)
                {
                    Pawn pawn = cachedCompMultiPawnsHolder.getPawns[i];
                    if (pawn != null)
                    {
                        tmpSingleThing.Clear();
                        tmpSingleThing.Add(pawn);
                        flag = true;
                        DoThingRow(pawn.def, pawn.stackCount, tmpSingleThing, inRect.width, ref curY, delegate (int x)
                        {
                            OnDropThing(pawn, x);
                        });
                        tmpSingleThing.Clear();
                    }
                }
            }
            if (!flag)
            {
                Widgets.NoneLabel(ref curY, inRect.width);
            }
            Widgets.EndGroup();
           
        }


        protected virtual void OnDropThing(Thing t, int count)
        {
            if(t is Pawn pawn)
            {
                cachedCompMultiPawnsHolder.DropPawn(pawn);
            }
            GenDrop.TryDropSpawn(t.SplitOff(count), base.SelThing.Position + DropOffset, base.SelThing.Map, ThingPlaceMode.Near, out var _);
        }

        protected void DoThingRow(ThingDef thingDef, int count, List<Thing> things, float width, ref float curY, Action<int> discardAction)
        {
            Rect rect = new Rect(0f, curY, width, 28f);
            if (canRemoveThings)
            {
                if (count != 1 && Widgets.ButtonImage(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), CaravanThingsTabUtility.AbandonSpecificCountButtonTex))
                {
                    Find.WindowStack.Add(new Dialog_Slider("RemoveSliderText".Translate(thingDef.label), 1, count, discardAction));
                }
                rect.width -= 24f;
                if (Widgets.ButtonImage(new Rect(rect.x + rect.width - 24f, rect.y + (rect.height - 24f) / 2f, 24f, 24f), CaravanThingsTabUtility.AbandonButtonTex))
                {
                    if (UseDiscardMessage)
                    {
                        string text = thingDef.label;
                        if (things.Count == 1 && things[0] is Pawn)
                        {
                            text = ((Pawn)things[0]).LabelShortCap;
                        }
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmRemoveItemDialog".Translate(text), delegate
                        {
                            discardAction(count);
                        }));
                    }
                    else
                    {
                        discardAction(count);
                    }
                }
                rect.width -= 24f;
            }
            if (things.Count == 1)
            {
                Widgets.InfoCardButton(rect.width - 24f, curY, things[0]);
            }
            else
            {
                Widgets.InfoCardButton(rect.width - 24f, curY, thingDef);
            }
            rect.width -= 24f;
            if (Mouse.IsOver(rect))
            {
                GUI.color = ThingHighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            if (thingDef.DrawMatSingle != null && thingDef.DrawMatSingle.mainTexture != null)
            {
                Rect rect2 = new Rect(4f, curY, 28f, 28f);
                if (things.Count == 1)
                {
                    Widgets.ThingIcon(rect2, things[0], 1f, null);
                }
                else
                {
                    Widgets.ThingIcon(rect2, thingDef, null, null, 1f, null, null);
                }
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect3 = new Rect(36f, curY, rect.width - 36f, rect.height);
            string text2 = ((things.Count != 1 || count != things[0].stackCount) ? GenLabel.ThingLabel(thingDef, null, count).CapitalizeFirst() : things[0].LabelCap);
            Text.WordWrap = false;
            Widgets.Label(rect3, text2.StripTags().Truncate(rect3.width));
            Text.WordWrap = true;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(rect, text2);
            if (Widgets.ButtonInvisible(rect))
            {
                SelectLater(things);
            }
            if (Mouse.IsOver(rect))
            {
                for (int i = 0; i < things.Count; i++)
                {
                    TargetHighlighter.Highlight(things[i]);
                }
            }
            curY += 28f;
        }

        protected void SelectLater(List<Thing> things)
        {
            thingsToSelect.Clear();
            thingsToSelect.AddRange(things);
        }
    }
}
