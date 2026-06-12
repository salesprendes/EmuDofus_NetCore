namespace Game.Job.Forjamagia
{
    public sealed class ContextoForjamagia
    {
        public IObjetoForjable Objeto { get; init; }
        public RunaForjamagia Runa { get; init; }
        public int NivelMago { get; init; }
        public ConfiguracionForjamagia Configuracion { get; init; }
        public double PesoRuna => CalculoPesos.DesdeCenti(PesoRunaCenti);
        public int PesoRunaCenti { get; init; }
        public int ValorActual { get; init; }
        public int ValorNuevo { get; init; }
        public int MaximoPlantilla { get; init; }
        public bool EsExo { get; init; }
        public bool EsOver { get; init; }
        public bool EstaEnJet { get; init; }
        public int DistanciaAlMaximo { get; init; }
        public double Pozo => CalculoPesos.DesdeCenti(PesoCentiPozo);
        public int PesoCentiPozo { get; init; }
        public int PesoCentiObjetoPlantilla { get; init; }
        public int PesoCentiObjetoActual { get; init; }
        public int PesoCentiOverObjeto { get; init; }
        public int PesoCentiOverObjetoNuevo { get; init; }
    }
}
