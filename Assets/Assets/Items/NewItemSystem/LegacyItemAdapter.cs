// LegacyItemAdapter.cs
using System;
using NewItemSystem;
using UnityEngine;

public static class LegacyItemAdapter {
    public static ItemInstance From(ItemObject legacy) {
        if (legacy == null) return null;
        try {
            var def = ItemRegistry.Get(legacy.id);
            if (def == null) {
                Debug.LogError($"No ItemDefinition found for id {legacy.id} when converting legacy item {legacy.name}");
                return null;
            }
            var inst = new ItemInstance(def);
            inst.uuid = legacy.uuid != Guid.Empty ? legacy.uuid : Guid.NewGuid();

            // Fill basic stackable data
            var stack = inst.GetState<StackState>();
            if (legacy is baitObject bait) {
                stack.currentAmount = bait.throwIns;
            } else if (legacy is FishObject fish) {
                stack.currentAmount = fish.amount;
            } else {
                stack.currentAmount = 1;
            }

            // Durability
            if (legacy is rodObject rod) {
                var dur = new DurabilityState { remaining = rod.throwIns };
                inst.SetState(dur);
            } else if (legacy is baitObject bait2) {
                var dur = new DurabilityState { remaining = bait2.throwIns };
                inst.SetState(dur);
            }

            return inst;
        } catch (Exception e) {
            Debug.LogError($"Failed to convert legacy item {legacy} : {e}");
            return null;
        }
    }
} 