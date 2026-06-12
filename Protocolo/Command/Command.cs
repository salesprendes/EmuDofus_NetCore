using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Protocolo.Framework.Command
{
    public abstract class Command<C> where C : CommandContext
    {
        private readonly List<SubCommand<C>> m_subCommands = new List<SubCommand<C>>();
        public abstract string[] Aliases { get; }
        public abstract string Description { get; }
        public IReadOnlyList<SubCommand<C>> SubCommands => m_subCommands;
        public string PrimaryAlias => Aliases.FirstOrDefault() ?? GetType().Name;

        internal bool MatchesAlias(string alias) => !string.IsNullOrEmpty(alias) && Aliases.Any(commandAlias => string.Equals(commandAlias, alias, StringComparison.OrdinalIgnoreCase));
        protected virtual bool CanExecute(C context) => true;

        protected virtual void Process(C context) { }

        public void Serialize(StringBuilder message) => Serialize(message, null, "");
        public void Serialize(StringBuilder message, string parent) => Serialize(message, null, parent);
        public void Serialize(StringBuilder message, C context) => Serialize(message, context, "");

        public void Serialize(StringBuilder message, C context, string parent)
        {
            if (context != null && !CanExecute(context))
                return;

            List<SubCommand<C>> visibleSubCommands = context == null ? m_subCommands : m_subCommands.Where(subCommand => subCommand.CanExecute(context)).ToList();

            if (m_subCommands.Count > 0)
            {
                if (visibleSubCommands.Count == 0)
                    return;

                message.Append("[").Append(PrimaryAlias).Append("]").Append('\n');
            }
            else
            {
                message.Append(parent).Append(PrimaryAlias).Append(" : ").Append(Description).Append('\n');
            }

            foreach (var subCommand in visibleSubCommands)
                subCommand.Serialize(message, context, PrimaryAlias + " ");
        }

        public bool Execute(C context)
        {
            if (!CanExecute(context))
            {
                OnCanExecuteFailed(context);
                return true;
            }

            if (context.TextCommandArgument.TryPeekWord(out var word))
            {
                var subCommand = m_subCommands.FirstOrDefault(command => command.MatchesAlias(word));
                if (subCommand != null)
                {
                    context.TextCommandArgument.NextWord();
                    return subCommand.Execute(context);
                }
            }

            Process(context);
            return true;
        }

        protected virtual void OnCanExecuteFailed(C context)
        {
        }

        internal void RegisterNestedSubCommands()
        {
            var nestedClasses = GetType().GetNestedTypes(BindingFlags.Public);
            foreach (var nestedType in nestedClasses)
            {
                if (!nestedType.IsAbstract && nestedType.IsSubclassOf(typeof(SubCommand<C>)))
                {
                    var subCommand = Activator.CreateInstance(nestedType) as SubCommand<C>;
                    if (subCommand != null)
                        AddSubCommand(subCommand);
                }
            }
        }

        private void AddSubCommand(SubCommand<C> subCommand)
        {
            ValidateSubCommandAliases(subCommand);

            foreach (var alias in subCommand.Aliases)
            {
                if (m_subCommands.Any(command => command.MatchesAlias(alias)))
                    throw new Exception(string.Format("El comando `{0}` ya tiene un subcomando con el alias `{1}`.", PrimaryAlias, alias));
            }

            subCommand.RegisterNestedSubCommands();
            m_subCommands.Add(subCommand);
        }

        private void ValidateSubCommandAliases(SubCommand<C> subCommand)
        {
            if (subCommand.Aliases == null || subCommand.Aliases.Length == 0)
                throw new Exception($"El subcomando {subCommand.GetType().FullName} debe tener al menos un alias.");

            foreach (var alias in subCommand.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    throw new Exception($"El subcomando {subCommand.GetType().FullName} tiene un alias vacio.");
            }

            var duplicateAlias = subCommand.Aliases.GroupBy(alias => alias, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);

            if (duplicateAlias != null)
                throw new Exception($"El subcomando {subCommand.GetType().FullName} tiene repetido el alias {duplicateAlias.Key}.");
        }
    }

    public abstract class SubCommand<C> : Command<C> where C : CommandContext
    {
    }
}
