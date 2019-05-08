namespace Airport {
   public class Taxiway {
      public static ConfigVar<int> Capacity  = ConfigVar.CreateRangeInt("tw_capacity", "Capacidade da pista de taxiamento.", 1, 1);
      public static ConfigVar<float> Duration = ConfigVar.CreateFloat("tw_duration", "Tempo de duração do taxiamento.", 2.0f);

      public static bool IsAvaliable() {
         var AirctaftStateMachine = Aircraft.StateMachine;

         return AirctaftStateMachine.GetObjectsCount(AircraftState.TaxiwayArriving, AircraftState.TaxiwayLeaving) < Capacity;
      }

      public static void Consume() {
         while (IsAvaliable()) {
            if (Aircraft.SelectFirst(out var LandingAircraft, AircraftState.Landing) && LandingAircraft.StateTime >= Runway.LandingDuration) {
               LandingAircraft.State = AircraftState.TaxiwayArriving;

               continue;
            }
      
            if (Aircraft.StateMachine.GetObjectsCount(AircraftState.LinedUp) < Runway.LinedUpCapacity) {
               if (Aircraft.SelectFirst(out var Aicraft, AircraftState.OnGate, (Lhs, Rhs) => Lhs.Priority > Rhs.Priority ? Lhs : Rhs)) {
                  Aicraft.State = AircraftState.TaxiwayLeaving;

                  continue;
               }
            }

            break;
         }
      }
   }
}

