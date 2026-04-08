namespace User.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ExcelColumnAttribute : Attribute
    {
        public string Name { get; }
        public string? Format { get; } // 可选：用于日期或金额格式化
        public int? Order { get; set; }
        public bool? Ignore { get; set; }

        public ExcelColumnAttribute(string name, string? format = null, int? order = null, bool? ignore = null)
        {
            Name = name;
            Format = format;
            Order = order;
            Ignore = ignore;
        }
    }
}
