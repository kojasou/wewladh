using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Wewladh
{
    public static class Extensions
    {
        public static int Count<TItem>(this IEnumerable<Item> items) where TItem : Item
        {
            int count = 0;
            foreach (Item i in items)
            {
                if (i != null && i.GetType() == typeof(TItem))
                    count += i.Amount;
            }
            return count;
        }
        public static int IndexOf<TItem>(this Item[] items) where TItem : Item
        {
            for (int i = 0; i < items.Length; i++)
            {
                if ((items[i] != null) && (items[i].GetType() == typeof(TItem)))
                    return i;
            }
            return -1;
        }
        public static bool Contains<TItem>(this IEnumerable<Item> items) where TItem : Item
        {
            foreach (var item in items)
            {
                if ((item != null) && (item.GetType() == typeof(TItem)))
                    return true;
            }
            return false;
        }
        public static bool Contains<TSkill>(this IEnumerable<Skill> skills) where TSkill : Skill
        {
            foreach (var skill in skills)
            {
                if ((skill != null) && (skill.GetType() == typeof(TSkill)))
                    return true;
            }
            return false;
        }
        public static bool Contains<TSpell>(this IEnumerable<Spell> spells) where TSpell : Spell
        {
            foreach (var spell in spells)
            {
                if ((spell != null) && (spell.GetType() == typeof(TSpell)))
                    return true;
            }
            return false;
        }

        public static int Count(this IEnumerable<Item> items, string name)
        {
            int count = 0;
            foreach (Item i in items)
            {
                if (i != null && i.GetType().Name == name)
                    count += i.Amount;
            }
            return count;
        }
        public static int IndexOf(this Item[] items, string name)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if ((items[i] != null) && (items[i].GetType().Name == name))
                    return i;
            }
            return -1;
        }
        public static int IndexOf(this Skill[] skills, string name)
        {
            for (int i = 0; i < skills.Length; i++)
            {
                if ((skills[i] != null) && (skills[i].GetType().Name == name))
                    return i;
            }
            return -1;
        }
        public static int IndexOf(this Spell[] spells, string name)
        {
            for (int i = 0; i < spells.Length; i++)
            {
                if ((spells[i] != null) && (spells[i].GetType().Name == name))
                    return i;
            }
            return -1;
        }
        public static bool Contains(this IEnumerable<Item> items, string name)
        {
            foreach (var item in items)
            {
                if ((item != null) && (item.GetType().Name == name))
                    return true;
            }
            return false;
        }
        public static bool Contains(this IEnumerable<Skill> skills, string name)
        {
            foreach (var skill in skills)
            {
                if ((skill != null) && (skill.GetType().Name == name))
                    return true;
            }
            return false;
        }
        public static bool Contains(this IEnumerable<Spell> spells, string name)
        {
            foreach (var spell in spells)
            {
                if ((spell != null) && (spell.GetType().Name == name))
                    return true;
            }
            return false;
        }
    }
}