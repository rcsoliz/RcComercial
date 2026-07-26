namespace RcComercial.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Uuid7.NewGuid();
}
