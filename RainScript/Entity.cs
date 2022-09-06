namespace RainScript
{
    internal struct Entity
    {
        public readonly ulong entity;
        public Entity(ulong entity)
        {
            this.entity = entity;
        }
        public static explicit operator Entity(ulong entity)
        {
            return new Entity(entity);
        }
        public static readonly Entity NULL = new Entity();
    }
}
