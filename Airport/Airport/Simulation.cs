using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Airport {
   public class Simulation {
      public static float DeltaTime { get; private set; }
      public static float UnsacaledTime { get; private set; }
      public static int Frame { get; private set; }

      volatile static bool s_IsPaused;
      volatile static bool s_Quit;

      public static ConfigVar<float> Time = ConfigVar.CreateReadOnly<float>("time", "Tempo atual da execução.", 0);
      public static ConfigVar<float> TimeScale = ConfigVar.CreateRangeFloat("timescale", "Velocidade da simulação.", 1, 0.25f, 10);

      static Stopwatch s_Watch = new Stopwatch();
      static long s_LastSimulationTicks = 0;

      public static bool IsPaused => s_IsPaused;

      static ConcurrentQueue<Action> s_CommandBuffer = new ConcurrentQueue<Action>();

      public static void Execute(Action Action) {
         if (!IsPaused) {
            s_CommandBuffer.Enqueue(Action);
         }
      }

      public static void PauseToggle() {
         if (s_IsPaused) {
            Resume();
         }
         else {
            Pause();
         }
      }

      public static void Resume() {
         if (!s_IsPaused) {
            return;
         }

         s_IsPaused = false;
      }

      public static void Pause() {
         if (s_IsPaused) {
            return;
         }

         s_IsPaused = true;
      }

      [InitializeOnLoad]
      public static void Initialize() {
         s_Watch.Start();
      }

      public static void Quit() {
         s_Quit = true;
      }
   
      public static bool Simulate() {
         s_LastSimulationTicks = s_Watch.ElapsedTicks;
         Thread.Sleep(1);

         s_Watch.Stop();

         while (s_CommandBuffer.TryDequeue(out var Action)) {
            Action();
         }

         while (s_IsPaused) {
            Thread.Sleep(5);
         }

         s_Watch.Start();

         float UnsacaledDeltaTime = (float)(((s_Watch.ElapsedTicks - s_LastSimulationTicks) * 10E3 / Stopwatch.Frequency) / 10E3);

         DeltaTime = UnsacaledDeltaTime * TimeScale;
         UnsacaledTime += UnsacaledDeltaTime;
         Time.Value += DeltaTime;
         Frame++;
      
         return !s_Quit;
      }
   }
}

