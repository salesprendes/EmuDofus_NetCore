namespace Protocolo.Framework.Generic
{
    public sealed class BasicTaskProcessor : TaskProcessorBase
    {
        public BasicTaskProcessor(string name, int updateInterval = 30) : base(name, updateInterval) {}
    }
}
