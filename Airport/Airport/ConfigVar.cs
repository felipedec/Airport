using System;
using System.Collections.Generic;
using System.Reflection;

namespace Airport {
   [AttributeUsage(AttributeTargets.Method)]
   public class ConfigVarCommandAttribute : Attribute {
      public readonly string Command, Description;
      public readonly bool Evaluate;

      public ConfigVarCommandAttribute(string Command, string Description, bool Evaluate = true) {
         this.Command = Command;
         this.Description = Description;
         this.Evaluate = Evaluate;
      }
   }

   public abstract class ConfigVar {
      public static List<ConfigVar> ConfigVars =  new List<ConfigVar>();
      public static List<string> Commands;

      public readonly bool Evaluate;
      public readonly string Command, Description;
      public virtual bool IsCommand => false;
      public virtual bool IsReadOnly => false;

      public abstract void Set(string[] Args);
      public abstract string Get();

      public virtual string TypeName => string.Empty;

      public static bool Search(string Command, out ConfigVar ConfigVar) {
         int SearchIndex = Search(Command);

         if (SearchIndex >= 0) {
            ConfigVar = ConfigVars[SearchIndex];
            return true;
         }
         ConfigVar = null;
         return false;
      }

      public static int Search(string Command) {
         return Commands.BinarySearch(Command, new ConfigVarComparer());
      }

      [InitializeOnLoad]
      public static void Initialize() {
         var ConfigVarType = typeof(ConfigVar);
         var ConfigVarCommandAttrType = typeof(ConfigVarCommandAttribute);

         ConfigVars.Capacity = 64;

         foreach (var Type in ConfigVarType.Assembly.GetTypes()) {
            var Fields = Type.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var Field in Fields) {
               if (Field.FieldType.IsSubclassOf(ConfigVarType)) {
                  var Value = Field.GetValue(null);

                  if (Value == null) {
                     throw new InvalidOperationException("ConfigVar não pode ser nulo.");
                  }

                  ConfigVars.Add(Value as ConfigVar);
               }
            }

            var Methods = Type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var Method in Methods) {
               if (Method.IsDefined(ConfigVarCommandAttrType) && Method.ReturnType == typeof(void)) {
                  var Parameter = Method.GetParameters();

                  if (Parameter.Length == 1 && Parameter[0].ParameterType == typeof(string[])) {
                     foreach (var Attr in (ConfigVarCommandAttribute[])Method.GetCustomAttributes(ConfigVarCommandAttrType)) {
                        ConfigVars.Add(new ConfigVarCommand(Attr.Command, Attr.Description, (Action<string[]>)Delegate.CreateDelegate(typeof(Action<string[]>), Method), Attr.Evaluate));
                     }
                  }
               }
            }
         }
         
         ConfigVars.Sort(new ConfigVarComparer());
         Commands = new List<string>(ConfigVars.Count);

         for (int Index = 0; Index < ConfigVars.Count; Index++) {
            Commands.Add(ConfigVars[Index].Command);
         }
      }

      public ConfigVar(string Command, string Description, bool Evaluate = true) {
         this.Command = Command;
         this.Description = Description;
         this.Evaluate = Evaluate;
      }

      public static ConfigVar<int> CreateIntConfigVar(string Command, string Description, int DefaultValue) {
         return new ConfigVar<int>(Command, Description, DefaultValue, (Value) => {
            return Value.ToString();
         },
            (Value) => {
               if (int.TryParse(Value[0], out int Result)) {
                  return Result;
               }
               throw new InvalidOperationException("Valor inválido.");
            });
      }

      private static string ToString<T>(T Value) where T : unmanaged {
         return Value.ToString();
      }

      internal static T ReadOnly<T>(string[] Args) where T : unmanaged {
         throw new InvalidOperationException("Esta variável so pode ser lida.");
      }

      public static ConfigVar<T> CreateReadOnly<T>(string Command, string Description, T DefaultValue, Func<T, string> Getter) where T : unmanaged {
         return new ConfigVar<T>(Command, Description, DefaultValue, Getter, ReadOnly<T>);
      }

      public static ConfigVar<T> CreateReadOnly<T>(string Command, string Description, T DefaultValue) where T : unmanaged {
         return new ConfigVar<T>(Command, Description, DefaultValue, ToString, ReadOnly<T>);
      }

      public static ConfigVar<float> CreateFloat(string Command, string Description, float DefaultValue) {
         return new ConfigVar<float>(Command, Description, DefaultValue, ToString,
            (Value) => {
               if (float.TryParse(Value[0], out float Result)) {
                  return Result;
               }
               throw new InvalidOperationException("Valor inválido.");
            });
      }

      public static ConfigVar<int> CreateRangeInt(string Command, string Description, int DefaultValue, int Min = int.MinValue, int Max = int.MaxValue) {
         return new ConfigVar<int>(Command, Description, DefaultValue, ToString,
            (Value) => {
               if (int.TryParse(Value[0], out int Result)) {
                  if (Result > Max) {
                     Result = Max;
                  }
                  else if (Result < Min) {
                     Result = Min;
                  }

                  return Result;
               }
               throw new InvalidOperationException("Valor inválido.");
            });
      }

      public static ConfigVar<float> CreateRangeFloat(string Command, string Description, float DefaultValue, float Min = float.MaxValue, float Max = float.MaxValue) {
         return new ConfigVar<float>(Command, Description, DefaultValue, ToString,
            (Value) => {
               if (float.TryParse(Value[0], out float Result)) {
                  if (Result > Max) {
                     Result = Max;
                  }
                  else if (Result < Min) {
                     Result = Min;
                  }

                  return Result;
               }
               throw new InvalidOperationException("Valor inválido.");
            });
      }
   }


   public struct ConfigVarComparer : IComparer<ConfigVar>, IComparer<string> {
      public int Compare(ConfigVar Lhs, ConfigVar Rhs) {
         return string.Compare(Lhs.Command, Rhs.Command, true);
      }

      public int Compare(string Lhs, string Rhs) {
         return string.Compare(Lhs, Rhs, true);
      }
   }

   public class ConfigVarCommand : ConfigVar {
      readonly Action<string[]> m_Action;

      public override bool IsCommand => true;

      public override string Get() {
         m_Action(Array.Empty<string>());

         return null;
      }


      public override void Set(string[] Args) {
         m_Action(Args);
      }

      public void Execute() {
         m_Action(null);
      }

      public ConfigVarCommand(string Command, string Description, Action<string[]> CommandAction, bool Evaluate) : base(Command, Description, Evaluate) {
         m_Action = CommandAction;
      }
   }

   public class TypeNameUtility {
      private readonly static Dictionary<Type, string> s_FriendlyTypeNames = new Dictionary<Type, string>
      {
          { typeof(bool), "bool" },
          { typeof(byte), "byte" },
          { typeof(char), "char" },
          { typeof(decimal), "decimal" },
          { typeof(double), "double" },
          { typeof(float), "float" },
          { typeof(int), "int32" },
          { typeof(long), "int64" },
          { typeof(sbyte), "sbtye" },
          { typeof(short), "int32" },
          { typeof(string), "string" },
          { typeof(uint), "uint32" },
          { typeof(ulong), "uint64" },
          { typeof(ushort), "uint16" },
      };

      public static string GetFriendlyTypeName<T>() {
         var Type = typeof(T);

         if (s_FriendlyTypeNames.TryGetValue(Type, out var Result)) {
            return Result;
         }
         return Type.Name;
      }
   }

   public class ConfigVar<T> : ConfigVar where T : unmanaged {
      public T Value;
      public Func<string[], T> Setter;
      public Func<T, string> Getter;

      public override bool IsReadOnly => Setter == ReadOnly<T>;

      public ConfigVar(string Command, string Description, T DefaultValue, Func<T, string> Getter, Func<string[], T> Setter) : base(Command, Description) {
         Value = DefaultValue;
         this.Setter = Setter;
         this.Getter = Getter;
      }

      public override void Set(string[] Args) => this.Value = Setter(Args);
      public override string Get() => Getter(Value);
      public override string TypeName => TypeNameUtility.GetFriendlyTypeName<T>();

      public static implicit operator T(ConfigVar<T> Config) {
         return Config.Value;
      }

      public override string ToString() {
         return Value.ToString();
      }

   }
}
 