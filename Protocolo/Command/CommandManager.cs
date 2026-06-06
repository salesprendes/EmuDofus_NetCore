using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Protocolo.Framework.Command
{
    public sealed class CommandManager<C> where C : CommandContext
    {
        private readonly List<Command<C>> m_commands = new List<Command<C>>();
        private readonly IDictionary<string, Command<C>> m_commandsByAlias =
            new Dictionary<string, Command<C>>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<Command<C>> Commands => m_commands;

        public CommandManager()
        {
        }

        public bool Execute(C context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!context.TextCommandArgument.TryReadWord(out var word))
                return false;

            return m_commandsByAlias.TryGetValue(word, out var command) && command.Execute(context);
        }

        public void RegisterCommands()
        {
            RegisterCommands(Assembly.GetCallingAssembly());
        }

        public void RegisterCommands(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            foreach (var type in assembly.GetTypes())
            {
                if (CanRegister(type))
                    AddCommand((Command<C>)Activator.CreateInstance(type));
            }
        }

        public void AddCommand(Command<C> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            ValidateAliases(command);
            command.RegisterNestedSubCommands();

            foreach (var alias in command.Aliases)
                m_commandsByAlias.Add(alias, command);

            m_commands.Add(command);
        }

        public void RemoveCommand(Command<C> command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!m_commands.Remove(command))
                return;

            foreach (var alias in command.Aliases)
                m_commandsByAlias.Remove(alias);
        }

        private static bool CanRegister(Type type)
        {
            return type != null &&
                   !type.IsAbstract &&
                   type.IsSubclassOf(typeof(Command<C>)) &&
                   !typeof(SubCommand<C>).IsAssignableFrom(type);
        }

        private void ValidateAliases(Command<C> command)
        {
            if (command.Aliases == null || command.Aliases.Length == 0)
                throw new Exception(string.Format("El comando `{0}` debe tener al menos un alias.", command.GetType().FullName));

            foreach (var alias in command.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    throw new Exception(string.Format("El comando `{0}` tiene un alias vacio.", command.GetType().FullName));

                if (m_commandsByAlias.ContainsKey(alias))
                    throw new Exception(string.Format("Ya existe un comando registrado con el alias `{0}`.", alias));
            }

            var duplicateAlias = command.Aliases
                .GroupBy(alias => alias, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateAlias != null)
                throw new Exception(string.Format("El comando `{0}` tiene repetido el alias `{1}`.", command.GetType().FullName, duplicateAlias.Key));
        }
    }
}
