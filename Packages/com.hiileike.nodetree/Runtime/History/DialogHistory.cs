using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeTree
{
    [Serializable]
    public class DialogHistoryEntry
    {
        public int Index;
        public int ConversationOrder;
        public string OwnerId;
        public string OwnerName;
        public string Text;
        public bool IsPlayerChoice;
    }

    [Serializable]
    public class DialogConversationRecord
    {
        public int Order;
        public string TreeName;
        public int FirstEntryIndex;
        public int EntryCount;
    }

    [Serializable]
    public class DialogHistory
    {
        public const string PlayerName = "You";

        public List<DialogHistoryEntry> Entries = new List<DialogHistoryEntry>();
        public List<DialogConversationRecord> Conversations = new List<DialogConversationRecord>();

        public event Action Changed;

        public DialogConversationRecord Current => Conversations.Count > 0 ? Conversations[Conversations.Count - 1] : null;

        public void BeginConversation(string treeName)
        {
            Conversations.Add(new DialogConversationRecord
            {
                Order = Conversations.Count + 1,
                TreeName = treeName,
                FirstEntryIndex = Entries.Count,
            });
            Changed?.Invoke();
        }

        public void AddLine(string ownerId, string ownerName, string text)
        {
            Add(ownerId, ownerName, text, false);
        }

        public void AddChoice(string text)
        {
            Add(string.Empty, PlayerName, text, true);
        }

        public bool HasTalkedTo(string treeName)
        {
            return Conversations.Any(c => c.TreeName == treeName);
        }

        public IEnumerable<DialogHistoryEntry> EntriesFor(DialogConversationRecord conversation)
        {
            return Entries.Skip(conversation.FirstEntryIndex).Take(conversation.EntryCount);
        }

        public void Clear()
        {
            Entries.Clear();
            Conversations.Clear();
            Changed?.Invoke();
        }

        private void Add(string ownerId, string ownerName, string text, bool isChoice)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (Current == null)
                BeginConversation("Unknown");

            var conversation = Current;
            Entries.Add(new DialogHistoryEntry
            {
                Index = Entries.Count,
                ConversationOrder = conversation.Order,
                OwnerId = ownerId ?? string.Empty,
                OwnerName = ownerName ?? string.Empty,
                Text = text,
                IsPlayerChoice = isChoice,
            });
            conversation.EntryCount++;
            Changed?.Invoke();
        }
    }
}
