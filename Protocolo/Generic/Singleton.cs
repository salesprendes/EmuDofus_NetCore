using Protocolo.Framework.Generic.Logging;

namespace Protocolo.Framework.Generic
{
    public abstract class Singleton<T> where T : class, new()
    {
        public static ILogger Logger = LogManager.GetLogger(typeof(T));

        public static T Instance
        {
            get
            {
                return SingletonAllocator.instance;
            }
        }

        internal static class SingletonAllocator
        {
            internal static T instance;

            static SingletonAllocator()
            {
                instance = new T();
            }
        }
    }
}
