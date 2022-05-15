using Caliburn.Micro;
using System;
using System.Windows.Input;
using System.Windows.Media;

namespace Diplom.UI
{
    public class SelectableFunction
    {
        public FunctionContainer Function { get; set; }
        public bool IsSelected { get; set; }
    }

    public class FunctionContainer
    {
        public FunctionContainer(string _name, BindableCollection<Argument> _args, FunctionWithArgumentCommand _command, MoveFuncCommand _up, MoveFuncCommand _down, MoveFuncCommand _remove)
        {
            Name = _name;
            Args = _args;
            Command = _command;
            MoveUP = _up;
            MoveDOWN = _down;
            Remove = _remove;
            Command.args = Args;
        }

        public string Name { get; set; }

        public FunctionWithArgumentCommand Command { get; set; }
        public MoveFuncCommand MoveUP { get; set; }
        public MoveFuncCommand MoveDOWN { get; set; }
        public MoveFuncCommand Remove { get; set; }

        public void Run()
        {
            Command.Execute(Args);
        }

        public BindableCollection<Argument> Args { get; set; } = new BindableCollection<Argument>();
    }

    public class FunctionWithArgumentCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;
        public BindableCollection<Argument> args;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public FunctionWithArgumentCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(args);
        }
    }

    public class MoveFuncCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public MoveFuncCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }

    public abstract class Argument
    {
        public string Name { get; set; }

    }

    public class IntArgument : Argument
    {
        public IntArgument(string _name, int _min, int _max, int _value)
        {
            Name = _name; Min = _min; Max = _max; Value = _value;
        }

        public int Value { get; set; }

        public int Max { get; set; }
        public int Min { get; set; }


    }

    public class FloatArgument : Argument
    {
        public FloatArgument(string _name, float _min, float _max, float _value, double _tickFrequency)
        {
            Name = _name; Min = _min; Max = _max; Value = _value; TickFrequency = _tickFrequency;
        }

        public float Value { get; set; }
        public float Max { get; set; }
        public float Min { get; set; }
        public double TickFrequency { get; set; }

    }

    public class BoolArgument : Argument
    {
        public BoolArgument(string _name, bool _value)
        {
            Name = _name; Value = _value;
        }
        public bool Value { get; set; }

    }

    public class ColorArgument : Argument
    {
        public ColorArgument(string _name, Color _value)
        {
            Name = _name; Value = _value;
        }
        public Color Value { get; set; }

    }

    public class MapVariantArgument : Argument
    {
        public MapVariantArgument(string _name, MapVariant _value)
        {
            Name = _name; Value = _value;

            AllValues = Enum.GetValues(typeof(MapVariant));
        }
        public MapVariant Value { get; set; }
        public Array AllValues { get; set; }
    }

    public enum MapVariant
    {
        BandContrast, Euler, Strain
    }
}
