using System;

namespace NodeTree
{
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual
    }

    [Serializable]
    public class IntCondition : ConditionBase
    {
        public string Key;
        public int Value;
        public ComparisonOperator Operator;

        public override bool Evaluate()
        {
            int current = ConditionContext.GetInt(Key);
            return Operator switch
            {
                ComparisonOperator.Equal => current == Value,
                ComparisonOperator.NotEqual => current != Value,
                ComparisonOperator.LessThan => current < Value,
                ComparisonOperator.LessThanOrEqual => current <= Value,
                ComparisonOperator.GreaterThan => current > Value,
                ComparisonOperator.GreaterThanOrEqual => current >= Value,
                _ => false
            };
        }
    }
}
