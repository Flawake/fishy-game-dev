namespace TradeSystem
{
    using System;
    using ItemSystem;
    using Mirror;
    using UnityEngine;

    static class TradabilityRules
    {
        private static readonly Type[] TradableBaseTypes =
        {
            typeof(ShellBehaviour),
            typeof(FishBehaviour),
            typeof(BaitBehaviour),
        };

        public static bool IsTradable(ItemInstance item)
        {
            if (item.def.IsStatic || item.def.InfiniteUse)
            {
                return false;
            }
            foreach (Type tradableType in TradableBaseTypes)
            {
                if (item.def.GetBehaviour(tradableType) != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public enum TradableItemType
    {
        Bucks,
        Item
    }

    public class TradableItem
    {
        public TradableItemType Type { get; }
        public ItemInstance ItemInst { get; }
        public int Amount { get; internal set; }
        private TradableItem(TradableItemType type, int amount, ItemInstance item)
        {
            Type = type;
            Amount = amount;
            ItemInst = item;
        }

        public static TradableItem Bucks(int amount)
        {
            return new TradableItem(TradableItemType.Bucks, amount, null);
        }

        public bool isValid()
        {
            if (Type == TradableItemType.Item)
            {
                return TradabilityRules.IsTradable(ItemInst);
            }
            if (Type == TradableItemType.Bucks)
            {
                return true;
            }
            return false;
        }

        public static TradableItem FromItem(ItemInstance item, int amount)
        {
            if (!TradabilityRules.IsTradable(item))
            {
                //This should kick the player requesting to trade the item
                throw new InvalidOperationException($"{item.GetType().Name} is not tradable");
            }
            return new TradableItem(TradableItemType.Item, amount, item);
        }

        public Sprite GetSprite()
        {
            if (Type == TradableItemType.Bucks) {
                return null;
            }
            else
            {
                return ItemInst.def.Icon;
            }
        }

        public void SetAmount(int amount)
        {
            Amount = amount;
        }

        public int GetAmount()
        {
            return Amount;
        }
    }

    public static class TradableItemSerializer
    {
        public static void WriteTradableItem(this NetworkWriter writer, TradableItem item)
        {
            writer.WriteInt((int)item.Type);
            writer.WriteInt(item.Amount);

            bool hasItem = item.ItemInst != null;
            writer.WriteBool(hasItem);

            if (hasItem)
            {
                writer.Write(item.ItemInst);
            }
        }
        
        public static TradableItem ReadTradableItem(this NetworkReader reader)
        {
            TradableItemType type = (TradableItemType)reader.ReadInt();
            int amount = reader.ReadInt();

            bool hasItem = reader.ReadBool();
            ItemInstance item = null;
            if (hasItem)
            {
                item = reader.Read<ItemInstance>();
            }

            return type switch
            {
                TradableItemType.Bucks => TradableItem.Bucks(amount),
                TradableItemType.Item => TradableItem.FromItem(item, amount),
                _ => throw new InvalidOperationException("Unknown TradableItemType")
            };
        }
    }
}