using System;

namespace NodeTree
{
    [Serializable]
    public class BoolCondition : ConditionBase
    {
        public string Key;
        public bool ExpectedValue = true;

        public override bool Evaluate() => ConditionContext.GetBool(Key) == ExpectedValue;
    }
}
