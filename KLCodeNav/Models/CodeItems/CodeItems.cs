using System.Collections.Generic;

namespace KLCodeNav
{
    public class CodeItems : List<BaseCodeItem>
    {
        public CodeItems() { }

        public CodeItems(IEnumerable<BaseCodeItem> collection) : this()
        {
            AddRange(collection);
        }
    }
}
