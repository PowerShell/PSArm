namespace PSArm.Templates.Primitives
{
    public interface IArmExpression
    {
        string ToInnerExpressionString();
    }

    public abstract class ArmExpression : ArmElement, IArmExpression
    {
        public abstract string ToInnerExpressionString();
    }
}
