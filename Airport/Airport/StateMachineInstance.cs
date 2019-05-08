using System;
using System.Collections.Generic;

namespace Airport {
   public class StateMachineInstance<T, K> : IDisposable where T : Enum where K : class {
      public K Object;
      public readonly StateMachine<T, K> StateMachine;
      
      LinkedListNode<K> m_StateObjectNode;
      StateMachine<T, K>.State m_CurrentState;

      float m_StateEnterTime;
      readonly LinkedListNode<StateMachineInstance<T, K>> m_InstanceNode;

      public float StateTime => Simulation.Time - m_StateEnterTime;

      internal StateMachineInstance(K Object, StateMachine<T, K> StateMachine, LinkedListNode<StateMachineInstance<T, K>> Node) {
         this.Object = Object;
         this.StateMachine = StateMachine;

         m_InstanceNode = Node;
      }

      public void Update() {
         if (m_CurrentState != null) {
            if (m_CurrentState.ExitTime != null && m_CurrentState.ExitTime <= StateTime) {
               SwitchTo(m_CurrentState.OnExit);

               return;
            }

            m_CurrentState.Update?.Invoke(this, Object);
         }
      }

      public void Shutdown() {
         if (m_CurrentState != null && m_CurrentState.Leave != null) {
            m_CurrentState.Leave?.Invoke(this, Object);
         }

         m_CurrentState = null;
      }

      public void SwitchTo(T StateId, bool Silent = false) {
         // sair do estado anteior
         if (m_CurrentState != null) {
            if (!Silent) {
               m_CurrentState.Leave?.Invoke(this, Object);
            }
            m_CurrentState.RemoveObject(m_StateObjectNode);
         }

         m_CurrentState = StateMachine[StateId];
         m_StateObjectNode = null;

         // entrar no novo estado
         if (m_CurrentState != null) {
            if (!Silent) {
               m_CurrentState.Enter?.Invoke(this, Object);
            }
            m_StateEnterTime = Simulation.Time;

            if (m_CurrentState.IsExitState) {
               Object = null;

               StateMachine.Instances.Remove(m_InstanceNode);
            }
            else {
               m_StateObjectNode = m_CurrentState.AddObject(Object);
            }
         }
         else {
            m_StateObjectNode = null;
         }
      }

      internal T GetCurrentState() {
         return m_CurrentState.Id;
      }

      public void Dispose() {
         if (m_InstanceNode != null && StateMachine.Instances == m_InstanceNode.List) {
            StateMachine.Instances.Remove(m_InstanceNode);
         }

         Shutdown();
      }
   }
}

