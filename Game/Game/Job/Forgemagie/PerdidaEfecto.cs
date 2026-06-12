using Game.Spell;

namespace Game.Job.Forjamagia
{
    public readonly struct PerdidaEfecto
    {
        public EffectEnum Efecto { get; }

        public int PuntosPerdidos { get; }

        public double PesoPerdido => CalculoPesos.DesdeCenti(PesoPerdidoCenti);

        public int PesoPerdidoCenti { get; }

        public PerdidaEfecto(EffectEnum efecto, int puntosPerdidos, int pesoPerdidoCenti)
        {
            Efecto = efecto;
            PuntosPerdidos = puntosPerdidos;
            PesoPerdidoCenti = pesoPerdidoCenti;
        }

        public PerdidaEfecto(EffectEnum efecto, int puntosPerdidos, double pesoPerdido)
            : this(efecto, puntosPerdidos, CalculoPesos.ACenti(pesoPerdido))
        {
        }
    }
}
