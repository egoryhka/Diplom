using Caliburn.Micro;
using OxyPlot.Series;
using System;
using System.Windows.Input;

namespace Diplom.UI
{
    public class SelectableAnalys
    {
        public AnalysContainer Analys { get; set; }
        public bool IsSelected { get; set; }
    }

    public class AnalysContainer
    {
        public AnalysContainer(string _name, BindableCollection<AnalysData> _analysData, AnalysWithArgumentCommand _command, MoveAnalysBlockCommand _remove)
        {
            Name = _name;
            AnalysData = _analysData;
            Command = _command;
            Remove = _remove;
            Command.args = _analysData;
        }
        public string Name { get; set; }

        public AnalysWithArgumentCommand Command { get; set; }
        public MoveAnalysBlockCommand Remove { get; set; }
        public BindableCollection<AnalysData> AnalysData { get; set; } = new BindableCollection<AnalysData>();
    }

    public class MoveAnalysBlockCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public MoveAnalysBlockCommand(Action<object> execute, Func<object, bool> canExecute = null)
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

    public class AnalysWithArgumentCommand : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;
        public BindableCollection<AnalysData> args;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public AnalysWithArgumentCommand(Action<object> execute, Func<object, bool> canExecute = null)
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


    public abstract class AnalysData
    {
        public string Name { get; set; }
    }

    public class Diagram : AnalysData
    {
        public Diagram(string _name, string _xName, string _yName)
        {
            Name = _name; xName = _xName; yName = _yName;
        }

        public BindableCollection<ColumnItem> Values { get; set; } = new BindableCollection<ColumnItem>();
        public BindableCollection<string> CategoriesLabels { get; set; } = new BindableCollection<string>();
        public string xName { get; set; }
        public string yName { get; set; }

    }

    public class Table : AnalysData
    {
        public Table(string _name)
        {
            Name = _name;
        }

        public BindableCollection<object> Values { get; set; } = new BindableCollection<object>();
    }

    public class PhaseConsist
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public float Percent { get; set; }

    }
}