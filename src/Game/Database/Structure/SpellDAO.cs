using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("hechizos")]
    public class SpellDAO : DataAccessObject<SpellDAO>
    {
        private int _id;
        private string _nombre;
        private int _sprite;
        private string _spriteInfos;
        private string _nivel1;
        private string _nivel2;
        private string _nivel3;
        private string _nivel4;
        private string _nivel5;
        private string _nivel6;
        private string _afectados;
        private string _condiciones;
        private string _descripcion;


        public int id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }
        public int sprite
        {
            get => _sprite;
            set => SetProperty(ref _sprite, value);
        }
        public string spriteInfos
        {
            get => _spriteInfos;
            set => SetProperty(ref _spriteInfos, value);
        }
        public string nivel1
        {
            get => _nivel1;
            set => SetProperty(ref _nivel1, value);
        }
        public string nivel2
        {
            get => _nivel2;
            set => SetProperty(ref _nivel2, value);
        }
        public string nivel3
        {
            get => _nivel3;
            set => SetProperty(ref _nivel3, value);
        }
        public string nivel4
        {
            get => _nivel4;
            set => SetProperty(ref _nivel4, value);
        }
        public string nivel5
        {
            get => _nivel5;
            set => SetProperty(ref _nivel5, value);
        }
        public string nivel6
        {
            get => _nivel6;
            set => SetProperty(ref _nivel6, value);
        }
        public string afectados
        {
            get => _afectados;
            set => SetProperty(ref _afectados, value);
        }
        public string condiciones
        {
            get => _condiciones;
            set => SetProperty(ref _condiciones, value);
        }
        public string descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }
    }
}
