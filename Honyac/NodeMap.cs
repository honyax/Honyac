using System;
using System.Collections.Generic;
using System.Linq;

namespace Honyac
{
    /// <summary>
    /// 抽象構文木
    /// トークンリストを抽象構文技にマッピングする。
    /// 
    /// 以下が各項目を優先度の低い順に並べたもの。
    ///  1. ==, !=
    ///  2. <, <=, >, >=
    ///  3. + -
    ///  4. * /
    ///  5. 単項+, 単項-
    ///  6. ( )
    /// 
    /// 上記に従い、EBNF(Extended Backus-Naur form)を実装する
    ///  program    = stmt*
    ///  stmt       = expr ";"
    ///             | "if" "(" expr ")" stmt ("else" stmt)?
    ///             | "return" expr ";"
    ///  expr       = assign
    ///  assign     = equality ( "=" assign)?
    ///  equality   = relational ("==" relational | "!=" relational)*
    ///  relational = add ("<" add | "<=" add | ">" add | ">=" add)*
    ///  add        = mul ("+" mul | "-" mul)*
    ///  mul        = unary ("*" unary | "/" unary)*
    ///  unary      = ("+" | "-")? primary
    ///  primary    = num | ident | "(" expr ")"
    ///  
    /// </summary>
    public class NodeMap
    {
        private TokenList TokenList { get; set; }

        public Node Head { get { return this.Nodes.FirstOrDefault(); } }

        public List<Node> Nodes { get; set; } = new List<Node>();

        public List<LVar> LVars { get; set; } = new List<LVar>();

        private NodeMap() { }

        public static NodeMap Create(TokenList tokenList)
        {
            var nodeMap = new NodeMap();
            nodeMap.TokenList = tokenList;
            nodeMap.Analyze();
            return nodeMap;
        }

        private void Analyze()
        {
            CreateLVars();
            Program();
        }

        private void CreateLVars()
        {
            foreach (var token in TokenList)
            {
                if (token.Kind != TokenKind.Ident)
                    continue;

                if (LVars.Exists((lvar) => lvar.Name == token.Str))
                    continue;

                var lvar = new LVar
                {
                    Name = token.Str,
                    Offset = (LVars.Count + 1) * 8,
                };
                LVars.Add(lvar);
            }
        }

        private Node NewNode(NodeKind kind, Node lhs, Node rhs)
        {
            var node = new Node();
            node.Kind = kind;
            node.Nodes = Tuple.Create(lhs, rhs);
            return node;
        }

        private Node NewNodeNum(int value)
        {
            var node = new Node();
            node.Kind = NodeKind.Num;
            node.Value = value;
            return node;
        }

        private Node NewNodeIdent(int offset)
        {
            var node = new Node();
            node.Kind = NodeKind.Lvar;
            node.Offset = offset;
            return node;
        }

        private void Program()
        {
            while (!TokenList.IsEof())
            {
                Nodes.Add(Stmt());
            }
        }

        private Node Stmt()
        {
            Node node;

            if (TokenList.Consume(TokenKind.If))
            {
                node = new Node();
                node.Kind = NodeKind.If;
                TokenList.Expect('(');
                node.Condition = Expr();
                TokenList.Expect(')');
                var thenNode = Stmt();
                Node elseNode = null;
                if (TokenList.Consume(TokenKind.Else))
                {
                    elseNode = Stmt();
                }
                node.Nodes = Tuple.Create(thenNode, elseNode);
            }
            else if (TokenList.Consume(TokenKind.Return))
            {
                node = new Node();
                node.Kind = NodeKind.Return;
                node.Nodes = Tuple.Create(Expr(), null as Node);
                TokenList.Expect(';');
            }
            else
            {
                node = Expr();
                TokenList.Expect(';');
            }
            return node;
        }

        private Node Expr()
        {
            return Assign();
        }

        private Node Assign()
        {
            var node = Equality();
            if (TokenList.Consume('='))
            {
                node = NewNode(NodeKind.Assign, node, Assign());
            }
            return node;
        }

        private Node Equality()
        {
            var node = Relational();
            for (; ; )
            {
                if (TokenList.Consume("=="))
                {
                    node = NewNode(NodeKind.Eq, node, Relational());
                }
                else if (TokenList.Consume("!="))
                {
                    node = NewNode(NodeKind.Ne, node, Relational());
                }
                else
                {
                    return node;
                }
            }
        }

        private Node Relational()
        {
            var node = Add();
            for (; ; )
            {
                if (TokenList.Consume('<'))
                {
                    node = NewNode(NodeKind.Lt, node, Add());
                }
                else if (TokenList.Consume('>'))
                {
                    node = NewNode(NodeKind.Lt, Add(), node);
                }
                else if (TokenList.Consume("<="))
                {
                    node = NewNode(NodeKind.Le, node, Add());
                }
                else if (TokenList.Consume(">="))
                {
                    node = NewNode(NodeKind.Le, Add(), node);
                }
                else
                {
                    return node;
                }
            }
        }

        private Node Add()
        {
            var node = Mul();
            for (; ; )
            {
                if (TokenList.Consume('+'))
                {
                    node = NewNode(NodeKind.Add, node, Mul());
                }
                else if (TokenList.Consume('-'))
                {
                    node = NewNode(NodeKind.Sub, node, Mul());
                }
                else
                {
                    return node;
                }
            }
        }

        private Node Mul()
        {
            var node = Unary();
            for (; ; )
            {
                if (TokenList.Consume('*'))
                {
                    node = NewNode(NodeKind.Mul, node, Unary());
                }
                else if (TokenList.Consume('/'))
                {
                    node = NewNode(NodeKind.Div, node, Unary());
                }
                else
                {
                    return node;
                }
            }
        }

        private Node Unary()
        {
            if (TokenList.Consume('+'))
            {
                return Primary();
            }
            else if (TokenList.Consume('-'))
            {
                return NewNode(NodeKind.Sub, NewNodeNum(0), Primary());
            }
            else
            {
                return Primary();
            }
        }

        private Node Primary()
        {
            // 次のトークンが "(" なら、"(" expr ")" のはず
            if (TokenList.Consume('('))
            {
                var node = Expr();
                TokenList.Expect(')');
                return node;
            }

            var identToken = TokenList.ConsumeIdent();
            if (identToken != null)
            {
                var offset = LVars.FirstOrDefault(lvar => lvar.Name == identToken.Str).Offset;
                return NewNodeIdent(offset);
            }
            else
            {
                var value = TokenList.ExpectNumber();
                return NewNodeNum(value);
            }
        }
    }

    public class Node
    {
        /// <summary>ノード種別</summary>
        public NodeKind Kind { get; set; }

        /// <summary>左右の子ノード</summary>
        public Tuple<Node, Node> Nodes { get; set; }

        /// <summary>KindがNumの場合の数値</summary>
        public int Value { get; set; }

        /// <summary>KindがLvarの場合の変数へのオフセット値</summary>
        public int Offset { get; set; }

        /// <summary>KindがIfの場合の条件ノード</summary>
        public Node Condition { get; set; }

        public override string ToString()
        {
            return $"Kind:{Kind} Value:{Value} Offset:{Offset}";
        }
    }

    /// <summary>
    /// ノード種別
    /// </summary>
    public enum NodeKind
    {
        Add,    // +
        Sub,    // -
        Mul,    // *
        Div,    // /
        Eq,     // ==
        Ne,     // !=
        Lt,     // <
        Le,     // <=
        Assign, // =
        If,     // if
        Return, // return
        Lvar,   // ローカル変数
        Num,    // 整数
    }

    /// <summary>
    /// 変数
    /// </summary>
    public class LVar
    {
        public string Name { get; set; }
        public int Offset { get; set; }
    }
}
