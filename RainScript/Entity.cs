namespace RainScript
{
    internal readonly struct Entity
    {
        public readonly ulong entity;
        public Entity(ulong entity)
        {
            this.entity = entity;
        }
        public override string ToString()
        {
            return "Entity:" + entity.ToString();
        }
        public static readonly Entity NULL = new Entity();
    }
}
