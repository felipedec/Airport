using System;
using System.Collections;
using System.Collections.Generic;

namespace Airport {
   public delegate void StateDelegate<T, K>(StateMachineInstance<T, K> StateMachine, K Aircraft) where T : Enum where K : class;

   public class StateMachine<T, K> : IEnumerable<K> where T : Enum where K : class {
      internal class State {
         public State(T Id, StateDelegate<T, K> Enter, StateDelegate<T, K> Update, StateDelegate<T, K> Leave, bool IsExitState) {
            this.Id = Id;
            this.Enter = Enter;
            this.Update = Update;
            this.Leave = Leave;
            this.IsExitState = IsExitState;

            Objects = new LinkedList<K>();
         }
         public T Id, OnExit;
         public ConfigVar<float> ExitTime;
         public bool IsExitState;
         public StateDelegate<T, K> Enter, Leave, Update;
         public Action OnChange;

         public readonly LinkedList<K> Objects;

         public LinkedListNode<K> AddObject(K Object) {
            if (!IsExitState) {
               var Node = Objects.AddLast(Object);
               OnChange?.Invoke();

               return Node;
            }

            return null;
         }

         public void RemoveObject(LinkedListNode<K> Node) {
            if (!IsExitState) {
               Objects.Remove(Node);

               OnChange?.Invoke();
            }
         }
      }

      readonly Dictionary<T, State> m_StatesMap = new Dictionary<T, State>();

      public readonly LinkedList<StateMachineInstance<T, K>> Instances = new LinkedList<StateMachineInstance<T, K>>();

      internal State this[T Id] {
         get {
            return m_StatesMap[Id];
         }
      }

      public void ForEach(Action<K> Operation, params T[] States) {
         if (Operation == null) {
            return;
         }

         foreach (var State in States) {
            foreach (var Object in GetObjectsByState(State)) {
               Operation(Object);
            }
         }
      }

      public LinkedList<K> GetObjectsByState(T State) {
         return m_StatesMap[State].Objects;
      }

      public void Update() {
         for (var Node = Instances.First; Node != null; Node = Node.Next) {
            Node.Value.Update();
         }
      }

      public bool AreStatesEmpty(params T[] States) {
         return GetObjectsCount(States) == 0;
      }

      public int GetObjectsCount(params T[] States) {
         int Result = 0;

         foreach(var State in States) {
            Result += m_StatesMap[State].Objects.Count;
         }

         return Result;
      }

      public void Add(T Id, StateDelegate<T, K> Enter = null, StateDelegate<T, K> Update = null, StateDelegate<T, K> Leave = null, bool IsExitTime = false) {
         m_StatesMap[Id] = new State(Id, Enter, Update, Leave, IsExitTime);
      }

      public void SetExitTime(T Id, T OnExit, ConfigVar<float> ExitTime) {
         var State = m_StatesMap[Id];

         State.OnExit = OnExit;
         State.ExitTime = ExitTime;
      }

      public void SubscribeStateChange(T Id, Action OnChange) {
         var State = m_StatesMap[Id];

         State.OnChange += OnChange ?? throw new ArgumentNullException(nameof(OnChange));
      }

      public void UnsubscribeStateChange(T Id, Action OnChange) {
         var State = m_StatesMap[Id];

         State.OnChange -= OnChange ?? throw new ArgumentNullException(nameof(OnChange));
      }

      public StateMachineInstance<T, K> CreateInstance(K Object, T EntryState) {
         if (Object == null) {
            throw new ArgumentNullException(nameof(Object));
         }

         var Node = Instances.AddLast(default(StateMachineInstance<T, K>));
         var Instance = new StateMachineInstance<T, K>(Object, this, Node);

         Node.Value = Instance;
         Instance.SwitchTo(EntryState);

         return Instance;
      }

      internal void DestroyInstance(StateMachineInstance<T, K> Instance) {
         if (Instance == null) {
            throw new ArgumentNullException(nameof(Instance));
         }

         if (Instance.StateMachine != this) {
            throw new InvalidOperationException();
         }

         Instance.Dispose();
      }

      public IEnumerator<K> GetEnumerator() {
         foreach (var State in m_StatesMap) {
            foreach (var Obj in State.Value.Objects) {
               yield return Obj;
            }
         }
      }

      IEnumerator IEnumerable.GetEnumerator() {
         return GetEnumerator();
      }
   }
}

