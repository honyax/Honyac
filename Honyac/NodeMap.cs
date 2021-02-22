using System;
using System.Collections.Generic;
using System.Text;

namespace Honyac
{
    /// <summary>
    /// 抽象構文木
    /// トークンリストを抽象構文技にマッピングする。
    /// 
    /// 以下のEBNF(Extended Backus-Naur form)を実装する
    ///  expr    = mul ("+" mul | "-" mul)*
    ///  mul     = primary("*" primary | "/" primary)*
    ///  primary = num | "(" expr ")"
    ///  
    /// </summary>
    public class NodeMap
    {
        private TokenList TokenList { get; set; }

        public Node Head { get; set; }

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
            this.Head = Expr();
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

        private Node Expr()
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
            var node = Primary();
            for (; ; )
            {
                if (TokenList.Consume('*'))
                {
                    node = NewNode(NodeKind.Mul, node, Primary());
                }
                else if (TokenList.Consume('/'))
                {
                    node = NewNode(NodeKind.Div, node, Primary());
                }
                else
                {
                    return node;
                }
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
        Num,    // 整数
    }
}
