using Game.Manager;
using ProtoBuf;
using System;

namespace Game.Spell
{
    [Flags]
    public enum EffectEnum : int
    {
        DANO_BRUTO = -7,                                  // daño sin elemento (interno)
        STAT_MAS_ARMADURA_AIRE = -6,                          // armadura de aire (interno)
        STAT_MAS_ARMADURA_AGUA = -5,                          // armadura de agua (interno)
        STAT_MAS_ARMADURA_FUEGO = -4,                         // armadura de fuego (interno)
        STAT_MAS_ARMADURA_TIERRA = -3,                        // armadura de tierra (interno)
        STAT_MAS_ARMADURA_NEUTRAL = -2,                       // armadura neutral (interno)
        NINGUNO = -1,                                    // sin efecto (interno)

        MOVIMIENTO_TELETRANSPORTAR = 4,                             // E4: teletransporta a #1 casillas máximo
        MOVIMIENTO_EMPUJAR = 5,                                     // E5: hace retroceder #1 casilla(s)
        MOVIMIENTO_ATRAER = 6,                                      // E6: hace avanzar #1 casilla(s)
        MOVIMIENTO_INTERCAMBIAR_POSICION = 8,                        // E8: intercambia las posiciones de 2 jugadores
        DEFENSA_EVASION = 9,                                     // E9: esquiva #1% de los golpes retrocediendo #2 casilla(s)
        PANDA_CARGAR = 50,                                // E50: permite levantar a un jugador
        PANDA_LANZAR = 51,                                // E51: lanza a un jugador
        STAT_ROBO_PM = 77,                                    // E77: roba #1 a #2 PM
        STAT_MAS_PM_BONUS = 78,                                    // E78: añade +#1 a #2 PM
        COMBATE_SUERTE_ECAFLIP = 79,                              // E79: #3% de daños x#1 o cura x#2
        CURACION_VIDA_DEVUELTA = 81,                               // E81: PDV devueltos #1 a #2
        ROBO_VIDA_FIJO = 82,                                  // E82: roba #1 a #2 PDV (fijo)
        STAT_ROBO_PA = 84,                                    // E84: roba #1 a #2 PA
        DANO_VIDA_AGUA = 85,                               // E85: daño = % vida del atacante (agua)
        DANO_VIDA_TIERRA = 86,                             // E86: daño = % vida del atacante (tierra)
        DANO_VIDA_AIRE = 87,                               // E87: daño = % vida del atacante (aire)
        DANO_VIDA_FUEGO = 88,                              // E88: daño = % vida del atacante (fuego)
        DANO_VIDA_NEUTRAL = 89,                            // E89: daño = % vida del atacante (neutral)
        DANO_ENTREGA_VIDA = 90,                            // E90: entrega #1 a #2 % de su vida
        ROBO_VIDA_AGUA = 91,                              // E91: roba #1 a #2 PDV (agua)
        ROBO_VIDA_TIERRA = 92,                            // E92: roba #1 a #2 PDV (tierra)
        ROBO_VIDA_AIRE = 93,                              // E93: roba #1 a #2 PDV (aire)
        ROBO_VIDA_FUEGO = 94,                             // E94: roba #1 a #2 PDV (fuego)
        ROBO_VIDA_NEUTRAL = 95,                           // E95: roba #1 a #2 PDV (neutro)
        DANO_AGUA = 96,                                   // E96: daños #1 a #2 (agua)
        DANO_TIERRA = 97,                                 // E97: daños #1 a #2 (tierra)
        DANO_AIRE = 98,                                   // E98: daños #1 a #2 (aire)
        DANO_FUEGO = 99,                                  // E99: daños #1 a #2 (fuego)
        DANO_NEUTRAL = 100,                               // E100: daños #1 a #2 (neutrales)
        STAT_MENOS_PA_ESQUIVABLE = 101,                        // E101: PA perdidos por el blanco #1 a #2
        STAT_MAS_ARMADURA = 105,                             // E105: daños reducidos de #1 a #2
        DEFENSA_DEVOLVER_HECHIZO = 106,                           // E106: reenvía un hechizo de nivel #2 máximo
        STAT_MAS_DANO_DEVUELTO = 107,                         // E107: daños devueltos #1 a #2
        CURACION = 108,                                  // E108: PDV devueltos #1 a #2
        DANO_PROPIO = 109,                                // E109: daños para el lanzador #1 a #2
        STAT_MAS_VIDA = 110,                                 // E110: +#1 a #2 a la vida
        STAT_MAS_PA = 111,                                   // E111: +#1 a #2 PA
        STAT_MAS_DANO = 112,                                 // E112: +#1 a #2 a los daños
        STAT_MULTIPLICAR_DANO = 114,                           // E114: multiplica los daños por #1
        STAT_MAS_DANO_CRITICO = 115,                          // E115: +#1 a #2 a los golpes críticos
        STAT_MENOS_ALCANCE = 116,                             // E116: -#1 a -#2 al alcance
        STAT_MAS_ALCANCE = 117,                              // E117: +#1 a #2 al alcance
        STAT_MAS_FUERZA = 118,                               // E118: +#1 a #2 a la fuerza
        STAT_MAS_AGILIDAD = 119,                             // E119: +#1 a #2 a la agilidad
        STAT_MAS_PA_BIS = 120,                                // E120: añade +#1 a #2 PA
        STAT_MAS_DANO_BIS = 121,                              // E121: +#1 a #2 a los daños
        STAT_MAS_FALLO_CRITICO = 122,                         // E122: +#1 a #2 a los fallos críticos
        STAT_MAS_SUERTE = 123,                               // E123: +#1 a #2 a la suerte
        STAT_MAS_SABIDURIA = 124,                            // E124: +#1 a #2 a la sabiduría
        STAT_MAS_VITALIDAD = 125,                            // E125: +#1 a #2 a la vitalidad
        STAT_MAS_INTELIGENCIA = 126,                         // E126: +#1 a #2 a la inteligencia
        STAT_MENOS_PM_ESQUIVABLE = 127,                        // E127: PM perdidos #1 a #2
        STAT_MAS_PM = 128,                                   // E128: +#1 a #2 PM
        KAMAS_ROBO = 130,                                // E130: roba #1 a #2 kamas
        DANO_POR_PA = 131,                                 // E131: #1 PA utilizados hacen perder #2 PDV
        BUFF_QUITAR_TODOS = 132,                       // E132: quita los embrujos
        STAT_MAS_DANO_PORCENTAJE = 138,                       // E138: aumenta los daños un #1 a #2%
        STAT_MAS_ENERGIA = 139,                              // E139: devuelve #1 a #2 puntos de energía
        COMBATE_PASAR_TURNO = 140,                                // E140: hace pasar el siguiente turno
        COMBATE_MATAR_OBJETIVO = 141,                             // E141: mata al blanco
        STAT_MAS_DANO_FISICO = 142,                           // E142: +#1 a #2 a los daños físicos
        STAT_MAS_DANO_MAGICO = 143,                           // E143 (lista: "PDV devueltos") — aquí se usa como daños mágicos
        DANO_SIN_BOOST = 144,                               // E144: daños #1 a #2 (no boosteados)
        STAT_MENOS_DANO_FIJO = 145,                            // E145: -#1 a -#2 a los daños
        APARIENCIA_CAMBIAR = 149,                         // E149: cambia la apariencia
        ESTADO_INVISIBILIDAD = 150,                             // E150: vuelve invisible al personaje
        STAT_MENOS_SUERTE = 152,                              // E152: -#1 a -#2 a la suerte
        STAT_MENOS_VITALIDAD = 153,                           // E153: -#1 a -#2 a la vitalidad
        STAT_MENOS_AGILIDAD = 154,                            // E154: -#1 a -#2 a la agilidad
        STAT_MENOS_INTELIGENCIA = 155,                        // E155: -#1 a -#2 a la inteligencia
        STAT_MENOS_SABIDURIA = 156,                           // E156: -#1 a -#2 a la sabiduría
        STAT_MENOS_FUERZA = 157,                              // E157: -#1 a -#2 a la fuerza
        STAT_MAS_PODS = 158,                                 // E158: permite cargar #1 a #2 pods más
        STAT_MENOS_PODS = 159,                                // E159: reduce en #1 a #2 los pods que cargas
        STAT_MAS_ESQUIVA_PA = 160,                            // E160: +#1 a #2% de evitar pérdidas de PA
        STAT_MAS_ESQUIVA_PM = 161,                            // E161: +#1 a #2% de evitar pérdidas de PM
        STAT_MENOS_ESQUIVA_PA = 162,                           // E162: -#1 a #2% de evitar pérdidas de PA
        STAT_MENOS_ESQUIVA_PM = 163,                           // E163: -#1 a #2% de evitar pérdidas de PM
        STAT_MENOS_DANO = 164,                                // E164: daños reducidos de #1%
        STAT_MAESTRIA = 165,                                  // E165: aumenta los daños (#1) un #2%
        STAT_MENOS_PA = 168,                                  // E168: -#1 a -#2 PA
        STAT_MENOS_PM = 169,                                  // E169: -#1 a -#2 PM
        STAT_MENOS_DANO_CRITICO = 171,                         // E171: -#1 a -#2 a los golpes críticos
        STAT_MENOS_DANO_MAGICO = 172,                          // E172: reducción mágica disminuida de #1 a #2
        STAT_MENOS_DANO_FISICO = 173,                          // E173: reducción física disminuida de #1 a #2
        STAT_MAS_INICIATIVA = 174,                           // E174: +#1 a #2 a la iniciativa
        STAT_MENOS_INICIATIVA = 175,                          // E175: -#1 a #2 a la iniciativa
        STAT_MAS_PROSPECCION = 176,                          // E176: +#1 a #2 a la prospección
        STAT_MENOS_PROSPECCION = 177,                         // E177: -#1 a #2 a la prospección
        STAT_MAS_CURAS = 178,                                // E178: +#1 a #2 a las curaciones
        STAT_MENOS_CURAS = 179,                               // E179: -#1 a #2 a las curas
        INVOCACION_DOBLE = 180,                              // E180: crea un doble del lanzador
        INVOCACION_CRIATURA = 181,                                   // E181: invoca una criatura
        STAT_MAS_INVOCACIONES_MAX = 182,                      // E182: +#1 a #2 criaturas invocables
        STAT_MAS_REDUCCION_DANO_FISICO = 183,                  // E183 (lista: "reducción mágica") — aquí reducción física
        STAT_MAS_REDUCCION_DANO_MAGICO = 184,                  // E184 (lista: "reducción física") — aquí reducción mágica
        INVOCACION_ESTATICA = 185,                           // E185: invoca una criatura estática
        STAT_MENOS_DANO_PORCENTAJE = 186,                      // E186: disminuye los daños un #1 a #2%
        ALINEAMIENTO_CAMBIAR = 188,                       // E188: cambiar la alineación
        KAMAS_MAS = 194,                                // E194: gana #1 a #2 kamas
        COMBATE_PERCEPCION = 202,                                // E202: revela todos los objetos invisibles
        STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA = 210,        // E210: #1 a #2% de resistencia a la tierra
        STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA = 211,          // E211: #1 a #2% de resistencia al agua
        STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE = 212,          // E212: #1 a #2% de resistencia al aire
        STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO = 213,         // E213: #1 a #2% de resistencia al fuego
        STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL = 214,       // E214: #1 a #2% de resistencia neutral
        STAT_MENOS_RESISTENCIA_PORCENTAJE_TIERRA = 215,       // E215: #1 a #2% de debilidad a la tierra
        STAT_MENOS_RESISTENCIA_PORCENTAJE_AGUA = 216,         // E216: #1 a #2% de debilidad al agua
        STAT_MENOS_RESISTENCIA_PORCENTAJE_AIRE = 217,         // E217: #1 a #2% de debilidad al aire
        STAT_MENOS_RESISTENCIA_PORCENTAJE_FUEGO = 218,        // E218: #1 a #2% de debilidad al fuego
        STAT_MENOS_RESISTENCIA_PORCENTAJE_NEUTRAL = 219,      // E219: #1 a #2% de debilidad neutral
        STAT_MAS_DANO_DEVUELTO_OBJETO = 220,                   // E220: reenvía #1 daños
        STAT_MAS_DANO_TRAMPA = 225,                           // E225: +#1 a #2 a los daños con trampas
        STAT_MAS_RESISTENCIA_TIERRA = 240,                  // E240: +#1 a #2 a la resistencia a la tierra
        STAT_MAS_RESISTENCIA_AGUA = 241,                    // E241: +#1 a #2 a la resistencia al agua
        STAT_MAS_RESISTENCIA_AIRE = 242,                    // E242: +#1 a #2 a la resistencia al aire
        STAT_MAS_RESISTENCIA_FUEGO = 243,                   // E243: +#1 a #2 a la resistencia al fuego
        STAT_MAS_RESISTENCIA_NEUTRAL = 244,                 // E244: +#1 a #2 a la resistencia neutral
        STAT_MENOS_RESISTENCIA_TIERRA = 245,                 // E245: -#1 a #2 a la resistencia a la tierra
        STAT_MENOS_RESISTENCIA_AGUA = 246,                   // E246: -#1 a #2 a la resistencia al agua
        STAT_MENOS_RESISTENCIA_AIRE = 247,                   // E247: -#1 a #2 a la resistencia al aire
        STAT_MENOS_RESISTENCIA_FUEGO = 248,                  // E248: -#1 a #2 a la resistencia al fuego
        STAT_MENOS_RESISTENCIA_NEUTRAL = 249,                // E249: -#1 a #2 a la resistencia neutral
        STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA = 250,     // E250: #1 a #2% de res. tierra en JcJ
        STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA = 251,       // E251: #1 a #2% de res. agua en JcJ
        STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE = 252,       // E252: #1 a #2% de res. aire en JcJ
        STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO = 253,      // E253: #1 a #2% de res. fuego en JcJ
        STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL = 254,    // E254: #1 a #2% de res. neutral en JcJ
        STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_TIERRA = 255,    // E255: #1 a #2% de debilidad tierra en JcJ
        STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_AGUA = 256,      // E256: #1 a #2% de debilidad agua en JcJ
        STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_AIRE = 257,      // E257: #1 a #2% de debilidad aire en JcJ
        STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_FUEGO = 258,     // E258: #1 a #2% de debilidad fuego en JcJ
        STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL = 259,   // E259: #1 a #2% de debilidad neutral en JcJ
        STAT_MAS_RESISTENCIA_PVP_TIERRA = 260,               // E260: +#1 a #2 a la res. tierra en JcJ
        STAT_MAS_RESISTENCIA_PVP_AGUA = 261,                 // E261: +#1 a #2 a la res. agua en JcJ
        STAT_MAS_RESISTENCIA_PVP_AIRE = 262,                 // E262: +#1 a #2 a la res. aire en JcJ
        STAT_MAS_RESISTENCIA_PVP_FUEGO = 263,                // E263: +#1 a #2 a la res. fuego en JcJ
        STAT_MAS_RESISTENCIA_PVP_NEUTRAL = 264,              // E264: +#1 a #2 a la res. neutral en JcJ
        STAT_MAS_ARMADURA_BIS = 265,                          // E265: daños reducidos de #1 a #2
        STAT_ROBO_SUERTE = 266,                               // E266: #1 a -#2 robo de suerte
        STAT_ROBO_VITALIDAD = 267,                            // E267: #1 a -#2 robo de vitalidad
        STAT_ROBO_AGILIDAD = 268,                             // E268: #1 a -#2 robo de agilidad
        STAT_ROBO_INTELIGENCIA = 269,                         // E269: #1 a -#2 robo de inteligencia
        STAT_ROBO_SABIDURIA = 270,                            // E270: #1 a -#2 robo de sabiduría
        STAT_ROBO_FUERZA = 271,                               // E271: #1 a -#2 robo de fuerza
        HECHIZO_MAS_DANO = 293,                       // E293: aumenta los daños base del hechizo #1 en #3
        STAT_ROBO_ALCANCE = 320,                              // E320: roba #1 a #2 de alcance
        COMBATE_COLOCAR_TRAMPA = 400,                             // E400: pone una trampa de nivel #2
        COMBATE_COLOCAR_GLIFO = 401,                              // E401: pone un glifo de nivel #2
        COMBATE_COLOCAR_GLIFO_BIS = 402,                           // E402: pone un glifo de nivel #2 (glifo de Blop)
        PRISMA_COLOCAR = 513,                             // E513: coloca un prisma
        MOVIMIENTO_TELETRANSPORTAR_ZAAP_GUARDADO = 600,               // E600: teletransporta al último registro
        OFICIO_APRENDER = 603,                            // E603: aprende el oficio #3
        HECHIZO_APRENDER = 604,                           // E604: aprende el hechizo #3
        EXPERIENCIA_MAS = 605,                          // E605: +#1 a #2 puntos de XP
        CARACTERISTICA_MAS_FUERZA = 607,                 // E607: +#1 a #2 a la fuerza (característica)
        CARACTERISTICA_MAS_SUERTE = 608,                 // E608: +#1 a #2 a la suerte (característica)
        CARACTERISTICA_MAS_AGILIDAD = 609,               // E609: +#1 a #2 a la agilidad (característica)
        CARACTERISTICA_MAS_VITALIDAD = 610,              // E610: +#1 a #2 a la vitalidad (característica)
        CARACTERISTICA_MAS_INTELIGENCIA = 611,           // E611: +#1 a #2 a la inteligencia (característica)
        CARACTERISTICA_MAS_PUNTOS = 612,                 // E612: +#1 a #2 puntos de característica
        HECHIZO_MAS_PUNTOS = 613,                        // E613: +#1 a #2 puntos de hechizo
        INVOCACION_INFO = 628,                            // E628: invoca: #3
        NINGUN_EFECTO = 666,                               // E666: ningún efecto adicional
        DANO_VIDA_NEUTRAL_BIS = 671,                        // E671: daño = % vida del atacante (neutral)
        DANO_PUNICION = 672,                                  // E672: daño = % vida del atacante (neutral)
        CARACTERISTICA_MAS_SABIDURIA = 678,              // +#1 a #2 a la sabiduría (característica)
        CAPTURA_STATS_PIEDRA_ALMA = 705,                           // E705: #1% captura de alma de potencia #3
        MONTURA_PROBABILIDAD_CAPTURA = 706,                // E706: #1% de probabilidad de capturar una montura
        GREMIO_RENOMBRAR = 725,                           // E725: cambiar el nombre del gremio #4
        CAPTURA_ALMA_BONUS = 750,                          // E750: bonus de captura #1 a #2%
        MONTURA_BONUS_EXP = 751,                           // E751: bonus de XP del dragopavo #1 a #2%
        COMBATE_SACRIFICIO = 765,                                // E765: sacrificio (intercambia el blanco del golpe)
        MOVIMIENTO_EMPUJAR_MIEDO = 783,                              // E783: hace retroceder hasta la casilla objetivo
        CURACION_AL_ATACAR = 786,                             // E786: cura durante el ataque (vampirismo)
        CASTIGO_MAS = 788,                              // E788: castigo #2 durante #3 turno(s)
        OBJETO_RECIBIDO = 805,                                  // E805: recibido el #1
        OBJETO_ULTIMA_COMIDA = 808,                              // E808: ha comido el #1
        BOOST_MAS = 811,                                // E811 (lista: "turno(s) restante(s)") — boost interno
        OBJETO_RESISTENCIA_ETEREA = 812,                         // E812: resistencia #2 / #3 (durabilidad)
        COMBATE_INICIAR = 905,                            // E905: lanza un combate contra #2
        ESTADO_MAS = 950,                               // E950: estado #3
        ESTADO_QUITAR = 951,                              // E951: quita el estado '#3'
        ALINEAMIENTO_ID = 960,                            // E960: alineación #3
        ALINEAMIENTO_GRADO = 961,                         // E961: rango #3
        OBJETIVO_NIVEL = 962,                             // E962: nivel #3
        OBJETO_FECHA_CREACION = 963,                             // E963: creada hace #3 día(s)
        OBJETIVO_NOMBRE = 964,                            // E964: apellidos #4
        OBJETO_VIVO_ID_GRAFICO = 970,                             // E970: objeto vivo (gfx, interno)
        OBJETO_VIVO_HUMOR = 971,                                 // E971: objeto vivo (humor, interno)
        OBJETO_VIVO_APARIENCIA = 972,                            // E972: objeto vivo (apariencia, interno)
        OBJETO_VIVO_TIPO = 973,                                  // E973: objeto vivo (tipo, interno)
        OBJETO_VIVO_EXPERIENCIA = 974,                           // E974: objeto vivo (xp, interno)
        OBJETO_PUEDE_INTERCAMBIARSE = 983,                       // E983: intercambiable desde el #1
        OBJETO_MODIFICADO_POR = 985,                             // E985: modificado por #4
        OBJETO_PROPIETARIO = 987,                               // E987: pertenece a #4
        OBJETO_FABRICADO_POR = 988,                              // E988: fabricado por #4
        MONTURA_PROPIETARIO = 996,                        // E996: pertenece a #4 (montura)
        OBJETO_NOMBRE = 997,                                    // E997: nombre #4

        // Efectos internos de diálogo/objeto (no son effectId de hechizo).
        BDD_RESPUESTA_DIALOGO = 2000,
        BDD_SALIR_DIALOGO = 2001,
        BDD_ABRIR_BANCO = 2002,
        BDD_SUMAR_ESTADISTICA = 2003,
        BDD_SUMAR_OBJETO = 2004,
        BDD_TELETRANSPORTAR = 2005,
        BDD_REINICIAR_STATS = 2006,
        BDD_REINICIAR_HECHIZOS = 2007,
        BDD_APRENDER_OFICIO = 2008,
        BDD_QUITAR_OBJETO = 2009,
        BDD_CREAR_GREMIO = 2010,
        BDD_INICIAR_COMBATE = 2011,
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Serializable]
    public sealed class SpellEffect
    {
        public int SpellId;
        public int SpellLevel;
        public int Type;
        public int Value1;
        public int Value2;
        public int Value3;
        public int Duration;
        public int Chance;

        [ProtoIgnore]
        [NonSerialized]
        private SpellLevel _level;

        [ProtoIgnore] public EffectEnum TypeEnum => (EffectEnum)Type;

        [ProtoIgnore] public bool IsBurst => Duration == 0;

        [ProtoIgnore] public bool IsDispellable => Duration > 0;

        [ProtoIgnore]
        public SpellLevel Level
        {
            get
            {
                if (_level == null)
                    _level = SpellManager.Instance.GetSpellLevel(SpellId, SpellLevel);
                return _level;
            }
        }
    }
}
