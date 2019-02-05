using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace WPFUserControl
{
    public class AutoCompleteControler
    {
        #region events
        public delegate void ObjectChangedEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event ObjectChangedEventHandler ObjectChanged;
        public delegate void LeavingEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event LeavingEventHandler Leaving;
        public delegate void LeavingViaShiftEventHandler(object sender, AutoCompleteTextBoxControlEventArgs e);
        public event LeavingViaShiftEventHandler LeavingViaShift;
        #endregion

        #region attributes
        private List<Tuple<string, object>> _searchPool;

        private string _textEntered;
        public enum E_toogleDirection { up, down };
        public enum E_autoCompleteMode { regular, showlist };
        private int _autoCompletePosition = -1;
        private E_autoCompleteMode _currentMode;

        private RichTextBox _richTextBox;
        private ListBox _lbAutoComplete;
        private Run _runEnteredText;
        private Run _runAutoCompleteText;
        Popup _listBoxPopUp;

        private object _autocompletedObject;
        private bool _tabPressed;


        public int AutoCompletePosition { get => _autoCompletePosition; set => _autoCompletePosition = value; }
        public AutoCompleteTextBox _actb;
        #endregion

        #region constructors
        public AutoCompleteControler(RichTextBox richTextBox, ListBox listBox, Run runEnteredText, Run runAutoCompleteText, Popup listBoxPopUp, AutoCompleteTextBox actb)
        {
            _richTextBox = richTextBox;
            _lbAutoComplete = listBox;
            _runEnteredText = runEnteredText;
            _runAutoCompleteText = runAutoCompleteText;
            _listBoxPopUp = listBoxPopUp;

            // since the "ListBoxReadOnly"-property is set only "OnApplyTemplate()", the value is not up to date during the contractor
            // therefore the whole usercontrol needs to be shared with the controler
            // later in the code the value is set (OnApplyTemplate() was called by the framework) - therefore we can use
            // "_actb.ListBoxReadOnly" later...
            _actb = actb;

            _autocompletedObject = null;
            _searchPool = new List<Tuple<string, object>>();

            _currentMode = E_autoCompleteMode.regular;
        }
        #endregion

        #region methods
        public void AddSearchPool(string text, object o)
        {
            _searchPool.Add(Tuple.Create(text, o));
        }

        public void CreateAutoCompleteTextCore(string text, bool remove = false)
        {
            // update the text entered (or deleted) by the User
            _textEntered = remove ? _textEntered.Remove(_textEntered.Length - 1, 1) : _textEntered + text;

            // search in the autocomplete list
            List<Tuple<string, object>> foundObject = FindObjects(_textEntered);

            // set the first part of the text in the control
            _runEnteredText.Text = _textEntered;

            // show or hide list box depending on autocomplete entries
            //_lbAutoComplete.Visibility = foundObject.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            ChangeVisiblity(foundObject.Count > 0 ? Visibility.Visible : Visibility.Hidden);

            // text was found to autocomplete
            if (foundObject.Count > 0)
            {
                AutoCompletePosition = 0;
                SetAutoCompleteString(foundObject[AutoCompletePosition].Item1);

                UpdateAutoCompleteListBox(foundObject, AutoCompletePosition);
            }
            // text was not found to autocomplete
            else
            {
                // no autocomplete active
                AutoCompletePosition = -1;

                // reset the autocompleted object
                _autocompletedObject = null;

                _runAutoCompleteText.Text = string.Empty;
                _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;
            }
        }

        public void CreateAutoCompleteText(string text, bool remove = false)
        {     
            // *** that needs most likely some refactoring since it's completed and partly redunant
            // *** anways it looks like it works for now

            // readonly means the use can only select from the entries of the listbox
            // check the constructor for more information about _actb.ListBoxReadOnly
            if (_actb.ListBoxReadOnly && !((remove && (string.IsNullOrEmpty(_textEntered)))))
            {
                // create a text to check if there is a hit in the search pool (autocomplete is possible)
                string textEnteredToCheck = remove ? _textEntered.Remove(_textEntered.Length - 1, 1) : _textEntered + text;

                // go ahead with regular processing if 
                // * no entries for autocompleting found or
                // * the entered text is empty (after deleting the last character)
                List<Tuple<string, object>> foundObjects = FindObjects(textEnteredToCheck);
                if ( (foundObjects.Count > 0) || (textEnteredToCheck == string.Empty))
                {
                    // .. go ahead as normal
                    CreateAutoCompleteTextCore(text, remove);
                }
            }

            // not readonly 
            else
            {
                // remove not possible if there is not text entered
                if (!((remove && (string.IsNullOrEmpty(_textEntered)))))
                {
                    CreateAutoCompleteTextCore(text, remove);
                }
            }

            // update the preview current object
            UpdatePreviewObject();
        }

        public void SetAutoCompleteString(string autoCompleteFullText)
        {
            // get the part of the string which is autocompleted
            string autoComplete = (_textEntered != null) ? autoCompleteFullText.Substring(_textEntered.Length) : string.Empty;

            // set the second part of the text in the textbox
            _runAutoCompleteText.Text = autoComplete;

            // reset the cursor
            var charoffset = (_textEntered != null) ? _textEntered.Length : 0;
            var txtPtr = GetPointerFromCharOffset(charoffset, _richTextBox.Document.ContentStart, _richTextBox.Document);
            _richTextBox.CaretPosition = txtPtr;
        }

        public bool SelectKey(string key)
        {
            // *** Work in process ***

            var index = _searchPool.FindIndex(t => t.Item1 == key);

            if (index != -1)
            {
                // 
                //_runEnteredText.Text = key;
                //_runAutoCompleteText.Text = string.Empty;

                // move cursor to end of text
                //_richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;

                

                // search in the autocomplete list
                List<Tuple<string, object>> foundObject = FindObjects(key);

                //AutoCompletePosition = 0;
                AutoCompletePosition = index;
                SetAutoCompleteString(foundObject[AutoCompletePosition].Item1);

                // select current item
                _lbAutoComplete.SelectedIndex = AutoCompletePosition;

                // move cursor to end of text
                _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsInSearchpool(string key, object o)
        {
            return _searchPool.Contains(new Tuple<string, object>(key, o));
        }

        public bool IsKeyInSearchpool(string key)
        {
            object o = _searchPool.Find(t => t.Item1 == key);

            return o != null;
        }

        public List<Tuple<string, object>> ObjectsWithKey(string key)
        {
            return _searchPool.FindAll(t => t.Item1 == key);
        }

        public bool RemoveObject(string key, object o)
        {
            return _searchPool.Remove(Tuple.Create(key, o));
        }

        public int RemoveObjectsByKey(string key)
        {
            // find the objects to be removed
            List<Tuple<string, object>> objectToRemove = new List<Tuple<string, object>>();
            foreach (Tuple<string, object> t in _searchPool)
            {
                if (t.Item1 == key)
                {
                    objectToRemove.Add(t);
                }
            }

            // remove the objects
            foreach (Tuple<string, object> t in objectToRemove)
            {
                _searchPool.Remove(t);
            }

            return objectToRemove.Count;
        }

        public void ClearSearchPool()
        {
            _searchPool.Clear();
        }

        public void UpdateObject(string keyOriginal, object oOriginal, string keyModified, object oModified)
        {
            // remove original and add new (modified) object
            RemoveObject(keyOriginal, oOriginal);
            AddSearchPool(keyModified, oModified);
        }

        public void SortSearchPool()
        {
            _searchPool.Sort((x, y) => string.Compare(x.Item1, y.Item1));
        }

        public void ToggleAutoCompleteText(E_toogleDirection direction)
        {
            switch (_currentMode)
            {
                case E_autoCompleteMode.regular:
                    RegularToggle(direction);
                    break;
                case E_autoCompleteMode.showlist:
                    ShowListToggle(direction);
                    break;
                default:
                    // unknown enum value
                    Debug.Assert(false);
                    break;
            }
        }

        private void ShowListToggle(E_toogleDirection direction)
        {
            // down-key
            if ((direction == E_toogleDirection.down) && (AutoCompletePosition + 1 < _searchPool.Count))
            {
                // increase autocomplete position and set the new autocomplete string
                SetAutoCompleteString(_searchPool[++AutoCompletePosition].Item1);

                // select the correct entry in listbox
                SelectAutoCompleteListBoxItem(AutoCompletePosition);
            }

            // up-key
            else if ((direction == E_toogleDirection.up) && (AutoCompletePosition > 0))
            {
                // decrease autocomplete position and set the new autocomplete string
                SetAutoCompleteString(_searchPool[--AutoCompletePosition].Item1);

                // select the correct entry in listbox
                SelectAutoCompleteListBoxItem(AutoCompletePosition);

                _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;
            }

            // set cursor to end of string in textbox
            _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;

            // update the assgined object
            UpdatePreviewObject();
        }

        private void RegularToggle(E_toogleDirection direction)
        {
            // press cursor down with already open (visiable) listbox and entered text
            if (AutoCompletePosition != -1)
            {
                // search in the autocomplete list
                List<Tuple<string, object>> foundObjects = FindObjects(_textEntered);

                // number of found autocomplete entries
                var autoCompleteCount = foundObjects.Count;

                // down-key
                if ((direction == E_toogleDirection.down) && (AutoCompletePosition + 1 < autoCompleteCount))
                {
                    // increase autocomplete position and set the new autocomplete string
                    SetAutoCompleteString(foundObjects[++AutoCompletePosition].Item1);

                    // select the correct entry in listbox
                    SelectAutoCompleteListBoxItem(AutoCompletePosition);
                }

                // up-key
                else if ((direction == E_toogleDirection.up) && (AutoCompletePosition > 0))
                {
                    // decrease autocomplete position and set the new autocomplete string
                    SetAutoCompleteString(foundObjects[--AutoCompletePosition].Item1);

                    // select the correct entry in listbox
                    SelectAutoCompleteListBoxItem(AutoCompletePosition);
                }
            }

            // press cursor-down 
            // - with empty textbox
            // - not empty search pool
            // so show all entries
            else if (string.IsNullOrEmpty(GetCurrentText()) && (direction == E_toogleDirection.down) && (_searchPool.Count > 0))
            {
                // change the mode to "showlist"
                _currentMode = E_autoCompleteMode.showlist;
                ShowFullList();
            }

            // update the preview current object
            UpdatePreviewObject();
        }

        public bool DoAutoComplete()
        {
            bool autoCompleteDone;

            if (_autoCompletePosition != -1)    
            {
                // replace entered text with autocompleted text
                _textEntered = _runEnteredText.Text + _runAutoCompleteText.Text;

                // set the richtextbox runs correctly (no autocomplete text)
                _runEnteredText.Text = _textEntered;
                _runAutoCompleteText.Text = string.Empty;

                // move cursor to end of text
                _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;

                // hide and clear the listbox
                _lbAutoComplete.Items.Clear();
                ChangeVisiblity(Visibility.Hidden);

                // autocomplete sucessfull
                autoCompleteDone = true;

                // reset the autocomplete position
                _autoCompletePosition = -1;
            }
            else
            {
                // no autcomplete done
                autoCompleteDone = false;
            }

            return autoCompleteDone;
        }

        public void HandleEscapePressed()
        {
            // escape closes the autocompleted box and just takes the entered text
            // useful the new text is only the beginning of an existing auto complete text
            // e. g. new text: "Zwei" already existing auto complete text "Zwei glorreiche Hallunken"

            // set the richtextbox runs correctly (no autocomplete text)
            _runEnteredText.Text = _textEntered;
            _runAutoCompleteText.Text = string.Empty;

            // hide and clear the listbox
            _lbAutoComplete.Items.Clear();
            //_lbAutoComplete.Visibility = Visibility.Hidden;
            ChangeVisiblity(Visibility.Hidden);

            // reset the autocomplete position
            _autoCompletePosition = -1;

            // raise the event
            AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(GetCurrentObject(), _textEntered);
            OnLeaving(arg);
        }

        public void HandleTabPressed()
        {
            switch (_currentMode)
            {
                case E_autoCompleteMode.regular:
                    RegularTabPressed();
                    break;
                case E_autoCompleteMode.showlist:
                    ShowListTabPressed();
                    break;
                default:
                    // unknown enum value
                    Debug.Assert(false);
                    break;
            }
        }

        private void RegularTabPressed()
        {
            // remember this for advanced event handling (LostFocus)
            _tabPressed = true;

            // try to autocomplete and check if sucessfull
            bool autoCompleteDone = DoAutoComplete();

            // if no autocomplete was done and tab was pressed, raise the "Leaving" Event
            if (!autoCompleteDone)
            {
                // raise the event
                AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(GetCurrentObject(), _textEntered);
                OnLeaving(arg);
            }
        }

        private void ShowListTabPressed()
        {
            // reset mode
            _currentMode = E_autoCompleteMode.regular;

            // remember this for advanced event handling (LostFocus)
            _tabPressed = true;

            // replace entered text with autocompleted text
            _textEntered = _runEnteredText.Text + _runAutoCompleteText.Text;

            // set the richtextbox runs correctly (no autocomplete text)
            _runEnteredText.Text = _searchPool[AutoCompletePosition].Item1;
            _runAutoCompleteText.Text = string.Empty;

            // move cursor to end of text
            _richTextBox.CaretPosition = _richTextBox.Document.ContentEnd;

            // hide and clear the listbox
            _lbAutoComplete.Items.Clear();
            ChangeVisiblity(Visibility.Hidden);

            // reset the autocomplete position
            _autoCompletePosition = -1;
        }
       
        public void HandleReturnPressed()
        {
            // to do
        }

        public void HandleShiftTabPressed()
        {
            // remember this for advanced event handling (LostFocus)

            _tabPressed = true;

            // try to autocomplete and check if sucessfull
            bool autoCompleteDone = DoAutoComplete();

            // if no autocomplete was done and tab was pressed, raise the "Leaving" Event
            if (!autoCompleteDone)
            {
                // raise the event
                AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(GetCurrentObject(), _textEntered);
                OnLeavingViaShift(arg);
            }
        }

        public void HandleLostFocus()
        {
            // handle the situation that the user clicks outside the control
            // in this case, autocomplete needs to be triggered the the Leaving-Event needs to be raised

            // leaving the control via tab pressed is handled seperatly
            if (!_tabPressed)
            {
                // do the autocomplete
                DoAutoComplete();

                // raise the event
                AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(GetCurrentObject(), _textEntered);
                OnLeaving(arg);
            }
            else
            {
                // reset the tabpressed status
                _tabPressed = false;
            }
        }

        public void HandleSelectionChange(string text, int index)
        {
            // update the autocomplete position
            AutoCompletePosition = index;

            // take the selected item text for doing the auto completion
            SetAutoCompleteString(text);

            //
            UpdatePreviewObject();
        }

        public object GetAutoSelectObject()
        {
            return _autoCompletePosition != -1 ? _searchPool.ElementAt(_autoCompletePosition).Item2 : null;
        }

        public void ClearText()
        {
            _runEnteredText.Text = string.Empty;
            _runAutoCompleteText.Text = string.Empty;
            _textEntered = string.Empty;
            _autoCompletePosition = -1;
            _lbAutoComplete.Items.Clear();
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

        private void ShowFullList()
        {
            // show the listbox
            //_lbAutoComplete.Visibility = Visibility.Visible;
            ChangeVisiblity(Visibility.Visible);

            // position if first entry
            AutoCompletePosition = 0;

            // show complete search pool (== all entries)
            UpdateAutoCompleteListBox(_searchPool, AutoCompletePosition);

            // set texts
            _textEntered = string.Empty;
            _runEnteredText.Text = string.Empty;
            _runAutoCompleteText.Text = _searchPool[0].Item1;
        }

        private void UpdatePreviewObject()
        {
            // get the "object" of the selected item (if position is valid)
            // ... and remember it for reporting the current autocompleted object
            _autocompletedObject = GetCurrentObject();

            var currentText = GetCurrentText();

            // raise the event
            AutoCompleteTextBoxControlEventArgs arg = new AutoCompleteTextBoxControlEventArgs(_autocompletedObject, currentText);
            OnObjectChanged(arg);
        }

        private List<Tuple<string, object>> FindObjects(string searchText)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                return _searchPool.FindAll(s => s.Item1.StartsWith(searchText));
            }
            else
            {
                return new List<Tuple<string, object>>();
            }
        }

        private void UpdateAutoCompleteListBox(List<Tuple<string, object>> foundObject, int autoCompletePosition)
        {
            // remove all existing items
            _lbAutoComplete.Items.Clear();

            // add new items
            foreach (Tuple<string,object> o in foundObject)
            {
                _lbAutoComplete.Items.Add(o);
                _lbAutoComplete.DisplayMemberPath = "Item1";
                _lbAutoComplete.SelectedValuePath = "Item2";
            }

            // select current item
            SelectAutoCompleteListBoxItem(autoCompletePosition);
        }

        private void SelectAutoCompleteListBoxItem(int index)
        {
            // select current item
            _lbAutoComplete.SelectedIndex = index;

            // scroll as required
            _lbAutoComplete.ScrollIntoView(_lbAutoComplete.Items[index]);
        }

        private TextPointer GetPointerFromCharOffset(Int32 charOffset, TextPointer startPointer, FlowDocument document)
        {
            TextPointer navigator = startPointer;
            if (charOffset == 0)
            {
                return navigator;
            }

            TextPointer nextPointer = navigator;
            Int32 counter = 0;
            
            while (nextPointer != null && counter < charOffset)
            {
                if (nextPointer.CompareTo(document.ContentEnd) == 0)
                {
                    // If we reach to the end of document, return the EOF pointer.
                    return nextPointer;
                }

                if (nextPointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    nextPointer = nextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
                    counter++;
                }
                else
                {
                    // If the current pointer is not pointing at a character, we should move to next insertion point
                    // without incrementing the character counter.
                    nextPointer = nextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
                }
            }

            return nextPointer;
        }

        internal object GetCurrentObject()
        {
            // get the "object" of the selected item (if position is valid)
            return _autoCompletePosition != -1 ? ((Tuple<string, object>)_lbAutoComplete.SelectedItem).Item2 : _autocompletedObject;
        }
        
        internal string GetCurrentText()
        {
            return new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd).Text.TrimEnd('\r', '\n');
        }

        private void ChangeVisiblity(Visibility visibility)
        {
            // workaround 1 since it looks like the visblity event is not fired always
            _lbAutoComplete.Visibility = visibility;

            // workaround 2 - there is some refresh problem on TabItems (applying to the second TabItem),
            // so IsOpen needs to be set twice to refresh
            _listBoxPopUp.IsOpen = (visibility != Visibility.Visible);
            _listBoxPopUp.IsOpen = (visibility == Visibility.Visible);
        }
        #endregion
    }
}
