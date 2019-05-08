using System;

namespace Airport {
   [Flags]
   public enum AircraftState {
      AllAirborneMask = Airborne | AirborneOutOfFuel,
      TaxingMask = TaxiwayArriving | TaxiwayLeaving,

      /// <summary>
      /// Aeronave no gate, aguardando pra line up.
      /// </summary>
      OnGate = (1 << 0),

      /// <summary>
      /// Aeronave no ar que esta sem gasolina.
      /// </summary>
      AirborneOutOfFuel = (1 << 1),

      /// <summary>
      /// Aeronave aguardando a pista ficar disponível pra pousar.
      /// </summary>
      Airborne = (1 << 2),

      /// <summary>
      /// Aeronave pousando.
      /// </summary>
      Landing = (1 << 3),

      /// <summary>
      /// Aeronave decolando.
      /// </summary>
      TakingOff = (1 << 4),

      /// <summary>
      /// Aeronave taxiada até a fila aguardando a pista.
      /// </summary>
      LinedUp = (1 << 5),

      /// <summary>
      /// Aeronave taxiando para partir do aeroporto.
      /// </summary>
      TaxiwayLeaving = (1 << 6),

      /// <summary>
      /// Aeronave taxiando após chegar no aeroporto.
      /// </summary>
      TaxiwayArriving = (1 << 7),

      /// <summary>
      /// Estado ao qual não é criado nenhuma referencia a aeronave, caso nenhuma 
      /// referencia a ela tenha sido feita fora da maquina de estado, ela sera destruída.
      /// </summary>
      Exit = (1 << 8),

      /// <summary>
      /// Aeronave ociosa..
      /// </summary>
      Idle = (1 << 9)
   }
}

