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
    ///  function   = type "*"* ident "(" (type "*"* ident ("," type "*"* ident)* )? ")" stmt
    ///  stmt       = expr ";"
    ///             | "{" stmt* "}"
    ///             | "if" "(" expr ")" stmt ("else" stmt)?
    ///             | "while" "(" expr ")" stmt
    ///             | "for" "(" expr? ";" expr? ";" expr? ")" stmt
    ///             | "return" expr ";"
    ///             | type "*"* ident ("[" num "]")? ";"
    ///  expr       = assign
    ///  assign     = equality ("=" assign)?
    ///  equality   = relational ("==" relational | "!=" relational)*
    ///  relational = add ("<" add | "<=" add | ">" add | ">=" add)*
    ///  add        = mul ("+" mul | "-" mul)*
    ///  mul        = unary ("*" unary | "/" unary)*
    ///  unary      = ("+" | "-")? primary
    ///             | "&" unary
    ///             | "*" unary
    ///             | "sizeof" unary
    ///  primary    = num
    ///             | ident "(" (expr ("," expr)* )? ")"
    ///             | ident "[" expr "]"
    ///             | ident
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

        private void CreateLVars(List<Token> tokenList, List<Token> argumentTokens, List<LVar> lVars)
        {
            var offset = 0;

            // 引数トークンから引数のリストを作成
            var argIndex = 0;
            for (var i = 0; i < argumentTokens.Count; )
            {
                // 引数トークンの形式は決まっている。最初はタイプのはず。
                var typeToken = argumentTokens[i];
                if (typeToken.Kind != TokenKind.Type)
                    throw new ArgumentException($"Invalid ArgumentToken:{typeToken} ArgumentTokenIndex:{i}");
                i++;

                // "*"の数を取得
                var pointerCount = 0;
                for (; "*".Equals(argumentTokens[i + pointerCount].Str); pointerCount++)
                    ;
                i += pointerCount;

                // 次のトークンはidentのはず
                var identToken = argumentTokens[i];
                i++;

                offset += 8;
                var lvar = new LVar
                {
                    Name = identToken.Str,
                    Kind = typeToken.TypeKind,
                    Offset = offset,
                    PointerCount = pointerCount,
                    ArraySize = 0,
                    ArgIndex = argIndex,
                };

                lVars.Add(lvar);
                argIndex++;

                // 次のトークンがあるとしたら","だが、ここで終わりの可能性もある
                var commaToken = i < argumentTokens.Count ? argumentTokens[i] : null;
                if (commaToken == null)
                    break;
                if (!commaToken.Str.Equals(","))
                    throw new ArgumentException($"Not comma token:{commaToken} index:{i}");
                i++;
            }

            for (var i = 0; i < tokenList.Count; )
            {
                var typeToken = tokenList[i];
                i++;
                if (typeToken.Kind != TokenKind.Type)
                    continue;

                // "*"の数を取得
                var pointerCount = 0;
                for (; "*".Equals(tokenList[i + pointerCount].Str); pointerCount++)
                    ;
                i += pointerCount;

                // 次のトークンはidentのはず
                var identToken = tokenList[i];
                i++;

                // さらに次のトークンが「(」の場合は関数宣言なのでスキップ
                // 「[」の場合は配列
                var nextToken = tokenList[i];
                i++;
                if (nextToken.Str.Equals("("))
                    continue;

                var arraySize = 0;
                if (nextToken.Str.Equals("["))
                {
                    // 配列として定義されている場合は、配列の数を取得してpointerCountを加算
                    // 配列をポインタとして考えるため
                    var numToken = tokenList[i];
                    i++;
                    arraySize = numToken.Value;
                    pointerCount++;

                    // 次は閉じカッコのはず（カッコの中の計算は今の所許容していない）
                    var endBracketToken = tokenList[i];
                    if (!endBracketToken.Str.Equals("]"))
                        throw new ArgumentException($"Not end bracket token:{endBracketToken} index:{i}");
                    i++;
                }

                // 同じ変数名があった場合はException
                if (lVars.Exists((lvar) => lvar.Name == identToken.Str))
                    throw new ArgumentException($"Duplicate Identifier:{identToken.Str}");

                // 配列の場合は、変数のアドレスを設定した後で配列の要素分だけオフセットを加算する必要がある
                offset += 8 * arraySize;

                offset += 8;
                var lvar = new LVar
                {
                    Name = identToken.Str,
                    Kind = typeToken.TypeKind,
                    Offset = offset,
                    PointerCount = pointerCount,
                    ArraySize = arraySize,
                    ArgIndex = -1,
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
            while (TokenList.Consume('*'))
                ;

            var identToken = TokenList.ExpectIdent();
            TokenList.Expect('(');

            // 引数用のトークン
            var argumentTokens = new List<Token>();

            // 引数を作成。閉じカッコが来るまで繰り返す
            for (; ; )
            {
                if (TokenList.Consume(')'))
                    break;

                var token = TokenList.Consume();
                argumentTokens.Add(token);
            }

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
            CreateLVars(tokens, argumentTokens, lVars);

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
                if (TokenList.Consume('['))
                {
                    // 配列の場合は配列の宣言までを読み込む
                    TokenList.ExpectNumber();
                    TokenList.Expect(']');
                }

                var lvar = LVars.FirstOrDefault(lv => lv.Name == identToken.Str);
                if (lvar == null)
                    throw new ArgumentException($"Unknown Identifier:{identToken.Str}");

                node = new Node();
                node.Kind = NodeKind.Type;
                node.LVar = lvar;
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
            else if (TokenList.Consume('&'))
            {
                return NewNode(NodeKind.Addr, Unary(), null);
            }
            else if (TokenList.Consume('*'))
            {
                return NewNode(NodeKind.DeRef, Unary(), null);
            }
            else if (TokenList.Consume(TokenKind.Sizeof))
            {
                var node = Unary();
                switch (node.Kind)
                {
                    case NodeKind.Lvar:
                        int size;
                        if (node.LVar.PointerCount > 0)
                        {
                            size = 8;
                        }
                        else
                        {
                            size = TypeUtils.TypeDic[node.LVar.Kind].Size;
                        }
                        return NewNodeNum(size);

                    case NodeKind.Num:
                        // sizeofに数値が指定されている場合はint型とする
                        return NewNodeNum(TypeUtils.TypeDic[TypeKind.Int].Size);

                    default:
                        throw new ArgumentException($"Invalid sizeof node:{node}");
                }
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
                    node.Arguments = new Stack<Node>();

                    // 引数がある場合は設定する
                    while (!TokenList.Consume(')'))
                    {
                        var argNode = Expr();
                        node.Arguments.Push(argNode);

                        // 次に引数が続く場合はカンマが設定されている
                        TokenList.Consume(',');
                    }
                    return node;
                }
                else if (TokenList.Consume('['))
                {
                    var lvar = LVars.FirstOrDefault(lv => lv.Name == identToken.Str);
                    if (lvar == null)
                        throw new ArgumentException($"Unknown Identifier:{identToken.Str}");

                    // identTokenの次に「[」が来る場合は配列
                    // 配列は、以下のように読み替える
                    // a[3] => *(a + 3)
                    var identNode = NewNodeIdent(lvar);
                    var exprNode = Expr();
                    TokenList.Expect(']');
                    var addNode = NewNode(NodeKind.Add, identNode, exprNode);
                    var node = NewNode(NodeKind.DeRef, addNode, null);
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

        /// <summary>
        /// 関数への引数。
        /// 引数は後ろから順番にレジスタに設定していくので、Stackとした。
        /// 関数呼び出しの時のみ有効。その他の場合はnull
        /// </summary>
        public Stack<Node> Arguments { get; set; }

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
        /// <summary>
        /// ポインタの数
        /// int a; の場合は0
        /// int **a; の場合は2
        /// </summary>
        public int PointerCount { get; set; }
        /// <summary>
        /// 配列の数
        /// int a[5]; の場合は5
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// 関数の引数のインデックス
        /// 引数であれば 0 ～ 5 の値を取る
        /// 引数でない場合は -1
        /// </summary>
        public int ArgIndex { get; set; }

        /// <summary>
        /// 変数が関数の引数かどうか
        /// </summary>
        public bool IsArgment => ArgIndex >= 0;
    }
}
