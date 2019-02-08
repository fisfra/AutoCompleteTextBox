using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFUserControl
{
    /// <summary>
    /// Interaction logic for AutoCompleteTextBox.xaml
    /// </summary>
    public partial class AutoCompleteTextBox : UserControl
    {
        const string c_AutoCompleteWidthPropertyName = "AutoCompleteWidth";
        const string c_AutoCompleteTextBoxHightPropertyName = "AutoCompleteTextBoxHeight";
        const string c_AutoCompleteTextBoxListBoxReadOnlyPropertyName = "ListBoxReadOnly";

        private readonly SolidColorBrush autoCompleteForeground = Brushes.White;
        private readonly SolidColorBrush autoCompleteBackground = new SolidColorBrush(SystemColors.HighlightColor) { Opacity = 0.5 };
        private readonly Brush cursorColor = Brushes.Black;

        private AutoCompleteControler _acControler;

        public delegate void ObjectChangedEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event ObjectChangedEventHandler ObjectChanged;

        public delegate void LeavingEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event LeavingEventHandler Leaving;

        public delegate void LeavingViaShiftEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event LeavingViaShiftEventHandler LeavingViaShift;

        // dependant properties which can be set for the user control
        public static readonly DependencyProperty AutoCompleteWidthDependency = 
                               DependencyProperty.Register(c_AutoCompleteWidthPropertyName, typeof(string), typeof(AutoCompleteTextBox), 
                                                           new FrameworkPropertyMetadata("100", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AutoCompleteTextBoxHeightDependency =
                               DependencyProperty.Register(c_AutoCompleteTextBoxHightPropertyName, typeof(string), typeof(AutoCompleteTextBox),
                                                           new FrameworkPropertyMetadata("100", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ListBoxReadOnlyDependency =
                               DependencyProperty.Register(c_AutoCompleteTextBoxListBoxReadOnlyPropertyName, typeof(bool), typeof(AutoCompleteTextBox),
                                                           new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        #region constructor
        public AutoCompleteTextBox()
        {
            InitializeComponent();

            runAutocompleteText.Foreground = autoCompleteForeground;
            runAutocompleteText.Background = autoCompleteBackground;
            rtbText.CaretBrush = cursorColor;

            _acControler = new AutoCompleteControler(rtbText, lbAutoComplete, runEnteredText, runAutocompleteText, ListBoxPopUp, this);

            _acControler.ObjectChanged += _acControler_ObjectChanged;
            _acControler.Leaving += _acControler_Leaving;
            _acControler.LeavingViaShift += _acControler_LeavingViaShift;

            rtbText.LostFocus += RtbText_LostFocus;
        }
        #endregion

        #region methods
        public string AutoCompleteWidth
        {
            get
            {
                return (string) GetValue(AutoCompleteWidthDependency);
            }
            set
            {
                SetValue(AutoCompleteWidthDependency, value);
            }
        }

        public string AutoCompleteTextBoxHeight
        {
            get
            {
                return (string) GetValue(AutoCompleteTextBoxHeightDependency);
            }
            set
            {
                SetValue(AutoCompleteTextBoxHeightDependency, value);
            }
        }

        public bool ListBoxReadOnly
        {
            get
            {
                return (bool) GetValue(ListBoxReadOnlyDependency);
            }
            set
            {
                SetValue(ListBoxReadOnlyDependency, value);
            }
        }

        public void ClearSearchPool()
        {
            _acControler.ClearSearchPool();
        }

        public bool HasObject(string key, object o)
        {
            return _acControler.IsInSearchpool(key, o);
        }

        public bool HasKey(string key)
        {
            return _acControler.IsKeyInSearchpool(key);
        }

        public List<Tuple<string, object>> GetObjectsWithKey(string key)
        {
            return _acControler.ObjectsWithKey(key);
        }

        public void AddObject(string key, object o)
        {
            _acControler.AddSearchPool(key, o);
        }

        public bool RemoveObject(string key, object o)
        {
            return _acControler.RemoveObject(key, o);
        }

        public int RemoveObjectsByKey(string key)
        {
            return _acControler.RemoveObjectsByKey(key);
        }

        public void UpdateObject(string keyOriginal, object oOriginal, string keyModified, object oModified)
        {
            _acControler.UpdateObject(keyOriginal, oOriginal, keyModified, oModified);
        }

        public void SortSearchPool()
        {
            _acControler.SortSearchPool();
        }

        public void ClearText()
        {
            _acControler.ClearText();
        }

        public void SetFocus()
        {
            rtbText.Focus();
        }

        public bool SelectKey(string key)
        {
            return _acControler.SelectKey(key);
        }

        public object GetCurrentObject()
        {
            return _acControler.GetCurrentObject();
        }
       
        public string GetCurrentText()
        {
            return _acControler.GetCurrentText();
        }

        protected void OnObjectChanged(AutoCompleteTextBoxControlEventArgs e)
        {
            ObjectChangedEventHandler handler = ObjectChanged;

            if (handler != null)
            {
                ObjectChanged(this, e);
            }
        }

        protected void OnLeaving(AutoCompleteTextBoxControlEventArgs e)
        {
            LeavingEventHandler handler = Leaving;

            if (handler != null)
            {
                Leaving(this, e);
            }
        }

        protected void OnLeavingViaShift(AutoCompleteTextBoxControlEventArgs e)
        {
            LeavingViaShiftEventHandler handler = LeavingViaShift;

            if (handler != null)
            {
                LeavingViaShift(this, e);
            }
        }

        private void RtbText_LostFocus(object sender, RoutedEventArgs e)
        {
            // if clicked outside the listbox with the autocomplete entries, this is a lost of focus of the user control
            // clicks inside the listbox is still inside the user control, even though it is outside the text box
            Point mousePositon = Mouse.GetPosition(lbAutoComplete);
            if ( (mousePositon.X > lbAutoComplete.ActualWidth) || (mousePositon.Y > lbAutoComplete.ActualHeight))
            {
                _acControler.HandleLostFocus();
            }
        }

        private void _acControler_Leaving(object sender, AutoCompleteTextBoxControlEventArgs e)
        {
            AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(e.Object, e.Text);
            OnLeaving(arg);
        }

        private void _acControler_LeavingViaShift(object sender, AutoCompleteTextBoxControlEventArgs e)
        {
            AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(e.Object,e.Text);
            OnLeavingViaShift(arg);
        }

        private void _acControler_ObjectChanged(object sender, AutoCompleteTextBoxControlEventArgs e)
        {
            AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(e.Object, e.Text);
            OnObjectChanged(arg);
        }

        private void rtbText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // build the text
            _acControler.CreateAutoCompleteText(e.Text);

            // stop further handling of event (otherwise type character is shown)
            e.Handled = true;
        }

        private void rtbText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                // disable some keys
                case Key.Left:
                case Key.Home:
                case Key.Right:
                case Key.End:
                    e.Handled = true;
                    break;

                // return
                case Key.Return:
                    e.Handled = true;
                    _acControler.HandleReturnPressed();
                    break;

                // arrow up
                case Key.Up:
                    _acControler.ToggleAutoCompleteText(AutoCompleteControler.E_toogleDirection.up);
                    break;

                // arrow down
                case Key.Down:
                    _acControler.ToggleAutoCompleteText(AutoCompleteControler.E_toogleDirection.down);
                    break;

                // backspace
                case Key.Back:
                    _acControler.CreateAutoCompleteText(string.Empty, true);
                    e.Handled = true;
                    break;

                // space
                case Key.Space:
                    _acControler.CreateAutoCompleteText(" ");
                    e.Handled = true;
                    break;

                // tab (with or without shift)
                case Key.Tab:
                    if ( (Keyboard.IsKeyDown(Key.LeftShift)) || (Keyboard.IsKeyDown(Key.RightShift))) 
                        _acControler.HandleShiftTabPressed();
                    else
                        _acControler.HandleTabPressed();                    
                    e.Handled = true;
                    break;

                // escape
                case Key.Escape:
                    _acControler.HandleEscapePressed();
                    e.Handled = true;
                    break;

                default:
                    // do nothing
                    break;
            }
        }

        private void lbAutoComplete_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;

            // check if items was changed by mouse selection
            if ((lb.SelectedIndex != -1) && (_acControler.AutoCompletePosition != lb.SelectedIndex))
            {
                // let the controler do the work (with the text and index)
                _acControler.HandleSelectionChange(((Tuple<string, object>)lb.SelectedItem).Item1, lb.SelectedIndex);

                // set focus to the textbox
                rtbText.Focus();
            }
        }

        private void lbAutoComplete_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Return:
                    _acControler.DoAutoComplete();
                    e.Handled = true;
                    break;
            }
        }
        #endregion
    }
}
