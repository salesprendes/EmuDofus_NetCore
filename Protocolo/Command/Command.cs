using System;
using System.Text;

namespace Protocolo.Framework.Command
{
    public abstract class Command<C> where C : CommandContext
    {
        public abstract string[] Aliases { get; }
        public abstract string Description { get; }
        public string PrimaryAlias => GetPrimaryAlias();
        protected virtual bool CanExecute(C context) => true;

        protected virtual void Process(C context) {}
        public void Serialize(StringBuilder message) => Serialize(message, null, "");
        public void Serialize(StringBuilder message, string parent) => Serialize(message, null, parent);
        public void Serialize(StringBuilder message, C context) => Serialize(message, context, "");

        public void Serialize(StringBuilder message, C context, string parent)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!CanSerialize(context))
                return;

            message.Append(parent).Append(PrimaryAlias).Append(" : ").Append(Description).Append('\n');
        }

        public bool Execute(C context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!CanExecute(context))
            {
                OnCanExecuteFailed(context);
                return true;
            }

            Process(context);
            return true;
        }

        protected virtual void OnCanExecuteFailed(C context) {}
        private bool CanSerialize(C context) => context == null || CanExecute(context);
        private string GetPrimaryAlias() => Aliases is { Length: > 0 } ? Aliases[0] : GetType().Name;
    }
}
