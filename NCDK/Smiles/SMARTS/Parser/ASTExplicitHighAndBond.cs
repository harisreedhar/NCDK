/* Generated By:JJTree: Do not edit this line. ASTExplicitHighAndBond.java Version 4.3 */
/* JavaCCOptions:MULTI=true,NODE_USES_PARSER=false,VISITOR=true,TRACK_TOKENS=false,NODE_PREFIX=AST,NODE_EXTENDS=,NODE_FACTORY=,SUPPORT_CLASS_VISIBILITY_PUBLIC=true */
namespace NCDK.Smiles.SMARTS.Parser
{

    public
    class ASTExplicitHighAndBond : SimpleNode
    {
        public ASTExplicitHighAndBond(int id)
          : base(id)
        {
        }

        public ASTExplicitHighAndBond(SMARTSParser p, int id)
          : base(p, id)
        {
        }


        /** Accept the visitor. **/
        public override object jjtAccept(SMARTSParserVisitor visitor, object data)
        {
            return visitor.Visit(this, data);
        }
    }
}
