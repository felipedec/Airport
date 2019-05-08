using System;
using System.IO;
using System.Text;

namespace Airport {
   class Program {
      public static ConfigVar<float> SimulationPrintRange = ConfigVar.CreateFloat("simulation_print_range", "Tempo minímo entre um print da simulação.", 1.0f);

      static void Main(string[] args) {
         Initializer.Initialize();

         ConsoleMode.Define("desktop", Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
         ConsoleMode.ExecuteCommandFile(new string[] { "autoexec", "optional" });

         var StandardOut = Console.Out;
         var StringBuildier = new StringBuilder();
         var SimulationOut = new StringWriter(StringBuildier);

         float LastPrint = 0;

         var AircraftStateMachine = Aircraft.StateMachine;


         while (Simulation.Simulate()) {
            // executar tarefa agendadas 
            ConsoleMode.ExecuteTasks();

            // definir a stream de saída para a simualação
            Console.SetOut(SimulationOut);

            // atualizar as instancias da nossa maquina de estados..
            AircraftStateMachine.Update();

            // consumir os recursos do aeroporto de acordo com a disponibilidade
            Runway.Consume();
            Taxiway.Consume();

            // retornar a stream de saida padrão
            Console.SetOut(StandardOut);

            if (Simulation.UnsacaledTime - LastPrint > SimulationPrintRange && StringBuildier.Length > 0) {
               string RunwayState = AircraftStateMachine.AreStatesEmpty(AircraftState.Landing, AircraftState.TakingOff) ? "Disponível" : "Ocupada";

               string HeaderText = $"Tempo: {Simulation.Time:0:2F}s " +
                  $"| Runway: {RunwayState} " +
                  $"| Gate: {AircraftStateMachine.GetObjectsCount(AircraftState.OnGate)}/5 " +
                  $"| Taxiway: {AircraftStateMachine.GetObjectsCount(AircraftState.TaxiwayArriving, AircraftState.TaxiwayLeaving)}/{Taxiway.Capacity} " +
                  $"| Line-up: {AircraftStateMachine.GetObjectsCount(AircraftState.LinedUp)}/{Runway.LinedUpCapacity} " +
                  $"| Airborne: {AircraftStateMachine.GetObjectsCount(AircraftState.Airborne, AircraftState.Airborne)}";

               var HorziontalLine = new string('─', Console.WindowWidth - 2);

               ConsoleUtility.WriteLine(HorziontalLine, ConsoleColor.DarkMagenta);
               ConsoleUtility.WriteLine(HeaderText, ConsoleColor.Yellow);
               ConsoleUtility.WriteLine(HorziontalLine, ConsoleColor.DarkMagenta);

               Console.Write(StringBuildier.ToString());

               StringBuildier.Clear();

               LastPrint = Simulation.UnsacaledTime;
            }
         }
      }

      [InitializeOnLoad]
      public static void RegistreInputCommands() {
         Input.RegistreCommand(ConsoleKey.Oem3, ConsoleMode.Open);
         Input.RegistreCommand(ConsoleKey.Home, ConsoleMode.Open);
      }
   }
}
 