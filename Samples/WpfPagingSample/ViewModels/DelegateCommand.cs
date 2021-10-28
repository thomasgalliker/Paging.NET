using System;
using System.Windows.Input;

namespace WpfPagingSample.ViewModels
{
    public class DelegateCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Func<bool> canExecute;

        public DelegateCommand(Action<T> execute)
            : this(execute, () => true)
        {
            this.execute = execute;
        }

        public DelegateCommand(Action<T> execute, Func<bool> canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void Execute(object parameter)
        {
            var param = default(T);
            if (parameter is T p)
            {
                param = p;
            }

            this.execute(param);
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}