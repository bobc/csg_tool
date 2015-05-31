using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using QUT.Gppg;

namespace CSGImport
{
    public partial class Parser
    {
        Parser(Lexer s) : base(s) { }

        public Node Output;
        

        public static Node ParseFile(string Filename)
        {
            Node result = null;
            System.IO.TextReader reader;
            reader = new System.IO.StreamReader(Filename);

            Parser parser = new Parser(new Lexer(reader));
            //parser.Trace = true;
            try
            {
                if (parser.Parse())
                    result = parser.Output;
            }
            finally
            {
                reader.Close();
            }
            return result;
        }

        public static Node ParseStream(MemoryStream stream)
        {
            Node result = null;

            System.IO.TextReader reader;
            reader = new System.IO.StreamReader(stream);

            Parser parser = new Parser(new Lexer(reader));
            //parser.Trace = true;
            try
            {
                if (parser.Parse())
                    result = parser.Output;
            }
            finally
            {
                reader.Close();
            }

            return result;
        }

        /*
         * Copied from GPPG documentation.
         */
        class Lexer : QUT.Gppg.AbstractScanner<Node, LexLocation>
        {
            private System.IO.TextReader reader;
            private StringBuilder text = new StringBuilder();

            //
            // Version 1.2.0 needed the following code.
            // In V1.2.1 the base class provides this empty default.
            //
            // public override LexLocation yylloc { 
            //     get { return null; } 
            //     set { /* skip */; }
            // }
            //

            public Lexer(System.IO.TextReader reader)
            {
                this.reader = reader;
            }

            public override int yylex()
            {
                bool debug = false;
                int result = get_token();

                if (debug)
                {
                    Tokens token = (Tokens)result;
                    string leaf = "";
                    if (yylval != null)
                        leaf = yylval.Unparse();
                    Console.WriteLine("lex=" + token + " " + leaf);
                }
                return result;
            }

            private int get_token ()
            {
                char ch;
                char peek;
                int ord = reader.Read();
                this.text.Length = 0;

                //
                // Must check for EOF
                //
                if (ord == -1)
                    return (int)Tokens.EOF;
                else
                    ch = (char)ord;

                if (ch == '\n')
                    return get_token();
                if (ch == '\t')
                    return get_token();
                else if (char.IsWhiteSpace( ch )) { // Skip white space
                    while (char.IsWhiteSpace( peek = (char)reader.Peek() )) {
                        ord = reader.Read();
                    }
                    return get_token();
                }
                else if (char.IsDigit(ch) || ch == '-')
                {
                    text.Append(ch);
                    while (char.IsDigit(peek = (char)reader.Peek()))
                        text.Append((char)reader.Read());
                    if ((peek = (char)reader.Peek()) == '.')
                        text.Append((char)reader.Read());
                    while (char.IsDigit(peek = (char)reader.Peek()))
                        text.Append((char)reader.Read());
                    try
                    {
                        yylval = Parser.MakeNumberLeaf(double.Parse(text.ToString()));
                        return (int)Tokens.NUMBER;
                    }
                    catch (FormatException)
                    {
                        this.yyerror("Illegal number \"{0}\"", text);
                        return (int)Tokens.error;
                    }
                }
                else if (char.IsLetter(ch) || ch=='$')
                {
                    text.Append(char.ToLower(ch));

                    peek = (char)reader.Peek();
                    while (char.IsLetter(peek) || char.IsDigit(peek))
                    {
                        text.Append(char.ToLower((char)reader.Read()));
                        peek = (char)reader.Peek();
                    }

                    switch (text.ToString())
                    {
                        case "cube":
                            return (int)Tokens.cube;
                        case "sphere":
                            return (int)Tokens.sphere;
                        case "cylinder":
                            return (int)Tokens.cylinder;
                        case "difference":
                            return (int)Tokens.difference;
                        case "union":
                            return (int)Tokens.union;
                        case "intersection":
                            return (int)Tokens.intersection;
                        case "group":
                            return (int)Tokens.group;
                        case "multmatrix":
                            return (int)Tokens.multmatrix;
                        case "color":
                            return (int)Tokens.color;

                        case "false":
                            yylval = Parser.MakeNumberLeaf(0);
                            return (int)Tokens._false;
                        case "true":
                            yylval = Parser.MakeNumberLeaf(1);
                            return (int)Tokens._true;

                        default:
                            yylval = Parser.MakeIdLeaf(text.ToString());
                            return (int)Tokens.ID;
                    }
                }
                else
                    switch (ch)
                    {
                        case '(': return (int)Tokens.LPAREN;
                        case ')': return (int)Tokens.RPAREN;
                        case '{': return (int)Tokens.OBRACE;
                        case '}': return (int)Tokens.EBRACE;
                        case '[': return (int)Tokens.OSQUARE;
                        case ']': return (int)Tokens.ESQUARE;

                        case ',': return (int)Tokens.COMMA;
                        case ';': return (int)Tokens.SEMICOL;
                        case '=': return (int)Tokens.EQ;
                        case '.': return (int)Tokens.DOT;

                        case '%': return (int)Tokens.MODIFIER_BACK;
                        case '#': return (int)Tokens.MODIFIER_DEBUG;
                        case '!': return (int)Tokens.MODIFIER_ROOT;
                        case '*': return (int)Tokens.MODIFIER_DISABLE;

                        default:
                            Console.Error.WriteLine("Illegal character '{0}'", ch);
                            return get_token();
                    }
            }

            public override void yyerror(string format, params object[] args)
            {
             //   Console.WriteLine(string.Format("at ({0],{1}): ", yylloc.StartLine, yylloc.StartColumn));
                Console.WriteLine(format, args);
            }
        }

        //
        // Now the node factory methods
        //
        public void StoreResult (Node node)
        {
            Output = node;
        }



        public static Node MakeBinary(NodeTag tag, Node lhs, Node rhs)
        {
            return new Binary(tag, lhs, rhs);
        }

        public static Node MakeUnary(NodeTag tag, Node child)
        {
            return new Unary(tag, child);
        }

        //
        public static Node MakeIdLeaf(string s)
        {
            return new Leaf(s);
        }

        public static Node MakeNumberLeaf(double v)
        {
            return new Leaf(v);
        }

        public static Node MakeLeafVector(Node n1, Node n2)
        {
            return new Leaf((n1 as Leaf).Value, (n2 as Leaf).Value);
        }

        public static Node MakeLeafVector(Node n1, Node n2, Node n3)
        {
            return new Leaf((n1 as Leaf).Value, (n2 as Leaf).Value, (n3 as Leaf).Value);
        }

        public static Node MakeLeafVector(Node n1, Node n2, Node n3, Node n4)
        {
            return new Leaf((n1 as Leaf).Value, (n2 as Leaf).Value, (n3 as Leaf).Value, (n4 as Leaf).Value);
        }

        public static Node MakeLeafMatrix(Node n1, Node n2, Node n3, Node n4)
        {
            return new Leaf((n1 as Leaf).Vector, (n2 as Leaf).Vector, (n3 as Leaf).Vector, (n4 as Leaf).Vector);
        }

        public class CircularEvalException : Exception
        {
            internal CircularEvalException() { }
        }
    }

    // ==================================================================================
    //  Start of Node Definitions
    // ==================================================================================

    public enum NodeTag { error, 
        name, number, vector, matrix,
        arg, arglist,
        cube, sphere, cylinder,
        color,
        group, 
        difference, union, intersect, multmatrix,
        blocklist
    }

    public abstract class Node
    {
        readonly NodeTag tag;
        protected bool active = false;
        public NodeTag Tag { get { return this.tag; } }

        protected Node(NodeTag tag) { this.tag = tag; }
        
        public abstract string Unparse();

        public void Prolog()
        {
            if (this.active)
                throw new Parser.CircularEvalException();
            this.active = true;
        }

        public void Epilog() { this.active = false; }
    }

    public class Leaf : Node
    {
        string name;
        double value;
        double[] vector;
        double[][] matrix;

        internal Leaf(string s) : base(NodeTag.name) { this.name = s; }
        
        internal Leaf(double v) : base(NodeTag.number) { this.value = v; }

        internal Leaf(double v1, double v2) : base(NodeTag.vector)
        {
            this.vector = new double[] { v1, v2};
        }

        internal Leaf(double v1,double v2,double v3) : base(NodeTag.vector) 
        {
            this.vector = new double[] { v1, v2, v3 };
        }

        internal Leaf(double v1, double v2, double v3,double v4) : base(NodeTag.vector)
        {
            this.vector = new double[] { v1, v2, v3, v4 };
        }

        internal Leaf(double [] v1, double [] v2, double [] v3, double [] v4) : base(NodeTag.matrix)
        {
            this.matrix = new double[][] { v1, v2, v3, v4 };
        }

        public string Name { get { return name; } }
        public double Value { get { return value; } }
        public double[] Vector { get { return vector; } }
        public double[][] Matrix { get { return matrix; } }

        private static string GetVectorString(double[] vector)
        {
            string result="[";

            result += vector[0].ToString();

            for (int j=1; j < vector.Length; j++)
                result += "," + vector[j].ToString();
            result += "]";
            return result;
        }

        public override string Unparse()
        {
            switch (Tag)
            {
                case NodeTag.name:
                    return this.name.ToString();
                case NodeTag.number:
                    return this.value.ToString();
                case NodeTag.vector:
                    return string.Format ("[{0}]", Leaf.GetVectorString(vector) );

                case NodeTag.matrix:
                    return string.Format("[{0},{1},{2},{3}]", Leaf.GetVectorString(matrix[0]),
                        Leaf.GetVectorString(matrix[1]),
                        Leaf.GetVectorString(matrix[2]),
                        Leaf.GetVectorString(matrix[3]));
                default:
                    return "leaf";
            }
        }
    }

    public class Unary : Node
    {
        Node child;

        internal Unary(NodeTag t, Node c) : base(t) { this.child = c; }

        public override string Unparse()
        {
            return String.Format("( - {0})", this.child.Unparse());
        }
    }

    public class Binary : Node
    {
        public Node lhs;
        public Node rhs;

        internal Binary(NodeTag t, Node l, Node r) : base(t)
        {
            this.lhs = l; this.rhs = r;
        }


        public override string Unparse()
        {
            string op = "";
            switch (this.Tag)
            {
                case NodeTag.group: op = "group"; break;
                case NodeTag.cube: op = "cube"; break;
                case NodeTag.arg: op = "arg"; break;
                case NodeTag.arglist: op = "arglist"; break;

                default:
                    op = Tag.ToString(); break;
            }
            string str_lhs = "null";
            string str_rhs = "null";

            if (lhs != null)
                str_lhs = lhs.Unparse();
            if (rhs != null)
                str_rhs = rhs.Unparse();

            return String.Format("{0} ({1}, {2})", op, str_lhs, str_rhs);
        }
    }
    // ==================================================================================

}
