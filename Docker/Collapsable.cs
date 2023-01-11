using System;

namespace Dockord.Docker
{
    public class Collapsable<T> : IProgress<T>
    {
        private T value;

        public void Report(T value)
        {
            this.value = value;
        }

        public T ReturnValue()
        {
            return value;
        }
    }
}
