using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using CadCommon;

using Lexing;

namespace FileImportExport.STEP
{
    public class StepFile : FileBase
    {
        public Header Header;
        public List<Statement> Statements;


        public string LastError ="";

        public override bool LoadFromFile(string FileName)
        {
            bool result = false;
            StepLexer Lexer = new StepLexer();

            LastError = "";
            try
            {
                if (Lexer.Initialise(FileName))
                {
                    Statements = new List<Statement>();

                    if (Lexer.CurToken.Value == "ISO-10303-21")
                    {
                        ParseStatement(Lexer);

                        if (Lexer.CurToken.Value == "HEADER")
                        {
                            Header = ParseHeader(Lexer);

                            if (Lexer.CurToken.Value == "DATA")
                            {
                                ParseStatement(Lexer);

                                result = true;
                                while (result && (Lexer.CurToken.Type != Lexing.TokenType.EOF) && (Lexer.CurToken.Value == "#"))
                                {
                                    Statements.Add(ParseNumberedStatement(Lexer));
                                }

                                result = false;
                                if (Lexer.CurToken.Value == "ENDSEC")
                                {
                                    ParseStatement(Lexer);

                                    if (Lexer.CurToken.Value == "END-ISO-10303-21")
                                    {
                                        ParseStatement(Lexer);
                                        result = true;
                                    }
                                    else
                                        Lexer.ErrorAtCurToken("Expected END-ISO-10303-21");
                                }
                                else
                                    Lexer.ErrorAtCurToken("Expected ENDSEC (data section)");
                            }
                            else
                                Lexer.ErrorAtCurToken("Expected DATA section");
                        }
                        else
                            Lexer.ErrorAtCurToken("Expected HEADER section");
                    }
                    else
                    {
                        Lexer.ErrorAtCurToken("Unsupported header type");
                    }
                }
            }
            catch
            {
                result = false;
            }

            LastError = Lexer.LastError;
            return result;
        }

        public bool SaveToFile(string FileName)
        {
            bool result = true;
            TextWriter writer = File.CreateText(FileName);

            writer.WriteLine("ISO-10303-21;");
            Header.WriteFile(writer);

            writer.WriteLine("DATA;");

            foreach (Statement statement in Statements)
            {
                statement.WriteFile(writer);
//                writer.WriteLine(";");
            }

            writer.WriteLine("ENDSEC;");

            writer.WriteLine("END-ISO-10303-21;");
            writer.Close();

            return result;
        }


        void ParseBlock(StepLexer Lexer)
        {
        }

        public Header ParseHeader(StepLexer Lexer)
        {
            Header result = new Header();
            Statement statement;

            // HEADER;
            // ...
            // ENDSEC;

            if (Lexer.CurToken.Value == "HEADER")
            {
                // node ;
                statement = ParseStatement(Lexer);

                statement = ParseStatement(Lexer);
                while (statement.Node.Name != "ENDSEC")
                {
                    result.Nodes.Add(statement.Node);

                    statement = ParseStatement(Lexer);
                }

                if (statement.Node.Name != "ENDSEC")
                    Lexer.ErrorAtCurToken("Expected ENDSEC (data section)");
            }
            else
                Lexer.ErrorAtCurToken("Expected HEADER");
  
            return result;
        }

        public Statement ParseStatement(StepLexer Lexer)
        {
            Statement result = new Statement();

            result.LineNum = 0;
            result.Node = ParseNode(Lexer);

            if (Lexer.CurToken.Value == ";")
                Lexer.GetNextToken();
            else
                Lexer.ErrorAtCurToken("Expected ;");
            return result;
        }

        public Statement ParseNumberedStatement(StepLexer Lexer)
        {
            Statement result = new Statement();

            // # int = node ;
            // # int = ( expr_list )

            if (Lexer.CurToken.Value[0] == '#')
            {
                Lexer.GetNextToken();
                result.LineNum = Lexer.CurToken.IntValue;

                Lexer.GetNextToken();

                if (Lexer.CurToken.Value[0] == '=')
                {
                    Lexer.GetNextToken();
                    result.Node = ParseNode(Lexer);

                    if (Lexer.CurToken.Value == ";")
                        Lexer.GetNextToken();
                    else
                        Lexer.ErrorAtCurToken("Expected ;");

                }
                else
                    Lexer.ErrorAtCurToken("Expected =");

            }

            return result;
        }

        public Node ParseNode (StepLexer Lexer)
        {
            Node result = new Node();

            // name ( arg [, arg] )
            //  ( arg [arg]* )

            if (Lexer.CurToken.Type == TokenType.Name)
            {
                result.Name = Lexer.CurToken.Value;
                Lexer.GetNextToken();
            }

            if (Lexer.CurToken.Value == "(")
            {
                Lexer.GetNextToken();

                // parse arg list
                while (Lexer.CurToken.Value != ")")
                {
                    result.Args.Add (ParseExpression(Lexer));

                    if (Lexer.CurToken.Value == ",")
                        Lexer.GetNextToken();
                }

                if (Lexer.CurToken.Value == ")")
                    Lexer.GetNextToken();
            }

            return result;
        }

        public Expression ParseExpression(StepLexer Lexer)
        {
            Expression result = new Expression();
            // string , num, ref, // .T. .F.
            // ( expr [,expr] )
            // name  ( expr [,expr] )

            if (Lexer.CurToken.Type == TokenType.Name)
            {
                result.value = new Token(TokenType.Name, Lexer.CurToken.Value);
                Lexer.GetNextToken();
            }

            if (Lexer.CurToken.Value == "(")
            {
                //TODO:
                //result.value = new Token (TokenType.StringLiteral, "()");
                result.Elements = new List<Expression>();

                Lexer.GetNextToken();

                while (Lexer.CurToken.Value != ")")
                {
                    result.Elements.Add (ParseExpression(Lexer));

                    if (Lexer.CurToken.Value == ",")
                        Lexer.GetNextToken();
                    // expect , ?
                }

                if (Lexer.CurToken.Value == ")")
                    Lexer.GetNextToken();
            }
            else if (Lexer.CurToken.Value == "#")
            {
                Lexer.GetNextToken();
                // expect: integer

                result.value = new Token(TokenType.Name, "#" + Lexer.CurToken.Value);

                Lexer.GetNextToken();
            }
            else
            {
                // int, float, stringlit
                result.value = new Token(Lexer.CurToken);
                Lexer.GetNextToken();
            }

            return result;
        }
    }

    public class StepLexer : GeneralLexer
    {
        public StepLexer()
        {
            LineComment = "";

            InitMap(ref WhiteSpaceMap);
            AddMap(ref WhiteSpaceMap, WhiteSpace);

            InitMap(ref IdentFirstMap);
            AddMapRanges(ref IdentFirstMap, "'A'-'Z','a'-'z'");

            InitMap(ref IdentRestMap);
            AddMapRanges(ref IdentRestMap, "'A'-'Z','a'-'z','0'-'9','-','_'");

        }

        public override void GetNextToken()
        {
            base.GetNextToken();

            if (CurToken.Value == ".")
            {
                base.GetNextToken();
                string s = CurToken.Value;

                base.GetNextToken();

                CurToken.Value = "." + s + ".";
                // symbol?
            }
            //else if (CurToken.Value == "#")
            //{
            //    // # num
            //    base.GetNextToken();
            //    CurToken.Value = "#" + CurToken.Value;

            //}

        }
    }


    public class Header 
    {
        public List<Node> Nodes;

        public Header()
        {
            Nodes = new List<Node>();
        }

        public void WriteFile(TextWriter writer)
        {

            writer.WriteLine("HEADER;");

            foreach (Node node in Nodes)
            {
                node.WriteFile(writer);
                writer.WriteLine(";");
            }

            writer.WriteLine("ENDSEC;");
        }
    }

    
    public class Statement
    {
        public int LineNum;
        public Node Node;

        public Statement()
        {
            Node = new Node();
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", LineNum, Node.Name);
        }

        public void WriteFile(TextWriter writer)
        {
            if (LineNum > 0)
            {
                writer.Write("#");
                writer.Write(LineNum);
                writer.Write("=");
            }
            Node.WriteFile(writer);

            writer.WriteLine(";");
        }
    }

    public class Node
    {
        public string Name;
        public List<Expression> Args;

        public Node()
        {
            Args = new List<Expression>();
        }

        public void WriteFile(TextWriter writer)
        {
            if (Name != null)
                writer.Write(Name);
            else
                writer.Write(" ");

            writer.Write("(");

            for (int j=0; j < Args.Count; j++)
            {
                Args[j].WriteFile(writer);

                if (j != Args.Count-1)
                    writer.Write(",");
            }

            writer.Write(")");
        }

        public override string ToString()
        {
            string s;
            if (Name != null)
                s = Name;
            else
                s = " ";

            s += "(";
            for (int j = 0; j < Args.Count; j++)
            {
                s += Args[j].ToString();

                if (j != Args.Count-1)
                    s += ",";
            }
            s += ")";

            return s;
        }

    }

    public class Expression
    {
        // simple: value
        // aggregate : elements
        // typecast? : value.value, elements

        public Token value;
        public List<Expression> Elements;


        public void WriteFile(TextWriter writer)
        {
            if (Elements != null)
            {
                if (value != null)
                    writer.Write(value.Value);
                
                writer.Write("(");
                for (int j = 0; j < Elements.Count; j++)
                {
                    Elements[j].WriteFile(writer);
                    if (j != Elements.Count - 1)
                        writer.Write(",");
                }
                writer.Write(")");
            }
            else if (value != null)
            {
                // simple
                switch (value.Type)
                {
                    case TokenType.StringLiteral:
                        writer.Write("'" + value.Value + "'");
                        break;
                    case TokenType.IntegerVal:
                        writer.Write(value.IntValue);
                        break;
                    case TokenType.FloatVal:
                        if (value.RealValue == Math.Truncate(value.RealValue))
                            writer.Write(value.RealValue.ToString("f1"));
                        else
                            writer.Write(value.RealValue.ToString("g"));
                        break;
                    case TokenType.Name:
                        writer.Write(value.Value);
                        break;
                    case TokenType.Symbol:
                        writer.Write(value.Value);
                        break;
                    default:
                        writer.Write(value.Value);
                        break;
                }
            }
            // else error?            
        }

        public override string ToString()
        {
            string s = value.ToString();

            return s;
        }
    }

}
