public interface IApplicable<in TFrom, out TTo>
{
    public void Apply(TFrom obj);

    public TTo From(TFrom obj);
}