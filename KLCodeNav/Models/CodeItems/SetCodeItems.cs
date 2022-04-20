using System.Collections.Generic;

namespace KLCodeNav
{
    public class SetCodeItems : List<BaseCodeItem>
    {
        public SetCodeItems() { }

        public SetCodeItems(IEnumerable<BaseCodeItem> collection) : this()
        {
            AddRange(collection);
        }
    }
}