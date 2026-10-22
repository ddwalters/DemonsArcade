using System;

namespace NodeTree
{
    public enum ConditionFailBehavior { Stop, Skip, Jump }

    [Serializable]
    public abstract class ConditionBase
    {
        public abstract bool Evaluate();
    }
}
