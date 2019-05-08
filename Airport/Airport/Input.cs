using System;
using System.Collections.Generic;
using System.Threading;

namespace Airport {
   class Input {
      static Dictionary<ConsoleKey, Action> s_ActionsMap = new Dictionary<ConsoleKey, Action>();
      static Thread s_InputThread;

      [InitializeOnLoad]
      public static void Initialize() {
         s_InputThread = new Thread(() => {
            Thread.Sleep(300);

            while (true) {
               while (Simulation.IsPaused);

               var Key = Console.ReadKey(true);
               
               if (s_ActionsMap.TryGetValue(Key.Key, out var Action)) {
                  Simulation.Execute(Action);
               }

               Thread.Sleep(10);
            }
         });

         s_InputThread.Start();
      }

      public static void RegistreCommand(ConsoleKey Key, Action Action) {
         s_ActionsMap.Add(Key, Action);
      }
   }
}
 