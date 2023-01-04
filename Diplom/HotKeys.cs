using System.Windows.Input;

namespace Diplom
{
    public static class HotKeys
    {
        static HotKeys()
        {
            InputGestureCollection inputs = new InputGestureCollection();
            inputs.Add(new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl+S"));

            SaveCommand = new RoutedUICommand("Save", "SaveCommand", typeof(HotKeys), inputs);
        }

        public static RoutedCommand SaveCommand { get; private set; }
    }
}