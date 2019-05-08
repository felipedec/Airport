using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Airport {
   public delegate T Select<T>(T Lhs, T Rhs);

   public class Aircraft {
      public static ConfigVar<float> AltitiudeSpeed = ConfigVar.CreateFloat("aircraft_altitude_speed", "Velocidade com que a aeronava perde altitude.", 100);

      static ConfigVar<int> s_LastTransponderCode = ConfigVar.CreateReadOnly("aircraft_max_transponder", "Transponder da ultima aeronave criada.", 0, FormatTransponder);

      public static StateMachine<AircraftState, Aircraft> StateMachine;

      public int Priority { get; set; }
      public int FlightId { get; set; }
      public float Weight { get; set; }
      public float Fuel { get; set; }
      public float Altitude { get; set; }
      public float FuelOverTime { get; set; }
      public string Origin { get; set; }

      readonly int m_TransponderCode;
      readonly StateMachineInstance<AircraftState, Aircraft> m_StateMachineInstance;

      public float Range => CalculateRange();
      public bool FuelEmpty => Fuel <= 0;

      // o padrão de escrita do transponder é em base octal com 4 digitos.
      public string Transponder => FormatTransponder(m_TransponderCode);

      public AircraftState State {
         get {
            return m_StateMachineInstance.GetCurrentState();
         }
         set {
            m_StateMachineInstance.SwitchTo(value);
         }
      }

      public float StateTime => m_StateMachineInstance.StateTime;

      public float CalculateRange() {
         float Range = 0;

         if (FuelOverTime > 0) {
            Range += Fuel / FuelOverTime;
         }

         Range += Altitude / AltitiudeSpeed;

         return Range;
      }

      public static string FormatTransponder(int TransponderCode) {
         return Convert.ToString(TransponderCode, 8).PadLeft(4, '0');
      }

      [ConfigVarCommand("aircraft_clear", "Destruir todas as aeronaves.")]
      public static void DestroyAircrafts(string[] Aircrafts) {
         Console.WriteLine("Aeronaves destruídas.");
         StateMachine.Instances.Clear();
      }

      [ConfigVarCommand("create_aircraft", "Crair uma aeronave em fabricação.")]
      public static void CreateAircraft(string[] Args) {
         new Aircraft(AircraftState.Idle);
      }

      [ConfigVarCommand("print_aircraft_fields", "Crair uma aeronave em fabricação.")]
      public static void PrintAircraftFields(string[] Args) {
         var Type = typeof(Aircraft);
         var Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

         Console.WriteLine(Properties.ToStringTable(new string[] { "Nome", "Tipo" }, P => P.Name, P => P.PropertyType.Name));
      }

      [ConfigVarCommand("print_aircraft_states", "Crair uma aeronave em fabricação.")]
      public static void PrintAircraftStates(string[] Args) {
         var Type = typeof(Aircraft);
         var Properties = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

         Console.WriteLine(Enum.GetNames(typeof(AircraftState)).ToStringTable(new string[] { "Nome" }, P => P));
      }

      [ConfigVarCommand("print_aircrafts", "Mostrar aeronaves.")]
      public static void PrintAircrafts(string[] Args) {
         var States = (AircraftState)~0;

         int StatesTokenIndex = 0;
         bool Except = false; 

         if (Args.Length > 1) {
            StatesTokenIndex = 1;

            if (Args[0].Equals("not", StringComparison.OrdinalIgnoreCase)) {
               Except = true;
            }
            else {
               throw new InvalidOperationException("Primeiro parametro inválido.");
            }
         }

         if (Args.Length > 0) {
            if (!Enum.TryParse(Args[StatesTokenIndex], true, out States)) {
               throw new InvalidOperationException("Digite os estados separados por virgulas. (ex: Landing, Taxing).");
            }
         }

         if (Except) {
            States = ~States;
         }

         var FiltredAircrafts = new List<Aircraft>(StateMachine.Instances.Count);

         foreach (var AircraftStateMachineInstance in StateMachine.Instances) {
            var Aircraft = AircraftStateMachineInstance.Object;

            if ((Aircraft.State & States) == 0) {
               continue;
            }

            FiltredAircrafts.Add(Aircraft);
         }

         string Table = FiltredAircrafts.ToStringTable(new string[] {
            "#", "Prioridade", "Combustível (L)", "Gasto (L/m)", "Peso (kg)", "Estado", "Vôo (#)", "Alt. (km)" , "Origem", "Range (s)" }, 
            A => A.Transponder, A => A.Priority, A => A.Fuel, A => A.FuelOverTime, A => A.Weight, A => A.State, A => A.FlightId, A => A.Altitude, A => A.Origin, A => A.Range);

         Console.WriteLine(Table);
      }

      [ConfigVarCommand("aircraft_set_state", "Definir o estado de uma aeronave.")]
      public static void SetAircraftState(string[] Args) {
         if (Args.Length >= 2) {
            var Aircraft = FindAircraftByTransponder(Args[0]);

            if (Aircraft != null) {
               if (Enum.TryParse(Args[1], true, out AircraftState State)) {
                  Aircraft.m_StateMachineInstance.SwitchTo(State, true);
               }
               else {
                  Console.WriteLine("Estado inválido.");
               }
            }
            else {
               Console.WriteLine("Aeronave não encontrada.");
            }
         }
         else {
            Console.WriteLine("Uso: aircraft_set_field <transponder> <estate>");
         }
      }


      [ConfigVarCommand("aircraft_set_field", "Definir valor de um campo de uma aeronave.")]
      public static void SetAircraftFieldCommand(string[] Args) {
         const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.IgnoreCase;

         if (Args.Length != 3) {
            Console.Write("Uso: <transponder> <campo> <valor>");

            return;
         }

         var Aircraft = FindAircraftByTransponder(Args[0]);


         if (Aircraft != null) {
            var FieldName = Args[1];

            var Field = typeof(Aircraft).GetField(FieldName, BindingFlags);

            if (Field != null) {
               var Converter = TypeDescriptor.GetConverter(Field.FieldType);
               var Result = Converter.ConvertFrom(Args[2]);

               Field.SetValue(Aircraft, Result);
               Console.WriteLine($"\"aircraft.{FieldName}\" = \"{Field.GetValue(Aircraft)}\"");
            }
            else {
                  var Property = typeof(Aircraft).GetProperty(FieldName, BindingFlags);
               if (Property != null) {
                  var Converter = TypeDescriptor.GetConverter(Property.PropertyType);
                  var Result = Converter.ConvertFrom(Args[2]);

                  Property.SetValue(Aircraft, Result);
                  Console.WriteLine($"\"aircraft.{FieldName}\" = \"{Property.GetValue(Aircraft)}\"");
               }
               else {
                  Console.WriteLine($"'{Args[1]}' campo inválido.");
               }
            }
         }
         else {
            Console.WriteLine("Nenhuma aeronave encontrada.");
         }
      }

      static int s_CachedAircraftTransponder;
      static Aircraft s_CachedAircraft;

      public static bool SelectFirst(out Aircraft First, AircraftState From, Select<Aircraft> Select) {
         var Aircrafts = GetAircraftsByState(From);

         if (Aircrafts.Count == 0) {
            First = null;

            return false;
         }

         First = Aircrafts.First.Value;

         foreach (var AirboneAircraft in GetAircraftsByState(From)) {
            First = Select(First, AirboneAircraft);
         }

         return true;
      }

      public static bool SelectFirst(out Aircraft First, AircraftState From) {
         var Aircrafts = GetAircraftsByState(From);

         if (Aircrafts.Count == 0) {
            First = null;

            return false;
         }

         First = Aircrafts.First.Value;

         return true;
      }

      public static Aircraft FindAircraftByTransponder(string Transponder) {
         int TransponderCode = 0;

         try {
            TransponderCode = Convert.ToInt32(Transponder, 8);
         }
         catch {
            return null;
         }
         
         if (s_CachedAircraftTransponder == TransponderCode) {
            return s_CachedAircraft;
         }

         s_CachedAircraftTransponder = TransponderCode;
         s_CachedAircraft = null;

         foreach (var Instance in StateMachine.Instances) {
            if (Instance.Object.m_TransponderCode == TransponderCode) {
               s_CachedAircraft = Instance.Object as Aircraft;

               return s_CachedAircraft;
            }
         }

         return null;
      }

      [InitializeOnLoad]
      public static void Initialize() {
         if (StateMachine != null) {
            return;
         }

         StateMachine = new StateMachine<AircraftState, Aircraft>();

         #region Configurar Estados

         /* AircraftState.Airborne */

         StateMachine.Add(AircraftState.Airborne, Update: ConsumeFuel);

         /* AircraftState.AirborneOutOfFuel */

         StateMachine.Add(AircraftState.AirborneOutOfFuel,
         Enter: (StateMachine, Aircraft) => {
            // o avião esta sem combustivel, sua prioridade é critica
            ConsoleUtility.WriteLine($"{Aircraft} esta sem combustível e perdendo altitude.", ConsoleColor.Red);

            // garantir mesmo que o avião esta sem gasolina
            Aircraft.Fuel = 0;
         },

         Update: (StateMachine, Aircraft) => {
            // como o avião esta sem combustivel, esta perdendo altitude
            Aircraft.Altitude = Math.Max(0, Aircraft.Altitude - AltitiudeSpeed * Simulation.DeltaTime);

            if (Aircraft.Altitude == 0) {
               Console.WriteLine($"Infelizmente a {Aircraft} não conseguiu aguadar o suficiente para que a pista ficasse disponível e caiu.", ConsoleColor.Red);

               Aircraft.State = AircraftState.Exit;
            }
         });

         /* AircraftState.OnGate */

         StateMachine.Add(AircraftState.OnGate,
         Enter: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} aguardando pista de taxiamento.");
         });

         /* AircraftState.TakingOff */

         StateMachine.Add(AircraftState.TakingOff,
         Update: ConsumeFuel,
         Enter: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} esta se preparando para decolar.");
         },
         Leave: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} decolou e partiu para o seu destino.");

            OnRunwayAvaliable();
         });

         /* AircraftState.Landing */

         StateMachine.Add(AircraftState.Landing,
         Enter: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} iniciou o pouso.");
         }, 
         Leave: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} efetuou o pouso e desocupou a pista.");

            OnRunwayAvaliable();
         });

         /* AircraftState.TaxiwayLeaving */

         StateMachine.Add(AircraftState.TaxiwayLeaving,
         Enter: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} saiu do gate e esta taxiando até a pista.");
         });


         /* AircraftState.TaxiwayArriving */

         StateMachine.Add(AircraftState.TaxiwayArriving,
         Enter: (StateMachine, Aircraft) => {
            Console.WriteLine($"{Aircraft} saiu da pista e esta taxiando até o hangar.");
         });
         

         /* AircraftState.LineUp */

         StateMachine.Add(AircraftState.LinedUp,
         Enter: (StateMachine, Aircraft) => Console.WriteLine($"{Aircraft} esta aguardado autorização para entrar na pista."),
         Leave: (StateMachine, Aircraft) => Console.WriteLine($"{Aircraft} foi autorizado a entrar na pista.")
         );

         /* AircraftState.Exit */

         StateMachine.Add(AircraftState.Exit, IsExitTime: true);

         /* AircraftState.Idle */

         StateMachine.Add(AircraftState.Idle);

         /* Exit Times */

         StateMachine.SetExitTime(AircraftState.TakingOff, AircraftState.Exit, Runway.TakingOffDuration);
         StateMachine.SetExitTime(AircraftState.TaxiwayArriving, AircraftState.Exit, Taxiway.Duration);
         StateMachine.SetExitTime(AircraftState.TaxiwayLeaving, AircraftState.LinedUp, Taxiway.Duration);

         #endregion Configurar Estados
      }

      private static void OnRunwayAvaliable() {
         foreach (var Instance in StateMachine.Instances) {
            var Aircraft = Instance.Object;

            if ((Aircraft.State & AircraftState.LinedUp | AircraftState.OnGate | AircraftState.TaxiwayArriving) != 0) {
               Aircraft.Priority++;
            }
         }
      }

      public static LinkedList<Aircraft> GetAircraftsByState(AircraftState State) {
         return StateMachine.GetObjectsByState(State);
      }

      public static bool HasAircraftOnState(AircraftState State) {
         var Aircrafts = StateMachine.GetObjectsByState(State);

         return Aircrafts.Count > 0;
      }

      public static Aircraft GetAircraftByState(AircraftState State) {
         var Aircrafts = StateMachine.GetObjectsByState(State);

         return Aircrafts.Count > 0 ? Aircrafts.First.Value : null;
      }

      private static void ConsumeFuel(StateMachineInstance<AircraftState, Aircraft> StateMachine, Aircraft Aircraft) {
         Aircraft.Fuel = Math.Max(0, Aircraft.Fuel - Simulation.DeltaTime * Aircraft.FuelOverTime);

         if (Aircraft.Fuel == 0) {
            Aircraft.State = AircraftState.AirborneOutOfFuel;
         }
      }

      public Aircraft(AircraftState EntryState) {
         m_TransponderCode = ++s_LastTransponderCode.Value;
         m_StateMachineInstance = StateMachine.CreateInstance(this, EntryState);
      }

      ~Aircraft() {
         StateMachine.DestroyInstance(m_StateMachineInstance);
      }

      public override string ToString() {
         return $"Aeronave (Transponder: {Transponder})";
      }
   }
}

