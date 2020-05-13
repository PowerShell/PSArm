namespace PSArm.Schema
{
    public interface IDslSchemaVisitor
    {
        void VisitCommandKeyword(string commandName, DslCommandSchema command);

        void VisitBodyCommandKeyword(string commandName, DslBodyCommandSchema bodyCommand);

        void VisitArrayKeyword(string commandName, DslArraySchema array);

        void VisitBlockKeyword(string commandName, DslBlockSchema block);
    }
}
