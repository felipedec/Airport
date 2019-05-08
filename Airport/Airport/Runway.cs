namespace Airport {
   public class Runway {
      public static ConfigVar<int> LinedUpCapacity = ConfigVar.CreateRangeInt("rw_line_capacity", "Capacidade de aeronaves enfileiradas para decolarem.", 1, 1);
      public static ConfigVar<float> LandingDuration = ConfigVar.CreateFloat("rw_landing_time", "Tempo de duração da pouso das aeroanves", 2.0f);
      public static ConfigVar<float> TakingOffDuration = ConfigVar.CreateFloat("rw_takingoff_time", "Tempo de duração da decolagem das aeroanves", 2.0f);
      public static ConfigVar<float> SafetyRangeThreshold = ConfigVar.CreateFloat("rw_safety_range_threshold", "Limiar de autonomia segura.", 3.0f);

      public static bool IsAvaliable() {
         var AirctaftStateMachine = Aircraft.StateMachine;

         return AirctaftStateMachine.AreStatesEmpty(AircraftState.TakingOff, AircraftState.Landing);
      }

      public static void Consume() {
         if (IsAvaliable()) {
            var Next = SelectNextAircraft();

            if (Next == null) {
               return;
            }

            bool IsNextAirborne = (Next.State & AircraftState.AllAirborneMask) > 0;

            void IncrementPriority(Aircraft Aircraft) {
               Aircraft.Priority++;
            }

            // starvation
            if (IsNextAirborne) {
               Aircraft.StateMachine.ForEach(IncrementPriority, AircraftState.OnGate, AircraftState.LinedUp, AircraftState.TaxiwayLeaving);
            }
            else {
               Aircraft.StateMachine.ForEach(IncrementPriority, AircraftState.Airborne);
            }

            Next.State = IsNextAirborne ? AircraftState.Landing : AircraftState.TakingOff;
         }
      }

      private static Aircraft SelectNextAircraft() {
         Aircraft RangeSelect(Aircraft Lhs, Aircraft Rhs) => Lhs.Range < Rhs.Range ? Lhs : Rhs;
         Aircraft PrioritySelect(Aircraft Lhs, Aircraft Rhs) => Lhs.Priority > Rhs.Priority ? Lhs : Rhs;

         if (Aircraft.SelectFirst(out var AircraftOutOfFuel, AircraftState.AirborneOutOfFuel, RangeSelect)) {
            return AircraftOutOfFuel;
         }

         if (Aircraft.SelectFirst(out var LeastAircraftRange, AircraftState.Airborne, RangeSelect) && LeastAircraftRange.Range < SafetyRangeThreshold) {
            return LeastAircraftRange;
         }

         bool LineUp = Aircraft.SelectFirst(out var HighPriorityLinedUp, AircraftState.LinedUp, PrioritySelect);
         bool Airborne = Aircraft.SelectFirst(out var HighPriorityOnAirborne, AircraftState.Airborne, PrioritySelect);

         if (LineUp && Airborne) {
            return PrioritySelect(HighPriorityLinedUp, HighPriorityOnAirborne);
         }

         return LineUp ? HighPriorityLinedUp : HighPriorityOnAirborne;     
      }
   }
}

