using System;
using System.Collections.Generic;
using System.Text;

namespace Honyac
{
    public class Type
    {
        public TypeKind Kind { get; }
        public string Name { get; }
        public int Size { get; }

        public Type(TypeKind kind, string name, int size)
        {
            this.Kind = kind;
            this.Name = name;
            this.Size = size;
        }

    }

    public enum TypeKind
    {
        None,
        Int,        // int
    }

    public static class TypeUtils
    {
        public static readonly Dictionary<TypeKind, Type> TypeDic = new Dictionary<TypeKind, Type>()
        {
            // TODO: intのサイズはひとまず8にする。4にすると各種ニーモニックの対応が必要になるので
            {TypeKind.Int, new Type(TypeKind.Int, "int", 8) },
        };
    }
}
