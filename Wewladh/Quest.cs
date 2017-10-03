using System.Collections.Generic;

namespace Wewladh
{
    public abstract class QuestFamily
    {
        public string Name { get; protected set; }
        public List<string> QuestTypes { get; private set; }
        public QuestFamily()
        {
            this.Name = string.Empty;
            this.QuestTypes = new List<string>();
        }
        public virtual void QuestCompleted(Player p, Quest q)
        {

        }
        public virtual void FamilyCompleted(Player p, Quest q)
        {

        }
    }

    public abstract class Quest
    {
        public int ID { get; set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public int MinimumLevel { get; protected set; }
        public int MaximumLevel { get; protected set; }
        public List<string> Prerequisites { get; private set; }
        public List<QuestStep> Steps { get; private set; }
        public int CurrentStep { get; set; }
        public QuestProgress Progress { get; set; }
        public QuestStep QuestStep
        {
            get { return Steps[CurrentStep - 1]; }
        }
        public Quest()
        {
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.MinimumLevel = 1;
            this.MaximumLevel = 100;
            this.Prerequisites = new List<string>();
            this.Steps = new List<QuestStep>();
            this.CurrentStep = 1;
        }
        public QuestStep this[int step]
        {
            get { return Steps[step - 1]; }
        }
    }

    public class QuestStep
    {
        public string Description { get; set; }
        public long ExpReward { get; set; }
        public long GoldReward { get; set; }
        public List<string> ItemReward { get; set; }
        public string Reward { get; set; }
        public Dictionary<string, QuestObjective> Objectives { get; private set; }
        public List<string> NpcLocations { get; private set; }
        public QuestProgress Progress { get; set; }
        public QuestDelegate OnAbandon { get; set; }
        public QuestDelegate OnComplete { get; set; }
        public QuestStep()
        {
            this.Description = string.Empty;
            this.Reward = string.Empty;
            this.Objectives = new Dictionary<string, QuestObjective>();
            this.NpcLocations = new List<string>();
            this.ItemReward = new List<string>();
            this.OnAbandon = (p) => { };
            this.OnComplete = (p) => { };
        }
        public QuestObjective this[string objective]
        {
            get { return Objectives[objective]; }
        }
    }

    public class QuestObjective
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int Requirement { get; set; }
        public int Count { get; set; }
        public string MiscData { get; set; }
        public string RequiredItemType { get; set; }
        public string RequiredSkillType { get; set; }
        public string RequiredSpellType { get; set; }
        public List<string> RequiredKilledTypes { get; set; }
        public bool GroupKill { get; set; }
        public QuestObjectiveType Type { get; set; }
        public QuestCondition IsComplete { get; set; }
        public QuestObjective()
        {
            this.Name = string.Empty;
            this.DisplayName = string.Empty;
            this.RequiredItemType = string.Empty;
            this.RequiredSkillType = string.Empty;
            this.RequiredSpellType = string.Empty;
            this.RequiredKilledTypes = new List<string>();
            this.MiscData = string.Empty;
            this.IsComplete = (p) => { return Requirement <= Count; };
        }
    }

    public delegate void QuestDelegate(Player p);
    public delegate bool QuestCondition(Player p);

    public enum QuestObjectiveType
    {
        None,
        Kill,
        Item,
        Skill,
        Spell,
        Misc
    }

    public enum QuestProgress
    {
        Unstarted,
        InProgress,
        Finished
    }
}