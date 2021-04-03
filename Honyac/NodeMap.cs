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
    ///  program    = function*
    ///  function   = type ident "(" ")" stmt
    ///  stmt       = expr ";"
    ///             | "{" stmt* "}"
    ///             | "if" "(" expr ")" stmt ("else" stmt)?
    ///             | "while" "(" expr ")" stmt
    ///             | "for" "(" expr? ";" expr? ";" expr? ")" stmt
    ///             | "return" expr ";"
    ///             | type "*"* ident ";"
    ///  expr       = assign
    ///  assign     = equality ( "=" assign)?
    ///  equality   = relational ("==" relational | "!=" relational)*
    ///  relational = add ("<" add | "<=" add | ">" add | ">=" add)*
    ///  add        = mul ("+" mul | "-" mul)*
    ///  mul        = unary ("*" unary | "/" unary)*
    ///  unary      = ("+" | "-")? primary
    ///             | "&" unary
    ///             | "*" unary
    ///  primary    = num
    ///             | ident ( "(" ")" )?
    ///             | "(" expr ")"
    ///  
    /// </summary>
    public class NodeMap
    {
        private TokenList TokenList { get; set; }

        public Node Head { get { return this.Nodes.FirstOrDefault(); } }

        public List<Node> Nodes { get; set; } = new List<Node>();

        public List<LVar> LVars { get; set; }

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
            Program();
        }

        private void CreateLVars(List<Token> tokenList, List<LVar> lVars)
        {
            for (var i = 0; i < tokenList.Count; i++)
            {
                var token = tokenList[i];
                if (token.Kind != TokenKind.Type)
                    continue;

                // "*"の数を取得
                var pointerCount = 0;
                for (; "*".Equals(tokenList[i + pointerCount + 1].Str); pointerCount++)
                    ;

                // 次のトークンはidentのはず
                var identToken = tokenList[i + pointerCount + 1];

                // さらに次のトークンが「(」の場合は関数宣言なのでスキップ
                var nextToken = (i + pointerCount + 2) < tokenList.Count ? tokenList[i + pointerCount + 2] : null;
                if (nextToken != null && "(".Equals(nextToken.Str))
                    continue;

                // 同じ変数名があった場合はException
                if (lVars.Exists((lvar) => lvar.Name == identToken.Str))
                    throw new ArgumentException($"Duplicate Identifier:{identToken.Str}");

                var lvar = new LVar
                {
                    Name = identToken.Str,
                    Kind = token.TypeKind,
                    Offset = (lVars.Count + 1) * 8,
                    PointerCount = pointerCount,
                };
                lVars.Add(lvar);
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

        private Node NewNodeIdent(LVar lvar)
        {
            var node = new Node();
            node.Kind = NodeKind.Lvar;
            node.Offset = lvar.Offset;
            node.LVar = lvar;
            return node;
        }

        private void Program()
        {
            while (!TokenList.IsEof())
            {
                Nodes.Add(Function());
            }
        }

        private Node Function()
        {
            // 関数の戻り地の型宣言。ひとまず無視
            TokenList.Expect(TokenKind.Type);

            var identToken = TokenList.ExpectIdent();
            TokenList.Expect('(');
            TokenList.Expect(')');

            // TODO: 変数はblock単位で持ちたいが、一旦関数単位で持つようにしてみる
            // 関数内のローカル変数のリストを作成するため、この関数内のトークンだけを抜き出す
            var tokens = new List<Token>();
            var lVars = new List<LVar>();
            var bracketNum = 0;
            for (var i = TokenList.CurrentIndex; i < TokenList.Count; i++)
            {
                if ("{".Equals(TokenList[i].Str))
                    bracketNum++;
                else if ("}".Equals(TokenList[i].Str))
                    bracketNum--;

                tokens.Add(TokenList[i]);
                if (bracketNum == 0)
                    break;
            }
            CreateLVars(tokens, lVars);

            // TODO: 何とかしたい
            // 配下のNodeから参照できるよう、関数内部でコピーする
            this.LVars = lVars;

            // この以降にblockが続くが、blockはstmtで実現できているのでNodesのItem1にblockとなるstmtを設定する
            var node = new Node();
            node.Kind = NodeKind.Function;
            node.FuncName = identToken.Str;
            node.Nodes = Tuple.Create(Stmt(), null as Node);
            node.LVars = lVars;

            return node;
        }

        private Node Stmt()
        {
            Node node;

            if (TokenList.Consume('{'))
            {
                node = new Node();
                node.Kind = NodeKind.Block;
                node.Bodies = new List<Node>();

                // '}' が来るまでstmtを繰り返す
                while (!TokenList.Consume('}'))
                {
                    node.Bodies.Add(Stmt());
                }
            }
            else if (TokenList.Consume(TokenKind.If))
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
            else if (TokenList.Consume(TokenKind.While))
            {
                node = new Node();
                node.Kind = NodeKind.While;
                TokenList.Expect('(');
                node.Condition = Expr();
                TokenList.Expect(')');
                node.Nodes = Tuple.Create(Stmt(), null as Node);
            }
            else if (TokenList.Consume(TokenKind.For))
            {
                node = new Node();
                node.Kind = NodeKind.For;
                TokenList.Expect('(');
                if (!TokenList.Consume(';'))
                {
                    // 初期化処理ノード
                    node.Initialize = Expr();
                    TokenList.Expect(';');
                }

                if (!TokenList.Consume(';'))
                {
                    // 条件式ノード
                    node.Condition = Expr();
                    TokenList.Expect(';');
                }

                if (!TokenList.Consume(')'))
                {
                    // ループ処理ノード
                    node.Loop = Expr();
                    TokenList.Expect(')');
                }
                node.Nodes = Tuple.Create(Stmt(), null as Node);
            }
            else if (TokenList.Consume(TokenKind.Return))
            {
                node = new Node();
                node.Kind = NodeKind.Return;
                node.Nodes = Tuple.Create(Expr(), null as Node);
                TokenList.Expect(';');
            }
            else if (TokenList.Consume(TokenKind.Type))
            {
                // 型宣言の場合は、ひとまず型宣言ノードを作成するのみ。
                while (TokenList.Consume('*'))
                    ;
                var identToken = TokenList.ExpectIdent();
                TokenList.Expect(';');
                node = new Node();
                node.Kind = NodeKind.Type;
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
            else if (TokenList.Consume('&'))
            {
                return NewNode(NodeKind.Addr, Unary(), null);
            }
            else if (TokenList.Consume('*'))
            {
                return NewNode(NodeKind.DeRef, Unary(), null);
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
                // identTokenの次に「(」が来る場合は関数コール
                if (TokenList.Consume('('))
                {
                    var node = new Node();
                    node.Kind = NodeKind.FuncCall;
                    node.FuncName = identToken.Str;
                    TokenList.Expect(')');
                    return node;
                }
                else
                {
                    var lvar = LVars.FirstOrDefault(lv => lv.Name == identToken.Str);
                    if (lvar == null)
                        throw new ArgumentException($"Unknown Identifier:{identToken.Str}");

                    return NewNodeIdent(lvar);
                }
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

        /// <summary>Kindが制御構文（if, while, for）の場合の条件ノード</summary>
        public Node Condition { get; set; }

        /// <summary>Kindがforの場合の初期化ノード</summary>
        public Node Initialize { get; set; }

        /// <summary>
        /// Kindがforの場合の繰り返し処理用ノード
        /// Nodesの中に入れても良かったが、どちらが繰り返しでどちらが中身か分からなくなりそうだったので
        /// </summary>
        public Node Loop { get; set; }

        /// <summary>
        /// KindがBlockの場合に、内包するNode. KindがBlock以外の場合はnull
        /// </summary>
        public List<Node> Bodies { get; set; }

        /// <summary>
        /// 関数名。
        /// KindがFuncCall, Functionの場合に有効
        /// </summary>
        public string FuncName { get; set; }

        /// <summary>
        /// 変数リスト。
        /// KindがFunctionの場合のみ有効。その他の場合はnull
        /// </summary>
        public List<LVar> LVars { get; set; }

        /// <summary>
        /// 変数。
        /// KindがLVarの場合のみ有効。その他の場合はnull
        /// </summary>
        public LVar LVar { get; set; }

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
        Add,        // +
        Sub,        // -
        Mul,        // *
        Div,        // /
        Eq,         // ==
        Ne,         // !=
        Lt,         // <
        Le,         // <=
        Assign,     // =
        If,         // if
        While,      // while
        For,        // for
        Return,     // return
        Block,      // { ... }
        FuncCall,   // 関数コール
        Function,   // 関数
        Lvar,       // ローカル変数
        Addr,       // アドレス &
        DeRef,      // ポインタ *
        Type,       // 型
        Num,        // 整数
    }

    /// <summary>
    /// 変数
    /// </summary>
    public class LVar
    {
        public string Name { get; set; }
        public TypeKind Kind { get; set; }
        public int Offset { get; set; }
        public int PointerCount { get; set; }
    }
}
