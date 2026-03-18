using RogueDeal.Items;
using System.Collections.Generic;
using System.Linq;

namespace RogueDeal.Player
{
    public class Inventory
    {
        private readonly Dictionary<BaseItem, int> items = new Dictionary<BaseItem, int>();
        private int gold = 0;

        public int Gold => gold;
        public IReadOnlyDictionary<BaseItem, int> Items => items;

        public bool AddItem(BaseItem item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return false;

            if (items.ContainsKey(item))
            {
                items[item] += quantity;
            }
            else
            {
                items[item] = quantity;
            }

            return true;
        }

        public bool RemoveItem(BaseItem item, int quantity = 1)
        {
            if (item == null || !items.ContainsKey(item))
                return false;

            if (items[item] < quantity)
                return false;

            items[item] -= quantity;
            
            if (items[item] <= 0)
            {
                items.Remove(item);
            }

            return true;
        }

        public bool HasItem(BaseItem item, int quantity = 1)
        {
            if (item == null || !items.ContainsKey(item))
                return false;

            return items[item] >= quantity;
        }

        public int GetItemCount(BaseItem item)
        {
            if (item == null || !items.ContainsKey(item))
                return 0;

            return items[item];
        }

        public void AddGold(int amount)
        {
            gold += amount;
            if (gold < 0)
                gold = 0;
        }

        public bool SpendGold(int amount)
        {
            if (gold < amount)
                return false;

            gold -= amount;
            return true;
        }

        public List<T> GetItemsOfType<T>() where T : BaseItem
        {
            return items.Keys.OfType<T>().ToList();
        }

        public void Clear()
        {
            items.Clear();
            gold = 0;
        }
    }
}
