using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class DynamicObject
    {
        public dynamic Instance = new System.Dynamic.ExpandoObject();

        public void AddProperty(string name, object value)
        {
            ((IDictionary<string, object>)this.Instance).Add(name, value);
        }

        public void ChangeProperty(string name, object value)
        {
            if (((IDictionary<string, object>)this.Instance).ContainsKey(name))
            {
                ((IDictionary<string, object>)this.Instance)[name] = value;
            }
        }

        public dynamic GetProperty(string name)
        {
            if (((IDictionary<string, object>)this.Instance).ContainsKey(name))
                return ((IDictionary<string, object>)this.Instance)[name];
            else
                return null;
        }

        public bool PropertyExist(string name)
        {
            if (((IDictionary<string, object>)this.Instance).ContainsKey(name))
                return true;
            else
                return false;
        }

        public List<string> GetKeys()
        {
            List<string> keys = new List<string>();
            foreach (var key in ((IDictionary<string, object>)this.Instance).Keys)
            {
                keys.Add(key);
            }
            return keys;
        }

        public String[] GetValueArray()
        {
            var values = ((IDictionary<string, object>)this.Instance).Values;
            List<string> arr = new List<string>();
            foreach (var item in values)
            {
                arr.Add(item.ToString());
            }
            return arr.ToArray();
        }
    }
}
