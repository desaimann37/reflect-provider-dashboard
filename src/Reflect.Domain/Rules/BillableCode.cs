namespace Reflect.Domain.Rules;

public sealed record BillableCode(string Code, int Units)
{
    public string Label => Units > 1 ? $"{Code} x{Units}" : Code;
}
