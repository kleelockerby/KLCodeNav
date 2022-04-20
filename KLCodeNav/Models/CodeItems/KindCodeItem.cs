using System.ComponentModel;

namespace KLCodeNav
{
    public enum KindCodeItem
    {
        [Description("Fields")]
        Field,

        [Description("Constructors")]
        Constructor,

        [Description("Destructors")]
        Destructor,

        [Description("Delegates")]
        Delegate,

        [Description("Events")]
        Event,

        [Description("Enums")]
        Enum,

        [Description("Indexers")]
        Indexer,

        [Description("Interfaces")]
        Interface,

        [Description("Properties")]
        Property,

        [Description("Methods")]
        Method,

        [Description("Structs")]
        Struct,

        [Description("Classes")]
        Class,

        [Description("Namespaces")]
        Namespace,

        [Description("Regions")]
        Region,

        [Description("Usings")]
        Using
    }
}
