using Game.Spell;

namespace Game.Job.Forjamagia
{
    public readonly struct RunaForjamagia
    {
        public EffectEnum Estadistica { get; }

        public int Potencia { get; }

        public RangoRuna Rango { get; }

        public RunaForjamagia(EffectEnum estadistica, int potencia, RangoRuna rango = RangoRuna.Desconocido)
        {
            Estadistica = estadistica;
            Potencia = potencia;
            Rango = rango;
        }

        public bool EsValida => Potencia > 0;
    }
}
