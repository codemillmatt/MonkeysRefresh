using System;
namespace MonkeyFinder
{
    public class EmptyDocument<T>
    {
        public string Id { get; set; }
        public T Document { get; set; }
    }
}
