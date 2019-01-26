using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFUserControl
{
    public class AutoCompleteTextBoxControlEventArgs : EventArgs
    {
        public AutoCompleteTextBoxControlEventArgs(object o, string text)
        {
            Object = o;
            Text = text;
        }

        public object Object { get; }
        public string Text { get; }
    }
}
