
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TokenizableStrings;
using StardewValley.Inventories;
using StardewValley.GameData.Machines;
using Netcode;

// modified version of Object.
// the only difference is that machines of this type can take arbitrary Items as inputs rather than just Objects
// and various things have been removed that mean it shouldn't be used for non-machines
// we use Extra Machine Config for non-Object outputs

namespace StardewValley {
    public class ArbitraryItemMachine : Object {

        public new readonly NetRef<Item> heldObject = new NetRef<Item>();

        public ArbitraryItemMachine(Vector2 tileLocation, string itemId, bool isRecipe = false) : base() {
            itemId = ValidateUnqualifiedItemId(itemId);
            base.isRecipe.Value = isRecipe;
            base.ItemId = itemId;
            canBeSetDown.Value = true;
            bigCraftable.Value = true;
            if (Game1.bigCraftableData.TryGetValue(itemId, out var data)) {
                name = data.Name ?? ItemRegistry.GetDataOrErrorItem(base.QualifiedItemId).InternalName;
                price.Value = data.Price;
                type.Value = "Crafting";
                base.Category = -9;
                setOutdoors.Value = data.CanBePlacedOutdoors;
                setIndoors.Value = data.CanBePlacedIndoors;
                fragility.Value = data.Fragility;
                isLamp.Value = data.IsLamp;
            }
            ResetParentSheetIndex();
            TileLocation = tileLocation;
            initializeLightSource(this.tileLocation.Value);
	    }

        public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who, bool returnFalseIfItemConsumed = false) {
            if (isTemporarilyInvisible) {
                return false;
            }
            GameLocation location = Location;
            if (dropInItem.QualifiedItemId == "(O)872" && autoLoadFrom == null && TryApplyFairyDust(probe)) {
                return true;
            }
            MachineData machineData = GetMachineData();
            if (machineData != null) {
                if (heldObject.Value != null && !machineData.AllowLoadWhenFull) {
                    return false;
                }
                if (probe && MinutesUntilReady > 0) {
                    return false;
                }
                if (PlaceInMachine(machineData, dropInItem, probe, who)) {
                    if (returnFalseIfItemConsumed && !probe) {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            return false;
        }

        public new bool PlaceInMachine(MachineData machineData, Item inputItem, bool probe, Farmer who, bool showMessages = true, bool playSounds = true) {
            if (machineData == null || inputItem == null) {
                return false;
            }
            if (heldObject.Value != null) {
                if (!machineData.AllowLoadWhenFull) {
                    return false;
                }
                if (inputItem.QualifiedItemId == lastInputItem.Value?.QualifiedItemId) {
                    return false;
                }
            }
            if (!MachineDataUtility.HasAdditionalRequirements(autoLoadFrom ?? who.Items, machineData.AdditionalConsumedItems, out var failedRequirement)) {
                if (showMessages && failedRequirement.InvalidCountMessage != null && !probe && autoLoadFrom == null) {
                    CurrentParsedItemCount = failedRequirement.RequiredCount;
                    Game1.showRedMessage(TokenParser.ParseText(failedRequirement.InvalidCountMessage, null, ParseItemCount));
                    who.ignoreItemConsumptionThisFrame = true;
                }
                return false;
            }
            GameLocation location = Location;
            if (!MachineDataUtility.TryGetMachineOutputRule(this, machineData, MachineOutputTrigger.ItemPlacedInMachine, inputItem, who, location, out var outputRule, out var triggerRule, out var outputRuleIgnoringCount, out var triggerIgnoringCount)) {
                if (showMessages && !probe && autoLoadFrom == null) {
                    if (outputRuleIgnoringCount != null) {
                        string invalidCountMessage = outputRuleIgnoringCount.InvalidCountMessage ?? machineData.InvalidCountMessage;
                        if (!string.IsNullOrWhiteSpace(invalidCountMessage)) {
                            CurrentParsedItemCount = triggerIgnoringCount.RequiredCount;
                            Game1.showRedMessage(TokenParser.ParseText(invalidCountMessage, null, ParseItemCount));
                            who.ignoreItemConsumptionThisFrame = true;
                        }
                    }
                    else if (machineData.InvalidItemMessage != null && GameStateQuery.CheckConditions(machineData.InvalidItemMessageCondition, location, who, null, who.ActiveObject)) {
                        Game1.showRedMessage(TokenParser.ParseText(machineData.InvalidItemMessage));
                        who.ignoreItemConsumptionThisFrame = true;
                    }
                }
                return false;
            }
            if (probe) {
                return true;
            }
            if (!OutputMachine(machineData, outputRule, inputItem, who, location, probe)) {
                return false;
            }
            if (machineData.AdditionalConsumedItems != null) {
                IInventory inventory = autoLoadFrom ?? who.Items;
                foreach (MachineItemAdditionalConsumedItems additionalRequirement in machineData.AdditionalConsumedItems) {
                    inventory.ReduceId(additionalRequirement.ItemId, additionalRequirement.RequiredCount);
                }
            }
            if (triggerRule.RequiredCount > 0) {
                ConsumeInventoryItem(who, inputItem, triggerRule.RequiredCount);
            }
            if (machineData.LoadEffects != null) {
                foreach (MachineEffects effect in machineData.LoadEffects) {
                    if (PlayMachineEffect(effect, playSounds)) {
                        _machineAnimation = effect;
                        _machineAnimationLoop = false;
                        _machineAnimationIndex = 0;
                        _machineAnimationFrame = -1;
                        _machineAnimationInterval = 0;
                        break;
                    }
                }
            }
            //base.playCustomMachineLoadEffects();//?
            MachineDataUtility.UpdateStats(machineData.StatsToIncrementWhenLoaded, inputItem, 1);
            return true;
        }
    }
}