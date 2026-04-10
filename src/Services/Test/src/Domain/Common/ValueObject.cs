namespace User.Domain.Common
{
    public abstract class ValueObject
    {
        protected static bool EqualOperator(ValueObject? left, ValueObject? right)
        {
            if (ReferenceEquals(left, null) ^ ReferenceEquals(right, null))
            {
                return false;
            }
            return ReferenceEquals(left, null) || left.Equals(right);
        }

        protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
        {
            return !(EqualOperator(left, right));
        }

        // 修改为 object? 允许组件为 null
        protected abstract IEnumerable<object?> GetEqualityComponents();

        public override bool Equals(object? obj) // 加上 ?
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            var other = (ValueObject)obj;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            // 使用 HashCode 结构体替代 LINQ Aggregate，既消除了警告又提升了性能
            var hash = new HashCode();
            foreach (var obj in GetEqualityComponents())
            {
                hash.Add(obj);
            }
            return hash.ToHashCode();
        }

        // MemberwiseClone 总是返回非 null，但需要强转
        public ValueObject? GetCopy()
        {
            return MemberwiseClone() as ValueObject;
        }
    }
}
