using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("hechizos")]
    public class SpellDAO : DataAccessObject<SpellDAO>
    {
        public int id { get; set; }
        public string nombre { get; set; }
        public int sprite { get; set; }
        public string spriteInfos { get; set; }
        public string nivel1 { get; set; }
        public string nivel2 { get; set; }
        public string nivel3 { get; set; }
        public string nivel4 { get; set; }
        public string nivel5 { get; set; }
        public string nivel6 { get; set; }
        public string afectados { get; set; }
        public string condiciones { get; set; }
        public string descripcion { get; set; }
    }
}
